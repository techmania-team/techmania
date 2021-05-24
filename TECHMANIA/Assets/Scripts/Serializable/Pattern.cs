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
        if (minPulseInclusive > maxPulseInclusive)
        {
            return new SortedSet<Note>();
        }
        if (notes.Count == 0)
        {
            return new SortedSet<Note>();
        }
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

    // Returns all notes whose pulse and lane are both between
    // first and last, inclusive.
    public List<Note> GetRangeBetween(Note first, Note last)
    {
        List<Note> range = new List<Note>();
        int minPulse = first.pulse < last.pulse ?
            first.pulse : last.pulse;
        int maxPulse = first.pulse + last.pulse - minPulse;
        int minLane = first.lane < last.lane ?
            first.lane : last.lane;
        int maxLane = first.lane + last.lane - minLane;

        foreach (Note candidate in GetViewBetween(minPulse, maxPulse))
        {
            if (candidate.lane >= minLane &&
                candidate.lane <= maxLane)
            {
                range.Add(candidate);
            }
        }
        return range;
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
    // Enables CalculateTimeOfAllNotes, GetLengthInSeconds,
    // TimeToPulse and PulseToTime.
    public void PrepareForTimeCalculation()
    {
        timeEvents = new List<TimeEvent>();
        bpmEvents.ForEach(e => timeEvents.Add(e));
        timeStops.ForEach(t => timeEvents.Add(t));
        timeEvents.Sort((TimeEvent e1, TimeEvent e2) =>
        {
            if (e1.pulse != e2.pulse)
            {
                return e1.pulse - e2.pulse;
            }
            if (e1.GetType() == e2.GetType()) return 0;
            if (e1 is BpmEvent) return -1;
            return 1;
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

        foreach (TimeEvent e in timeEvents)
        {
            e.time = currentTime +
                secondsPerPulse * (e.pulse - currentPulse);

            if (e is BpmEvent)
            {
                currentTime = e.time;
                currentBpm = (float)(e as BpmEvent).bpm;
                secondsPerPulse = 60f / (pulsesPerBeat * currentBpm);
            }
            else if (e is TimeStop)
            {
                float durationInSeconds = (e as TimeStop).duration *
                    secondsPerPulse;
                currentTime = e.time + durationInSeconds;
                (e as TimeStop).endTime = currentTime;
                (e as TimeStop).bpmAtStart = currentBpm;
            }

            currentPulse = e.pulse;
        }
    }

    // Returns the time between the start of the first non-empty
    // scan, and the end of t he last non-empty scan. Ignores
    // end-of-scan.
    public float GetLengthInSeconds()
    {
        if (notes.Count == 0) return 0f;

        int pulsesPerScan = pulsesPerBeat * patternMetadata.bps;
        int minPulse = int.MaxValue;
        int maxPulse = int.MinValue;

        foreach (Note n in notes)
        {
            int pulse = n.pulse;
            int endPulse = pulse;
            if (n is HoldNote)
            {
                endPulse += (n as HoldNote).duration;
            }
            if (n is DragNote)
            {
                endPulse += (n as DragNote).Duration();
            }

            if (pulse < minPulse) minPulse = pulse;
            if (endPulse > maxPulse) maxPulse = endPulse;
        }

        int firstScan = minPulse / pulsesPerScan;
        float startOfFirstScan = PulseToTime(
            firstScan * pulsesPerScan);
        int lastScan = maxPulse / pulsesPerScan;
        float endOfLastScan = PulseToTime(
            (lastScan + 1) * pulsesPerScan);
        return endOfLastScan - startOfFirstScan;
    }

    public void CalculateTimeOfAllNotes()
    {
        foreach (Note n in notes)
        {
            n.time = PulseToTime(n.pulse);
            if (n is HoldNote)
            {
                HoldNote h = n as HoldNote;
                h.endTime = PulseToTime(h.pulse + h.duration);
                if (Ruleset.instance != null)
                {
                    h.gracePeriodStart = h.endTime -
                        Ruleset.instance.longNoteGracePeriod;
                }
            }
            if (n is DragNote)
            {
                DragNote d = n as DragNote;
                d.endTime = PulseToTime(d.pulse + d.Duration());
                if (Ruleset.instance != null)
                {
                    d.gracePeriodStart = d.endTime -
                        Ruleset.instance.longNoteGracePeriod;
                }
            }
        }
    }

    // Works for negative times too.
    public float TimeToPulse(float time)
    {
        float referenceBpm = (float)patternMetadata.initBpm;
        float referenceTime = (float)patternMetadata.firstBeatOffset;
        int referencePulse = 0;

        // Find the immediate TimeEvent before specified pulse.
        for (int i = timeEvents.Count - 1; i >= 0; i--)
        {
            TimeEvent e = timeEvents[i];
            if (e.time > time) continue;
            if (e.time == time) return e.pulse;

            if (e is BpmEvent)
            {
                referenceBpm = (float)(e as BpmEvent).bpm;
                referenceTime = e.time;
            }
            else if (e is TimeStop)
            {
                if ((e as TimeStop).endTime >= time)
                {
                    return e.pulse;
                }
                referenceBpm = (float)(e as TimeStop).bpmAtStart;
                referenceTime = (e as TimeStop).endTime;
            }
            referencePulse = e.pulse;
            break;
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

        // Find the immediate TimeEvent before specified pulse.
        for (int i = timeEvents.Count - 1; i >= 0; i--)
        {
            TimeEvent e = timeEvents[i];
            if (e.pulse > pulse) continue;
            if (e.pulse == pulse) return e.time;

            if (e is BpmEvent)
            {
                referenceBpm = (float)(e as BpmEvent).bpm;
                referenceTime = e.time;
            }
            else if (e is TimeStop)
            {
                referenceBpm = (float)(e as TimeStop).bpmAtStart;
                referenceTime = (e as TimeStop).endTime;
            }
            referencePulse = e.pulse;
            break;
        }

        float secondsPerPulse = 60f / (pulsesPerBeat * referenceBpm);

        return referenceTime +
            secondsPerPulse * (pulse - referencePulse);
    }
    #endregion

    #region Statistics and Radar
    public int NumPlayableNotes(int playableLanes = 4)
    {
        int count = 0;
        foreach (Note n in notes)
        {
            if (n.lane < playableLanes) count++;
        }
        return count;
    }
    #endregion
}
