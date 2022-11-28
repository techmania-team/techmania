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
        public VisualTreeAsset chainHead;
        public VisualTreeAsset chainNode;
        public VisualTreeAsset dragNote;
        public VisualTreeAsset holdNote;
        public VisualTreeAsset holdExtension;
        public VisualTreeAsset repeatHead;
        public VisualTreeAsset repeatHeadHold;
        public VisualTreeAsset repeatNote;
        public VisualTreeAsset repeatHold;
        public VisualTreeAsset repeatHoldExtension;
        public VisualTreeAsset repeatPathExtension;

        public VisualTreeAsset GetForType(NoteType type)
        {
            return type switch
            {
                NoteType.Basic => basicNote,
                NoteType.ChainHead => chainHead,
                NoteType.ChainNode => chainNode,
                NoteType.Drag => dragNote,
                NoteType.Hold => holdNote,
                NoteType.RepeatHead => repeatHead,
                NoteType.RepeatHeadHold => repeatHeadHold,
                NoteType.Repeat => repeatNote,
                NoteType.RepeatHold => repeatHold,
                _ => null
            };
        }

        public VisualTreeAsset GetHoldExtensionForType(NoteType type)
        {
            return type switch
            {
                NoteType.Hold => holdExtension,
                NoteType.RepeatHeadHold => repeatHoldExtension,
                NoteType.RepeatHold => repeatHoldExtension,
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
    // Accessible from Lua via GameState.scoreKeeper
    public ScoreKeeper scoreKeeper { get; private set; }

    public VFXManager vfxManager;
    public ComboText comboText;

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

    private IEnumerator LoadSequence()
    {
        Action<Status> reportLoadError = (Status status) =>
        {
            state.SetLoadError();
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
        hitboxVisible = true;

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
        noteManager = new NoteManager(layout);
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

        // Prepare for VFX and combo text.
        vfxManager.Prepare(layout.laneHeight, timer);
        comboText.ResetSize();
        comboText.Hide();

        // Initialize scores.
        scoreKeeper = new ScoreKeeper(setup);
        scoreKeeper.Prepare(setup.patternAfterModifier,
            timer.firstScan, timer.lastScan,
            playableNotes: noteManager.playableNotes);

        // Load complete; wait on theme to begin game.
        state.SetLoadComplete();
        setup.onLoadComplete?.Function?.Call();
    }

    #region State machine
    public void BeginLoading()
    {
        // Lock down the track so the theme can't change them later.
        setup.lockedTrackFolder = string.Copy(setup.trackFolder);

        StartCoroutine(LoadSequence());
    }

    public void Begin()
    {
        timer.Begin();
        bg.Begin();
        layout.Begin();

        JumpToScan(timer.firstScan);
    }

    public void Pause()
    {
        timer.Pause();
        bg.Pause();
        keysoundPlayer.Pause();
        scoreKeeper.Pause();
    }

    public void Unpause()
    {
        timer.Unpause();
        bg.Unpause();
        keysoundPlayer.Unpause();
        scoreKeeper.Unpause();
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
        comboText.Hide();
    }
    #endregion

    #region Other theme APIs
    public void UpdateBgBrightness()
    {
        bg.UpdateBgBrightness();
    }

    public void ResetElementSizes()
    {
        layout.ResetSize();
        noteManager.ResetSize();
        vfxManager.ResetSize(layout.laneHeight);
        comboText.ResetSize();
    }

    public void ActivateFever()
    {
        scoreKeeper.ActivateFever();
    }
    #endregion

    #region Jumping scans
    public void JumpToScan(int scan)
    {
        // Clamp scan into bounds.
        scan = Mathf.Clamp(scan, timer.firstScan, timer.lastScan);

        timer.JumpToScan(scan);
        noteManager.JumpToScan(timer.IntScan, timer.IntPulse);
        input.JumpToScan();
        scoreKeeper.JumpToScan();
        vfxManager.JumpToScan();

        // Play keysounds before the current time if they last enough.
        foreach (NoteList l in noteManager.notesInLane)
        {
            l.ForEachRemoved((INoteHolder holder) =>
            {
                keysoundPlayer.PlayFromHalfway(holder.note,
                    hidden: setup.patternAfterModifier.IsHidden(
                        holder.note.lane),
                    timer.BaseTime);
            });
        }
    }
    #endregion

    #region Update
    // Update is called once per frame
    void Update()
    {
        if (state == null) return;

        if (state.state == ThemeApi.GameState.State.Ongoing)
        {
            timer.Update(comboTickCallback: ComboTick);
            bg.Update(timer.PrevFrameBaseTime, timer.BaseTime);
            layout.Update(timer.Scan);
            noteManager.Update(timer, scoreKeeper);
            input.Update();
            scoreKeeper.UpdateFever();

            CheckForStageFailed();
            CheckForStageClear();

            if (state.state != ThemeApi.GameState.State.Complete)
            {
                // If game hasn't completed from stage failed or
                // stage clear, call the update callback.
                setup.onUpdate?.Function?.Call(timer);
            }
        }
    }

    private void CheckForStageFailed()
    {
        if (scoreKeeper.hp <= 0 &&
            Modifiers.instance.mode != Modifiers.Mode.NoFail)
        {
            scoreKeeper.score.stageFailed = true;
            setup.onStageFailed?.Function?.Call(scoreKeeper);
            state.SetComplete();
        }
    }

    private void CheckForStageClear()
    {
        if (state.state != ThemeApi.GameState.State.Ongoing) return;
        if (timer.BaseTime <= timer.patternEndTime) return;
        if (Modifiers.instance.mode == Modifiers.Mode.Practice) return;
        if (!scoreKeeper.score.AllNotesResolved()) return;

        scoreKeeper.DeactivateFever();
        setup.onStageClear?.Function?.Call(scoreKeeper);
        state.SetComplete();
    }
    #endregion

    #region Responding to input
    public void HitNote(NoteElements elements, float timeDifference)
    {
        Judgement judgement = GameInputManager
            .TimeDifferenceToJudgement(elements.note,
            timeDifference, timer.speed);

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
        noteManager.ResolveNote(elements);
        scoreKeeper.ResolveNote(elements.note.type, judgement);
        vfxManager.SpawnResolvedVFX(elements, judgement);
        comboText.Show(elements, judgement, scoreKeeper);
        elements.Resolve();

        setup.onNoteResolved?.Function?.Call(elements.note,
            judgement, scoreKeeper);
        if (scoreKeeper.score.AllNotesResolved())
        {
            setup.onAllNotesResolved?.Function?.Call(scoreKeeper);
        }
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
        NoteElements upcomingNote = noteManager.GetUpcoming(
            lane,
            setup.patternAfterModifier.patternMetadata.controlScheme);

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

    #region Responding to time
    public void ComboTick()
    {
        foreach (KeyValuePair<NoteElements, Judgement> pair in
            input.ongoingNotes)
        {
            NoteElements elements = pair.Key;
            Judgement judgement = pair.Value;
            scoreKeeper.IncrementCombo();
            comboText.Show(elements, judgement, scoreKeeper);

            setup.onComboTick?.Function.Call(scoreKeeper.currentCombo);
        }
    }
    #endregion
}
