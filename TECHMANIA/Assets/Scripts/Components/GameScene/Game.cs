using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

    private Stopwatch stopwatch;
    private float initialTime;
    private float time;
    private int pulsesPerScan;
    private int pulse;
    private int scan;
    private int lastScan;

    public static event UnityAction<float> FloatPulseChanged;
    public static event UnityAction<int> ScanChanged;

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

        

        // And now we wait for the resources to load.
        stopwatch = null;
        StartCoroutine(LoadAndStartGame());
    }

    // Update is called once per frame
    void Update()
    {
        if (stopwatch == null)
        {
            // Game not started yet.
            return;
        }

        float newTime = (float)stopwatch.Elapsed.TotalSeconds
            + initialTime;
        float floatPulse = GameSetup.pattern.TimeToPulse(newTime);
        pulse = Mathf.FloorToInt(floatPulse);
        int newScan = Mathf.FloorToInt(floatPulse / pulsesPerScan);

        if (time < 0f && newTime >= 0f)
        {
            backingTrackSource.timeSamples = Mathf.FloorToInt(
                newTime * backingTrackSource.clip.frequency);
            backingTrackSource.Play();
        }
        time = newTime;
        if (newScan > scan)
        {
            Debug.Log($"Starting scan {newScan} at {newTime}.");
            ScanChanged?.Invoke(newScan);
        }
        scan = newScan;

        FloatPulseChanged.Invoke(floatPulse);
    }

    private IEnumerator LoadAndStartGame()
    {
        yield return new WaitUntil(() =>
        {
            return resourceLoader.LoadComplete();
        });
        // Wait 1 more frame just in case.
        yield return null;

        // Calculations.
        GameSetup.pattern.PrepareForTimeCalculation();
        GameSetup.pattern.CalculateTimeOfAllNotes();
        initialTime = (float)GameSetup.pattern.patternMetadata
            .firstBeatOffset;
        pulse = 0;
        scan = 0;

        // Rewind till 1 scan before the backing track starts.
        pulsesPerScan = Pattern.pulsesPerBeat *
            GameSetup.pattern.patternMetadata.bps;
        while (initialTime >= 0f)
        {
            scan--;
            pulse -= pulsesPerScan;
            initialTime = GameSetup.pattern.PulseToTime(pulse);
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
            pulsesPerScan;

        // Create scan objects.
        Dictionary<int, Scan> scanObjects = new Dictionary<int, Scan>();
        for (int i = scan; i <= lastScan; i++)
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

        // Create note objects.
        foreach (NoteWithSound n in sortedNotes)
        {
            int scanOfN = n.note.pulse / pulsesPerScan;
            scanObjects[scanOfN].SpawnNoteObject(notePrefab, n.note, n.sound);
        }

        // Play audio and start timer.
        backingTrackSource.clip = resourceLoader.GetClip(
            GameSetup.pattern.patternMetadata.backingTrack);
        stopwatch = new Stopwatch();
        stopwatch.Start();
        time = initialTime;
        Debug.Log($"Starting game at scan {scan}, time {initialTime}.");

        // Ensure that a ScanChanged event is fired at the first update.
        scan--;
    }
}
