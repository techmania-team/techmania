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
                _ => null
            };
        }
    }
    public NoteTemplates noteTemplates;

    private ThemeApi.GameSetup setup;
    private ThemeApi.GameState state;
    private GameTimer timer;
    private GameBackground bg;
    private KeysoundPlayer keysoundPlayer;
    private GameLayout layout;
    private NoteManager noteManager;
    private GameInputManager input;

    public VFXManager vfxManager;
    // TODO: combo text manager goes here

    // TODO: when this changes, tell layout to reset scanlines' size.
    public static bool autoPlay;
    public static bool hitboxVisible;

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
        bg = new GameBackground(
            setup.patternAfterModifier,
            setup.bgContainer.inner,
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

        // Switches.
        autoPlay = Modifiers.instance.mode == Modifiers.Mode.AutoPlay;
        hitboxVisible = false;

        // Keysound player.
        keysoundPlayer = new KeysoundPlayer(setup.assistTick);
        keysoundPlayer.Prepare();

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
            noteTemplates);

        // Set up bg to play hidden notes.
        bg.SetNoteManager(noteManager, keysoundPlayer,
            playableLanes: setup.patternAfterModifier
                .patternMetadata.playableLanes);

        // Prepare for input.
        input = new GameInputManager(setup.patternAfterModifier,
            this, layout, noteManager, timer);
        input.Prepare();

        // Prepare for VFX. TODO: also combo text.
        vfxManager.Prepare(layout.laneHeight);

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
        keysoundPlayer.Pause();
    }

    public void Unpause()
    {
        timer.Unpause();
        bg.Unpause();
        keysoundPlayer.Unpause();
    }

    public void Conclude()
    {
        timer.Dispose();
        bg.Conclude();
        keysoundPlayer.Dispose();
        layout.Dispose();
        noteManager.Dispose();
        input.Dispose();
        vfxManager.Dispose();
    }

    public void UpdateBgBrightness()
    {
        bg.UpdateBgBrightness();
    }

    public void ResetSize()
    {
        layout.ResetSize();
        noteManager.ResetSize();
        vfxManager.ResetSize(layout.laneHeight);
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
            noteManager.Update(timer);
            input.Update();
        }
    }

    #region Responding to input
    public void HitNote(NoteElements elements, float timeDifference)
    {
        Judgement judgement = Judgement.Miss;
        float absDifference = Mathf.Abs(timeDifference);
        absDifference /= timer.speed;

        foreach (Judgement j in new List<Judgement>{
            Judgement.RainbowMax,
            Judgement.Max,
            Judgement.Cool,
            Judgement.Good,
            Judgement.Miss
        })
        {
            if (absDifference <= elements.note.timeWindow[j])
            {
                judgement = j;
                break;
            }
        }

        switch (elements.note.type)
        {
            case NoteType.Hold:
            case NoteType.Drag:
            case NoteType.RepeatHeadHold:
            case NoteType.RepeatHold:
                if (judgement == Judgement.Miss)
                {
                    // Missed notes do not enter Ongoing state.
                    ResolveNote(elements, judgement);
                }
                else
                {
                    // Register an ongoing note.
                    input.RegisterOngoingNote(elements, judgement);
                    elements.SetOngoing();
                    vfxManager.SpawnOngoingVFX(elements, judgement);
                }
                break;
            default:
                ResolveNote(elements, judgement);
                break;
        }

        keysoundPlayer.Play(elements.note,
            hidden: false, emptyHit: false);
    }

    public void ResolveNote(NoteElements elements,
        Judgement judgement)
    {
        // Remove note from lists.
        noteManager.ResolveNote(elements);

        // TODO: update score, combo and fever.
        vfxManager.SpawnResolvingVFX(elements, judgement);
        // TODO: show combo text.

        elements.Resolve();
    }

    public void StopKeysoundIfPlaying(Note n)
    {
        keysoundPlayer.StopIfPlaying(n);
    }

    public void EmptyHitForFinger(int lane)
    {
        if (lane == GameLayout.kOutsideAllLanes)
        {
            return;
        }

        // Find the upcoming note.
        NoteElements upcomingNote = null;
        switch (setup.patternAfterModifier.patternMetadata
            .controlScheme)
        {
            case ControlScheme.Touch:
                if (noteManager.notesInLane[lane].Count == 0)
                    break;
                upcomingNote = noteManager.notesInLane[lane].First()
                    as NoteElements;
                break;
            case ControlScheme.Keys:
                // Keys should not call this method.
                break;
            case ControlScheme.KM:
                if (noteManager.mouseNotesInLane[lane].Count == 0) 
                    break;
                upcomingNote = noteManager.mouseNotesInLane[lane]
                    .First() as NoteElements;
                break;
        }

        if (upcomingNote == null) return;
        if (input.IsOngoing(upcomingNote)) return;
        keysoundPlayer.Play(upcomingNote.note,
            hidden: false, emptyHit: true);
    }

    public void EmptyHitForKeyboard(Note note)
    {
        keysoundPlayer.Play(note,
            hidden: false, emptyHit: true);
    }
    #endregion
}
