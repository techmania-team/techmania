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
using Debug = UnityEngine.Debug;  // Not System.Diagnostics.Debug

public class Game : MonoBehaviour
{
    [Header("Scans")]
    public GraphicRaycaster raycaster;
    public Transform topScanContainer;
    public GameObject topScanTemplate;
    public Transform bottomScanContainer;
    public GameObject bottomScanTemplate;

    [Header("Audio")]
    public ResourceLoader resourceLoader;
    public AudioSource backingTrackSource;
    public List<AudioSource> keysoundSources;

    [Header("Prefabs")]
    public GameObject notePrefab;

    [Header("VFX")]
    public VFXSpawner vfxSpawner;
    public JudgementText judgementText;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI maxComboText;
    public Dialog pauseDialog;

    public static Score score { get; private set; }
    public static int currentCombo { get; private set; }
    public static int maxCombo { get; private set; }

    public const float kBreakThreshold = 0.3f;
    public const float kGoodThreshold = 0.15f;
    public const float kCoolThreshold = 0.1f;
    public const float kMaxThreshold = 0.05f;
    public const float kRainbowMaxThreshold = 0.03f;

    private Stopwatch stopwatch;
    private float initialTime;
    public static float Time { get; private set; }
    public static int PulsesPerScan { get; private set; }
    public static float FloatPulse { get; private set; }
    public static int Pulse { get; private set; }
    public static int Scan { get; private set; }
    private int lastScan;

    public static event UnityAction<int> ScanChanged;
    public static event UnityAction<int> ScanAboutToChange;

    private static List<List<KeyCode>> keysForLane;

    // Each lane is sorted by pulse.
    private List<LinkedList<NoteObject>> noteObjectsInLane;

    private class NoteWithSound
    {
        public Note note;
        public string sound;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (GameSetup.track == null)
        {
            SceneManager.LoadScene("Main Menu");
        }
        resourceLoader.LoadResources(GameSetup.trackPath);

        Input.simulateMouseWithTouches = false;
        NoteObject.NoteBreak += OnNoteBreak;
        score = new Score();

        // And now we wait for the resources to load.
        stopwatch = null;
        StartCoroutine(LoadAndStartGame());
    }

    private void OnDestroy()
    {
        Input.simulateMouseWithTouches = true;
        NoteObject.NoteBreak -= OnNoteBreak;
    }

    #region Initialization
    private IEnumerator LoadAndStartGame()
    {
        yield return new WaitUntil(() =>
        {
            return resourceLoader.LoadComplete();
        });
        // Wait 1 more frame just in case.
        yield return null;

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
        }
        sortedNotes.Sort((NoteWithSound n1, NoteWithSound n2) =>
        {
            return n1.note.pulse - n2.note.pulse;
        });

        // Find last scan. Make sure it ends later than the backing
        // track, so we don't cut the track short.
        backingTrackSource.clip = resourceLoader.GetClip(
            GameSetup.pattern.patternMetadata.backingTrack);
        lastScan = sortedNotes[sortedNotes.Count - 1].note.pulse /
            PulsesPerScan;
        while (GameSetup.pattern.PulseToTime((lastScan + 1) * PulsesPerScan)
            < backingTrackSource.clip.length)
        {
            lastScan++;
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
        for (int i = 0; i < 4; i++)
        {
            noteObjectsInLane.Add(new LinkedList<NoteObject>());
        }
        for (int i = sortedNotes.Count - 1; i >= 0; i--)
        {
            NoteWithSound n = sortedNotes[i];
            int scanOfN = n.note.pulse / PulsesPerScan;
            NoteObject noteObject = scanObjects[scanOfN]
                .SpawnNoteObject(notePrefab, n.note, n.sound);

            noteObjectsInLane[n.note.lane].AddFirst(noteObject);
        }

        // Ensure that a ScanChanged event is fired at the first update.
        Scan--;

        // Miscellaneous initialization.
        fingerInLane = new Dictionary<int, int>();
        currentCombo = 0;
        maxCombo = 0;
        score.Initialize(sortedNotes.Count);

        // Play audio and start timer.
        
        stopwatch = new Stopwatch();
        stopwatch.Start();
        Time = initialTime;
    }

