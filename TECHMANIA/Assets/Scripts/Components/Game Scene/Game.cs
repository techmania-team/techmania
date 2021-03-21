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
    [Header("Background")]
    public Image backgroundImage;
    public VideoPlayer videoPlayer;
    public RawImage bga;
    public Image brightnessCover;

    [Header("Scans")]
    public GraphicRaycaster raycaster;
    public Transform topScanContainer;
    public GameObject topScanTemplate;
    public List<RectTransform> topScanEmptyTouchReceivers;
    public Transform bottomScanContainer;
    public GameObject bottomScanTemplate;
    public List<RectTransform> bottomScanEmptyTouchReceivers;

    [Header("Audio")]
    public AudioSourceManager audioSourceManager;

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

    [Header("VFX")]
    public VFXSpawner vfxSpawner;
    public JudgementText judgementText;

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

    [Header("UI - Other")]
    public GameObject topBar;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI maxComboText;
    public RectTransform hpBar;
    public GameObject noFailIndicator;
    public GameObject loadingBar;
    public MaterialProgressBar loadingProgress;
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

    private const int kPlayableLanes = 4;
    private const int kComboTickInterval = 60;
    // Combo ticks are pulses where each ongoing note increases
    // combo by 1. Ongoing notes add 1 more combo when
    // resolved. Combo ticks are, by default, 60 pulses apart.
    private int previousComboTick;

    private bool loading;
    private bool hitboxVisible;
    
    #region Timers
    // The stopwatch provides the "base time", which drives
    // the backing track, BGA, hidden notes and auto-played notes.
    private Stopwatch stopwatch;
    private static float BaseTime { get; set; }

    private static float offset;
    // The public timer is compensated for offset, to be used for
    // scanlines and notes. All public timers are based on this time.
    public static float Time => BaseTime - offset;
    public static int PulsesPerScan { get; private set; }
    public static float FloatPulse { get; private set; }
    private static int Pulse { get; set; }
    public static int Scan { get; private set; }
    private int lastScan;
    private Stopwatch feverTimer;
    private float initialTime;
    #endregion

    public static event UnityAction<int> ScanChanged;
    public static event UnityAction<int> ScanAboutToChange;
    public static event UnityAction<bool> HitboxVisibilityChanged;

    private static List<List<KeyCode>> keysForLane;

    // Each linked list represents one lane; each lane is
    // sorted by pulse. Resolved notes are removed
    // from the corresponding linked list. This makes it easy to
    // find the upcoming note in each lane, and in turn:
    // - play keysounds on empty hits
    // - check the Break condition on upcoming notes
    private List<LinkedList<NoteObject>> noteObjectsInLane;
    // noteObjectsInLane separated into mouse and keyboard notes.
    // In KM, Each input device only care about notes in its
    // corresponding list.
    private List<LinkedList<NoteObject>> notesForMouseInLane;
    private List<LinkedList<NoteObject>> notesForKeyboardInLane;
    private int numPlayableNotes;

    // Value is the judgement at note's head.
    private Dictionary<NoteObject, Judgement> ongoingNotes;
    private Dictionary<NoteObject, bool> ongoingNoteIsHitOnThisFrame;

    // Start is called before the first frame update
    void Start()
    {
        if (GameSetup.track == null)
        {
            SceneManager.LoadScene("Main Menu");
        }
        Input.simulateMouseWithTouches = false;
        score = new Score();
        loading = true;
        topBar.SetActive(false);
        middleFeverBar.SetActive(false);
        loadingBar.SetActive(true);
        loadingProgress.SetValue(0f);
        stopwatch = null;

        // Load options.
        Options.RefreshInstance();
        Ruleset.RefreshInstance();

        // Start the load sequence.
        StartCoroutine(LoadSequence());
    }

    private void OnDestroy()
    {
        Input.simulateMouseWithTouches = true;
    }

    #region Initialization
    private void ReportFatalError(string message)
    {
        messageDialog.Show(message, closeCallback: () =>
        {
            MainMenuPanel.skipToTrackSelect = true;
            Curtain.DrawCurtainThenGoToScene("Main Menu");
        });
    }

    private bool backgroundImageLoaded;
    private AudioClip backingTrackClip;
    private bool backingTrackLoaded;
    private bool keysoundsLoaded;
    private IEnumerator LoadSequence()
    {
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
        else
        {
            backgroundImage.color = Color.clear;
        }

        // Step 2: load backing track, if any. This allows calculating
        // the number of scans.
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

        // Step 3: load keysounds, if any.
        keysoundsLoaded = false;
        ResourceLoader.CacheAllKeysounds(GameSetup.trackFolder,
            GameSetup.pattern,
            OnKeysoundLoadComplete,
            OnKeysoundLoadProgress);
        yield return new WaitUntil(() => keysoundsLoaded);

        // Step 4: load BGA, if any.
        bool hasBga;
        if (GameSetup.pattern.patternMetadata.bga != null &&
            GameSetup.pattern.patternMetadata.bga != "")
        {
            hasBga = true;
            string fullPath = Path.Combine(GameSetup.trackFolder,
                GameSetup.pattern.patternMetadata.bga);
            videoPlayer.url = fullPath;
            videoPlayer.errorReceived += VideoPlayerErrorReceived;
            videoPlayer.Prepare();
            yield return new WaitUntil(() => videoPlayer.isPrepared);
            videoPlayer.errorReceived -= VideoPlayerErrorReceived;
            PrepareVideoPlayer();
        }
        else
        {
            hasBga = false;
            bga.color = Color.clear;
        }

        // Step 5: initialize pattern. This sadly cannot be done
        // asynchronously.
        InitializePattern();

        // Loading complete.
        loading = false;
        topBar.SetActive(true);
        noFailIndicator.SetActive(GameSetup.noFail);
        middleFeverBar.SetActive(true);
        loadingBar.SetActive(false);
        if (hasBga)
        {
            backgroundImage.color = Color.clear;
        }

        yield return null;  // Wait 1 more frame just in case.

        // Start timer. Backing track will start when timer hits 0;
        // BGA will start when timer hits bgaOffset.
        stopwatch = new Stopwatch();
        stopwatch.Start();
        int offsetMs = GameSetup.pattern.patternMetadata.controlScheme
            == ControlScheme.Touch ?
            Options.instance.touchOffsetMs :
            Options.instance.keyboardMouseOffsetMs;
        offset = offsetMs * 0.001f;
        BaseTime = initialTime;
    }

    private void OnImageLoadComplete(Texture2D texture, string error)
    {
        if (error != null)
        {
            backgroundImage.color = Color.clear;
            ReportFatalError(error);
            return;
        }

        backgroundImage.sprite =
            ResourceLoader.CreateSpriteFromTexture(texture);
        backgroundImage.color = Color.white;
        backgroundImage.GetComponent<AspectRatioFitter>()
            .aspectRatio =
            texture.width / texture.height;
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
        GameSetup.pattern.CalculateTimeOfAllNotes();
        initialTime = (float)GameSetup.pattern.patternMetadata
            .firstBeatOffset;
        Pulse = 0;
        Scan = 0;
        previousComboTick = 0;

        // Rewind till 1 scan before the backing track starts.
        PulsesPerScan = Pattern.pulsesPerBeat *
            GameSetup.pattern.patternMetadata.bps;
        while (initialTime >= 0f)
        {
            Scan--;
            Pulse -= PulsesPerScan;
            initialTime = GameSetup.pattern.PulseToTime(Pulse);
        }

        // Resize empty touch receivers to fit scan margins.
        float laneHeightRelative =
            (1f - Ruleset.instance.scanMargin * 2f) * 0.25f;
        topScanEmptyTouchReceivers[0].anchorMin = new Vector2(
            0f, 0.5f + laneHeightRelative);
        topScanEmptyTouchReceivers[0].anchorMax = new Vector2(
            1f, 1f);
        topScanEmptyTouchReceivers[1].anchorMin = new Vector2(
            0f, 0.5f);
        topScanEmptyTouchReceivers[1].anchorMax = new Vector2(
            1f, 0.5f + laneHeightRelative);
        topScanEmptyTouchReceivers[2].anchorMin = new Vector2(
            0f, 0.5f - laneHeightRelative);
        topScanEmptyTouchReceivers[2].anchorMax = new Vector2(
            1f, 0.5f);
        topScanEmptyTouchReceivers[3].anchorMin = new Vector2(
            0f, 0f);
        topScanEmptyTouchReceivers[3].anchorMax = new Vector2(
            1f, 0.5f - laneHeightRelative);
        for (int i = 0; i < 4; i++)
        {
            bottomScanEmptyTouchReceivers[i].anchorMin =
                topScanEmptyTouchReceivers[i].anchorMin;
            bottomScanEmptyTouchReceivers[i].anchorMax =
                topScanEmptyTouchReceivers[i].anchorMax;
        }

        // Find last scan. Make sure it ends later than the backing
        // track and BGA, so we don't cut either short.
        CalculateLastScan();

        // Create scan objects.
        Dictionary<int, Scan> scanObjects =
            new Dictionary<int, Scan>();
        for (int i = Scan; i <= lastScan; i++)
        {
            Transform parent = (i % 2 == 0) ?
                bottomScanContainer : topScanContainer;
            GameObject template = (i % 2 == 0) ?
                bottomScanTemplate : topScanTemplate;
            GameObject scanObject = Instantiate(template, parent);
            scanObject.SetActive(true);

            Scan s = scanObject.GetComponent<Scan>();
            s.scanNumber = i;
            s.Initialize();
            scanObjects.Add(i, s);
        }

        // Create note objects. In reverse order, so earlier notes
        // are drawn on the top.
        // Also organize them as linked lists, so empty hits can
        // play the keysound of upcoming notes.
        noteObjectsInLane = new List<LinkedList<NoteObject>>();
        notesForMouseInLane = new List<LinkedList<NoteObject>>();
        notesForKeyboardInLane = new List<LinkedList<NoteObject>>();
        numPlayableNotes = 0;
        NoteObject nextChainNode = null;
        List<List<NoteObject>> unmanagedRepeatNotes =
            new List<List<NoteObject>>();
        for (int i = 0; i < kPlayableLanes; i++)
        {
            noteObjectsInLane.Add(new LinkedList<NoteObject>());
            notesForMouseInLane.Add(
                new LinkedList<NoteObject>());
            notesForKeyboardInLane.Add(
                new LinkedList<NoteObject>());
            unmanagedRepeatNotes.Add(new List<NoteObject>());
        }
        foreach (Note n in GameSetup.pattern.notes.Reverse())
        {
            int scanOfN = n.GetScanNumber(
                GameSetup.pattern.patternMetadata.bps);
            bool hidden = n.lane >= kPlayableLanes;
            if (!hidden) numPlayableNotes++;

            NoteObject noteObject = SpawnNoteObject(
                n, scanObjects[scanOfN], hidden);
            NoteAppearance appearance = noteObject
                .GetComponent<NoteAppearance>();

            while (noteObjectsInLane.Count <= n.lane)
            {
                noteObjectsInLane.Add(new LinkedList<NoteObject>());
                notesForMouseInLane.Add(
                    new LinkedList<NoteObject>());
                notesForKeyboardInLane.Add(
                    new LinkedList<NoteObject>());
                unmanagedRepeatNotes.Add(new List<NoteObject>());
            }
            noteObjectsInLane[n.lane].AddFirst(noteObject);
            switch (n.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.Drag:
                    notesForMouseInLane[n.lane]
                        .AddFirst(noteObject);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHead:
                case NoteType.RepeatHeadHold:
                case NoteType.Repeat:
                case NoteType.RepeatHold:
                    notesForKeyboardInLane[n.lane]
                        .AddFirst(noteObject);
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
                appearance.SetNextChainNode(
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

            // Tell drag notes about their input latency so they can
            // move their hitboxes accordingly.
            if (n.type == NoteType.Drag)
            {
                appearance.SetInputLatency(LatencyForNote(n));
            }
        }

        // Broadcast the initial hitbox visibility.
        hitboxVisible = false;
        HitboxVisibilityChanged?.Invoke(hitboxVisible);

        // Ensure that a ScanChanged event is fired at the first update.
        Scan--;

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

        // Miscellaneous initialization.
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
                feverInstruction.text = "TOUCH";
                break;
            case ControlScheme.Keys:
            case ControlScheme.KM:
                feverInstruction.text = "PRESS SPACE";
                break;
        }
        ongoingNotes = new Dictionary<NoteObject, Judgement>();
        ongoingNoteIsHitOnThisFrame =
            new Dictionary<NoteObject, bool>();
        noteToAudioSource = new Dictionary<Note, AudioSource>();
    }

    #region Subroutines of InitializePattern
    private void InitializeKeysForLane()
    {
        keysForLane = new List<List<KeyCode>>();
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

    private void CalculateLastScan()
    {
        SortedSet<Note> notes = GameSetup.pattern.notes;

        lastScan = 0;
        if (notes.Count > 0)
        {
            lastScan = notes.Max.pulse / PulsesPerScan;
        }
        if (backingTrackClip != null)
        {
            while (GameSetup.pattern.PulseToTime(
                (lastScan + 1) * PulsesPerScan)
                < backingTrackClip.length)
            {
                lastScan++;
            }
        }
        if (videoPlayer.url != "")
        {
            while (GameSetup.pattern.PulseToTime(
                (lastScan + 1) * PulsesPerScan)
                < videoPlayer.length)
            {
                lastScan++;
            }
        }

        // Look at all hold and drag notes in the last few scans
        // in case their duration outlasts the currently considered
        // last scan.
        int pulseBorder = (lastScan - 1) * PulsesPerScan;
        foreach (Note n in GameSetup.pattern.GetViewBetween(
            pulseBorder, int.MaxValue))
        {
            int endingPulse;
            if (n is HoldNote)
            {
                endingPulse = n.pulse + (n as HoldNote).duration;
            }
            else if (n is DragNote)
            {
                endingPulse = n.pulse + (n as DragNote).Duration();
            }
            else
            {
                continue;
            }
            int endingScan = endingPulse / PulsesPerScan;
            if (endingScan > lastScan)
            {
                lastScan = endingScan;
            }
        }
    }

    private NoteObject SpawnNoteObject(Note n,
        Scan scan, bool hidden)
    {
        GameObject prefab = null;
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
        NoteAppearance headAppearance = 
            head.GetComponent<NoteAppearance>();
        headAppearance.ManageRepeatNotes(notesToManage);
        headAppearance.DrawRepeatHeadBeforeRepeatNotes();
        if (notesToManage.Count > 0)
        {
            NoteObject lastRepeatNote = notesToManage[0];
            int lastRepeatNotePulse = lastRepeatNote.note.pulse;
            if (lastRepeatNote.note is HoldNote)
            {
                lastRepeatNotePulse +=
                    (lastRepeatNote.note as HoldNote).duration;
            }
            headAppearance.DrawRepeatPathTo(lastRepeatNotePulse,
                positionEndOfScanOutOfBounds: 
                !lastRepeatNote.note.endOfScan);

            // Create path extensions if the head and last
            // note are in different scans.
            int headScan = head.note.GetScanNumber(
                GameSetup.pattern.patternMetadata.bps);
            int lastScan = lastRepeatNote.note.GetScanNumber(
                GameSetup.pattern.patternMetadata.bps);
            if (lastRepeatNote.note is HoldNote)
            {
                lastScan = lastRepeatNotePulse / PulsesPerScan;
            }
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
        bga.texture = renderTexture;
        bga.color = Color.white;
        bga.GetComponent<AspectRatioFitter>().aspectRatio =
            (float)videoPlayer.width / videoPlayer.height;
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

    private InputDevice DeviceForNote(Note n)
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

    private float LatencyForNote(Note n)
    {
        int latencyMs = Options.instance.GetLatencyForDevice(
            DeviceForNote(n));
        return latencyMs * 0.001f;
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

        if (!IsPaused() && !loading)
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

        if (Input.GetKeyDown(KeyCode.F5))
        {
            hitboxVisible = !hitboxVisible;
            HitboxVisibilityChanged?.Invoke(hitboxVisible);
        }

        UpdateTime();
        UpdateFever();
        HandleInput();
        UpdateOngoingNotes();
        UpdateUI();
        UpdateBrightness();
    }

    private void UpdateTime()
    {
        float oldBaseTime = BaseTime;
        float oldTime = Time;
        BaseTime = (float)stopwatch.Elapsed.TotalSeconds + initialTime;
        FloatPulse = GameSetup.pattern.TimeToPulse(Time);
        int newPulse = Mathf.FloorToInt(FloatPulse);
        int newScan = Mathf.FloorToInt(FloatPulse / PulsesPerScan);

        // Play backing track if base time hits 0.
        if (oldBaseTime < 0f && BaseTime >= 0f &&
            backingTrackClip != null)
        {
            audioSourceManager.PlayBackingTrack(backingTrackClip,
                BaseTime);
        }

        // Play bga if base time hits bgaOffset.
        if (oldBaseTime < GameSetup.pattern.patternMetadata.bgaOffset &&
            BaseTime >= GameSetup.pattern.patternMetadata.bgaOffset &&
            GameSetup.pattern.patternMetadata.bga != null &&
            GameSetup.pattern.patternMetadata.bga != "")
        {
            videoPlayer.time = BaseTime - 
                GameSetup.pattern.patternMetadata.bgaOffset;
            videoPlayer.Play();
        }

        // Fire ScanAboutToChange if we are 7/8 into the next scan.
        if (RoundingDownIntDivision(
                Pulse + PulsesPerScan / 8, PulsesPerScan) !=
            RoundingDownIntDivision(
                newPulse + PulsesPerScan / 8, PulsesPerScan))
        {
            ScanAboutToChange?.Invoke(
                (newPulse + PulsesPerScan / 8) / PulsesPerScan);
        }
        if (newScan > Scan)
        {
            ScanChanged?.Invoke(newScan);
        }
        Pulse = newPulse;
        Scan = newScan;

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
            LinkedList<NoteObject> lane =
                noteObjectsInLane[laneIndex];
            if (lane.Count == 0) continue;
            NoteObject upcomingNote = lane.First.Value;

            if (laneIndex < kPlayableLanes)
            {
                if (GameSetup.autoPlay)
                {
                    // Auto-play notes when it comes to their time.
                    // Also, auto-play uses the base time.
                    if (oldBaseTime < upcomingNote.note.time
                        && BaseTime >= upcomingNote.note.time)
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
                            + Ruleset.instance.breakThreshold
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
                if (oldBaseTime < upcomingNote.note.time &&
                    BaseTime >= upcomingNote.note.time)
                {
                    PlayKeysound(upcomingNote);
                    upcomingNote.gameObject.SetActive(false);
                    lane.RemoveFirst();
                }
            }
        }

        // Check for end of pattern.
        if (Scan > lastScan)
        {
            if (feverState == FeverState.Active)
            {
                feverState = FeverState.Idle;
                score.FeverOff();
            }
            Curtain.DrawCurtainThenGoToScene("Result");
        }
    }

    private void HandleInput()
    {
        if (IsPaused())
        {
            return;
        }
        if (GameSetup.autoPlay)
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
                for (int lane = 0; lane < kPlayableLanes; lane++)
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
                for (int lane = 0; lane < kPlayableLanes; lane++)
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
            judgementText.Show(pair.Key, pair.Value);
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
            float gracePeriodStart = 0f;
            float endTime = 0f;
            if (pair.Key.note is HoldNote)
            {
                HoldNote holdNote = pair.Key.note as HoldNote;
                gracePeriodStart = holdNote.gracePeriodStart +
                    latency;
                endTime = holdNote.endTime + latency;
            }
            else if (pair.Key.note is DragNote)
            {
                DragNote dragNote = pair.Key.note as DragNote;
                gracePeriodStart = dragNote.gracePeriodStart +
                    latency;
                endTime = dragNote.endTime + latency;
            }
            if (Time >= endTime)
            {
                // Resolve note.
                ResolveNote(pair.Key, ongoingNotes[pair.Key]);
                ongoingNotes.Remove(pair.Key);
                continue;
            }

            if (pair.Value == false
                && !GameSetup.autoPlay
                && Time < gracePeriodStart)
            {
                // No hit on this note during this frame, resolve
                // as a Miss.
                ResolveNote(pair.Key, Judgement.Miss);
                StopKeysoundIfPlaying(pair.Key);
                ongoingNotes.Remove(pair.Key);
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
        feverButtonFilling.anchorMax = new Vector2(feverAmount, 1f);
        feverButtonAnimator.SetBool("Fever Ready", feverState == FeverState.Ready);
        middleFeverBarFilling.anchorMin = new Vector2(
            0.5f - feverAmount * 0.5f, 0f);
        middleFeverBarFilling.anchorMax = new Vector2(
            0.5f + feverAmount * 0.5f, 1f);
        middleFeverText.SetActive(feverState == FeverState.Ready);

        // Other
        hpBar.anchorMax = new Vector2(
            (float)hp / Ruleset.instance.maxHp, 1f);
        scoreText.text = score.CurrentScore().ToString();
        maxComboText.text = maxCombo.ToString();
    }

    private void UpdateBrightness()
    {
        float a = brightnessCover.color.a;
        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            a -= 0.1f;
        }
        if (Input.GetKeyDown(KeyCode.PageDown))
        {
            a += 0.1f;
        }
        a = Mathf.Clamp01(a);
        brightnessCover.color = new Color(
            brightnessCover.color.r,
            brightnessCover.color.g,
            brightnessCover.color.b,
            a);
    }
    #endregion

    #region Fever
    public void OnFeverButtonPointerDown()
    {
        if (feverState != FeverState.Ready) return;
        if (GameSetup.autoPlay) return;
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
            feverBonusText.text = "FEVER BONUS   +" + feverBonus;
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
        if (fingerInLane.ContainsKey(finger) &&
            fingerInLane[finger] == kOutsideAllLanes)
        {
            // Special case: when player clicks the pause button,
            // we receive a FingerDown but no FingerUp, because
            // events are not processed during pause. When user
            // resumes game later, the first FingerDown will
            // result in an error because the finger is already
            // down.
            //
            // To work around that, we suppress this error if
            // the finger is previously in lane -1.
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
        foreach (RaycastResult r in results)
        {
            NoteHitbox touchReceiver = r.gameObject
                .GetComponent<NoteHitbox>();
            if (touchReceiver != null)
            {
                NoteObject n = touchReceiver
                    .GetComponentInParent<NoteObject>();

                if (ongoingNotes.ContainsKey(n))
                {
                    // Ignore ongoing notes, and don't play sound
                    // either.
                    break;
                }
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
                        continue;
                    }
                }
                NoteObject noteToCheck = n;
                if (n.note.type == NoteType.RepeatHead ||
                    n.note.type == NoteType.RepeatHeadHold)
                {
                    noteToCheck = n.GetComponent<NoteAppearance>()
                        .GetFirstUnresolvedRepeatNote();
                }
                float correctTime = noteToCheck.note.time
                    + LatencyForNote(noteToCheck.note);
                float difference = Time - correctTime;
                if (Mathf.Abs(difference) > 
                    Ruleset.instance.breakThreshold)
                {
                    // The touch or click is too early or too late
                    // for this note. Ignore.
                    continue;
                }
                else
                {
                    // The touch or click lands on this note.
                    HitNote(noteToCheck, difference);
                    break;
                }
            }

            EmptyTouchReceiver emptyReceiver = r.gameObject
                .GetComponent<EmptyTouchReceiver>();
            if (emptyReceiver != null)
            {
                EmptyHit(emptyReceiver.lane);
                break;
            }
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
                noteToCheck = n.GetComponent<NoteAppearance>()
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
        NoteObject earliestNote = noteObjectsInLane[lane]
            .First.Value;
        if (ongoingNotes.ContainsKey(earliestNote))
        {
            // Do nothing.
            return;
        }
        float correctTime = earliestNote.note.time
            + LatencyForNote(earliestNote.note);
        float difference = Time - correctTime;
        if (Mathf.Abs(difference) > Ruleset.instance.breakThreshold)
        {
            // The keystroke is too early or too late
            // for this note. Ignore.
            PlayKeysound(earliestNote);
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
        for (int i = 0; i < kPlayableLanes; i++)
        {
            if (notesForKeyboardInLane[i].Count == 0)
            {
                continue;
            }
            NoteObject n = notesForKeyboardInLane[i].First.Value;
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
        if (Mathf.Abs(difference) > Ruleset.instance.breakThreshold)
        {
            // The keystroke is too early or too late
            // for this note. Ignore.
            PlayKeysound(earliestNote);
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

        Judgement judgement;
        float absDifference = Mathf.Abs(timeDifference);
        if (absDifference <= Ruleset.instance.rainbowMaxWindow)
        {
            judgement = Judgement.RainbowMax;
        }
        else if (absDifference <= Ruleset.instance.maxWindow)
        {
            judgement = Judgement.Max;
        }
        else if (absDifference <= Ruleset.instance.coolWindow)
        {
            judgement = Judgement.Cool;
        }
        else if (absDifference <= Ruleset.instance.goodWindow)
        {
            judgement = Judgement.Good;
        }
        else
        {
            judgement = Judgement.Miss;
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

        if (judgement != Judgement.Miss)
        {
            PlayKeysound(n);
        }
    }

    private void EmptyHit(int lane)
    {
        NoteObject upcomingNote = null;
        switch (GameSetup.pattern.patternMetadata.controlScheme)
        {
            case ControlScheme.Touch:
                if (noteObjectsInLane[lane].Count > 0)
                {
                    upcomingNote = noteObjectsInLane[lane]
                        .First.Value;
                }
                break;
            case ControlScheme.KM:
                if (notesForMouseInLane[lane].Count > 0)
                {
                    upcomingNote = notesForMouseInLane[lane]
                        .First.Value;
                }
                break;
            case ControlScheme.Keys:
                // Keys should not call this method.
                break;
        }

        if (upcomingNote != null &&
            !ongoingNotes.ContainsKey(upcomingNote))
        {
            PlayKeysound(upcomingNote);
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
            hp += feverState == FeverState.Active ? 
                Ruleset.instance.hpRecoveryDuringFever : 
                Ruleset.instance.hpRecovery;
            if (hp >= Ruleset.instance.maxHp)
            {
                hp = Ruleset.instance.maxHp;
            }

            if (feverState == FeverState.Idle &&
                (judgement == Judgement.RainbowMax ||
                 judgement == Judgement.Max))
            {
                feverAmount += feverCoefficient / numPlayableNotes;
                if (GameSetup.autoPlay) feverAmount = 0f;
                if (feverAmount >= 1f)
                {
                    feverState = FeverState.Ready;
                    feverAmount = 1f;
                }
            }
        }
        else
        {
            SetCombo(0);
            hp -= feverState == FeverState.Active ? 
                Ruleset.instance.hpLossDuringFever :
                Ruleset.instance.hpLoss;
            if (hp < 0) hp = 0;
            if (hp <= 0 && !GameSetup.noFail)
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
        score.LogNote(judgement);

        // Appearances and VFX.
        vfxSpawner.SpawnVFXOnResolve(n, judgement);
        n.GetComponent<NoteAppearance>().Resolve();
        // Call this after updating combo to show the correct
        // combo on judgement text.
        judgementText.Show(n, judgement);
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
    // note, so they can be stopped later.
    private Dictionary<Note, AudioSource> noteToAudioSource;
    private void PlayKeysound(NoteObject n)
    {
        if (n.note.sound == "") return;

        AudioClip clip = ResourceLoader.GetCachedClip(n.note.sound);
        AudioSource source = audioSourceManager.PlayKeysound(clip,
            n.note.lane >= kPlayableLanes,
            startTime: 0f,
            n.note.volume, n.note.pan);
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
        return pauseDialog.gameObject.activeSelf;
    }

    public void OnPauseButtonClickOrTouch()
    {
        stopwatch.Stop();
        feverTimer?.Stop();
        audioSourceManager.PauseAll();
        if (videoPlayer.isPrepared) videoPlayer.Pause();
        pauseDialog.Show(closeCallback: () =>
        {
            stopwatch.Start();
            feverTimer?.Start();
            audioSourceManager.UnpauseAll();
            if (videoPlayer.isPrepared) videoPlayer.Play();
        });
        MenuSfx.instance.PlayPauseSound();
    }
    #endregion
}
