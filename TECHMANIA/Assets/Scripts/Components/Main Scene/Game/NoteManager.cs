using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class NoteManager
{
    private GameLayout layout;

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

    // Extensions.
    private Dictionary<int, List<HoldExtension>> holdExtensionsInScan;
    private Dictionary<int, List<RepeatPathExtension>>
        repeatPathExtensionsInScan;

    private int playableLanes;
    public int playableNotes { get; private set; }

    public NoteManager(GameLayout layout)
    {
        this.layout = layout;

        notesInScan = new Dictionary<int, List<NoteElements>>();
        notesInLane = new List<NoteList>();
        mouseNotesInLane = new List<NoteList>();
        keyboardNotesInLane = new List<NoteList>();
        holdExtensionsInScan = new Dictionary<
            int, List<HoldExtension>>();
        repeatPathExtensionsInScan = new Dictionary<
            int, List<RepeatPathExtension>>();
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
        List<List<RepeatNoteElementsBase>> unmanagedRepeatNotesInLane 
            = new List<List<RepeatNoteElementsBase>>();
        for (int i = 0; i < playableLanes; i++)
        {
            unmanagedRepeatNotesInLane.Add(new 
                List<RepeatNoteElementsBase>());
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
                if (template == null) continue;

                noteElements = n.type switch
                {
                    NoteType.Basic => new BasicNoteElements(n),
                    NoteType.ChainHead => new ChainHeadElements(n),
                    NoteType.ChainNode => new ChainNodeElements(n),
                    NoteType.Drag => new DragNoteElements(n),
                    NoteType.Hold => new HoldNoteElements(n),
                    NoteType.RepeatHead => new RepeatHeadElements(n),
                    NoteType.RepeatHeadHold => new 
                        RepeatHeadHoldElements(n),
                    NoteType.Repeat => new RepeatNoteElements(n),
                    NoteType.RepeatHold => new RepeatHoldElements(n),
                    _ => null
                };
                if (noteElements == null) continue;

                noteElements.Initialize(floatScan, intScan, 
                    p, template, layout);
                layout.PlaceNoteElements(floatScan, intScan,
                    noteElements);

                // Count the number of playable notes; ScoreKeeper
                // needs this number.
                playableNotes++;

                // Spawn hold extensions if necessary.
                if (n.type == NoteType.Hold ||
                    n.type == NoteType.RepeatHeadHold ||
                    n.type == NoteType.RepeatHold)
                {
                    SpawnHoldExtensions(noteElements, noteTemplates,
                        intScan, p.patternMetadata.bps);
                }

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

                // Establish management between repeat (hold) heads
                // and repeat (hold) notes.
                if (n.type == NoteType.Repeat ||
                    n.type == NoteType.RepeatHold)
                {
                    unmanagedRepeatNotesInLane[n.lane].Add(
                        noteElements as RepeatNoteElementsBase);
                }
                else if (n.type == NoteType.RepeatHead ||
                    n.type == NoteType.RepeatHeadHold)
                {
                    List<RepeatNoteElementsBase> unmanagedNotes =
                        unmanagedRepeatNotesInLane[n.lane];
                    unmanagedNotes.Reverse();
                    ManageRepeatNotes(
                        noteElements as RepeatHeadElementsBase,
                        unmanagedNotes,
                        noteTemplates.repeatPathExtension,
                        intScan, p.patternMetadata.bps);
                    unmanagedNotes.Clear();
                }
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

    private void SpawnHoldExtensions(NoteElements elements,
        GameController.NoteTemplates noteTemplates,
        int intScan, int bps)
    {
        HoldNote holdNote = elements.note as HoldNote;
        VisualTreeAsset template = noteTemplates
            .GetHoldExtensionForType(holdNote.type);

        // Which scan does this note end on?
        // If a hold note ends at a scan divider, we don't
        // want to spawn an unnecessary extension, thus the
        // -1.
        int pulsesPerScan = bps * Pattern.pulsesPerBeat;
        int lastScan = (holdNote.pulse + holdNote.duration - 1)
            / pulsesPerScan;

        for (int scan = intScan + 1; scan <= lastScan; scan++)
        {
            TemplateContainer templateContainer =
                template.Instantiate();

            HoldExtension extension = new HoldExtension(elements,
                scan, bps, layout);
            if (!holdExtensionsInScan.ContainsKey(scan))
            {
                holdExtensionsInScan.Add(scan,
                    new List<HoldExtension>());
            }
            holdExtensionsInScan[scan].Add(extension);

            extension.trail.Initialize(templateContainer);
            extension.trail.InitializeSize();
            layout.PlaceExtension(scan, elements.note.lane,
                templateContainer);
        }
    }

    private void ManageRepeatNotes(RepeatHeadElementsBase head,
        List<RepeatNoteElementsBase> unmanagedNotes,
        VisualTreeAsset pathExtensionTemplate,
        int intScanOfHead, int bps)
    {
        head.ManageRepeatNotes(unmanagedNotes);
        NoteElements lastManagedNote = head;
        if (unmanagedNotes.Count > 0)
        {
            lastManagedNote = unmanagedNotes[^1];
        }

        // To spawn repeat path extensions, first calculate which
        // scan the last managed note is / ends on.
        int pulseOfLastManagedNote = lastManagedNote.note.pulse;
        int pulsesPerScan = bps * Pattern.pulsesPerBeat;
        bool decrementedPulse = false;
        if (lastManagedNote.note.type == NoteType.RepeatHeadHold ||
            lastManagedNote.note.type == NoteType.RepeatHold)
        {
            pulseOfLastManagedNote +=
                (lastManagedNote.note as HoldNote).duration;
            // If a hold note ends at a scan divider, we don't
            // want to spawn an unnecessary extension, thus the
            // -1.
            pulseOfLastManagedNote--;
            decrementedPulse = true;
        }
        else
        {
            // If a repeat note ends at a scan divider and is
            // end-of-scan, we don't want to spawn an unnecessary
            // extension, thus the -1.
            if (pulseOfLastManagedNote % pulsesPerScan == 0 &&
                lastManagedNote.note.endOfScan)
            {
                pulseOfLastManagedNote--;
                decrementedPulse = true;
            }
        }
        int intScanOfLastManagedNote = pulseOfLastManagedNote
            / pulsesPerScan;
        if (decrementedPulse) pulseOfLastManagedNote++;

        // Now we can spawn repeat extensions.
        for (int scan = intScanOfHead + 1;
            scan <= intScanOfLastManagedNote; scan++)
        {
            TemplateContainer templateContainer =
                pathExtensionTemplate.Instantiate();

            RepeatPathExtension extension = new RepeatPathExtension(
                head, scan, bps, layout);
            if (!repeatPathExtensionsInScan.ContainsKey(scan))
            {
                repeatPathExtensionsInScan.Add(scan,
                    new List<RepeatPathExtension>());
            }
            repeatPathExtensionsInScan[scan].Add(extension);

            extension.path.Initialize(templateContainer);
            layout.PlaceExtension(scan, head.note.lane,
                templateContainer);
        }

        // Initialize the repeat path and all extensions regarding
        // the last managed note, all at once.
        head.repeatPathAndExtensions
            .InitializeWithLastManagedNote(
            pulseOfLastManagedNote, intScanOfLastManagedNote);
        head.repeatPathAndExtensions.InitializeSize();
        head.repeatPathAndExtensions
            .PlaceAllPathsBehindManagedNotes(unmanagedNotes);
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
        // Put notes and extensions in the upcoming scan in
        // Prepare state if needed.
        if (timer.PrevFrameIntScan != timer.IntScan)
        {
            int nextScan = timer.IntScan + 1;
            if (notesInScan.ContainsKey(nextScan))
            {
                notesInScan[nextScan].ForEach(
                    e => e.Prepare());
            }
            if (holdExtensionsInScan.ContainsKey(nextScan))
            {
                holdExtensionsInScan[nextScan].ForEach(
                    e => e.Prepare());
            }
            if (repeatPathExtensionsInScan.ContainsKey(nextScan))
            {
                repeatPathExtensionsInScan[nextScan].ForEach(
                    e => e.Prepare());
            }
        }

        // Put notes and extensions in the upcoming scan in
        // Active state if needed.
        float relativeScan = timer.Scan - timer.IntScan;
        float prevFrameRelativeScan = timer.PrevFrameScan -
            timer.PrevFrameIntScan;
        if (relativeScan >= 0.875f && prevFrameRelativeScan < 0.875f)
        {
            int nextScan = timer.IntScan + 1;
            if (notesInScan.ContainsKey(nextScan))
            {
                notesInScan[nextScan].ForEach(
                    e => e.Activate());
            }
            if (holdExtensionsInScan.ContainsKey(nextScan))
            {
                holdExtensionsInScan[nextScan].ForEach(
                    e => e.Activate());
            }
            if (repeatPathExtensionsInScan.ContainsKey(nextScan))
            {
                repeatPathExtensionsInScan[nextScan].ForEach(
                    e => e.Activate());
            }
        }

        // Update notes with time, mainly to update sprites.
        for (int scan = timer.IntScan - 2;
            scan <= timer.IntScan + 2;
            scan++)
        {
            if (!notesInScan.ContainsKey(scan)) continue;
            notesInScan[scan].ForEach(e =>
                e.UpdateTime(timer, scoreKeeper));
        }
    }

    public void JumpToScan(int scan, int pulse)
    {
        // Reset note lists.
        System.Action<List<NoteList>> resetLists =
            (List<NoteList> listOfList) =>
            {
                foreach (NoteList l in listOfList)
                {
                    l.Reset();
                    l.RemoveUpTo(pulse);
                }
            };
        resetLists(notesInLane);
        resetLists(keyboardNotesInLane);
        resetLists(mouseNotesInLane);

        // Reset states of NoteElements.
        foreach (KeyValuePair<int, List<NoteElements>> pair in 
            notesInScan)
        {
            int thisScan = pair.Key;
            foreach (NoteElements e in pair.Value)
            {
                e.ResetToInactive();
                if (scan > thisScan)
                {
                    e.Resolve();
                }
                else if (scan == thisScan)
                {
                    e.Activate();
                }
                else if (scan == thisScan - 1)
                {
                    e.Prepare();
                }
            }
        }

        // Reset states of extensions.
        foreach (KeyValuePair<int, List<HoldExtension>> pair in
            holdExtensionsInScan)
        {
            int thisScan = pair.Key;
            if (scan == thisScan)
            {
                pair.Value.ForEach(e => e.Activate());
            }
            else if (scan == thisScan - 1)
            {
                pair.Value.ForEach(e => e.Prepare());
            }
        }
        foreach (KeyValuePair<int, List<RepeatPathExtension>> pair in
            repeatPathExtensionsInScan)
        {
            int thisScan = pair.Key;
            if (scan == thisScan)
            {
                pair.Value.ForEach(e => e.Activate());
            }
            else if (scan == thisScan - 1)
            {
                pair.Value.ForEach(e => e.Prepare());
            }
        }
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
