using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;  // For stopwatch
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;  // Not System.Diagnostics.Debug

public enum Judgement
{
    RainbowMax,
    Max,
    Cool,
    Good,
    Miss,
    Break
}

public enum InputDevice
{
    Touchscreen,
    Keyboard,
    Mouse
}

public class Game : MonoBehaviour
{
    public GlobalResourceLoader globalResourceLoader;
    public bool inEditor;

    [Header("Background")]
    public RectTransform backgroundContainer;
    public Image backgroundImage;
    public VideoPlayer videoPlayer;
    public RawImage bga;
    // In practice mode the player may jump back and forth between
    // points where BGA is playing and where BGA is not playing.
    //
    // If we stop and play BGA accordingly, the problem arises that
    // stopping a video also unloads it, so when we try to play it
    // again it can only play from the beginning, making it difficult
    // to seek to any non-0 time.
    //
    // Therefore, after loading the BGA we immedialy play and then
    // pause it. We never stop the BGA. When the player jumps to
    // a point before BGA starts, we seek to the video's beginning
    // and pause. Now we can jump to any time.
    //
    // But then another problem arises that, before the BGA starts
    // playing, the 1st frame of the video remains visible, when
    // the player expects a black screen. Therefore we add this BGA
    // cover to simulate the black screen and hide the fact that the
    // BGA is started and paused.
    public Image bgaCover;
    public Image brightnessCover;

    [Header("Scans")]
    public GraphicRaycaster raycaster;
    public ScanBackground topScanBackground;
    public Transform topScanContainer;
    public GameObject topScanTemplate;
    public ScanBackground bottomScanBackground;
    public Transform bottomScanContainer;
    public GameObject bottomScanTemplate;

    [Header("Audio")]
    public AudioSourceManager audioSourceManager;
    public AudioClip assistTick;

    [Header("Prefabs")]
    public GameObject basicNotePrefab;
    public GameObject chainHeadPrefab;
    public GameObject chainNodePrefab;
    public GameObject holdNotePrefab;
    public GameObject holdExtensionPrefab;
    public GameObject dragNotePrefab;
    public GameObject repeatHeadPrefab;
    public GameObject repeatHeadHoldPrefab;
    public GameObject repeatNotePrefab;
    public GameObject repeatHoldPrefab;
    public GameObject repeatPathExtensionPrefab;
    public GameObject repeatHoldExtensionPrefab;
    public GameObject hiddenNotePrefab;

    [Header("VFX")]
    public VFXSpawner vfxSpawner;
    public ComboText comboText;

    [Header("UI - Fever")]
    public GameObject middleFeverBar;
    public RectTransform middleFeverBarFilling;
    public GameObject middleFeverText;
    public TextMeshProUGUI feverBonusText;
    public Animator feverBonusAnimator;
    public RectTransform feverButtonFilling;
    public TextMeshProUGUI feverInstruction;
    public Animator feverButtonAnimator;
    public AudioSource feverSoundSource;

    [Header("UI - Practice")]
    public GameObject practiceTopBar;
    public TextMeshProUGUI scanDisplay;
    public TextMeshProUGUI loopDisplay;
    public TextMeshProUGUI speedDisplay;
    public Toggle autoPlayToggle;
    public Toggle showHitboxToggle;

    [Header("UI - Other")]
    public GameObject topBar;
    public GameObject regularTopBar;
    public GameObject pauseButton;
    public GameObject backButton;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI maxComboText;
    public RectTransform hpBar;
    public GameObject noFailIndicator;
    public GameObject loadingBar;
    public MaterialProgressBar loadingProgress;
    public GameObject fpsCounter;
    public JudgementTally judgementTally;
    public PauseDialog pauseDialog;
    public MessageDialog messageDialog;
    public GameObject stageFailedScreen;

    public static Score score { get; private set; }
    public static int currentCombo { get; private set; }
    public static int maxCombo { get; private set; }
    private int hp;

    public enum FeverState
    {
        Idle,  // Accummulates with MAXes
        Ready,  // No longer accummulates, awaiting activation
        Active  // Decreases with time
    }
    private float feverCoefficient;
    public static FeverState feverState { get; private set; }
    public static float feverAmount { get; private set; }

    public static int playableLanes => 
        GameSetup.pattern.patternMetadata.playableLanes;
    private const int kComboTickInterval = 60;
    // Combo ticks are pulses where each ongoing note increases
    // combo by 1. Ongoing notes add 1 more combo when
    // resolved. Combo ticks are, by default, 60 pulses apart.
    private int previousComboTick;

    private bool loading;

    public static void InjectFeverAndCombo(FeverState feverState,
        int currentCombo)
    {
        Game.feverState = feverState;
        Game.currentCombo = currentCombo;
    }
    
    #region Timers
    // The stopwatch provides the "base time", which drives
    // the backing track, BGA, hidden notes and auto-played notes.
    private Stopwatch stopwatch;
    private static float BaseTime { get; set; }

    private static float offset;
    // The public timer is compensated for offset, to be used for
    // scanlines and notes. All public timers are based on this time.
    public static float Time
    {
        get
        {
            if (autoPlay) return BaseTime;
            return BaseTime - offset;
        }
    }
    public static int PulsesPerScan { get; private set; }
    public static float FloatPulse { get; private set; }
    public static float FloatBeat { get; private set; }
    public static float FloatScan { get; private set; }
    private static int Pulse { get; set; }
    public static int Scan { get; private set; }
    private float endOfPatternBaseTime;
    private int firstScan;
    private int lastScan;
    private Stopwatch feverTimer;
    private float initialTime;

    public static void InjectBaseTimeAndOffset(float baseTime,
        float offset)
    {
        BaseTime = baseTime;
        autoPlay = false;
        Game.offset = offset;
    }
    #endregion

    #region Practice Mode Config
    private int loopStart;
    private int loopEnd;
    private int speedPercentage;
    private float speed { get => speedPercentage * 0.01f; }

    public static bool hitboxVisible { get; private set; }
    public static bool autoPlay { get; private set; }
    #endregion

    #region Editor Preview
    [HideInInspector]
    public Options optionsBackup;
    #endregion

    private Dictionary<int, Scan> scanObjects;
    public static event UnityAction<int> ScanChanged;
    public static event UnityAction<int> ScanAboutToChange;
    public static event UnityAction<int> JumpedToScan;

    public static List<List<KeyCode>> keysForLane { get; private set; }

    // Each NoteList represents one lane; each lane is sorted by
    // pulse.
    private List<NoteList> noteObjectsInLane;
    // noteObjectsInLane separated into mouse and keyboard notes.
    // In KM, Each input device only care about notes in its
    // corresponding list.
    private List<NoteList> notesForMouseInLane;
    private List<NoteList> notesForKeyboardInLane;
    private int numPlayableNotes;

    // Value is the judgement at note's head.
    private Dictionary<NoteObject, Judgement> ongoingNotes;
    private Dictionary<NoteObject, bool> ongoingNoteIsHitOnThisFrame;
    private Dictionary<NoteObject, float> ongoingNoteLastInput;

    #region Monobehavior messages
    // Start is called before the first frame update
    private void OnEnable()
    {
        if (GameSetup.track == null)
        {
            SceneManager.LoadScene("Main Menu");
        }
        Input.simulateMouseWithTouches = false;
        score = new Score();
        loading = true;
        topBar.SetActive(false);
        pauseButton.SetActive(!inEditor);
        backButton.SetActive(inEditor);
        practiceTopBar.SetActive(Modifiers.instance.mode
            == Modifiers.Mode.Practice);
        regularTopBar.SetActive(Modifiers.instance.mode
            != Modifiers.Mode.Practice);
        middleFeverBar.SetActive(false);
        loadingBar.SetActive(true);
        loadingBar.GetComponent<CanvasGroup>().alpha =
            Options.instance.showLoadingBar ? 1f : 0f;
        loadingProgress.SetValue(0f);
        fpsCounter.SetActive(false);
        judgementTally.gameObject.SetActive(false);
        stopwatch = null;

        // Load options.
        Options.RefreshInstance();
        if (Options.instance.ruleset == Options.Ruleset.Custom)
        {
            Ruleset.LoadCustomRuleset();
        }
        GameSetup.trackOptions = Options.instance.GetPerTrackOptions(
            GameSetup.track.trackMetadata.guid);
        if (inEditor)
        {
            Options.MakeBackup();
            Options.instance.modifiers = new Modifiers();
            GameSetup.trackOptions.backgroundBrightness = 10;
            GameSetup.trackOptions.noVideo = false;
            Modifiers.instance.mode = Modifiers.Mode.Practice;

            practiceTopBar.SetActive(true);
            regularTopBar.SetActive(false);
        }

        // Start the load sequence.
        StartCoroutine(LoadSequence());
    }

