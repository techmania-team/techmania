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
    public Transform bottomScanContainer;
    public GameObject bottomScanTemplate;

    [Header("Audio")]
    public AudioSource backingTrackSource;
    public List<AudioSource> keysoundSources;

    [Header("Prefabs")]
    public GameObject basicNotePrefab;
    public GameObject chainHeadPrefab;
    public GameObject chainNodePrefab;

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
    public static FeverState feverState { get; private set; }
    public static float feverAmount { get; private set; }

    private const int kPlayableLanes = 4;
    private const float kBreakThreshold = 0.3f;
    private const float kGoodThreshold = 0.15f;
    private const float kCoolThreshold = 0.1f;
    private const float kMaxThreshold = 0.05f;
    private const float kRainbowMaxThreshold = 0.03f;
    private const int kMaxHp = 1000;
    private const int kHpLoss = 50;
    private const int kHpRecovery = 4;

    private Stopwatch stopwatch;
    private Stopwatch feverTimer;
    private float initialTime;
    private bool loading;
    public static float Time { get; private set; }
    public static int PulsesPerScan { get; private set; }
    public static float FloatPulse { get; private set; }
    public static int Pulse { get; private set; }
    public static int Scan { get; private set; }
    private int lastScan;

    public static event UnityAction<int> ScanChanged;
    public static event UnityAction<int> ScanAboutToChange;

    private static List<List<KeyCode>> keysForLane;

    // Each linked list represents one lane; each lane is
    // sorted by pulse. Resolved notes are removed
    // from the corresponding linked list. This makes it easy to
    // find the upcoming note in each lane, and in turn:
    // - play keysounds on empty hits
    // - check the Break condition on upcoming notes
    private List<LinkedList<NoteObject>> noteObjectsInLane;
    private int numPlayableNotes;

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
        stopwatch = null;

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
            WelcomeMat.skipToTrackSelect = true;
            Curtain.DrawCurtainThenGoToScene("Main Menu");
        });
    }

    private bool backgroundImageLoaded;
    private bool backingTrackLoaded;
    private bool keysoundsLoaded;
    private IEnumerator LoadSequence()
    {
        // Step 1: load background image, if any. This makes the
        // loading screen not too dull.
        if (GameSetup.track.trackMetadata.backImage != null &&
            GameSetup.track.trackMetadata.backImage != "")
        {
            string fullPath = GameSetup.trackFolder + "\\" +
                GameSetup.track.trackMetadata.backImage;
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
            string fullPath = GameSetup.trackFolder + "\\" +
                GameSetup.pattern.patternMetadata.backingTrack;
            backingTrackSource.clip = null;
            backingTrackLoaded = false;
            ResourceLoader.LoadAudio(fullPath,
                OnBackingTrackLoadComplete);
            yield return new WaitUntil(() => backingTrackLoaded);
        }

        // Step 3: load keysounds, if any.
        keysoundsLoaded = false;
        ResourceLoader.CacheSoundChannels(GameSetup.trackFolder,
            GameSetup.pattern,
            OnKeysoundLoadComplete);
        yield return new WaitUntil(() => keysoundsLoaded);

        // Step 4: load BGA, if any.
        if (GameSetup.track.trackMetadata.bga != null &&
            GameSetup.track.trackMetadata.bga != "")
        {
            string fullPath = GameSetup.trackFolder + "\\" +
                GameSetup.track.trackMetadata.bga;
            videoPlayer.url = fullPath;
            videoPlayer.errorReceived += VideoPlayerErrorReceived;
            videoPlayer.Prepare();
            yield return new WaitUntil(() => videoPlayer.isPrepared);
            videoPlayer.errorReceived -= VideoPlayerErrorReceived;
            PrepareVideoPlayer();
        }
        else
        {
            bga.color = Color.clear;
        }

        // Step 5: initialize pattern. This sadly cannot be done
        // asynchronously.
        InitializePattern();

        // Loading complete.
        loading = false;
        topBar.SetActive(true);
        middleFeverBar.SetActive(true);

        // Start timer. Backing track will start when timer hits 0;
        // BGA will start when timer hits bgaOffset.
        stopwatch = new Stopwatch();
        stopwatch.Start();
        Time = initialTime;
    }

    private void OnImageLoadComplete(Sprite sprite, string error)
    {
        if (error != null)
        {
            backgroundImage.color = Color.clear;
            ReportFatalError(error);
            return;
        }

        backgroundImage.sprite = sprite;
        backgroundImage.color = Color.white;
        backgroundImage.GetComponent<AspectRatioFitter>().aspectRatio =
            (float)sprite.rect.width / sprite.rect.height;
        backgroundImageLoaded = true;
    }

    private void OnBackingTrackLoadComplete(AudioClip clip, string error)
    {
        if (error != null)
        {
            ReportFatalError(error);
            return;
        }

        backingTrackSource.clip = clip;
        backingTrackLoaded = true;
    }

    private void InitializePattern()
    {
        // Prepare for keyboard input if applicable.
        if (GameSetup.pattern.patternMetadata.controlScheme == ControlScheme.Keys ||
            GameSetup.pattern.patternMetadata.controlScheme == ControlScheme.KM)
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

        // Rewind till 1 scan before the backing track starts.
        PulsesPerScan = Pattern.pulsesPerBeat *
            GameSetup.pattern.patternMetadata.bps;
        while (initialTime >= 0f)
        {
            Scan--;
            Pulse -= PulsesPerScan;
            initialTime = GameSetup.pattern.PulseToTime(Pulse);
        }

        // Sort all notes by pulse.
        List<NoteWithSound> sortedNotes = new List<NoteWithSound>();
        foreach (SoundChannel c in GameSetup.pattern.soundChannels)
        {
            foreach (Note n in c.notes)
            {
                sortedNotes.Add(new NoteWithSound()
                {
                    note = n,
                    sound = c.name
                });
            }
            foreach (Note n in c.holdNotes)
            {
                sortedNotes.Add(new NoteWithSound()
                {
                    note = n,
                    sound = c.name
                });
            }
            foreach (Note n in c.dragNotes)
            {
                sortedNotes.Add(new NoteWithSound()
                {
                    note = n,
                    sound = c.name
                });
            }
        }
        sortedNotes.Sort((NoteWithSound n1, NoteWithSound n2) =>
        {
            return n1.note.pulse - n2.note.pulse;
        });

        // Find last scan. Make sure it ends later than the backing
        // track, so we don't cut the track short.
        lastScan = 0;
        if (sortedNotes.Count > 0)
        {
            lastScan = sortedNotes[sortedNotes.Count - 1].note.pulse /
                PulsesPerScan;
        }
        if (backingTrackSource.clip != null)
        {
            while (GameSetup.pattern.PulseToTime((lastScan + 1) * PulsesPerScan)
            < backingTrackSource.clip.length)
            {
                lastScan++;
            }
        }

        // Create scan objects.
        Dictionary<int, Scan> scanObjects = new Dictionary<int, Scan>();
        for (int i = Scan; i <= lastScan; i++)
        {
            Transform parent = (i % 2 == 0) ? topScanContainer : bottomScanContainer;
            GameObject template = (i % 2 == 0) ? topScanTemplate : bottomScanTemplate;
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
        numPlayableNotes = 0;
        NoteObject nextChainNode = null;
        for (int i = sortedNotes.Count - 1; i >= 0; i--)
        {
            NoteWithSound n = sortedNotes[i];
            int scanOfN = n.note.pulse / PulsesPerScan;
            bool hidden = n.note.lane >= kPlayableLanes;
            if (!hidden) numPlayableNotes++;

            GameObject prefab = null;
            switch (n.note.type)
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
            }
            NoteObject noteObject = scanObjects[scanOfN]
                .SpawnNoteObject(prefab, n.note, n.sound, hidden);

            while (noteObjectsInLane.Count <= n.note.lane)
            {
                noteObjectsInLane.Add(new LinkedList<NoteObject>());
            }
            noteObjectsInLane[n.note.lane].AddFirst(noteObject);

            if (!hidden)
            {
                if (n.note.type == NoteType.ChainHead ||
                    n.note.type == NoteType.ChainNode)
                {
                    noteObject.GetComponent<NoteAppearance>()
                        .SetNextChainNode(nextChainNode);
                }
                if (n.note.type == NoteType.ChainHead)
                {
                    nextChainNode = null;
                }
                else if (n.note.type == NoteType.ChainNode)
                {
                    nextChainNode = noteObject;
                }
            }
        }

        // Ensure that a ScanChanged event is fired at the first update.
        Scan--;

        // Miscellaneous initialization.
        fingerInLane = new Dictionary<int, int>();
        currentCombo = 0;
        maxCombo = 0;
        score.Initialize(numPlayableNotes);
        hp = kMaxHp;
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
    }

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

    // By default -3 / 2 = -1 because reasons. We want -2.
    // This assumes b is positive.
    private int RoundingDownIntDivision(int a, int b)
    {
        if (a % b == 0) return a / b;
        if (a >= 0) return a / b;
        return (a / b) - 1;
    }

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

        UpdateTime();
        UpdateFever();
        HandleInput();
        UpdateUI();
        UpdateBrightness();
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

    private void UpdateTime()
    {
        float oldTime = Time;
        Time = (float)stopwatch.Elapsed.TotalSeconds + initialTime;
        FloatPulse = GameSetup.pattern.TimeToPulse(Time);
        int newPulse = Mathf.FloorToInt(FloatPulse);
        int newScan = Mathf.FloorToInt(FloatPulse / PulsesPerScan);

        // Play backing track if timer hits 0.
        if (oldTime < 0f && Time >= 0f && backingTrackSource.clip != null)
        {
            backingTrackSource.timeSamples = Mathf.FloorToInt(
                Time * backingTrackSource.clip.frequency);
            backingTrackSource.loop = false;
            backingTrackSource.Play();
        }

        // Play bga if timer hits bgaOffset.
        if (oldTime < GameSetup.track.trackMetadata.bgaOffset &&
            Time >= GameSetup.track.trackMetadata.bgaOffset &&
            GameSetup.track.trackMetadata.bga != null &&
            GameSetup.track.trackMetadata.bga != "")
        {
            videoPlayer.time = Time - GameSetup.track.trackMetadata.bgaOffset;
            videoPlayer.Play();
        }

        // Fire ScanAboutToChange if we are 7/8 into the next scan.
        if (RoundingDownIntDivision(Pulse + PulsesPerScan / 8, PulsesPerScan) !=
            RoundingDownIntDivision(newPulse + PulsesPerScan / 8, PulsesPerScan))
        {
            ScanAboutToChange?.Invoke(
                (newPulse + PulsesPerScan / 8) / PulsesPerScan);
        }
        Pulse = newPulse;
        if (newScan > Scan)
        {
            ScanChanged?.Invoke(newScan);
        }
        Scan = newScan;

        for (int laneIndex = 0; laneIndex < noteObjectsInLane.Count; laneIndex++)
        {
            LinkedList<NoteObject> lane = noteObjectsInLane[laneIndex];
            if (lane.Count == 0) continue;
            NoteObject upcomingNote = lane.First.Value;

            if (laneIndex < kPlayableLanes)
            {
                // Check for Break on upcoming notes in each playable lane.
                if (Time > upcomingNote.note.time + kBreakThreshold)
                {
                    ResolveNote(upcomingNote, Judgement.Break);
                }
            }
            else
            {
                // Play keyounds of upcoming notes in each hidden lane.
                if (oldTime < upcomingNote.note.time &&
                    Time >= upcomingNote.note.time)
                {
                    PlayKeysound(upcomingNote);
                    upcomingNote.gameObject.SetActive(false);
                    lane.RemoveFirst();
                }
            }
        }

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

        ControlScheme scheme = GameSetup.pattern.patternMetadata.controlScheme;
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
                            break;
                        case TouchPhase.Canceled:
                        case TouchPhase.Ended:
                            OnFingerUp(t.fingerId);
                            break;
                    }
                }
                break;
            case ControlScheme.Keys:
                for (int lane = 0; lane < 4; lane++)
                {
                    foreach (KeyCode key in keysForLane[lane])
                    {
                        if (Input.GetKeyDown(key))
                        {
                            OnKeyDownOnLane(lane);
                        }
                    }
                }
                break;
            case ControlScheme.KM:
                if (Input.GetMouseButtonDown(0))
                {
                    OnFingerDown(0, Input.mousePosition);
                }
                else if (Input.GetMouseButton(0))
                {
                    OnFingerMove(0, Input.mousePosition);
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    OnFingerUp(0);
                }
                break;
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
            (float)hp / kMaxHp, 1f);
        scoreText.text = score.CurrentScore().ToString();
        maxComboText.text = maxCombo.ToString();
    }
    #endregion

    #region Fever
    public void OnFeverButtonPointerDown()
    {
        if (feverState != FeverState.Ready) return;
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
        feverAmount = 1f - (float)feverTimer.Elapsed.TotalSeconds * 0.1f;
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
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(eventData, results);

        return results;
    }

    private void ProcessMouseOrFingerDown(List<RaycastResult> results)
    {
        foreach (RaycastResult r in results)
        {
            NoteObject n = r.gameObject
                .GetComponentInParent<NoteObject>();
            if (n != null)
            {
                float correctTime = n.note.time;
                float difference = Time - correctTime;
                if (Mathf.Abs(difference) > kBreakThreshold)
                {
                    // The touch or click is too early or too late
                    // for this note. Ignore.
                    continue;
                }
                else
                {
                    // The touch or click lands on this note.
                    HitNote(n, difference);
                    break;
                }
            }

            EmptyTouchReceiver receiver = r.gameObject.GetComponent<EmptyTouchReceiver>();
            if (receiver != null)
            {
                EmptyHit(receiver.lane);
                break;
            }
        }
    }

    private int RaycastResultToLane(List<RaycastResult> results)
    {
        foreach (RaycastResult r in results)
        {
            EmptyTouchReceiver receiver = r.gameObject.GetComponent<EmptyTouchReceiver>();
            if (receiver != null)
            {
                return receiver.lane;
            }
        }

        return kOutsideAllLanes;
    }
    #endregion

    #region Keyboard
    private void OnKeyDownOnLane(int lane)
    {
        if (noteObjectsInLane[lane].Count == 0)
        {
            EmptyHit(lane);
            return;
        }
        NoteObject earliestNote = noteObjectsInLane[lane].First.Value;
        float correctTime = earliestNote.note.time;
        float difference = Time - correctTime;
        if (Mathf.Abs(difference) > kBreakThreshold)
        {
            // The keystroke is too early or too late
            // for this note. Ignore.
            EmptyHit(lane);
        }
        else
        {
            // The keystroke lands on this note.
            HitNote(earliestNote, difference);
        }
    }
    #endregion

    #region Hitting Notes And Empty Hits
    private void HitNote(NoteObject n, float timeDifference)
    {
        Judgement judgement;
        float absDifference = Mathf.Abs(timeDifference);
        if (absDifference <= kRainbowMaxThreshold)
        {
            judgement = Judgement.RainbowMax;
            vfxSpawner.SpawnExplosionBigAt(n);
        }
        else if (absDifference <= kMaxThreshold)
        {
            judgement = Judgement.Max;
            vfxSpawner.SpawnExplosionBigAt(n);
        }
        else if (absDifference <= kCoolThreshold)
        {
            judgement = Judgement.Cool;
            vfxSpawner.SpawnExplosionMediumAt(n);
        }
        else if (absDifference <= kGoodThreshold)
        {
            judgement = Judgement.Good;
            vfxSpawner.SpawnExplosionSmallAt(n);
        }
        else
        {
            judgement = Judgement.Miss;
        }

        ResolveNote(n, judgement);

        PlayKeysound(n);
    }

    private void EmptyHit(int lane)
    {
        // Debug.Log("Empty hit on lane " + lane);
        if (noteObjectsInLane[lane].Count > 0)
        {
            NoteObject upcomingNote = noteObjectsInLane[lane]
                .First.Value;
            PlayKeysound(upcomingNote);
        }
    }

    private void ResolveNote(NoteObject n, Judgement judgement)
    {
        n.GetComponent<NoteAppearance>().Resolve();
        noteObjectsInLane[n.note.lane].Remove(n);

        if (judgement != Judgement.Miss &&
            judgement != Judgement.Break)
        {
            currentCombo++;
            hp += kHpRecovery;
            if (hp >= kMaxHp) hp = kMaxHp;

            if (feverState == FeverState.Idle &&
                (judgement == Judgement.RainbowMax ||
                 judgement == Judgement.Max))
            {
                feverAmount += 8f / numPlayableNotes;
                if (feverAmount >= 1f)
                {
                    feverState = FeverState.Ready;
                    feverAmount = 1f;
                }
            }
        }
        else
        {
            currentCombo = 0;
            hp -= kHpLoss;
            if (hp <= 0)
            {
                // Stage failed.
                score.stageFailed = true;
                stopwatch.Stop();
                backingTrackSource.Stop();
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
        if (currentCombo > maxCombo)
        {
            maxCombo = currentCombo;
        }
        judgementText.Show(n, judgement);
        score.LogNote(judgement);
    }

    private void PlayKeysound(NoteObject n)
    {
        if (n.sound == "") return;

        AudioClip clip = ResourceLoader.GetCachedClip(n.sound);
        keysoundSources[n.note.lane].clip = clip;
        keysoundSources[n.note.lane].Play();
    }

    private IEnumerator StageFailedSequence()
    {
        stageFailedScreen.SetActive(true);
        yield return new WaitForSeconds(4f);
        Curtain.DrawCurtainThenGoToScene("Result");
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
        backingTrackSource.Pause();
        if (videoPlayer.isPrepared) videoPlayer.Pause();
        pauseDialog.Show(closeCallback: () =>
        {
            stopwatch.Start();
            feverTimer?.Start();
            backingTrackSource.UnPause();
            if (videoPlayer.isPrepared) videoPlayer.Play();
        });
        MenuSfx.instance.PlayPauseSound();
    }
    #endregion
}
