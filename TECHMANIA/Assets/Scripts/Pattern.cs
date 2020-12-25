using System.Collections;
using System.Collections.Generic;

public partial class Pattern
{
    // The "main" part is defined in Track.cs. This part contains
    // editing routines and timing calculations.
    #region Editing
    public bool HasNoteAt(int pulse, int lane)
    {
        return GetNoteAt(pulse, lane) != null;
    }

    public Note GetNoteAt(int pulse, int lane)
    {
        Note note = new Note() { pulse = pulse, lane = lane };
        SortedSet<Note> view = notes.GetViewBetween(
            note, note);
        if (view.Count > 0) return view.Min;
        return null;
    }

    // Includes all lanes.
    public SortedSet<Note> GetViewBetween(int minPulseInclusive,
        int maxPulseInclusive)
    {
        return notes.GetViewBetween(new Note()
        {
            pulse = minPulseInclusive,
            lane = 0
        }, new Note()
        {
            pulse = maxPulseInclusive,
            lane = int.MaxValue
        });
    }

    // Of all the notes that are:
    // - before n in time (exclusive)
    // - of one of the specified types (any type if types is null)
    // - between lanes minLaneInclusive and maxLaneInclusive
    // Find and return the one closest to o. If there are
    // multiple such notes on the same pulse, return the one
    // with maximum lane.
    public Note GetClosestNoteBefore(int pulse,
        HashSet<NoteType> types,
        int minLaneInclusive, int maxLaneInclusive)
    {
        SortedSet<Note> view = GetViewBetween(0, pulse - 1);
        foreach (Note note in view.Reverse())
        {
            if (note.lane < minLaneInclusive) continue;
            if (note.lane > maxLaneInclusive) continue;
            if (types == null || types.Contains(note.type))
            {
                return note;
            }
        }

        return null;
    }

    // Of all the notes that are:
    // - after n in time (exclusive)
    // - of one of the specified types (any type if types is null)
    // - between lanes minLaneInclusive and maxLaneInclusive
    // Find and return the one closest to o. If there are
    // multiple such notes on the same pulse, return the one
    // with minimum lane.
    public Note GetClosestNoteAfter(int pulse,
        HashSet<NoteType> types,
        int minLaneInclusive, int maxLaneInclusive)
    {
        SortedSet<Note> view = GetViewBetween(
            pulse + 1, int.MaxValue);
        foreach (Note note in view)
        {
            if (note.lane < minLaneInclusive) continue;
            if (note.lane > maxLaneInclusive) continue;
            if (types == null || types.Contains(note.type))
            {
                return note;
            }
        }

        return null;
    }
    #endregion

    #region Timing
    // Sort BPM events by pulse, then fill their time fields.
    // Enables CalculateTimeOfAllNotes, TimeToPulse and PulseToTime.
    public void PrepareForTimeCalculation()
    {
        bpmEvents.Sort((BpmEvent e1, BpmEvent e2) =>
        {
            return e1.pulse - e2.pulse;
        });

        float currentBpm = (float)patternMetadata.initBpm;
        float currentTime = (float)patternMetadata.firstBeatOffset;
        int currentPulse = 0;
        // beat / minute = currentBpm
        // pulse / beat = pulsesPerBeat
        // ==>
        // pulse / minute = pulsesPerBeat * currentBpm
        // ==>
        // minute / pulse = 1f / (pulsesPerBeat * currentBpm)
        // ==>
        // second / pulse = 60f / (pulsesPerBeat * currentBpm)
        float secondsPerPulse = 60f / (pulsesPerBeat * currentBpm);

        foreach (BpmEvent e in bpmEvents)
        {
            e.time = currentTime +
                secondsPerPulse * (e.pulse - currentPulse);

            currentBpm = (float)e.bpm;
            currentTime = e.time;
            currentPulse = e.pulse;
            secondsPerPulse = 60f / (pulsesPerBeat * currentBpm);
        }
    }

    public void CalculateTimeOfAllNotes()
    {
        foreach (Note n in notes)
        {
            n.time = PulseToTime(n.pulse);
        }
    }

    // Works for negative times too.
    public float TimeToPulse(float time)
    {
        float referenceBpm = (float)patternMetadata.initBpm;
        float referenceTime = (float)patternMetadata.firstBeatOffset;
        int referencePulse = 0;

        // Find the immediate BpmEvent before specified pulse.
        for (int i = bpmEvents.Count - 1; i >= 0; i--)
        {
            BpmEvent e = bpmEvents[i];
            if (e.time <= time)
            {
                referenceBpm = (float)e.bpm;
                referenceTime = e.time;
                referencePulse = e.pulse;
                break;
            }
        }

        float secondsPerPulse = 60f / (pulsesPerBeat * referenceBpm);

        return referencePulse +
            (time - referenceTime) / secondsPerPulse;
    }

    // Works for negative pulses too.
    public float PulseToTime(int pulse)
    {
        float referenceBpm = (float)patternMetadata.initBpm;
        float referenceTime = (float)patternMetadata.firstBeatOffset;
        int referencePulse = 0;

        // Find the immediate BpmEvent before specified pulse.
        for (int i = bpmEvents.Count - 1; i >= 0; i--)
        {
            BpmEvent e = bpmEvents[i];
            if (e.pulse <= pulse)
            {
                referenceBpm = (float)e.bpm;
                referenceTime = e.time;
                referencePulse = e.pulse;
                break;
            }
        }

        float secondsPerPulse = 60f / (pulsesPerBeat * referenceBpm);

        return referenceTime +
            secondsPerPulse * (pulse - referencePulse);
    }
    #endregion
}
