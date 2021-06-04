using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A custom data structure for storing notes in Game.
// - Elements are set once, and then never removed.
// - Each element can be active or inactive. Initially all elements
//   are active. Removing an element sets it as inactive, instead
//   of actually removing, this way we can easily reset the NoteList
//   to the initial state.
// - A cursor always points to the first active element.
public class NoteList
{
    private List<NoteObject> list;
    private List<bool> active;
    private int first;
    private int count;
    public int Count { get { return count; } }

    #region Initialize
    public NoteList()
    {
        list = new List<NoteObject>();
        active = new List<bool>();
        count = 0;
        first = 0;
    }

    public void Add(NoteObject n)
    {
        list.Add(n);
        active.Add(true);
        count++;
    }

    // After calling this, we assume notes are sorted by pulse.
    public void Reverse()
    {
        list.Reverse();
    }
    #endregion

    public void Remove(NoteObject n)
    {
        for (int i = first; i < list.Count; i++)
        {
            if (list[i] == n)
            {
                active[i] = false;
                count--;
                if (i == first)
                {
                    do
                    { first++; }
                    while (first < list.Count && !active[first]);
                }
                return;
            }
        }
    }

    // For notes whose pulse is equal to pulseThreshold, they will
    // be removed if end-of-scan, and vice versa.
    public void RemoveUpTo(int pulseThreshold)
    {
        for (int i = 0; i < list.Count; i++)
        {
            bool beforeThreshold;
            if (list[i].note.pulse < pulseThreshold)
            {
                beforeThreshold = true;
            }
            else if (list[i].note.pulse == pulseThreshold)
            {
                beforeThreshold = list[i].note.endOfScan;
            }
            else
            {
                beforeThreshold = false;
            }

            if (beforeThreshold && active[i])
            {
                active[i] = false;
                count--;
            }
            if (!beforeThreshold)
            {
                first = i;
                return;
            }
        }
        first = list.Count;
    }

    public NoteObject First()
    {
        if (first >= list.Count)
        {
            throw new System.Exception("Attempting to get first element from an empty NoteList.");
        }
        return list[first];
    }

    public void Reset()
    {
        count = list.Count;
        for (int i = 0; i < Count; i++) active[i] = true;
    }
}
