using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// GameObjects organized in a very peculiar way so that adding,
// querying, deleting and enumerating can all be done at a
// hopefully reasonable speed. Specifically,
// - Adding/deleting a note costs O(logn)
// - Querying a note costs O(1)
// - Enumerating notes costs O(logn) to begin, then O(1) for each note
// where n is the number of pulses.
//
// This class does not interact with Notes in any way.
public class SortedNoteObjects
{
    // Key: pulse
    // Value: notes on this pulse
    // Pairs with empty values should not exist.
    private Dictionary<int, List<GameObject>> pulseToNotes;
    // The set of all keys in pulseToNotes, organized as a sorted
    // set so they can be enumerated in order.
    private SortedSet<int> populatedPulses;
    
    public SortedNoteObjects()
    {
        pulseToNotes = new Dictionary<int, List<GameObject>>();
        populatedPulses = new SortedSet<int>();
    }

    // Returns -1 if empty.
    public int GetMaxPulse()
    {
        if (populatedPulses.Count == 0) return -1;
        return populatedPulses.Max;
    }

    private int GetPulse(GameObject o)
    {
        return o.GetComponent<NoteObject>().note.pulse;
    }

    private int GetLane(GameObject o)
    {
        return o.GetComponent<NoteObject>().note.lane;
    }

    // Returns empty list of no note is at given pulse.
    public List<GameObject> GetAt(int pulse)
    {
        if (!pulseToNotes.ContainsKey(pulse)) return new List<GameObject>();
        return pulseToNotes[pulse];
    }

    public GameObject GetAt(int pulse, int lane)
    {
        if (!pulseToNotes.ContainsKey(pulse)) return null;
        return GetAt(pulse).Find((GameObject o) =>
        {
            return GetLane(o) == lane;
        });
    }

    // Gets the list of note objects whose pulse and lane are both
    // between first and last, inclusive.
    public List<GameObject> GetRange(GameObject first, GameObject last)
    {
        List<GameObject> answer = new List<GameObject>();
        int firstPulse = GetPulse(first), lastPulse = GetPulse(last);
        int minPulse = Mathf.Min(firstPulse, lastPulse);
        int maxPulse = Mathf.Max(firstPulse, lastPulse);
        int firstLane = GetLane(first), lastLane = GetLane(last);
        int minLane = Mathf.Min(firstLane, lastLane);
        int maxLane = Mathf.Max(firstLane, lastLane);

        foreach (int pulse in populatedPulses.GetViewBetween(minPulse, maxPulse))
        {
            foreach (GameObject o in GetAt(pulse))
            {
                int lane = GetLane(o);
                if (lane >= minLane && lane <= maxLane)
                {
                    answer.Add(o);
                }
            }
        }

        return answer;
    }

    public GameObject GetFirst()
    {
        if (populatedPulses.Count == 0) return null;
        List<GameObject> objects = GetAt(populatedPulses.Min);

        GameObject first = objects[0];
        int minLane = GetLane(first);
        for (int i = 1; i < objects.Count; i++)
        {
            int lane = GetLane(objects[i]);
            if (lane < minLane)
            {
                first = objects[i];
                minLane = lane;
            }
        }
        return first;
    }

    // Of all the notes that are:
    // - before o in time (exclusive)
    // - of one of the specified types (any type if types is null)
    // - between lanes minLaneInclusive and maxLaneInclusive
    // Find and return the one closest to o. If there are
    // multiple such notes on the same pulse, which one to return
    // is undefined.
    public GameObject GetClosestNoteBefore(GameObject pivot,
        HashSet<NoteType> types, int minLaneInclusive, int maxLaneInclusive)
    {
        int pivotPulse = GetPulse(pivot);
        return GetClosestNoteBefore(pivotPulse, types, minLaneInclusive, maxLaneInclusive);
    }

    public GameObject GetClosestNoteBefore(int pivotPulse,
        HashSet<NoteType> types, int minLaneInclusive, int maxLaneInclusive)
    {
        if (pivotPulse <= populatedPulses.Min) return null;
        foreach (int pulse in populatedPulses.GetViewBetween(
            populatedPulses.Min, pivotPulse - 1)
            .Reverse())
        {
            foreach (GameObject o in pulseToNotes[pulse])
            {
                if (types != null &&
                    !types.Contains(
                    o.GetComponent<NoteObject>().note.type)) continue;
                int lane = GetLane(o);
                if (lane < minLaneInclusive) continue;
                if (lane > maxLaneInclusive) continue;
                return o;
            }
        }
        return null;
    }

    // Of all the notes that are:
    // - after o in time (exclusive)
    // - of one of the specified types (any type if types is null)
    // - between lanes minLaneInclusive and maxLaneInclusive
    // Find and return the one closest to o. If there are
    // multiple such notes on the same pulse, which one to return
    // is undefined.
    public GameObject GetClosestNoteAfter(GameObject pivot,
        HashSet<NoteType> types, int minLaneInclusive, int maxLaneInclusive)
    {
        int pivotPulse = GetPulse(pivot);
        return GetClosestNoteAfter(pivotPulse, types, minLaneInclusive, maxLaneInclusive);
    }

    public GameObject GetClosestNoteAfter(int pivotPulse,
        HashSet<NoteType> types, int minLaneInclusive, int maxLaneInclusive)
    {
        if (pivotPulse >= populatedPulses.Max) return null;
        foreach (int pulse in populatedPulses.GetViewBetween(
            pivotPulse + 1, populatedPulses.Max))
        {
            foreach (GameObject o in pulseToNotes[pulse])
            {
                if (types != null &&
                    !types.Contains(
                    o.GetComponent<NoteObject>().note.type)) continue;
                int lane = GetLane(o);
                if (lane < minLaneInclusive) continue;
                if (lane > maxLaneInclusive) continue;
                return o;
            }
        }
        return null;
    }

    // Returns notes are sorted by pulse; order within pulse undefined.
    public List<GameObject> GetAllNotesOfType(HashSet<NoteType> types,
        int minLaneInclusive, int maxLaneInclusive)
    {
        List<GameObject> answer = new List<GameObject>();
        foreach (int pulse in populatedPulses)
        {
            foreach (GameObject o in pulseToNotes[pulse])
            {
                if (!types.Contains(
                    o.GetComponent<NoteObject>().note.type)) continue;
                int lane = GetLane(o);
                if (lane < minLaneInclusive) continue;
                if (lane > maxLaneInclusive) continue;
                answer.Add(o);
            }
        }
        return answer;
    }

    public bool HasAt(int pulse, int lane)
    {
        return GetAt(pulse, lane) != null;
    }

    public void Add(GameObject o)
    {
        int pulse = GetPulse(o);
        if (!pulseToNotes.ContainsKey(pulse))
        {
            pulseToNotes.Add(pulse, new List<GameObject>());
            populatedPulses.Add(pulse);
        }
        pulseToNotes[pulse].Add(o);
    }

    // No-op if not found.
    public void Delete(GameObject o)
    {
        int pulse = GetPulse(o);
        if (!pulseToNotes.ContainsKey(pulse)) return;
        pulseToNotes[pulse].Remove(o);
        if (pulseToNotes[pulse].Count == 0)
        {
            pulseToNotes.Remove(pulse);
            populatedPulses.Remove(pulse);
        }
    }
}
