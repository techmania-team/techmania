using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTimer
{
    #region Time
    private System.Diagnostics.Stopwatch stopwatch;
    // The stopwatch provides the "base time", which drives
    // the backing track, BGA, hidden notes and auto-played notes.
    public float BaseTime { get; private set; }
    // The base time of the previous frame.
    public float PrevFrameBaseTime { get; private set; }

    private float offset;
    // Apply offset to BaseTime to get GameTime, which drives
    // scanlines and playable notes.
    public float GameTime
    {
        get
        {
            if (GameController.autoPlay) return BaseTime;
            return BaseTime - offset;
        }
    }

    public float speed { get; private set; }
    #endregion

    #region Pulse, beat, scan
    public int PulsesPerScan { get; private set; }
    public float Pulse { get; private set; }
    public float Beat { get; private set; }
    public float Scan { get; private set; }

    // The same numbers rounded down, for convenience.
    public int IntPulse { get; private set; }
    public int IntBeat { get; private set; }
    public int IntScan { get; private set; }
    #endregion

    #region Pattern metadata
    private Pattern pattern;
    public int firstScan { get; private set; }
    public int lastScan { get; private set; }
    public float patternEndTime { get; private set; }

    // BaseTime is equal to this value when the stopwatch begins.
    private float initialTime;
    #endregion

    #region Methods
    public GameTimer(Pattern p)
    {
        stopwatch = new System.Diagnostics.Stopwatch();
        pattern = p;
        PulsesPerScan = Pattern.pulsesPerBeat * p.patternMetadata.bps;
        speed = 1f;
    }

    public void Prepare(float backingTrackLength,
        float bgaLength)
    {
        pattern.PrepareForTimeCalculation();
        pattern.CalculateTimeOfAllNotes(calculateTimeWindows: true);

        // Calculate offset.
        int offsetMs = pattern.patternMetadata.controlScheme
            == ControlScheme.Touch ?
            Options.instance.touchOffsetMs :
            Options.instance.keyboardMouseOffsetMs;
        offset = offsetMs * 0.001f;

        // Calculate the start time of the first note, and
        // the end time of the last note.
        float firstNoteStartTime = 0f;
        float lastNoteEndTime = 0f;
        foreach (Note n in pattern.notes)
        { 
            if (n.time < firstNoteStartTime)
            {
                firstNoteStartTime = n.time;
            }
            break;
        }
        foreach (Note n in pattern.notes)
        {
            float endOfDuration = n.time;
            float endOfKeysound = n.time;

            if (!string.IsNullOrEmpty(n.sound))
            {
                endOfKeysound += ResourceLoader.GetCachedClip(
                    n.sound).length;
            }
            if (n is HoldNote)
            {
                int endPulse = n.pulse + (n as HoldNote).duration;
                endOfDuration = pattern.PulseToTime(endPulse);
            }
            else if (n is DragNote)
            {
                int endPulse = n.pulse + (n as DragNote).Duration();
                endOfDuration = pattern.PulseToTime(endPulse);
            }

            lastNoteEndTime = Mathf.Max(lastNoteEndTime,
                endOfDuration, endOfKeysound);
        }

        // To calculate the start and end time of the pattern,
        // also take backing track and BGA into account.
        float patternStartTime = firstNoteStartTime;
        patternEndTime = lastNoteEndTime;
        if (!string.IsNullOrEmpty(
            pattern.patternMetadata.backingTrack))
        {
            patternEndTime = Mathf.Max(
                patternEndTime, backingTrackLength);
        }
        if (!string.IsNullOrEmpty(
            pattern.patternMetadata.bga) &&
            pattern.patternMetadata.waitForEndOfBga)
        {
            patternStartTime = Mathf.Min(patternStartTime,
                (float)pattern.patternMetadata.bgaOffset);
            patternEndTime = Mathf.Max(patternEndTime,
                bgaLength + (float)pattern.patternMetadata.bgaOffset);
        }

        // Calculate first and last scan.
        firstScan = Mathf.FloorToInt(
            pattern.TimeToPulse(patternStartTime) /
            PulsesPerScan);
        lastScan = Mathf.FloorToInt(
            pattern.TimeToPulse(patternEndTime) /
            PulsesPerScan);
        initialTime = pattern.PulseToTime(firstScan * PulsesPerScan);
    }

    public void Begin()
    {
        BaseTime = initialTime;
        PrevFrameBaseTime = initialTime - 0.01f;
        stopwatch.Start();
    }

    public void Update()
    {
        PrevFrameBaseTime = BaseTime;
        BaseTime = (float)stopwatch.Elapsed.TotalSeconds * speed
            + initialTime;
        Pulse = pattern.TimeToPulse(BaseTime);
        Beat = Pulse / Pattern.pulsesPerBeat;
        Scan = Pulse / PulsesPerScan;
        IntPulse = Mathf.FloorToInt(Pulse);
        IntBeat = Mathf.FloorToInt(Beat);
        IntScan = Mathf.FloorToInt(Scan);
    }

    public void Pause()
    {
        stopwatch.Stop();
    }

    public void Unpause()
    {
        stopwatch.Start();
    }

    public void Dispose()
    {
        stopwatch.Stop();
    }
    #endregion
}