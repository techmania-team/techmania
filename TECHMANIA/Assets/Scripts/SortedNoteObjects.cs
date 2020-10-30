using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A list of lists of GameObjects, each with a NoteObject.
// Outer list is sorted and indexed by pulse; inner list is
// not sorted, and may be empty, null, or nonexistant.
//
// This class does not interact with Notes in any way.
//
// TODO: make this a list of NoteObjects instead of GameObjects.
public class SortedNoteObjects
{
    private List<List<GameObject>> list;
    public SortedNoteObjects()
    {
        list = new List<List<GameObject>>();
    }

    public int GetMaxPulse()
    {
        return list.Count - 1;
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
        if (list.Count < pulse + 1) return null;
        return list[pulse];
    }

    public GameObject GetAt(int pulse, int lane)
    {
        if (list.Count < pulse + 1) return null;
        if (list[pulse] == null) return null;
        return list[pulse].Find((GameObject o) =>
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

        for (int pulse = minPulse; pulse <= maxPulse; pulse++)
        {
            if (list.Count < pulse + 1) continue;
            if (list[pulse] == null) continue;
            foreach (GameObject o in list[pulse])
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
        for (int pulse = 0; pulse < list.Count; pulse++)
        {
            if (list[pulse] == null) continue;
            if (list[pulse].Count == 0) continue;
            return list[pulse][0];
        }
        return null;
    }

    public GameObject GetLast()
    {
        for (int pulse = list.Count - 1; pulse >= 0; pulse--)
        {
            if (list[pulse] == null) continue;
            if (list[pulse].Count == 0) continue;
            return list[pulse][0];
        }
        return null;
    }

    public bool HasAt(int pulse, int lane)
    {
        return GetAt(pulse, lane) != null;
    }

    public void Add(GameObject o)
    {
        int pulse = GetPulse(o);
        while (list.Count < pulse + 1)
        {
            list.Add(null);
        }
        if (list[pulse] == null)
        {
            list[pulse] = new List<GameObject>();
        }
        list[pulse].Add(o);
    }

    // No-op if not found.
    public void Delete(GameObject o)
    {
        int pulse = GetPulse(o);
        if (list.Count < pulse + 1) return;
        list[pulse].Remove(o);
    }

    // No-op if no note exists at (pulse, lane).
    public void DeleteAt(int pulse, int lane)
    {
        GameObject o = GetAt(pulse, lane);
        if (o == null) return;
        list[pulse].Remove(o);
    }
}
