using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class NoteManager
{
    public GameLayout layout;

    // The main data structure to hold note elements, indexed
    // by lane.
    public List<NoteList> notesInLane { get; private set; }

    // A few other ways to hold note elements:
    
    // Keyed by scan number, so we can prepare and activate
    // entire scans at once. Only holds playable notes.
    //
    // Organized as a dictionary because:
    // - An end-of-scan note at pulse 0 is considered to be in
    //   scan -1
    // - Empty scans shouldn't take up space
    //
    // Therefore, beware that some scans may not exist in this
    // dictionary.
    public Dictionary<int, List<NoteElements>> notesInScan
        { get; private set; }
    // Playable notes separated into mouse and keyboard notes,
    // indexed by lane.
    // In KM, each input device only cares about notes in its
    // corresponding list.
    public List<NoteList> mouseNotesInLane { get; private set; }
    public List<NoteList> keyboardNotesInLane { get; private set; }

    private int playableLanes;
    public int playableNotes { get; private set; }

    public NoteManager(GameLayout layout)
    {
        this.layout = layout;

        notesInScan = new Dictionary<int, List<NoteElements>>();
        notesInLane = new List<NoteList>();
        mouseNotesInLane = new List<NoteList>();
        keyboardNotesInLane = new List<NoteList>();
    }

    public void Prepare(Pattern p,
        GameController.NoteTemplates noteTemplates)
    {
        playableLanes = p.patternMetadata.playableLanes;
        for (int i = 0; i < Pattern.kMaxLane; i++)
        {
            notesInLane.Add(new NoteList());
        }
        for (int i = 0; i < playableLanes; i++)
        {
            mouseNotesInLane.Add(new NoteList());
            keyboardNotesInLane.Add(new NoteList());
        }

        playableNotes = 0;
        ChainNodeElements lastCreatedChainNode = null;
        //List<List<NoteObject>> unmanagedRepeatNotes =
        //    new List<List<NoteObject>>();

        // Spawn note elements in reverse order, so earlier notes
        // are drawn on top. However, the xyzInLane lists should still
        // be in the original order.
        foreach (Note n in p.notes.Reverse())
        {
            float floatScan = (float)n.pulse / Pattern.pulsesPerBeat
                / p.patternMetadata.bps;
            int intScan = n.GetScanNumber(p.patternMetadata.bps);
            int lane = n.lane;
            bool hidden = p.IsHidden(lane);

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

                noteElements = n.type switch
                {
                    NoteType.Basic => new BasicNoteElements(n),
                    NoteType.ChainHead => new ChainHeadElements(n),
                    NoteType.ChainNode => new ChainNodeElements(n),
                    // TODO: other note types.
                    _ => null
                };
                if (noteElements == null) continue;

                noteElements.Initialize(floatScan, intScan,
                    template, layout);
                layout.PlaceNoteElements(floatScan, intScan,
                    noteElements);

                // Count the number of playable notes; ScoreKeeper
                // needs this number.
                playableNotes++;

                // Connect chain head / node to the node after it.
                if (n.type == NoteType.ChainHead ||
                    n.type == NoteType.ChainNode)
                {
                    (noteElements as ChainElementsBase)
                        .SetNextChainNode(lastCreatedChainNode);
                    if (n.type == NoteType.ChainHead)
                    {
                        lastCreatedChainNode = null;
                    }
                    else  // ChainNode
                    {
                        lastCreatedChainNode = noteElements
                            as ChainNodeElements;
                    }
                }

                // TODO: Establish management between repeat (hold) heads
                // and repeat (hold) notes.
            }

            // Add to data structures.
            notesInLane[lane].Add(noteElements);
            if (!hidden)
            {
                if (!notesInScan.ContainsKey(intScan))
                {
                    notesInScan.Add(intScan, new List<NoteElements>());
                }
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
        foreach (List<NoteElements> list in notesInScan.Values)
        {
            list.Reverse();
        }
        for (int i = 0; i < Pattern.kMaxLane; i++)
        {
            notesInLane[i].Reverse();
        }
        for (int i = 0; i < playableLanes; i++)
        { 
            mouseNotesInLane[i].Reverse();
            keyboardNotesInLane[i].Reverse();
        }
    }

    public void ResetSize()
    {
        foreach (List<NoteElements> list in notesInScan.Values)
        {
            foreach (NoteElements elements in list)
            {
                elements.ResetSize();
            }
        }
    }

    public void Update(GameTimer timer, ScoreKeeper scoreKeeper)
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
                elements.UpdateTime(timer, scoreKeeper);
            }
        };
        updateNotesInScan(timer.IntScan);
        updateNotesInScan(timer.IntScan + 1);
    }

    public void ResolveNote(NoteElements elements)
    {
        int lane = elements.note.lane;
        notesInLane[elements.note.lane].Remove(elements);

        if (lane < playableLanes)
        {
            switch (elements.note.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.Drag:
                    mouseNotesInLane[lane].Remove(elements);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHead:
                case NoteType.RepeatHeadHold:
                case NoteType.Repeat:
                case NoteType.RepeatHold:
                    keyboardNotesInLane[lane].Remove(elements);
                    break;
            }
        }
    }

    public NoteElements GetUpcoming(int lane, ControlScheme scheme)
    {
        NoteList listToCheck = scheme switch
        {
            ControlScheme.Touch => notesInLane[lane],
            // Not actually used
            ControlScheme.Keys => keyboardNotesInLane[lane],
            ControlScheme.KM => mouseNotesInLane[lane],
            _ => null
        };
        if (listToCheck.IsEmpty()) return null;
        return listToCheck.First() as NoteElements;
    }

    public void Dispose()
    {
        notesInScan.Clear();
        notesInLane.Clear();
        mouseNotesInLane.Clear();
        keyboardNotesInLane.Clear();
    }
}