    public static void InitializeKeysForLane()
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
    #endregion

    // By default -3 / 2 = -1 because reasons. We want -2.
    // This assumes b is positive.
    private int RoundingDownIntDivision(int a, int b)
    {
        if (a % b == 0) return a / b;
        if (a >= 0) return a / b;
        return (a / b) - 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (stopwatch == null)
        {
            // Game not started yet.
            return;
        }

        if (!IsPaused() && Input.GetKeyDown(KeyCode.Escape))
        {
            OnPauseButtonClickOrTouch();
        }

        UpdateTime();
        HandleInput();
        UpdateUI();
    }

    private void UpdateTime()
    {
        float newTime = (float)stopwatch.Elapsed.TotalSeconds
            + initialTime;
        FloatPulse = GameSetup.pattern.TimeToPulse(newTime);
        int newPulse = Mathf.FloorToInt(FloatPulse);
        int newScan = Mathf.FloorToInt(FloatPulse / PulsesPerScan);

        if (Time < 0f && newTime >= 0f)
        {
            backingTrackSource.timeSamples = Mathf.FloorToInt(
                newTime * backingTrackSource.clip.frequency);
            backingTrackSource.Play();
        }
        Time = newTime;
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

        if (Scan > lastScan)
        {
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
        scoreText.text = score.CurrentScore().ToString();
        maxComboText.text = maxCombo.ToString();
    }

    #region Mouse/Finger Tracking
    // In the context of mouse/finger tracking, the mouse
    // cursor is treated as finger #0.
    private Dictionary<int, int> fingerInLane;
    private const int kOutsideAllLanes = -1;

    private void OnFingerDown(int finger, Vector2 screenPosition)
    {
        List<RaycastResult> results = Raycast(screenPosition);
        int lane = RaycastResultToLane(results);
        fingerInLane.Add(finger, lane);
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
            NoteObject n = r.gameObject.GetComponent<NoteObject>();
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

        if (n.sound != "" && n.sound != UIUtils.kNone)
        {
            AudioClip clip = resourceLoader.GetClip(n.sound);
            keysoundSources[n.note.lane].clip = clip;
            keysoundSources[n.note.lane].Play();
        }
    }

    private void EmptyHit(int lane)
    {
        // Debug.Log("Empty hit on lane " + lane);
        if (noteObjectsInLane[lane].Count > 0)
        {
            NoteObject upcomingNote = noteObjectsInLane[lane]
                .First.Value;
            if (upcomingNote.sound != "" && upcomingNote.sound != UIUtils.kNone)
            {
                AudioClip clip = resourceLoader.GetClip(upcomingNote.sound);
                keysoundSources[lane].clip = clip;
                keysoundSources[lane].Play();
            }
        }
    }

    private void OnNoteBreak(NoteObject n)
    {
        ResolveNote(n, Judgement.Break);
    }

    private void ResolveNote(NoteObject n, Judgement judgement)
    {
        n.gameObject.SetActive(false);
        noteObjectsInLane[n.note.lane].Remove(n);

        if (judgement != Judgement.Miss &&
            judgement != Judgement.Break)
        {
            currentCombo++;
        }
        else
        {
            currentCombo = 0;
        }
        if (currentCombo > maxCombo)
        {
            maxCombo = currentCombo;
        }
        judgementText.Show(n, judgement);

        score.LogNote(judgement);
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
        backingTrackSource.Pause();
        pauseDialog.FadeIn();
        MenuSfx.instance.PlayPauseSound();
        StartCoroutine(ResumeGameAfterPauseDialogResolves());
    }

    private IEnumerator ResumeGameAfterPauseDialogResolves()
    {
        yield return new WaitUntil(() =>
        {
            return !IsPaused();
        });
        stopwatch.Start();
        backingTrackSource.UnPause();
    }
    #endregion
}