    private void OnDisable()
    {
        if (inEditor)
        {
            Options.RestoreBackup();
            SetSpeed(100);
            audioSourceManager.StopAll();

            // Clear out scans.
            for (int i = 0; i < topScanContainer.childCount; i++)
            {
                Transform child = topScanContainer.GetChild(i);
                if (child == topScanTemplate.transform) continue;
                Destroy(child.gameObject);
            }
            for (int i = 0; i < bottomScanContainer.childCount; i++)
            {
                Transform child = bottomScanContainer.GetChild(i);
                if (child == bottomScanTemplate.transform) continue;
                Destroy(child.gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        Options.instance.SaveToFile(Paths.GetOptionsFilePath());
        Input.simulateMouseWithTouches = true;
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus && !IsPaused() && !loading && !inEditor &&
            Options.instance.pauseWhenGameLosesFocus)
        {
            OnPauseButtonClickOrTouch(playSound: false);
        }
    }
    #endregion

    #region Initialization
    private void ReportFatalError(string message)
    {
        messageDialog.Show(message, closeCallback: () =>
        {
            if (inEditor)
            {
                GetComponentInChildren<TransitionToPanel>(
                    includeInactive: true).Invoke();
            }
            else
            {
                MainMenuPanel.skipToTrackSelect = true;
                Curtain.DrawCurtainThenGoToScene("Main Menu");
            }
        });
    }

    private bool backgroundImageLoaded;
    private AudioClip backingTrackClip;
    private bool backingTrackLoaded;
    private bool keysoundsLoaded;
    private IEnumerator LoadSequence()
    {
        // Hide all background and VFX so they don't "leak" from
        // one preview session to the next.
        backgroundImage.color = Color.clear;
        bga.color = Color.clear;
        HideBGACover();
        vfxSpawner.RemoveAll();
        comboText.Hide();
        topScanBackground.HideAllMarkers();
        bottomScanBackground.HideAllMarkers();

        // Step 1: load background image, if any. This makes the
        // loading screen not too dull.
        if (GameSetup.pattern.patternMetadata.backImage != null &&
            GameSetup.pattern.patternMetadata.backImage != "")
        {
            string fullPath = Path.Combine(GameSetup.trackFolder,
                GameSetup.pattern.patternMetadata.backImage);
            backgroundImageLoaded = false;
            ResourceLoader.LoadImage(fullPath,
                OnImageLoadComplete);
            yield return new WaitUntil(() => backgroundImageLoaded);
        }

        // Step 2: load skins, if told to.
        if (Options.instance.reloadSkinsWhenLoadingPattern)
        {
            GlobalResourceLoader.CompleteCallback loadSkinCallback =
                (status) =>
            {
                if (!status.ok)
                {
                    ReportFatalError(status.errorMessage);
                }
            };

            globalResourceLoader.LoadAllSkins(
                progressCallback: null,
                completeCallback: loadSkinCallback);
        }

        // Step 3: load backing track, if any.
        // This allows calculating the number of scans.
        if (GameSetup.pattern.patternMetadata.backingTrack != null &&
            GameSetup.pattern.patternMetadata.backingTrack != "")
        {
            string fullPath = Path.Combine(GameSetup.trackFolder,
                GameSetup.pattern.patternMetadata.backingTrack);
            backingTrackLoaded = false;
            ResourceLoader.LoadAudio(fullPath,
                OnBackingTrackLoadComplete);
            yield return new WaitUntil(() => backingTrackLoaded);
        }
        else
        {
            backingTrackClip = null;
        }

        // Step 4: load keysounds, if any.
        keysoundsLoaded = false;
        ResourceLoader.CacheAllKeysounds(GameSetup.trackFolder,
            GameSetup.pattern,
            OnKeysoundLoadComplete,
            OnKeysoundLoadProgress);
        yield return new WaitUntil(() => keysoundsLoaded);

        // Step 5: load BGA, if any.
        if (!GameSetup.trackOptions.noVideo &&
            GameSetup.pattern.patternMetadata.bga != null &&
            GameSetup.pattern.patternMetadata.bga != "")
        {
            string fullPath = Path.Combine(GameSetup.trackFolder,
                GameSetup.pattern.patternMetadata.bga);
            videoPlayer.url = fullPath;
            videoPlayer.errorReceived += VideoPlayerErrorReceived;
            videoPlayer.Prepare();
            yield return new WaitUntil(() => videoPlayer.isPrepared);
            videoPlayer.errorReceived -= VideoPlayerErrorReceived;
            PrepareVideoPlayer();  // This also shows BGA cover
            backgroundImage.color = Color.clear;
        }

        // Step 6: initialize pattern. This sadly cannot be done
        // asynchronously.
        InitializePattern();

        // Loading complete.
        loading = false;
        topBar.SetActive(true);
        if (Options.instance.backgroundScalingMode == 
            Options.BackgroundScalingMode.FillGameArea)
        {
            float topBarHeight = topBar.GetComponent<RectTransform>()
                .rect.height;
            backgroundContainer.anchoredPosition = new Vector2(
                0f, -0.5f * topBarHeight);
            backgroundContainer.sizeDelta = new Vector2(
                0f, -topBarHeight);
        }
        noFailIndicator.SetActive(
            Modifiers.instance.mode == Modifiers.Mode.NoFail);
        middleFeverBar.SetActive(true);
        loadingBar.SetActive(false);
        if (Options.instance.showFps)
        {
            fpsCounter.SetActive(true);
        }
        if (Options.instance.showJudgementTally &&
            Modifiers.instance.mode != Modifiers.Mode.Practice)
        {
            judgementTally.gameObject.SetActive(true);
            judgementTally.Refresh(score);
        }
        SetBrightness();
        topScanBackground.Initialize(
            Modifiers.instance.GetTopScanDirection(), 
            global::Scan.Position.Top);
        bottomScanBackground.Initialize(
            Modifiers.instance.GetBottomScanDirection(),
            global::Scan.Position.Bottom);

        int offsetMs =
            GameSetup.pattern.patternMetadata.controlScheme
            == ControlScheme.Touch ?
            Options.instance.touchOffsetMs :
            Options.instance.keyboardMouseOffsetMs;
        offset = offsetMs * 0.001f;

        yield return null;  // Wait 1 more frame just in case.

        // Start timer. Backing track will start when timer hits 0;
        // BGA will start when timer hits bgaOffset.
        stopwatch = new Stopwatch();
        stopwatch.Start();
        if (inEditor)
        {
            JumpToScan(GameSetup.beginningScanInEditorPreview);
        }
        else
        {
            JumpToScan(firstScan);
        }
    }

    private void OnImageLoadComplete(Status status,
        Texture2D texture)
    {
        if (!status.ok)
        {
            backgroundImage.color = Color.clear;
            ReportFatalError(status.errorMessage);
            return;
        }

        backgroundImage.sprite =
            ResourceLoader.CreateSpriteFromTexture(texture);
        backgroundImage.color = Color.white;
        backgroundImage.GetComponent<AspectRatioFitter>()
            .aspectRatio =
            (float)texture.width / texture.height;
        backgroundImageLoaded = true;
    }

    private void OnBackingTrackLoadComplete(
        AudioClip clip, string error)
    {
        if (error != null)
        {
            ReportFatalError(error);
            return;
        }

        backingTrackClip = clip;
        backingTrackLoaded = true;
    }

    private void InitializePattern()
    {
        // Prepare for keyboard input if applicable.
        if (GameSetup.pattern.patternMetadata
            .controlScheme == ControlScheme.Keys ||
            GameSetup.pattern.patternMetadata
            .controlScheme == ControlScheme.KM)
        {
            InitializeKeysForLane();
        }

        // Time calculations.
        GameSetup.pattern.PrepareForTimeCalculation();
        GameSetup.pattern.CalculateTimeOfAllNotes(
            calculateTimeWindows: true);
        firstScan = 0;
        previousComboTick = 0;

        // Rewind till 1 scan before the backing track starts.
        PulsesPerScan = Pattern.pulsesPerBeat *
            GameSetup.pattern.patternMetadata.bps;
        while (initialTime >= 0f)
        {
            firstScan--;
            initialTime = GameSetup.pattern.PulseToTime(
                firstScan * PulsesPerScan);
        }

        // Rewind further until 1 scan before the BGA starts.
        while (initialTime > GameSetup.pattern
            .patternMetadata.bgaOffset)
        {
            firstScan--;
            initialTime = GameSetup.pattern.PulseToTime(
                firstScan * PulsesPerScan);
        }

        // Find last scan. Make sure it ends later than the backing
        // track and BGA, so we don't cut either short.
        CalculateEndOfPattern();

        // Create scan objects.
        scanObjects = new Dictionary<int, Scan>();
        Scan.Direction topScanDirection = 
            Modifiers.instance.GetTopScanDirection();
        Scan.Direction bottomScanDirection =
            Modifiers.instance.GetBottomScanDirection();
        for (int i = firstScan; i <= lastScan; i++)
        {
            global::Scan.Position position = 
                Modifiers.instance.GetScanPosition(i);
            Transform parent = position switch
            {
                global::Scan.Position.Top => topScanContainer,
                global::Scan.Position.Bottom => bottomScanContainer,
                _ => null
            };
            GameObject template = position switch
            {
                global::Scan.Position.Top => topScanTemplate,
                global::Scan.Position.Bottom => bottomScanTemplate,
                _ => null
            };
            GameObject scanObject = Instantiate(template, parent);
            scanObject.SetActive(true);

            Scan s = scanObject.GetComponent<Scan>();
            global::Scan.Direction direction = position switch
            {
                global::Scan.Position.Top => topScanDirection,
                global::Scan.Position.Bottom => bottomScanDirection,
                _ => throw new NotImplementedException()
            };
            s.Initialize(scanNumber: i, direction, position);
            scanObjects.Add(i, s);
        }

        // Create note objects. In reverse order, so earlier notes
        // are drawn on the top.
        // Also organize them as linked lists, so empty hits can
        // play the keysound of upcoming notes.
        noteObjectsInLane = new List<NoteList>();
        notesForMouseInLane = new List<NoteList>();
        notesForKeyboardInLane = new List<NoteList>();
        numPlayableNotes = 0;
        NoteObject nextChainNode = null;
        List<List<NoteObject>> unmanagedRepeatNotes =
            new List<List<NoteObject>>();
        // Create at least as many lists as playable lanes, or
        // empty hits on the noteless lanes will generate errors.
        for (int i = 0; i < playableLanes; i++)
        {
            noteObjectsInLane.Add(new NoteList());
            notesForMouseInLane.Add(new NoteList());
            notesForKeyboardInLane.Add(new NoteList());
            unmanagedRepeatNotes.Add(new List<NoteObject>());
        }
        foreach (Note n in GameSetup.pattern.notes.Reverse())
        {
            int scanOfN = n.GetScanNumber(
                GameSetup.pattern.patternMetadata.bps);
            bool hidden = n.lane >= playableLanes;
            if (!hidden) numPlayableNotes++;

            NoteObject noteObject = SpawnNoteObject(
                n, scanObjects[scanOfN], hidden);
            NoteAppearance appearance = noteObject
                .GetComponent<NoteAppearance>();

            while (noteObjectsInLane.Count <= n.lane)
            {
                noteObjectsInLane.Add(new NoteList());
                notesForMouseInLane.Add(new NoteList());
                notesForKeyboardInLane.Add(new NoteList());
                unmanagedRepeatNotes.Add(new List<NoteObject>());
            }
            noteObjectsInLane[n.lane].Add(noteObject);
            switch (n.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.Drag:
                    notesForMouseInLane[n.lane].Add(noteObject);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHead:
                case NoteType.RepeatHeadHold:
                case NoteType.Repeat:
                case NoteType.RepeatHold:
                    notesForKeyboardInLane[n.lane]
                        .Add(noteObject);
                    break;
            }

            // The type-specific operations after this are skipped
            // for hidden notes.
            if (hidden) continue;

            // Create extensions for hold notes that cross scans.
            if (n.type == NoteType.Hold ||
                n.type == NoteType.RepeatHeadHold ||
                n.type == NoteType.RepeatHold)
            {
                CreateHoldExtensions(noteObject, scanObjects);
            }

            // Connect chain heads/nodes to the node after it.
            if (n.type == NoteType.ChainHead ||
                n.type == NoteType.ChainNode)
            {
                (appearance as ChainAppearanceBase).SetNextChainNode(
                    nextChainNode?.gameObject);
                if (n.type == NoteType.ChainHead)
                {
                    nextChainNode = null;
                }
                else // ChainNode
                {
                    nextChainNode = noteObject;
                }
            }

            // Establish management between repeat (hold) heads
            // and repeat (hold) notes.
            if (n.type == NoteType.Repeat ||
                n.type == NoteType.RepeatHold)
            {
                unmanagedRepeatNotes[n.lane].Add(
                    noteObject);
            }
            if (n.type == NoteType.RepeatHead ||
                n.type == NoteType.RepeatHeadHold)
            {
                ManageRepeatNotes(noteObject,
                    unmanagedRepeatNotes[n.lane],
                    scanObjects);
            }
        }
        foreach (NoteList l in noteObjectsInLane) l.Reverse();
        foreach (NoteList l in notesForKeyboardInLane) l.Reverse();
        foreach (NoteList l in notesForMouseInLane) l.Reverse();

        // Calculate Fever coefficient. The goal is for the Fever bar
        // to fill up in around 12.5 seconds.
        int lastPulse = (lastScan + 1) *
            GameSetup.pattern.patternMetadata.bps *
            Pattern.pulsesPerBeat;
        if (Ruleset.instance.constantFeverCoefficient)
        {
            feverCoefficient = 8f;
        }
        else
        {
            float trackLength =
                GameSetup.pattern.PulseToTime(lastPulse);
            feverCoefficient = trackLength / 12.5f;
        }
        Debug.Log("Fever coefficient is: " + feverCoefficient);

        // Initialize practice mode.
        loopStart = firstScan;
        loopEnd = lastScan;
        speedPercentage = 100;

        // Miscellaneous initialization.
        hitboxVisible = false;
        autoPlay = Modifiers.instance.mode == Modifiers.Mode.AutoPlay;
        fingerInLane = new Dictionary<int, int>();
        currentCombo = 0;
        maxCombo = 0;
        score.Initialize(numPlayableNotes);
        hp = Ruleset.instance.maxHp;
        feverState = FeverState.Idle;
        feverAmount = 0f;
        switch (GameSetup.pattern.patternMetadata.controlScheme)
        {
            case ControlScheme.Touch:
                feverInstruction.text = Locale.GetString(
                    "game_fever_instruction_touch");
                break;
            case ControlScheme.Keys:
            case ControlScheme.KM:
                feverInstruction.text = Locale.GetString(
                    "game_fever_instruction_keys_km");
                break;
        }
        ongoingNotes = new Dictionary<NoteObject, Judgement>();
        ongoingNoteIsHitOnThisFrame =
            new Dictionary<NoteObject, bool>();
        ongoingNoteLastInput = new Dictionary<NoteObject, float>();
        noteToAudioSource = new Dictionary<Note, AudioSource>();
    }

    #region Subroutines of InitializePattern
    private void InitializeKeysForLane()
    {
        keysForLane = new List<List<KeyCode>>();
        if (playableLanes >= 4)
        {
            keysForLane.Add(new List<KeyCode>()
            {
                KeyCode.BackQuote,
                KeyCode.Alpha0,
                KeyCode.Alpha1,
                KeyCode.Alpha2,
                KeyCode.Alpha3,
                KeyCode.Alpha4,
                KeyCode.Alpha5,
                KeyCode.Alpha6,
                KeyCode.Alpha7,
                KeyCode.Alpha8,
                KeyCode.Alpha9,
                KeyCode.Minus,
                KeyCode.Equals,
                KeyCode.KeypadDivide,
                KeyCode.KeypadMultiply,
                KeyCode.KeypadMinus
            });
        }
        if (playableLanes >= 3)
        {
            keysForLane.Add(new List<KeyCode>()
            {
                KeyCode.Q,
                KeyCode.W,
                KeyCode.E,
                KeyCode.R,
                KeyCode.T,
                KeyCode.Y,
                KeyCode.U,
                KeyCode.I,
                KeyCode.O,
                KeyCode.P,
                KeyCode.LeftBracket,
                KeyCode.RightBracket,
                KeyCode.Backslash,
                KeyCode.Keypad7,
                KeyCode.Keypad8,
                KeyCode.Keypad9
            });
        }
        keysForLane.Add(new List<KeyCode>()
        {
            KeyCode.A,
            KeyCode.S,
            KeyCode.D,
            KeyCode.F,
            KeyCode.G,
            KeyCode.H,
            KeyCode.J,
            KeyCode.K,
            KeyCode.L,
            KeyCode.Semicolon,
            KeyCode.Quote,
            KeyCode.Keypad4,
            KeyCode.Keypad5,
            KeyCode.Keypad6
        });
        keysForLane.Add(new List<KeyCode>()
        {
            KeyCode.Z,
            KeyCode.X,
            KeyCode.C,
            KeyCode.V,
            KeyCode.B,
            KeyCode.N,
            KeyCode.M,
            KeyCode.Comma,
            KeyCode.Period,
            KeyCode.Slash,
            KeyCode.Keypad1,
            KeyCode.Keypad2,
            KeyCode.Keypad3
        });
    }

    private void CalculateEndOfPattern()
    {
        endOfPatternBaseTime = 0f;
        if (backingTrackClip != null)
        {
            endOfPatternBaseTime = Mathf.Max(endOfPatternBaseTime,
                backingTrackClip.length);
        }
        bool waitForEndOfBga = GameSetup.pattern.patternMetadata
            .waitForEndOfBga;
        if (GameSetup.pattern.patternMetadata.playBgaOnLoop)
        {
            waitForEndOfBga = false;
        }
        if (videoPlayer.url != null && waitForEndOfBga)
        {
            endOfPatternBaseTime = Mathf.Max(endOfPatternBaseTime,
                (float)GameSetup.pattern.patternMetadata.bgaOffset +
                (float)videoPlayer.length);
        }
        foreach (Note n in GameSetup.pattern.notes)
        {
            float noteStartTime = n.time;
            float duration = 0f;
            if (n.sound != null && n.sound != "")
            {
                duration = ResourceLoader.GetCachedClip(
                    n.sound).length;
            }
            float noteEndTime = noteStartTime + duration;

            // For long notes, additionally check the note length, as
            // they may be longer than the keysounds.
            if (n is HoldNote)
            {
                int noteEndPulse = n.pulse + (n as HoldNote).duration;
                noteEndTime = Mathf.Max(noteEndTime,
                    GameSetup.pattern.PulseToTime(noteEndPulse)
                    + offset);
            }
            if (n is DragNote)
            {
                int noteEndPulse = n.pulse + (n as DragNote).Duration();
                noteEndTime = Mathf.Max(noteEndTime,
                    GameSetup.pattern.PulseToTime(noteEndPulse)
                    + offset);
            }

            endOfPatternBaseTime = Mathf.Max(endOfPatternBaseTime, 
                noteEndTime);
        }

        float maxPulse = GameSetup.pattern.TimeToPulse(
            endOfPatternBaseTime);
        lastScan = Mathf.FloorToInt(
            maxPulse / PulsesPerScan);
    }

    private NoteObject SpawnNoteObject(Note n,
        Scan scan, bool hidden)
    {
        GameObject prefab = null;
        if (hidden)
        {
            prefab = hiddenNotePrefab;
        }
        else
        {
            switch (n.type)
            {
                case NoteType.Basic:
                    prefab = basicNotePrefab;
                    break;
                case NoteType.ChainHead:
                    prefab = chainHeadPrefab;
                    break;
                case NoteType.ChainNode:
                    prefab = chainNodePrefab;
                    break;
                case NoteType.Hold:
                    prefab = holdNotePrefab;
                    break;
                case NoteType.Drag:
                    prefab = dragNotePrefab;
                    break;
                case NoteType.RepeatHead:
                    prefab = repeatHeadPrefab;
                    break;
                case NoteType.RepeatHeadHold:
                    prefab = repeatHeadHoldPrefab;
                    break;
                case NoteType.Repeat:
                    prefab = repeatNotePrefab;
                    break;
                case NoteType.RepeatHold:
                    prefab = repeatHoldPrefab;
                    break;
                default:
                    Debug.LogError("Unsupported note type: " +
                        n.type);
                    break;
            }
        }
        return scan.SpawnNoteObject(prefab, n, hidden);
    }

    private void CreateHoldExtensions(NoteObject n,
        Dictionary<int, Scan> scanObjects)
    {
        HoldNote holdNote = n.note as HoldNote;
        GameObject extensionPrefab;
        if (n.note.type == NoteType.Hold)
        {
            extensionPrefab = holdExtensionPrefab;
        }
        else  // RepeatHeadHold or RepeatHold
        {
            extensionPrefab = repeatHoldExtensionPrefab;
        }
        // If a hold note ends at a scan divider, we don't
        // want to spawn an unnecessary extension, thus the
        // -1.
        int scanOfN = holdNote.GetScanNumber(
            GameSetup.pattern.patternMetadata.bps);
        int lastScan = (holdNote.pulse + holdNote.duration - 1)
            / PulsesPerScan;
        for (int crossedScan = scanOfN + 1;
            crossedScan <= lastScan;
            crossedScan++)
        {
            HoldExtension extension = scanObjects[crossedScan]
                .SpawnHoldExtension(extensionPrefab, holdNote);
            n.GetComponent<NoteAppearance>()
                .RegisterHoldExtension(extension);
        }
    }

    private void ManageRepeatNotes(NoteObject head,
        List<NoteObject> notesToManage,
        Dictionary<int, Scan> scanObjects)
    {
        RepeatHeadAppearanceBase headAppearance = 
            head.GetComponent<RepeatHeadAppearanceBase>();
        headAppearance.ManageRepeatNotes(notesToManage);
        headAppearance.DrawRepeatHeadBeforeRepeatNotes();
        if (notesToManage.Count > 0)
        {
            int headScan = head.note.GetScanNumber(
                GameSetup.pattern.patternMetadata.bps);

            NoteObject lastRepeatNote = notesToManage[0];
            int lastScan = lastRepeatNote.note.GetScanNumber(
                GameSetup.pattern.patternMetadata.bps);
            int lastRepeatNotePulse = lastRepeatNote.note.pulse;
            if (lastRepeatNote.note is HoldNote)
            {
                lastRepeatNotePulse +=
                    (lastRepeatNote.note as HoldNote).duration;

                lastScan = lastRepeatNotePulse / PulsesPerScan;
                if (lastRepeatNotePulse % PulsesPerScan == 0)
                {
                    lastScan--;
                }
            }
            headAppearance.DrawRepeatPathTo(lastRepeatNotePulse,
                positionEndOfScanOutOfBounds: 
                headScan != lastScan);

            // Create path extensions if the head and last
            // note are in different scans.
            for (int crossedScan = headScan + 1;
                crossedScan <= lastScan;
                crossedScan++)
            {
                RepeatPathExtension extension =
                    scanObjects[crossedScan]
                    .SpawnRepeatPathExtension(
                        repeatPathExtensionPrefab,
                        head,
                        lastRepeatNotePulse);
                extension.DrawBeforeRepeatNotes();
                headAppearance.RegisterRepeatPathExtension(
                    extension);
            }
        }
        notesToManage.Clear();
    }
    #endregion

    private void OnKeysoundLoadProgress(float progress)
    {
        loadingProgress.SetValue(progress);
    }

    private void OnKeysoundLoadComplete(string error)
    {
        if (error != null)
        {
            ReportFatalError(error);
            return;
        }

        keysoundsLoaded = true;
    }

    private void VideoPlayerErrorReceived(VideoPlayer player, string error)
    {
        videoPlayer.errorReceived -= VideoPlayerErrorReceived;
        ReportFatalError(error);  // VideoPlayer's error message includes URL
    }

    private void PrepareVideoPlayer()
    {
        RenderTexture renderTexture = new RenderTexture(
            (int)videoPlayer.width,
            (int)videoPlayer.height,
            depth: 0);
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.isLooping = GameSetup.pattern.patternMetadata
            .playBgaOnLoop;
        bga.texture = renderTexture;
        bga.color = Color.white;
        bga.GetComponent<AspectRatioFitter>().aspectRatio =
            (float)videoPlayer.width / videoPlayer.height;
        ShowBGACover();
    }
    #endregion

    #region Utilities
    // By default -3 / 2 = -1 because reasons. We want -2.
    // This assumes b is positive.
    private int RoundingDownIntDivision(int a, int b)
    {
        if (a % b == 0) return a / b;
        if (a >= 0) return a / b;
        return (a / b) - 1;
    }

    private static InputDevice DeviceForNote(Note n)
    {
        if (GameSetup.pattern.patternMetadata.controlScheme
            == ControlScheme.Touch)
        {
            return InputDevice.Touchscreen;
        }
        if (GameSetup.pattern.patternMetadata.controlScheme
            == ControlScheme.Keys)
        {
            return InputDevice.Keyboard;
        }
        switch (n.type)
        {
            case NoteType.Basic:
            case NoteType.ChainHead:
            case NoteType.ChainNode:
            case NoteType.Drag:
                return InputDevice.Mouse;
            default:
                return InputDevice.Keyboard;
        }
    }

    private static float LatencyForNote(Note n)
    {
        int latencyMs = Options.instance.GetLatencyForDevice(
            DeviceForNote(n));
        return autoPlay ? 0f : latencyMs * 0.001f;
    }

    private void ShowBGACover()
    {
        bgaCover.color = new Color(
            bgaCover.color.r,
            bgaCover.color.g,
            bgaCover.color.b,
            1f);
    }

    private void HideBGACover()
    {
        bgaCover.color = new Color(
            bgaCover.color.r,
            bgaCover.color.g,
            bgaCover.color.b,
            0f);
    }

    public Vector2 GetScreenPositionOnCurrentScanline(int lane)
    {
        if (!scanObjects.ContainsKey(Scan))
        {
            return new Vector2(-100f, -100f);
        }
        Scan s = scanObjects[Scan];
        float x = s.GetComponentInChildren<Scanline>()
            .transform.position.x;
        ScanBackground scanBackground =
            Modifiers.instance.GetScanPosition(Scan) switch
            {
                global::Scan.Position.Top => topScanBackground,
                global::Scan.Position.Bottom => bottomScanBackground,
                _ => null
            };
        float y = scanBackground.GetMiddleYOfLaneInWorldSpace(lane);
        Vector3 worldPosition = new Vector3(x, y, 0f);
        return RectTransformUtility.WorldToScreenPoint(null,
            worldPosition);
    }
    #endregion

    #region Update
    // Update is called once per frame
    void Update()
    {
        if (stopwatch == null)
        {
            // Game not started yet.
            return;
        }

        if (stageFailedScreen.activeSelf)
        {
            // Stage failed; ignore all input.
            return;
        }

        if (!IsPaused() && !loading && !inEditor)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnPauseButtonClickOrTouch();
            }
            if (Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Backspace) ||
                Input.GetKeyDown(KeyCode.Keypad0) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnFeverButtonPointerDown();
            }
        }

