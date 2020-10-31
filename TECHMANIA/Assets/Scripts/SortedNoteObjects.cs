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
    // private List<List<GameObject>> list;

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

    public List<GameObject> GetAt(int pulse)
    {
        if (!pulseToNotes.ContainsKey(pulse)) return null;
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
            List<GameObject> potentialObjects = GetAt(pulse);
            if (potentialObjects == null) continue;
            foreach (GameObject o in potentialObjects)
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
