using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

[Serializable]
// A subset of Pattern meant to calculate fingerprint.
public class MinimizedPattern
{
    public ControlScheme controlScheme;
    public int playableLanes;
    public double initBpm;
    public int bps;
    public List<string> packedNotes;
    public List<BpmEvent> bpmEvents;
    public List<TimeStop> timeStops;

    // For anything we need to add in the future.
    public List<string> additionalStrings;

    public MinimizedPattern(Pattern p)
    {
        controlScheme = p.patternMetadata.controlScheme;
        playableLanes = p.patternMetadata.playableLanes;
        initBpm = p.patternMetadata.initBpm;
        bps = p.patternMetadata.bps;

        packedNotes = new List<string>();
        foreach (Note n in p.notes)
        {
            string soundBackup = n.sound;
            n.sound = "";
            packedNotes.Add(n.Pack());
            n.sound = soundBackup;
        }
        bpmEvents = p.bpmEvents;
        timeStops = p.timeStops;

        additionalStrings = new List<string>();
    }
}

public partial class Pattern
{
    // The "main" part is defined in Track.cs.

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
    // Enables CalculateTimeOfAllNotes, GetLengthInSecondsAndScans,
    // TimeToPulse, PulseToTime and CalculateRadar.
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

    // Returns the pattern length between the start of the first
    // non-empty scan, and the end of the last non-empty scan.
    // Ignores end-of-scan.
    public void GetLengthInSecondsAndScans(out float seconds,
        out int scans)
    {
        if (notes.Count == 0)
        {
            seconds = 0f;
            scans = 0;
            return;
        }

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

        seconds = endOfLastScan - startOfFirstScan;
        scans = lastScan - firstScan + 1;
    }