        UpdateTime();
        CheckForEndOfPattern();
        UpdateFever();
        HandleInput();
        UpdatePracticeMode();
        UpdateOngoingNotes();
        UpdateUI();
        UpdateBrightness();
    }

    private void UpdateTime()
    {
        float oldBaseTime = BaseTime;
        float oldTime = Time;
        BaseTime = (float)stopwatch.Elapsed.TotalSeconds * speed
            + initialTime;
        FloatPulse = GameSetup.pattern.TimeToPulse(Time);
        FloatBeat = FloatPulse / Pattern.pulsesPerBeat;
        FloatScan = FloatPulse / PulsesPerScan;
        int newPulse = Mathf.FloorToInt(FloatPulse);
        int newScan = Mathf.FloorToInt(FloatScan);

        // Play backing track if base time hits 0.
        if (oldBaseTime < 0f && BaseTime >= 0f &&
            backingTrackClip != null)
        {
            audioSourceManager.PlayBackingTrack(backingTrackClip,
                BaseTime);
        }

        // Play bga if base time hits bgaOffset.
        if (oldBaseTime <
            GameSetup.pattern.patternMetadata.bgaOffset &&
            BaseTime >=
            GameSetup.pattern.patternMetadata.bgaOffset &&
            GameSetup.pattern.patternMetadata.bga != null &&
            GameSetup.pattern.patternMetadata.bga != "")
        {
            HideBGACover();
            videoPlayer.Play();
        }

        // Fire scan events if applicable.
        if (RoundingDownIntDivision(
                Pulse + PulsesPerScan / 8, PulsesPerScan) !=
            RoundingDownIntDivision(
                newPulse + PulsesPerScan / 8, PulsesPerScan))
        {
            ScanAboutToChange?.Invoke(
                (newPulse + PulsesPerScan / 8) / PulsesPerScan);
        }
        bool jumpedScan = false;
        if (newScan > Scan)
        {
            ScanChanged?.Invoke(newScan);
            jumpedScan = ProcessScanChangeInPracticeMode(newScan);
        }
        if (!jumpedScan)
        {
            Pulse = newPulse;
            Scan = newScan;
        }

        // Handle combo ticks, if one occurred in this frame.
        while (previousComboTick + kComboTickInterval <=
            Pulse)
        {
            previousComboTick += kComboTickInterval;
            HandleComboTick();
        }

        // Handle upcoming notes.
        for (int laneIndex = 0;
            laneIndex < noteObjectsInLane.Count;
            laneIndex++)
        {
            NoteList lane = noteObjectsInLane[laneIndex];
            if (lane.Count == 0) continue;
            NoteObject upcomingNote = lane.First();

            if (laneIndex < playableLanes)
            {
                if (autoPlay)
                {
                    // Auto-play notes when it comes to their time.
                    if (Time >= upcomingNote.note.time &&
                        !ongoingNotes.ContainsKey(upcomingNote))
                    {
                        HitNote(upcomingNote, 0f);
                    }
                }
                else
                {
                    // Check for Break on upcoming notes in each
                    // playable lane.
                    if (Time > upcomingNote.note.time
                            + LatencyForNote(upcomingNote.note)
                            + upcomingNote.note.timeWindow[
                                Judgement.Miss] * speed
                        && !ongoingNotes.ContainsKey(upcomingNote))
                    {
                        ResolveNote(upcomingNote, Judgement.Break);
                    }
                }
            }
            else
            {
                // Play keyounds of upcoming notes in each
                // hidden lane, regardless of note type.
                if (BaseTime >= upcomingNote.note.time)
                {
                    PlayKeysound(upcomingNote, emptyHit: false);
                    upcomingNote.gameObject.SetActive(false);
                    lane.Remove(upcomingNote);
                }
            }
        }
    }

    private void CheckForEndOfPattern()
    {
        if (IsPaused()) return;
        if (BaseTime <= endOfPatternBaseTime) return;
        if (Modifiers.instance.mode == Modifiers.Mode.Practice) return;
        if (!score.AllNotesResolved()) return;

        if (feverState == FeverState.Active)
        {
            feverState = FeverState.Idle;
            score.FeverOff();
        }
        Curtain.DrawCurtainThenGoToScene("Result");
    }

    private void HandleInput()
    {
        if (IsPaused())
        {
            return;
        }
        if (autoPlay)
        {
            return;
        }

        // Input handling gets a bit complicated so here's a graph.
        //
        // Touch/KM                Keys/KM           Timer
        // --------                -------           -----
        // OnFingerDown/Move[0]    OnKeyDown[1]      UpdateTime[2]
        //     |                       |                 |
        // ProcessMouseOrFingerDown    |                 |
        //     |                       |                 |
        //     -------------------------                 |
        //                |                              |
        //             HitNote                           |
        //              |   |                            |
        //              |   ------------------------------
        //              |                 |
        //       RegisterOngoing       ResolveNote
        //
        // [0] mouse is considered finger #0; finger moving between
        // lanes will cause a new finger down event.
        // [1] takes a lane number in Keys, works on any lane in KM.
        // [2] the timer will resolve notes as Breaks when the
        // time window to play them has completely passed.
        //
        //
        //
        // Parallel to the above is the handling of ongoing notes.
        //
        // Touch/KM           Keys/KM         Update
        // --------           -------         ------
        // OnFingerHeld       OnKeyHeld[2]    UpdateOngoingNotes[1]
        //      |                 |                    |
        //      -------------------                    |
        //              |                              |
        //        HitOngoingNote[0]                    |
        //              |                              |
        //              --------------------------------
        //                              |
        //                         ResolveNote
        //
        // [0] marks the note as being hit on the current frame, or
        // resolves the note if its duration has passed.
        // [1] after all input is handled, any ongoing note not
        // marked on the current frame will be resolved as Misses.
        // [2] takes a lane number in Keys, works on any lane in KM.
        ControlScheme scheme = GameSetup.pattern.patternMetadata
            .controlScheme;
        switch (scheme)
        {
            case ControlScheme.Touch:
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    switch (t.phase)
                    {
                        case TouchPhase.Began:
                            OnFingerDown(t.fingerId, t.position);
                            break;
                        case TouchPhase.Moved:
                            OnFingerMove(t.fingerId, t.position);
                            OnFingerHeld(t.position);
                            break;
                        case TouchPhase.Stationary:
                            OnFingerHeld(t.position);
                            break;
                        case TouchPhase.Canceled:
                        case TouchPhase.Ended:
                            OnFingerUp(t.fingerId);
                            break;
                    }
                }
                break;
            case ControlScheme.Keys:
                for (int lane = 0; lane < playableLanes; lane++)
                {
                    foreach (KeyCode key in keysForLane[lane])
                    {
                        if (Input.GetKeyDown(key))
                        {
                            OnKeyDownOnLane(lane);
                        }
                        if (Input.GetKey(key))
                        {
                            OnKeyHeldOnLane(lane);
                        }
                    }
                }
                break;
            case ControlScheme.KM:
                if (Input.GetMouseButtonDown(0))
                {
                    OnFingerDown(0, Input.mousePosition);
                }
                if (Input.GetMouseButton(0))
                {
                    OnFingerMove(0, Input.mousePosition);
                    OnFingerHeld(Input.mousePosition);
                }
                if (Input.GetMouseButtonUp(0))
                {
                    OnFingerUp(0);
                }
                for (int lane = 0; lane < playableLanes; lane++)
                {
                    foreach (KeyCode key in keysForLane[lane])
                    {
                        if (Input.GetKeyDown(key))
                        {
                            OnKeyDownOnAnyLane();
                        }
                        if (Input.GetKey(key))
                        {
                            OnKeyHeldOnAnyLane();
                        }
                    }
                }
                break;
        }
    }

    private void HandleComboTick()
    {
        foreach (KeyValuePair<NoteObject, Judgement> pair in
            ongoingNotes)
        {
            SetCombo(currentCombo + 1);
            comboText.Show(pair.Key, pair.Value);
        }
    }

    private void UpdateOngoingNotes()
    {
        // This must happen after HandleInput.

        foreach (KeyValuePair<NoteObject, bool> pair in
            ongoingNoteIsHitOnThisFrame)
        {
            // Has the note's duration finished?
            float latency = LatencyForNote(pair.Key.note);
            float endTime = 0f;
            float gracePeriodLength = 0f;
            if (pair.Key.note is HoldNote)
            {
                HoldNote holdNote = pair.Key.note as HoldNote;
                endTime = holdNote.endTime + latency;
                gracePeriodLength = holdNote.gracePeriodLength;
            }
            else if (pair.Key.note is DragNote)
            {
                DragNote dragNote = pair.Key.note as DragNote;
                endTime = dragNote.endTime + latency;
                gracePeriodLength = dragNote.gracePeriodLength;
            }
            if (Time >= endTime)
            {
                // Resolve note.
                ResolveNote(pair.Key, ongoingNotes[pair.Key]);
                ongoingNotes.Remove(pair.Key);
                continue;
            }

            // Update time of last input.
            if (pair.Value == true)
            {
                // Will create new element if not existing.
                ongoingNoteLastInput[pair.Key] = Time;
            }
            else if (!autoPlay)
            {
                float lastInput = ongoingNoteLastInput[pair.Key];
                if (Time > lastInput + gracePeriodLength)
                {
                    // No input on this note for too long, resolve
                    // as MISS.
                    ResolveNote(pair.Key, Judgement.Miss);
                    StopKeysoundIfPlaying(pair.Key);
                    ongoingNotes.Remove(pair.Key);
                    ongoingNoteLastInput.Remove(pair.Key);
                }
            }
        }

        // Prepare for next frame.
        ongoingNoteIsHitOnThisFrame.Clear();
        foreach (KeyValuePair<NoteObject, Judgement> pair in
            ongoingNotes)
        {
            ongoingNoteIsHitOnThisFrame.Add(pair.Key, false);
        }
    }

    private void UpdateUI()
    {
        // Fever
        if (regularTopBar.activeSelf)
        {
            feverButtonFilling.anchorMax =
                new Vector2(feverAmount, 1f);
            feverButtonAnimator.SetBool("Fever Ready",
                feverState == FeverState.Ready);
            middleFeverBarFilling.anchorMin = new Vector2(
                0.5f - feverAmount * 0.5f, 0f);
            middleFeverBarFilling.anchorMax = new Vector2(
                0.5f + feverAmount * 0.5f, 1f);
            middleFeverText.SetActive(
                feverState == FeverState.Ready);
        }

        // Other
        hpBar.anchorMax = new Vector2(
            (float)hp / Ruleset.instance.maxHp, 1f);
        scoreText.text = score.CurrentScore().ToString();
        maxComboText.text = maxCombo.ToString();

        // Practice mode
        scanDisplay.text = $"{Scan} / {lastScan}";
        loopDisplay.text = $"{loopStart} - {loopEnd}";
        speedDisplay.text = $"{speed}x";
        autoPlayToggle.SetIsOnWithoutNotify(autoPlay);
        showHitboxToggle.SetIsOnWithoutNotify(hitboxVisible);
    }

    private void UpdateBrightness()
    {
        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            GameSetup.trackOptions.backgroundBrightness++;
        }
        if (Input.GetKeyDown(KeyCode.PageDown))
        {
            GameSetup.trackOptions.backgroundBrightness--;
        }
        GameSetup.trackOptions.backgroundBrightness =
            Mathf.Clamp(GameSetup.trackOptions.backgroundBrightness,
            0, 10);
        
        SetBrightness();
    }

    public void SetBrightness()
    {
        float coverAlpha = 1f - 
            0.1f * GameSetup.trackOptions.backgroundBrightness;
        brightnessCover.color = new Color(
            brightnessCover.color.r,
            brightnessCover.color.g,
            brightnessCover.color.b,
            coverAlpha);
    }
    #endregion

    #region Practice Mode
    private void UpdatePracticeMode()
    {
        if (Modifiers.instance.mode != Modifiers.Mode.Practice) return;
        if (Input.GetKeyDown(KeyCode.F3))
        {
            JumpToPreviousScan();
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            JumpToNextScan();
        }
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SetLoopStart();
        }    
        if (Input.GetKeyDown(KeyCode.F6))
        {
            SetLoopEnd();
        }
        if (Input.GetKeyDown(KeyCode.F7))
        {
            ResetLoop();
        }
        if (Input.GetKeyDown(KeyCode.F9))
        {
            DecreaseSpeed();
        }
        if (Input.GetKeyDown(KeyCode.F10))
        {
            IncreaseSpeed();
        }
        if (Input.GetKeyDown(KeyCode.F11))
        {
            ToggleAutoPlay();
        }
        if (Input.GetKeyDown(KeyCode.F12))
        {
            ToggleHitboxVisibility();
        }
    }

    private void ResetInitialTime()
    {
        initialTime = BaseTime -
            (float)stopwatch.Elapsed.TotalSeconds * speed;
    }

    private void JumpToScan(int scan)
    {
        // Clamp scan into bounds.
        scan = Mathf.Clamp(scan, firstScan, lastScan);

        // Set timer.
        Scan = scan;
        FloatBeat = Scan * GameSetup.pattern.patternMetadata.bps;
        FloatScan = Scan;
        Pulse = PulsesPerScan * Scan;
        FloatPulse = Pulse;
        BaseTime = GameSetup.pattern.PulseToTime(Pulse);
        ResetInitialTime();
        previousComboTick = Pulse;

        // Rebuild data structures.
        foreach (NoteList l in noteObjectsInLane)
        {
            l.Reset();
            l.RemoveUpTo(Pulse);
        }
        foreach (NoteList l in notesForKeyboardInLane)
        {
            l.Reset();
            l.RemoveUpTo(Pulse);
        }
        foreach (NoteList l in notesForMouseInLane)
        {
            l.Reset();
            l.RemoveUpTo(Pulse);
        }
        ongoingNotes.Clear();
        ongoingNoteIsHitOnThisFrame.Clear();

        // Fire scan events.
        JumpedToScan?.Invoke(Scan);

        // Start/stop backing track and BGA.
        audioSourceManager.StopAll();
        if (BaseTime >= 0f && backingTrackClip != null)
        {
            audioSourceManager.PlayBackingTrack(backingTrackClip,
                BaseTime);
        }
        if (GameSetup.pattern.patternMetadata.bga != null
            && GameSetup.pattern.patternMetadata.bga != ""
            && !GameSetup.trackOptions.noVideo)
        {
            if (BaseTime >= GameSetup.pattern.patternMetadata
                .bgaOffset)
            {
                double videoTime = BaseTime -
                    GameSetup.pattern.patternMetadata.bgaOffset;
                videoPlayer.time = videoTime;
                videoPlayer.Play();
                HideBGACover();
            }
            else
            {
                videoPlayer.time = 0;
                videoPlayer.Pause();
                ShowBGACover();
            }
        }

        // Play keysounds before this moment if they last enough.
        // Only go through the notes removed earlier; unremoved
        // notes will be handled by gameplay or UpdateTime.
        foreach (NoteList l in noteObjectsInLane)
        {
            l.ForEachRemoved((NoteObject noteObject) =>
            {
                Note n = noteObject.note;
                if (n.sound == null || n.sound == "") return;

                AudioClip clip = ResourceLoader.GetCachedClip(
                    n.sound);
                if (clip == null) return;
                if (n.time + clip.length > BaseTime)
                {
                    audioSourceManager.PlayKeysound(clip,
                        GameSetup.pattern.IsHiddenNote(n.lane),
                        startTime: BaseTime - n.time,
                        n.volumePercent, n.panPercent);
                }
            });
        }

        // Scoring.
        currentCombo = 0;

        // VFX.
        vfxSpawner.RemoveAll();
    }

    public void JumpToPreviousScan()
    {
        float floatScan = FloatPulse / PulsesPerScan;
        floatScan -= Mathf.Floor(floatScan);
        if (floatScan > 0.25f)
        {
            JumpToScan(Scan);
        }
        else
        {
            JumpToScan(Scan - 1);
        }
    }

    public void JumpToNextScan()
    {
        JumpToScan(Scan + 1);
    }

    public void SetLoopStart()
    {
        loopStart = Scan;
        if (loopEnd < loopStart) loopEnd = loopStart;
    }

    public void SetLoopEnd()
    {
        loopEnd = Scan;
        if (loopStart > loopEnd) loopStart = loopEnd;
    }

    public void ResetLoop()
    {
        loopStart = firstScan;
        loopEnd = lastScan;
    }

    // Returns whether the scan was changed in this method.
    private bool ProcessScanChangeInPracticeMode(int newScan)
    {
        if (Modifiers.instance.mode != Modifiers.Mode.Practice)
        {
            return false;
        }
        if (newScan > loopEnd)
        {
            JumpToScan(loopStart);
            return true;
        }
        if (newScan > lastScan)
        {
            JumpToScan(firstScan);
            return true;
        }
        return false;
    }

    private void SetSpeed(int newSpeedPercent)
    {
        newSpeedPercent = Mathf.Clamp(newSpeedPercent, 50, 200);
        if (speedPercentage == newSpeedPercent) return;

        speedPercentage = newSpeedPercent;
        ResetInitialTime();
        audioSourceManager.SetSpeed(speed);
        videoPlayer.playbackSpeed = speed;
    }

    public void DecreaseSpeed()
    {
        SetSpeed(speedPercentage - 5);
    }

    public void IncreaseSpeed()
    {
        SetSpeed(speedPercentage + 5);
    }

    public void ToggleAutoPlay()
    {
        autoPlay = !autoPlay;
    }

    public void ToggleHitboxVisibility()
    {
        hitboxVisible = !hitboxVisible;
    }
    #endregion

    #region Fever
    public void OnFeverButtonPointerDown()
    {
        if (feverState != FeverState.Ready) return;
        if (autoPlay) return;
        if (Modifiers.instance.fever == Modifiers.Fever.AutoFever)
        {
            return;
        }
        ActivateFever();
    }

    private void ActivateFever()
    {
        feverState = FeverState.Active;
        score.FeverOn();
        feverSoundSource.Play();
        feverTimer = new Stopwatch();
        // Technically, keyboard-induced activations start
        // later in the frame (during Update()) than
        // pointer-down-induced ones (during event handling), so
        // the latter may last a few more milliseconds.
        //
        // This shouldn't cause any problems though?
        feverTimer.Start();
    }

    private void UpdateFever()
    {
        if (feverState != FeverState.Active) return;
        feverAmount = 1f -
            (float)feverTimer.Elapsed.TotalSeconds * 0.1f;
        if (feverAmount < 0f)
        {
            feverAmount = 0f;
            feverTimer.Stop();
            feverTimer = null;
            feverState = FeverState.Idle;
            int feverBonus = score.FeverOff();
            feverBonusText.text = Locale.GetStringAndFormat(
                "game_fever_bonus_text", feverBonus);
            feverBonusAnimator.SetTrigger("Show");
        }
    }
    #endregion

    #region Mouse/Finger Tracking
    // In the context of mouse/finger tracking, the mouse
    // cursor is treated as finger #0.
    private Dictionary<int, int> fingerInLane;
    private const int kOutsideAllLanes = -1;

    private void OnFingerDown(int finger, Vector2 screenPosition)
    {
        List<RaycastResult> results = Raycast(screenPosition);
        int lane = RaycastResultToLane(results);
        if (fingerInLane.ContainsKey(finger))
        {
            // "Key already exists" error may occur around pausing.
            fingerInLane[finger] = lane;
        }
        else
        {
            fingerInLane.Add(finger, lane);
        }
        ProcessMouseOrFingerDown(results);
    }

    private void OnFingerMove(int finger, Vector2 screenPosition)
    {
        if (!fingerInLane.ContainsKey(finger))
        {
            OnFingerDown(finger, screenPosition);
            return;
        }
        List<RaycastResult> results = Raycast(screenPosition);
        int lane = RaycastResultToLane(results);
        if (fingerInLane[finger] != lane)
        {
            // Finger moved to new lane. Treat as a down event.
            ProcessMouseOrFingerDown(results);
            fingerInLane[finger] = lane;
        }
    }

    private void OnFingerUp(int finger)
    {
        fingerInLane.Remove(finger);
    }

    private List<RaycastResult> Raycast(Vector2 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(
            EventSystem.current);
        eventData.position = screenPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(eventData, results);

        return results;
    }

    private void ProcessMouseOrFingerDown(
        List<RaycastResult> results)
    {
        // This event may hit any number of the following things:
        // - The hit box of a not-ongoing note (aka. a newly hit note)
        // - The hit box of an ongoing note
        // - An empty touch receiver
        //
        // Ultimately the event can only go towards 1 thing, so they
        // are prioritized in the order listed.

        bool hitOngoingNote = false;
        EmptyTouchReceiver hitEmptyReceiver = null;

        foreach (RaycastResult r in results)
        {
            NoteHitbox touchReceiver = r.gameObject
                .GetComponent<NoteHitbox>();
            if (touchReceiver != null)
            {
                NoteObject n = touchReceiver
                    .GetComponentInParent<NoteObject>();
                NoteObject noteToCheck = n;
                if (n.note.type == NoteType.RepeatHead ||
                    n.note.type == NoteType.RepeatHeadHold)
                {
                    noteToCheck = n
                        .GetComponent<RepeatHeadAppearanceBase>()
                        .GetFirstUnresolvedRepeatNote();
                }

                if (ongoingNotes.ContainsKey(noteToCheck))
                {
                    hitOngoingNote = true;
                    // No need to check for empty touch
                    // receiver because they are lower priority.
                    continue;
                }

                bool ignoreNewlyHitNote = false;
                if (GameSetup.pattern.patternMetadata.controlScheme
                    == ControlScheme.KM)
                {
                    if (n.note.type == NoteType.Hold ||
                        n.note.type == NoteType.RepeatHead ||
                        n.note.type == NoteType.RepeatHeadHold ||
                        n.note.type == NoteType.Repeat ||
                        n.note.type == NoteType.RepeatHold)
                    {
                        // The mouse does not care about this note.
                        ignoreNewlyHitNote = true;
                    }
                }
                
                float correctTime = noteToCheck.note.time
                    + LatencyForNote(noteToCheck.note);
                float difference = Time - correctTime;
                if (Mathf.Abs(difference) > 
                    n.note.timeWindow[Judgement.Miss])
                {
                    // The touch or click is too early or too late
                    // for this note. Ignore.
                    ignoreNewlyHitNote = true;
                }

                if (!ignoreNewlyHitNote)
                {
                    // The touch or click lands on this note. Since
                    // these are most prioritized, we can ignore
                    // everything else.
                    HitNote(noteToCheck, difference);
                    break;
                }
            }

            EmptyTouchReceiver emptyReceiver = r.gameObject
                .GetComponent<EmptyTouchReceiver>();
            if (emptyReceiver != null)
            {
                hitEmptyReceiver = emptyReceiver;
            }
        }

        // If control reaches here, we did not hit any new note. Process
        // lower priority targets.
        if (hitOngoingNote)
        {
            // Do nothing.
            return;
        }
        if (hitEmptyReceiver != null)
        {
            EmptyHit(hitEmptyReceiver.lane);
        }
    }

    private int RaycastResultToLane(List<RaycastResult> results)
    {
        foreach (RaycastResult r in results)
        {
            EmptyTouchReceiver receiver = r.gameObject
                .GetComponent<EmptyTouchReceiver>();
            if (receiver != null)
            {
                return receiver.lane;
            }
        }

        return kOutsideAllLanes;
    }

    private void OnFingerHeld(Vector2 screenPosition)
    {
        List<RaycastResult> results = Raycast(screenPosition);
        foreach (RaycastResult r in results)
        {
            NoteObject n = r.gameObject
                .GetComponentInParent<NoteObject>();
            if (n == null) continue;
            NoteObject noteToCheck = n;
            if (n.note.type == NoteType.RepeatHead ||
                n.note.type == NoteType.RepeatHeadHold)
            {
                noteToCheck = n
                    .GetComponent<RepeatHeadAppearanceBase>()
                    .GetFirstUnresolvedRepeatNote();
            }
            if (ongoingNoteIsHitOnThisFrame.ContainsKey(noteToCheck))
            {
                ongoingNoteIsHitOnThisFrame[noteToCheck] = true;
            }
        }
    }
    #endregion

    #region Keyboard
    private void OnKeyDownOnLane(int lane)
    {
        // Keys only.

        if (noteObjectsInLane[lane].Count == 0)
        {
            // Do nothing.
            return;
        }
        NoteObject earliestNote = noteObjectsInLane[lane].First();
        if (ongoingNotes.ContainsKey(earliestNote))
        {
            // Do nothing.
            return;
        }
        float correctTime = earliestNote.note.time
            + LatencyForNote(earliestNote.note);
        float difference = Time - correctTime;
        if (Mathf.Abs(difference) >
            earliestNote.note.timeWindow[Judgement.Miss])
        {
            // The keystroke is too early or too late
            // for this note. Ignore.
            PlayKeysound(earliestNote, emptyHit: true);
        }
        else
        {
            // The keystroke lands on this note.
            HitNote(earliestNote, difference);
        }
    }

    private void OnKeyDownOnAnyLane()
    {
        // KM only.

        List<NoteObject> earliestNotes = new List<NoteObject>();
        int earliestPulse = int.MaxValue;
        for (int i = 0; i < playableLanes; i++)
        {
            if (notesForKeyboardInLane[i].Count == 0)
            {
                continue;
            }
            NoteObject n = notesForKeyboardInLane[i].First();
            if (ongoingNotes.ContainsKey(n))
            {
                continue;
            }
            if (n.note.pulse < earliestPulse)
            {
                earliestNotes.Clear();
                earliestNotes.Add(n);
                earliestPulse = n.note.pulse;
            }
            else if (n.note.pulse == earliestPulse)
            {
                earliestNotes.Add(n);
            }
        }
        if (earliestNotes.Count == 0)
        {
            // Do nothing.
            return;
        }

        NoteObject earliestNote = null;
        if (earliestNotes.Count == 1)
        {
            earliestNote = earliestNotes[0];
        }
        else
        {
            // Pick the first note that has no duration, if any.
            foreach (NoteObject n in earliestNotes)
            {
                if (n.note.type == NoteType.RepeatHead ||
                    n.note.type == NoteType.Repeat)
                {
                    earliestNote = n;
                    break;
                }
            }
            if (earliestNote == null)
            {
                earliestNote = earliestNotes[0];
            }
        }

        float correctTime = earliestNote.note.time
            + LatencyForNote(earliestNote.note);
        float difference = Time - correctTime;
        if (Mathf.Abs(difference) >
            earliestNote.note.timeWindow[Judgement.Miss])
        {
            // The keystroke is too early or too late
            // for this note. Ignore.
            PlayKeysound(earliestNote, emptyHit: true);
        }
        else
        {
            // The keystroke lands on this note.
            HitNote(earliestNote, difference);
        }
    }

    private void OnKeyHeldOnLane(int lane)
    {
        // Keys only.

        NoteObject noteToMark = null;
        foreach (KeyValuePair<NoteObject, bool> pair in
            ongoingNoteIsHitOnThisFrame)
        {
            if (pair.Value == true) continue;
            if (pair.Key.note.lane != lane) continue;
            noteToMark = pair.Key;
            break;
        }
        if (noteToMark != null)
        {
            ongoingNoteIsHitOnThisFrame[noteToMark] = true;
        }
    }

    private void OnKeyHeldOnAnyLane()
    {
        // KM only.

        NoteObject noteToMark = null;
        foreach (KeyValuePair<NoteObject, bool> pair in
            ongoingNoteIsHitOnThisFrame)
        {
            if (pair.Value == true) continue;
            noteToMark = pair.Key;
            break;
        }
        if (noteToMark != null)
        {
            ongoingNoteIsHitOnThisFrame[noteToMark] = true;
        }
    }
    #endregion

    #region Hitting Notes And Empty Hits
    private void HitNote(NoteObject n, float timeDifference)
    {
        // All code paths into this method should have ignored
        // ongoing notes.

        Judgement judgement = Judgement.Miss;
        float absDifference = Mathf.Abs(timeDifference);
        // Compensate for speed.
        absDifference /= speed;

        foreach (Judgement j in new List<Judgement>{
            Judgement.RainbowMax,
            Judgement.Max,
            Judgement.Cool,
            Judgement.Good,
            Judgement.Miss
        })
        {
            if (absDifference <= n.note.timeWindow[j])
            {
                judgement = j;
                break;
            }
        }

        vfxSpawner.SpawnVFXOnHit(n, judgement);

        switch (n.note.type)
        {
            case NoteType.Hold:
            case NoteType.Drag:
            case NoteType.RepeatHeadHold:
            case NoteType.RepeatHold:
                if (judgement == Judgement.Miss)
                {
                    // Missed notes do not enter Ongoing state.
                    ResolveNote(n, judgement);
                }
                else
                {
                    // Register an ongoing note.
                    ongoingNotes.Add(n, judgement);
                    ongoingNoteIsHitOnThisFrame.Add(n, true);
                    n.GetComponent<NoteAppearance>().SetOngoing();
                }
                break;
            default:
                ResolveNote(n, judgement);
                break;
        }

        PlayKeysound(n, emptyHit: false);
    }

    private void EmptyHit(int lane)
    {
        NoteObject upcomingNote = null;
        switch (GameSetup.pattern.patternMetadata.controlScheme)
        {
            case ControlScheme.Touch:
                if (noteObjectsInLane[lane].Count > 0)
                {
                    upcomingNote = noteObjectsInLane[lane].First();
                }
                break;
            case ControlScheme.KM:
                if (notesForMouseInLane[lane].Count > 0)
                {
                    upcomingNote = notesForMouseInLane[lane].First();
                }
                break;
            case ControlScheme.Keys:
                // Keys should not call this method.
                break;
        }

        if (upcomingNote != null
            && !ongoingNotes.ContainsKey(upcomingNote))
        {
            PlayKeysound(upcomingNote, emptyHit: true);
        }
    }

    private void ResolveNote(NoteObject n, Judgement judgement)
    {
        // Remove note from linked lists.
        noteObjectsInLane[n.note.lane].Remove(n);
        switch (n.note.type)
        {
            case NoteType.Basic:
            case NoteType.ChainHead:
            case NoteType.ChainNode:
            case NoteType.Drag:
                notesForMouseInLane[n.note.lane].Remove(n);
                break;
            case NoteType.Hold:
            case NoteType.RepeatHead:
            case NoteType.RepeatHeadHold:
            case NoteType.Repeat:
            case NoteType.RepeatHold:
                notesForKeyboardInLane[n.note.lane].Remove(n);
                break;
        }

        // Score, combo and fever.
        if (judgement != Judgement.Miss &&
            judgement != Judgement.Break)
        {
            SetCombo(currentCombo + 1);

            if (Modifiers.instance.mode != Modifiers.Mode.Practice)
            {
                hp += Ruleset.instance.GetHpDelta(
                    judgement, n.note.type,
                    feverState == FeverState.Active);
                if (hp >= Ruleset.instance.maxHp)
                {
                    hp = Ruleset.instance.maxHp;
                }

                if (feverState == FeverState.Idle &&
                    (judgement == Judgement.RainbowMax ||
                     judgement == Judgement.Max))
                {
                    feverAmount += feverCoefficient / numPlayableNotes;
                    if (autoPlay ||
                        Modifiers.instance.fever
                        == Modifiers.Fever.FeverOff)
                    {
                        feverAmount = 0f;
                    }
                    if (feverAmount >= 1f)
                    {
                        feverState = FeverState.Ready;
                        feverAmount = 1f;
                        if (Modifiers.instance.fever
                            == Modifiers.Fever.AutoFever)
                        {
                            ActivateFever();
                        }
                    }
                }
            }
        }
        else
        {
            SetCombo(0);

            if (Modifiers.instance.mode != Modifiers.Mode.Practice)
            {
                hp += Ruleset.instance.GetHpDelta(
                    judgement, n.note.type,
                    feverState == FeverState.Active);
                if (hp < 0) hp = 0;
                if (hp <= 0 &&
                    Modifiers.instance.mode !=
                    Modifiers.Mode.NoFail)
                {
                    // Stage failed.
                    score.stageFailed = true;
                    stopwatch.Stop();
                    audioSourceManager.StopAll();
                    StartCoroutine(StageFailedSequence());
                }

                if (feverState == FeverState.Idle ||
                    feverState == FeverState.Ready)
                {
                    if (judgement == Judgement.Miss)
                    {
                        feverAmount *= 0.75f;
                    }
                    else  // Break
                    {
                        feverAmount *= 0.5f;
                    }
                    feverState = FeverState.Idle;
                }
            }
        }
        if (Modifiers.instance.mode != Modifiers.Mode.Practice)
        {
            score.LogNote(judgement);
            judgementTally.Refresh(score);
        }

        // Appearances and VFX.
        vfxSpawner.SpawnVFXOnResolve(n, judgement);
        n.GetComponent<NoteAppearance>().Resolve();
        // Call this after updating combo to show the correct
        // combo on judgement text.
        comboText.Show(n, judgement);
    }

    private void SetCombo(int combo)
    {
        currentCombo = combo;
        if (currentCombo > maxCombo)
        {
            maxCombo = currentCombo;
        }
    }

    private IEnumerator StageFailedSequence()
    {
        stageFailedScreen.SetActive(true);
        yield return new WaitForSeconds(4f);

        Curtain.DrawCurtainThenGoToScene("Result");
    }
    #endregion

    #region Keysound
    // Records which AudioSource is playing the keysounds of which
    // note, so they can be stopped later. This is meant for long
    // notes, and do not care about assist ticks.
    private Dictionary<Note, AudioSource> noteToAudioSource;
    private void PlayKeysound(NoteObject n, bool emptyHit)
    {
        if (emptyHit
            && ((IList) new NoteType[]{
                NoteType.Hold,
                NoteType.Drag,
                NoteType.RepeatHeadHold,
                NoteType.RepeatHold
            }).Contains(n.note.type))
        {
            return;
        }

        bool hidden = GameSetup.pattern.IsHiddenNote(n.note.lane);
        if (Modifiers.instance.assistTick == 
            Modifiers.AssistTick.AssistTick
            && !hidden
            && !emptyHit)
        {
            audioSourceManager.PlaySfx(assistTick);
        }

        if (n.note is AssistTickNote)
        {
            audioSourceManager.PlaySfx(assistTick);
        }

        if (n.note.sound == null || n.note.sound == "") return;

        AudioClip clip = ResourceLoader.GetCachedClip(n.note.sound);
        AudioSource source = audioSourceManager.PlayKeysound(clip,
            hidden,
            startTime: 0f,
            n.note.volumePercent, n.note.panPercent);
        noteToAudioSource[n.note] = source;
    }

    private void StopKeysoundIfPlaying(NoteObject n)
    {
        if (n.note.sound == "") return;

        AudioClip clip = ResourceLoader.GetCachedClip(n.note.sound);
        if (noteToAudioSource[n.note].clip == clip)
        {
            noteToAudioSource[n.note].Stop();
            noteToAudioSource.Remove(n.note);
        }
    }
    #endregion

    #region Pausing
    public bool IsPaused()
    {
        if (pauseDialog == null) return false;
        return pauseDialog.gameObject.activeSelf;
    }

    public void OnPauseButtonClickOrTouch(bool playSound = true)
    {
        stopwatch.Stop();
        feverTimer?.Stop();
        audioSourceManager.PauseAll();
        if (videoPlayer.isPlaying) videoPlayer.Pause();
        pauseDialog.Show(closeCallback: () =>
        {
            stopwatch.Start();
            feverTimer?.Start();
            audioSourceManager.UnpauseAll();
            if (videoPlayer.isPaused) videoPlayer.Play();
        });
        if (playSound)
        {
            MenuSfx.instance.PlayPauseSound();
        }
    }
    #endregion
}
