using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameController : MonoBehaviour
{
    public static GameController instance { get; private set; }

    public VisualTreeAsset layoutTemplate;
    public VisualTreeAsset scanlineTemplate;

    [Serializable]
    public class NoteTemplates
    {
        public VisualTreeAsset basicNote;

        public VisualTreeAsset GetForType(NoteType type)
        {
            return type switch
            {
                NoteType.Basic => basicNote,
                _ => basicNote
            };
        }
    }
    public NoteTemplates noteTemplates;

    private ThemeApi.GameSetup setup;
    private ThemeApi.GameState state;
    private GameTimer timer;
    private GameBackground bg;
    private GameLayout layout;
    private NoteManager noteManager;

    public void SetSetupInstance(ThemeApi.GameSetup s)
    {
        setup = s;
    }
    public void SetStateInstance(ThemeApi.GameState s)
    {
        state = s;
    }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    public void BeginLoading()
    {
        // Lock down the track so the theme can't change them later.
        setup.lockedTrackFolder = string.Copy(setup.trackFolder);

        StartCoroutine(LoadSequence());
    }

    private IEnumerator LoadSequence()
    {
        Action<Status> reportLoadError = (Status status) =>
        {
            state.SetState(ThemeApi.GameState.State.LoadError);
            setup.onLoadError?.Function?.Call(status);
        };
        int filesLoaded = 0;
        int totalFiles = 0;
        Action<string> reportLoadProgress = (string fileJustLoaded) =>
        {
            filesLoaded++;
            setup.onLoadProgress?.Function?.Call(
                new ThemeApi.GameSetup.LoadProgress()
                {
                    fileJustLoaded = fileJustLoaded,
                    filesLoaded = filesLoaded,
                    totalFiles = totalFiles
                });
        };

        // Load track, track options and pattern. These are all
        // synchronous.
        string trackPath = Paths.Combine(setup.lockedTrackFolder,
            Paths.kTrackFilename);
        Track track;
        try
        {
            track = Track.LoadFromFile(trackPath) as Track;
        }
        catch (Exception ex)
        {
            reportLoadError(Status.FromException(ex));
            yield break;
        }

        setup.trackOptions = Options.instance.GetPerTrackOptions(
            track.trackMetadata.guid);

        setup.patternBeforeModifier = null;
        foreach (Pattern p in track.patterns)
        {
            if (p.patternMetadata.guid == setup.patternGuid)
            {
                setup.patternBeforeModifier = p;
                break;
            }
        }
        if (setup.patternBeforeModifier == null)
        {
            reportLoadError(Status.Error(
                Status.Code.NotFound,
                "The specified pattern is not found in the track."));
            yield break;
        }
        setup.patternAfterModifier = setup.patternBeforeModifier
            .ApplyModifiers(Modifiers.instance);

        // Step 0: calculate the number of files to load.
        totalFiles =
            1 +  // Background image
            1 +  // Skins
            1 +  // Backing track
            0 +  // Keysounds
            1;  // BGA
        HashSet<string> keysoundFullPaths = new HashSet<string>();
        foreach (Note n in setup.patternAfterModifier.notes)
        {
            if (n.sound != null && n.sound != "")
            {
                keysoundFullPaths.Add(Paths.Combine(
                    setup.lockedTrackFolder, n.sound));
            }
        }
        totalFiles += keysoundFullPaths.Count;

        // Step 1: load background image to display on the loading
        // screen.
        bg = new GameBackground(setup.bgContainer.inner,
            trackOptions: setup.trackOptions);
        string backImage = setup.patternAfterModifier.patternMetadata
            .backImage;
        if (!string.IsNullOrEmpty(backImage))
        {
            string path = Paths.Combine(
                setup.lockedTrackFolder, backImage);
            bool loaded = false;
            Status status = null;
            Texture2D texture = null;
            ResourceLoader.LoadImage(path,
                (Status loadStatus, Texture2D loadedTexture) =>
                {
                    loaded = true;
                    status = loadStatus;
                    texture = loadedTexture;
                });
            yield return new WaitUntil(() => loaded);
            if (!status.Ok())
            {
                reportLoadError(status);
                yield break;
            }
            bg.DisplayImage(texture);
        }
        reportLoadProgress(backImage);

        // Step 2: load skins, if told to.
        if (Options.instance.reloadSkinsWhenLoadingPattern)
        {
            bool loaded = false;
            Status status = null;
            GlobalResourceLoader.GetInstance().LoadAllSkins(
                progressCallback: null,
                completeCallback: (Status loadStatus) =>
                {
                    loaded = true;
                    status = loadStatus;
                });
            yield return new WaitUntil(() => loaded);
            if (!status.Ok())
            {
                reportLoadError(status);
                yield break;
            }
        }
        reportLoadProgress(Paths.kSkinFilename);

        // Step 3: load backing track.
        string backingTrackFilename = setup.patternAfterModifier
            .patternMetadata.backingTrack;
        if (!string.IsNullOrEmpty(backingTrackFilename))
        {
            string path = Paths.Combine(setup.lockedTrackFolder,
                backingTrackFilename);
            bool loaded = false;
            Status status = null;
            AudioClip clip = null;
            ResourceLoader.LoadAudio(path,
                (Status loadStatus, AudioClip loadedClip) =>
                {
                    loaded = true;
                    status = loadStatus;
                    clip = loadedClip;
                });
            yield return new WaitUntil(() => loaded);
            if (!status.Ok())
            {
                reportLoadError(status);
                yield break;
            }
            bg.SetBackingTrack(clip);
        }
        reportLoadProgress(backingTrackFilename);

        // Step 4: load keysounds.
        bool keysoundsLoaded = false;
        Status keysoundStatus = null;
        ResourceLoader.CacheAllKeysounds(setup.lockedTrackFolder,
            keysoundFullPaths,
            cacheAudioCompleteCallback: (Status status) =>
            {
                keysoundsLoaded = true;
                keysoundStatus = status;
            },
            fileLoadedCallback: (string fileJustLoaded) =>
            {
                reportLoadProgress(fileJustLoaded);
            });
        yield return new WaitUntil(() => keysoundsLoaded);
        if (!keysoundStatus.Ok())
        {
            reportLoadError(keysoundStatus);
            yield break;
        }

        // Step 5: load BGA.
        string bga = setup.patternAfterModifier.patternMetadata.bga;
        if (!string.IsNullOrEmpty(bga) &&
            !setup.trackOptions.noVideo)
        {
            string path = Paths.Combine(setup.lockedTrackFolder,
                bga);
            bool loaded = false;
            Status status = null;
            ThemeApi.VideoElement element = null;
            ThemeApi.VideoElement.CreateFromFile(path,
                (Status loadStatus,
                ThemeApi.VideoElement loadedElement) =>
                {
                    loaded = true;
                    status = loadStatus;
                    element = loadedElement;
                });
            yield return new WaitUntil(() => loaded);
            if (!status.Ok())
            {
                reportLoadError(status);
                yield break;
            }
            bg.SetBga(element,
                loop: setup.patternAfterModifier.patternMetadata
                    .playBgaOnLoop,
                offset: (float)setup.patternAfterModifier
                    .patternMetadata.bgaOffset);
        }
        reportLoadProgress(bga);

        // A few more synchronous loading steps.

        // Prepare timer.
        timer = new GameTimer(setup.patternAfterModifier);
        float backingTrackLength = 0f;
        float bgaLength = 0f;
        if (bg.backingTrack != null)
        {
            backingTrackLength = bg.backingTrack.length;
        }
        if (bg.bgaElement != null &&
            !setup.patternAfterModifier
                .patternMetadata.playBgaOnLoop &&
            setup.patternAfterModifier.patternMetadata.waitForEndOfBga)
        {
            bgaLength = bg.bgaElement.length;
        }
        timer.Prepare(backingTrackLength, bgaLength);

        // Prepare scanlines and scan countdowns.
        layout = new GameLayout(
            pattern: setup.patternAfterModifier,
            gameContainer: setup.gameContainer.inner,
            layoutTemplate: layoutTemplate);
        layout.Prepare(
            firstScan: timer.firstScan,
            lastScan: timer.lastScan,
            scanlineTemplate);
        yield return null;  // For layout update
        layout.ResetSize();

        // Spawn notes.
        noteManager = new NoteManager(layout: layout);
        noteManager.Prepare(
            setup.patternAfterModifier,
            lastScan: timer.lastScan,
            noteTemplates);
        Debug.Log("Lane height: " + layout.laneHeight);

        // TODO: prepare keyboard input.
        // TODO: Calculate Fever coefficient.
        // TODO: Initialize score.

        // Load complete; wait on theme to begin game.
        state.SetState(ThemeApi.GameState.State.LoadComplete);
        setup.onLoadComplete?.Function?.Call();
    }

    public void Begin()
    {
        timer.Begin();
        bg.Begin();
        layout.Begin();
    }

    public void Pause()
    {
        timer.Pause();
        bg.Pause();
    }

    public void Unpause()
    {
        timer.Unpause();
        bg.Unpause();
    }

    public void Conclude()
    {
        timer.Dispose();
        bg.Conclude();
        layout.Dispose();
        noteManager.Dispose();
    }

    public void UpdateBgBrightness()
    {
        bg.UpdateBgBrightness();
    }

    // Update is called once per frame
    void Update()
    {
        if (state == null) return;

        if (state.state == ThemeApi.GameState.State.Ongoing)
        {
            timer.Update();
            bg.Update(timer.BaseTime);
            layout.Update(timer.Scan);
        }
    }
}
