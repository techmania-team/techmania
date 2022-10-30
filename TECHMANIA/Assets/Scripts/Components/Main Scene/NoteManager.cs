using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class NoteManager
{
    public GameLayout layout;

    private class NoteList : NoteList<NoteElements> { }
    // The main data structure to hold note elements, indexed
    // by lane.
    private List<NoteList> notesInLane;

    // A few other ways to hold note elements:
    
    // Keyed by scan number, so we can prepare and activate
    // entire scans at once. Only holds playable notes.
    private Dictionary<int, List<NoteElements>> notesInScan;
    // Playable notes separated into mouse and keyboard notes,
    // indexed by lane.
    // In KM, each input device only cares about notes in its
    // corresponding list.
    private List<NoteList> mouseNotesInLane;
    private List<NoteList> keyboardNotesInLane;

    public NoteManager(GameLayout layout)
    {
        this.layout = layout;

        notesInScan = new Dictionary<int, List<NoteElements>>();
        notesInLane = new List<NoteList>();
        mouseNotesInLane = new List<NoteList>();
        keyboardNotesInLane = new List<NoteList>();
    }

    public void Prepare(Pattern p, int lastScan,
        GameController.NoteTemplates noteTemplates)
    {
        for (int i = -1; i < lastScan; i++)
        {
            // An end-of-scan note on pulse 0 is considered to be
            // in scan -1.
            notesInScan.Add(i, new List<NoteElements>());
        }
        for (int i = 0; i < Pattern.kMaxLane; i++)
        {
            notesInLane.Add(new NoteList());
        }
        for (int i = 0; i < p.patternMetadata.playableLanes; i++)
        {
            mouseNotesInLane.Add(new NoteList());
            keyboardNotesInLane.Add(new NoteList());
        }

        // Spawn note elements in reverse order, so earlier notes
        // are drawn on top. However, the xyzInLane lists should still
        // be in the original order.
        foreach (Note n in p.notes.Reverse())
        {
            float floatScan = (float)n.pulse / Pattern.pulsesPerBeat
                / p.patternMetadata.bps;
            int intScan = n.GetScanNumber(p.patternMetadata.bps);
            int lane = n.lane;
            bool hidden = p.IsHiddenNote(lane);

            // Ignore silent hidden notes.
            if (hidden && string.IsNullOrEmpty(n.sound))
            {
                continue;
            }

            NoteElements noteElements;
            if (hidden)
            {
                noteElements = new NoteElements(n);
            }
            else
            {
                // Spawn elements for playable notes.
                TemplateContainer template =
                    noteTemplates.GetForType(n.type)?.Instantiate();

                bool supportedType = true;
                switch (n.type)
                {
                    case NoteType.Basic:
                        noteElements = new BasicNoteElements(n);
                        break;
                    // TODO: other note types.
                    default:
                        noteElements = null;
                        supportedType = false;
                        break;
                }
                if (!supportedType) continue;
                noteElements.Initialize(floatScan, intScan,
                    template, layout);
                layout.PlaceNoteElements(floatScan, intScan,
                    noteElements);
            }

            // Add to data structures.
            notesInLane[lane].Add(noteElements);
            if (!hidden)
            {
                notesInScan[intScan].Add(noteElements);
                switch (n.type)
                {
                    case NoteType.Basic:
                    case NoteType.ChainHead:
                    case NoteType.ChainNode:
                    case NoteType.Drag:
                        mouseNotesInLane[lane].Add(noteElements);
                        break;
                    case NoteType.Hold:
                    case NoteType.RepeatHead:
                    case NoteType.RepeatHeadHold:
                    case NoteType.Repeat:
                    case NoteType.RepeatHold:
                        keyboardNotesInLane[lane].Add(noteElements);
                        break;
                }
            }
        }
        for (int i = 0; i < Pattern.kMaxLane; i++)
        {
            notesInLane[i].Reverse();
        }
        for (int i = 0; i < p.patternMetadata.playableLanes; i++)
        { 
            mouseNotesInLane[i].Reverse();
            keyboardNotesInLane[i].Reverse();
        }
    }

    public void ResetAspectRatio()
    {
        foreach (List<NoteElements> list in notesInScan.Values)
        {
            foreach (NoteElements elements in list)
            {
                elements.ResetAspectRatio();
            }
        }
    }

    public void Update(GameTimer timer)
    {
        // Put notes in the upcoming scan in Prepare state if needed.
        if (notesInScan.ContainsKey(timer.IntScan + 1))
        {
            foreach (NoteElements elements in
                notesInScan[timer.IntScan + 1])
            {
                if (elements.state == NoteElements.State.Inactive)
                {
                    elements.Prepare();
                }
            }
        }

        // Put notes in the upcoming scan in Active state if needed.
        float relativeScan = timer.Scan - timer.IntScan;
        if (relativeScan > 0.875f &&
            notesInScan.ContainsKey(timer.IntScan + 1))
        {
            foreach (NoteElements elements in
                notesInScan[timer.IntScan + 1])
            {
                if (elements.state == NoteElements.State.Prepare)
                {
                    elements.Activate();
                }
            }
        }

        // Update notes with time, mainly to update sprites.
        System.Action<int> updateNotesInScan = (int scan) =>
        {
            if (!notesInScan.ContainsKey(scan)) return;
            foreach (NoteElements elements in notesInScan[scan])
            {
                elements.UpdateTime(timer);
            }
        };
        updateNotesInScan(timer.IntScan);
        updateNotesInScan(timer.IntScan + 1);
    }

    public void Dispose()
    {
        notesInScan.Clear();
        notesInLane.Clear();
        mouseNotesInLane.Clear();
        keyboardNotesInLane.Clear();
    }
}
