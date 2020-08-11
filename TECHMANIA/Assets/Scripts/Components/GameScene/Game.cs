using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class Game : MonoBehaviour
{
    [Header("Scans")]
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

    public const float kBreakThreshold = 0.15f;
    public const float kGoodThreshold = 0.08f;
    public const float kCoolThreshold = 0.06f;
    public const float kMaxThreshold = 0.04f;
    public const float kRainbowMaxThreshold = 0.02f;

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

        NoteObject.NoteResolved += OnNoteResolved;

        // And now we wait for the resources to load.
        stopwatch = null;
        StartCoroutine(LoadAndStartGame());
    }

    private void OnDestroy()
    {
        NoteObject.NoteResolved -= OnNoteResolved;
    }

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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnPauseButtonClickOrTouch();
        }

        UpdateTime();
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
    }

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
            NoteObject.InitializeKeysForLane();
        }

        // Calculations.
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
        lastScan = sortedNotes[sortedNotes.Count - 1].note.pulse /
            PulsesPerScan;

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
        for (int i = sortedNotes.Count - 1; i >= 0; i--)
        {
            NoteWithSound n = sortedNotes[i];
            int scanOfN = n.note.pulse / PulsesPerScan;
            scanObjects[scanOfN].SpawnNoteObject(notePrefab, n.note, n.sound);
        }

        // Play audio and start timer.
        backingTrackSource.clip = resourceLoader.GetClip(
            GameSetup.pattern.patternMetadata.backingTrack);
        stopwatch = new Stopwatch();
        stopwatch.Start();
        Time = initialTime;

        // Ensure that a ScanChanged event is fired at the first update.
        Scan--;
    }

    public void OnPauseButtonClickOrTouch()
    {
        stopwatch.Stop();
        backingTrackSource.Pause();
        PauseDialog.Show();
        StartCoroutine(ResumeGameAfterPauseDialogResolves());
    }

    private IEnumerator ResumeGameAfterPauseDialogResolves()
    {
        yield return new WaitUntil(() =>
        {
            return PauseDialog.IsResolved();
        });
        stopwatch.Start();
        backingTrackSource.UnPause();
    }

    private void OnNoteResolved(NoteObject n, Judgement judge)
    {
        Debug.Log(judge);
        n.gameObject.SetActive(false);

        if (n.sound != "" && n.sound != UIUtils.kNone)
        {
            AudioClip clip = resourceLoader.GetClip(n.sound);
            keysoundSources[n.note.lane].clip = clip;
            keysoundSources[n.note.lane].Play();
        }
    }
}
