using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameController : MonoBehaviour
{
    public static GameController instance { get; private set; }
    private ThemeApi.GameSetup setup;
    private ThemeApi.GameState state;

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
            state.SetStateAndTriggerCallback(
                ThemeApi.GameState.State.LoadError);
            setup.onLoadError.Function.Call(status);
        };
        int filesLoaded = 0;
        int totalFiles = 0;
        Action<string> reportLoadProgress = (string fileJustLoaded) =>
        {
            filesLoaded++;
            setup.onLoadProgress.Function.Call(
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

        // TODO: hide background and VFX from previous game.
        setup.bgContainer.inner.style.unityBackgroundImageTintColor =
            Color.clear;

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
                    setup.trackFolder, n.sound));
            }
        }
        totalFiles += keysoundFullPaths.Count;

        // Step 1: load background image to display on the loading
        // screen.
        string backImage = setup.patternAfterModifier.patternMetadata
            .backImage;
        if (!string.IsNullOrEmpty(backImage))
        {
            string path = Paths.Combine(setup.trackFolder, backImage);
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

            VisualElement bg = setup.bgContainer.inner;
            bg.style.backgroundImage = new StyleBackground(texture);
            bg.style.unityBackgroundScaleMode = new 
                StyleEnum<ScaleMode>(ScaleMode.ScaleAndCrop);
            bg.style.unityBackgroundImageTintColor = Color.white;
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
        string backingTrack = setup.patternAfterModifier
            .patternMetadata.backingTrack;
        if (!string.IsNullOrEmpty(backingTrack))
        {
            string path = Paths.Combine(setup.trackFolder, 
                backingTrack);
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
        }
        reportLoadProgress(backingTrack);

        // Step 4: load keysounds.
        bool keysoundsLoaded = false;
        Status keysoundStatus = null;
        ResourceLoader.CacheAllKeysounds(setup.trackFolder,
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
            // TODO: set up video player.
        }
        reportLoadProgress(bga);

        // TODO: Step 6: initialize pattern.

        // Load complete; wait on theme to begin game.
        state.SetStateAndTriggerCallback(
            ThemeApi.GameState.State.LoadComplete);
    }

    public void Begin()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