    public void CalculateTimeOfAllNotes(bool calculateTimeWindows)
    {
        List<float> timeWindows = Ruleset.instance.timeWindows;
        if (calculateTimeWindows &&
            Options.instance.ruleset == Options.Ruleset.Legacy &&
            GameSetup.pattern.legacyRulesetOverride != null &&
            GameSetup.pattern.legacyRulesetOverride
                .timeWindows.Count > 0)
        {
            timeWindows = GameSetup.pattern.legacyRulesetOverride
                .timeWindows;
        }

        foreach (Note n in notes)
        {
            n.time = PulseToTime(n.pulse);
            float bpm = GetBPMAt(n.pulse);
            float secondsPerPulse =
                // seconds per minute *
                // minutes per beat *
                // beats per pulse
                60f / bpm / pulsesPerBeat;

            if (n is HoldNote)
            {
                HoldNote h = n as HoldNote;
                h.endTime = PulseToTime(h.pulse + h.duration);
                if (Ruleset.instance.longNoteGracePeriodInPulses)
                {
                    h.gracePeriodLength =
                        Ruleset.instance.longNoteGracePeriod *
                        secondsPerPulse;
                }
                else
                {
                    h.gracePeriodLength =
                        Ruleset.instance.longNoteGracePeriod;
                }
            }
            if (n is DragNote)
            {
                DragNote d = n as DragNote;
                d.endTime = PulseToTime(d.pulse + d.Duration());
                if (Ruleset.instance.longNoteGracePeriodInPulses)
                {
                    d.gracePeriodLength =
                        Ruleset.instance.longNoteGracePeriod *
                        secondsPerPulse;
                }
                else
                {
                    d.gracePeriodLength =
                        Ruleset.instance.longNoteGracePeriod;
                }
            }

            // Calculate time window according to current ruleset.
            if (!calculateTimeWindows) continue;
            n.timeWindow = new Dictionary<Judgement, float>();
            if (Ruleset.instance.timeWindowsInPulses)
            {
                n.timeWindow.Add(Judgement.RainbowMax,
                    secondsPerPulse * timeWindows[0]);
                n.timeWindow.Add(Judgement.Max,
                    secondsPerPulse * timeWindows[1]);
                n.timeWindow.Add(Judgement.Cool,
                    secondsPerPulse * timeWindows[2]);
                n.timeWindow.Add(Judgement.Good,
                    secondsPerPulse * timeWindows[3]);
                n.timeWindow.Add(Judgement.Miss,
                    secondsPerPulse * timeWindows[4]);
            }
            else
            {
                n.timeWindow.Add(Judgement.RainbowMax,
                    timeWindows[0]);
                n.timeWindow.Add(Judgement.Max,
                    timeWindows[1]);
                n.timeWindow.Add(Judgement.Cool,
                    timeWindows[2]);
                n.timeWindow.Add(Judgement.Good,
                    timeWindows[3]);
                n.timeWindow.Add(Judgement.Miss,
                    timeWindows[4]);
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

    public float GetBPMAt(int pulse)
    {
        float bpm = (float)patternMetadata.initBpm;
        foreach (BpmEvent e in bpmEvents)
        {
            if (e.pulse > pulse) break;
            bpm = (float)e.bpm;
        }
        return bpm;
    }
    #endregion

    #region Statistics and Radar
    public int NumPlayableNotes()
    {
        int count = 0;
        foreach (Note n in notes)
        {
            if (n.lane < patternMetadata.playableLanes) count++;
        }
        return count;
    }

    public struct RadarDimension
    {
        public float raw;
        public int normalized;  // 0-100
    }
    public struct Radar
    {
        public RadarDimension density;
        public RadarDimension peak;
        public RadarDimension speed;
        public RadarDimension chaos;
        public RadarDimension async;
        public RadarDimension shift;
        public float suggestedLevel;
        public int suggestedLevelRounded;
    }

    public Radar CalculateRadar()
    {
        Radar r = new Radar();

        PrepareForTimeCalculation();
        float seconds;
        int scans;
        GetLengthInSecondsAndScans(out seconds, out scans);

        int pulsesPerScan = patternMetadata.bps * pulsesPerBeat;

        // Some pre-processing.
        List<Note> playableNotes = new List<Note>();
        Dictionary<int, int> scanToNumNotes =
            new Dictionary<int, int>();
        int numChaosNotes = 0;
        float numAsyncNotes = 0;
        foreach (Note n in notes)
        {
            if (n.lane >= patternMetadata.playableLanes) continue;
            playableNotes.Add(n);

            int scan = n.pulse / pulsesPerScan;
            if (!scanToNumNotes.ContainsKey(scan))
            {
                scanToNumNotes.Add(scan, 0);
            }
            scanToNumNotes[scan]++;

            if (n.pulse % (pulsesPerBeat / 2) != 0)
            {
                numChaosNotes++;
            }

            switch (n.type)
            {
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                    numAsyncNotes += 0.5f;
                    break;
                case NoteType.Repeat:
                case NoteType.RepeatHold:
                    numAsyncNotes += 1f;
                    break;
            }
        }

        // Density: average number of notes per second.
        if (seconds == 0f)
        {
            r.density.raw = 0f;
        }
        else
        {
            r.density.raw = playableNotes.Count / seconds;
        }
        r.density.normalized = NormalizeRadarValue(r.density.raw,
            0.5f, 8f);

        // Peak: peak number of notes per second.
        foreach (KeyValuePair<int, int> pair in scanToNumNotes)
        {
            int scan = pair.Key;
            int numNotes = pair.Value;
            float startTime = PulseToTime(scan * pulsesPerScan);
            float endTime = PulseToTime((scan + 1) * pulsesPerScan);
            float peak = numNotes / (endTime - startTime);
            if (peak > r.peak.raw)
            {
                r.peak.raw = peak;
            }
        }
        r.peak.normalized = NormalizeRadarValue(r.peak.raw,
            1f, 18f);

        // Speed: average scans per minute.
        if (seconds == 0f)
        {
            r.speed.raw = 0f;
        }
        else
        {
            r.speed.raw = scans * 60 / seconds;
        }
        r.speed.normalized = NormalizeRadarValue(r.speed.raw,
            12f, 55f);

        // Chaos: percentage of notes that are not 4th or 8th notes.
        if (playableNotes.Count == 0)
        {
            r.chaos.raw = 0f;
        }
        else
        {
            r.chaos.raw = numChaosNotes * 100f / playableNotes.Count;
        }
        r.chaos.normalized = NormalizeRadarValue(r.chaos.raw,
            0f, 50f);

        // Async: percentage of notes that are hold or repeat notes.
        // A hold note counts as 0.5 async notes as they are not
        // that hard.
        if (playableNotes.Count == 0)
        {
            r.async.raw = 0f;
        }
        else
        {
            r.async.raw = numAsyncNotes * 100f / playableNotes.Count;
        }
        r.async.normalized = NormalizeRadarValue(r.async.raw,
            0f, 40f);

        // Shift: number of unique time events.
        int numTimeEvents = 0;
        for (int i = 0; i < timeEvents.Count; i++)
        {
            if (i > 0 &&
                timeEvents[i].pulse ==
                    timeEvents[i - 1].pulse &&
                timeEvents[i].GetType()
                    == timeEvents[i - 1].GetType())
            {
                continue;
            }
            numTimeEvents++;
        }
        r.shift.raw = numTimeEvents;

        // Suggested difficulty. Formulta calculated by
        // linear regression.
        r.suggestedLevel =
            r.density.raw * 0.85f +
            r.peak.raw * 0.12f +
            r.speed.raw * 0.02f +
            r.chaos.raw * 0f +
            r.async.raw * 0.03f +
            1.12f;
        r.suggestedLevelRounded = UnityEngine.Mathf.RoundToInt(
            r.suggestedLevel);

        return r;
    }

    private int NormalizeRadarValue(float raw,
        float min, float max)
    {
        float t = UnityEngine.Mathf.InverseLerp(min, max, raw);
        return UnityEngine.Mathf.RoundToInt(t * 100f);
    }
    #endregion

    #region Modifiers
    // Returns a clone with the modifiers applied:
    // - NotePosition
    // - Keysound
    // - AssistTick (only the AutoAssistTick option)
    // - ControlOverride
    // - ScrollSpeed
    //
    // Warning: the clone has different GUID and fingerprint.
    public Pattern ApplyModifiers(Modifiers modifiers)
    {
        Pattern p = CloneWithDifferentGuid();
        int playableLanes = patternMetadata.playableLanes;

        if (modifiers.notePosition == Modifiers.NotePosition.Mirror)
        {
            foreach (Note n in p.notes)
            {
                if (n.lane >= playableLanes) continue;
                n.lane = playableLanes - 1 - n.lane;
                if (n is DragNote)
                {
                    foreach (DragNode node in (n as DragNote).nodes)
                    {
                        node.anchor.lane = -node.anchor.lane;
                        node.controlLeft.lane =
                            -node.controlLeft.lane;
                        node.controlRight.lane =
                            -node.controlRight.lane;
                    }
                }
            }
        }

        if (modifiers.keysound == Modifiers.Keysound.AutoKeysound)
        {
            List<Note> addedNotes = new List<Note>();
            foreach (Note n in p.notes)
            {
                if (n.lane >= playableLanes) continue;
                if (n.sound == null || n.sound == "") continue;
                Note hiddenNote = n.Clone();
                hiddenNote.lane += kAutoKeysoundFirstLane;
                addedNotes.Add(hiddenNote);
                n.sound = "";
            }
            foreach (Note n in addedNotes)
            {
                p.notes.Add(n);
            }
        }

        if (modifiers.assistTick == 
            Modifiers.AssistTick.AutoAssistTick)
        {
            List<AssistTickNote> addedNotes =
                new List<AssistTickNote>();
            foreach (Note n in p.notes)
            {
                if (n.lane >= playableLanes) continue;
                AssistTickNote assistTickNote = new AssistTickNote()
                {
                    pulse = n.pulse,
                    lane = n.lane + kAutoAssistTickFirstLane
                };
                addedNotes.Add(assistTickNote);
            }
            foreach (Note n in addedNotes)
            {
                p.notes.Add(n);
            }
        }

        switch (modifiers.controlOverride)
        {
            case Modifiers.ControlOverride.None:
                break;
            case Modifiers.ControlOverride.OverrideToTouch:
                p.patternMetadata.controlScheme = ControlScheme.Touch;
                break;
            case Modifiers.ControlOverride.OverrideToKeys:
                p.patternMetadata.controlScheme = ControlScheme.Keys;
                break;
            case Modifiers.ControlOverride.OverrideToKM:
                p.patternMetadata.controlScheme = ControlScheme.KM;
                break;
        }

        if (modifiers.scrollSpeed == Modifiers.ScrollSpeed.HalfSpeed)
        {
            p.patternMetadata.bps *= 2;
        }

        return p;
    }
    #endregion

    #region Fingerprint
    public string fingerprint { get; private set; }

    public void CheckFingerprintCalculated()
    {
        if (fingerprint == null ||
            fingerprint.Length == 0)
        {
            throw new Exception("Fingerprint not calculated.");
        }
    }

    public void CalculateFingerprint()
    {
        // Minimize pattern, serialize, then convert to binary.
        MinimizedPattern miniP = new MinimizedPattern(this);
        string json = UnityEngine.JsonUtility.ToJson(miniP,
            prettyPrint: false);
        byte[] hashInput = Encoding.UTF8.GetBytes(json);

        // Compute hash.
        using System.Security.Cryptography.SHA256 sha =
            System.Security.Cryptography.SHA256.Create();
        byte[] hashOutput = sha.ComputeHash(hashInput);

        // Convert to string.
        StringBuilder stringBuilder = new StringBuilder();
        foreach (byte b in hashOutput)
        {
            stringBuilder.Append($"{b:x2}");
        }
        fingerprint = stringBuilder.ToString();
    }
    #endregion
}
