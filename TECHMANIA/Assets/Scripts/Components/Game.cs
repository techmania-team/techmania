using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    public ResourceLoader resourceLoader;
    public AudioSource backingTrackSource;
    public List<AudioSource> keysoundSources;

    private DateTime systemTimeOnGameStart;
    private float initialTime = 0f;
    private float time;
    private int pulsesPerScan = 0;
    private int pulse = 0;
    private int scan = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (GameSetup.track == null)
        {
            SceneManager.LoadScene("Main Menu");
        }
        resourceLoader.LoadResources(GameSetup.trackPath);

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

        // And now we wait for the resources to load.
        systemTimeOnGameStart = DateTime.MinValue;
        StartCoroutine(StartGameAfterLoading());
    }

    // Update is called once per frame
    void Update()
    {
        if (systemTimeOnGameStart == DateTime.MinValue)
        {
            // Game not started yet.
            return;
        }

        float newTime = (float)(DateTime.Now - systemTimeOnGameStart).TotalSeconds
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
        }
        scan = newScan;
    }

    private IEnumerator StartGameAfterLoading()
    {
        yield return new WaitUntil(() =>
        {
            return resourceLoader.LoadComplete();
        });
        backingTrackSource.clip = resourceLoader.GetClip(
            GameSetup.pattern.patternMetadata.backingTrack);
        systemTimeOnGameStart = DateTime.Now;
        time = initialTime;
        Debug.Log($"Starting game at scan {scan}, time {initialTime}.");
    }
}
