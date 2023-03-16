using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using System;

[MoonSharpUserData]
public class GameTimer
{
    #region Time
    private System.Diagnostics.Stopwatch stopwatch;
    // The stopwatch provides the "base time", which drives
    // the backing track, BGA, hidden notes and auto-played notes.
    public float baseTime { get; private set; }
    // The base time of the previous frame.
    public float prevFrameBaseTime { get; private set; }

    private float offset;
    // Apply offset to BaseTime to get GameTime, which drives
    // scanlines and playable notes.
    public float gameTime
    {
        get
        {
            if (GameController.instance.autoPlay) return baseTime;
            return baseTime - offset;
        }
    }

    public int speedPercent { get; private set; }
    public float speed => speedPercent * 0.01f;
    #endregion

    #region Pulse, beat, scan
    public int pulsesPerScan { get; private set; }
    public float pulse { get; private set; }
    public float beat { get; private set; }
    public float scan { get; private set; }

    // The same numbers rounded down, for convenience.
    public int intPulse { get; private set; }
    public int intBeat { get; private set; }
    public int intScan { get; private set; }

    // Scan numbers for the previous frame, for updating notes.
    public float prevFrameScan { get; private set; }
    public int prevFrameIntScan { get; private set; }
    #endregion

    #region Combo tick
    private const int kComboTickInterval = 60;
    // Combo ticks are pulses where each ongoing note increases
    // combo by 1. Ongoing notes add 1 more combo when
    // resolved. Combo ticks are, by default, 60 pulses apart.
    private int previousComboTick;
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
    [MoonSharpHidden]
    public GameTimer(Pattern p)
    {
        stopwatch = new System.Diagnostics.Stopwatch();
        pattern = p;
        pulsesPerScan = Pattern.pulsesPerBeat * p.patternMetadata.bps;
        speedPercent = 100;
    }

    [MoonSharpHidden]
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
            // Back up by 0.1s to make sure patternStartTime
            // is strictly less than 0.
            patternStartTime = Mathf.Min(
                patternStartTime, -0.1f);
            patternEndTime = Mathf.Max(
                patternEndTime, backingTrackLength);
        }
        if (!string.IsNullOrEmpty(
            pattern.patternMetadata.bga) &&
            pattern.patternMetadata.waitForEndOfBga)
        {
            // Back up by 0.1s to make sure patternStartTime
            // is strictly less than bgaOffset.
            patternStartTime = Mathf.Min(patternStartTime,
                (float)pattern.patternMetadata.bgaOffset
                - 0.1f);
            patternEndTime = Mathf.Max(patternEndTime,
                bgaLength + (float)pattern.patternMetadata.bgaOffset);
        }

        // Calculate first and last scan.
        firstScan = Mathf.FloorToInt(
            pattern.TimeToPulse(patternStartTime) /
            pulsesPerScan);
        lastScan = Mathf.FloorToInt(
            pattern.TimeToPulse(patternEndTime) /
            pulsesPerScan);
        initialTime = pattern.PulseToTime(firstScan * pulsesPerScan);
    }

    [MoonSharpHidden]
    public void Begin()
    {
        stopwatch.Start();
    }

    [MoonSharpHidden]
    public void Update(System.Action comboTickCallback)
    {
        prevFrameBaseTime = baseTime;
        prevFrameScan = scan;
        prevFrameIntScan = intScan;

        baseTime = (float)stopwatch.Elapsed.TotalSeconds * speed
            + initialTime;
        pulse = pattern.TimeToPulse(baseTime);
        beat = pulse / Pattern.pulsesPerBeat;
        scan = pulse / pulsesPerScan;
        intPulse = Mathf.FloorToInt(pulse);
        intBeat = Mathf.FloorToInt(beat);
        intScan = Mathf.FloorToInt(scan);

        // Handle combo ticks.
        while (previousComboTick + kComboTickInterval <= intPulse)
        {
            previousComboTick += kComboTickInterval;
            comboTickCallback();
        }
    }

    [MoonSharpHidden]
    public void Pause()
    {
        stopwatch.Stop();
    }

    [MoonSharpHidden]
    public void Unpause()
    {
        stopwatch.Start();
    }

    [MoonSharpHidden]
    public void Dispose()
    {
        stopwatch.Stop();
    }

    [MoonSharpHidden]
    public void JumpToScan(int scan)
    {
        intScan = scan;
        intBeat = scan * pattern.patternMetadata.bps;
        intPulse = intBeat * Pattern.pulsesPerBeat;
        this.scan = intScan;
        beat = intBeat;
        pulse = intPulse;
        previousComboTick = intPulse;

        // Reset initialTime so Update() continues to set the
        // correct base time.
        baseTime = pattern.PulseToTime(intPulse);
        ResetInitialTime();
    }

    [MoonSharpHidden]
    public void SetSpeed(int speedPercent)
    {
        this.speedPercent = speedPercent;
        ResetInitialTime();
    }

    private void ResetInitialTime()
    {
        initialTime = baseTime -
            (float)stopwatch.Elapsed.TotalSeconds * speed;
    }
    #endregion
}
