using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Pattern
{
    // Inspect the pattern for issues not caught at edit time.
    //
    // If found, copies the notes that cause the issue to
    // notesWithIssue, and returns an error message.
    // If not found, returns null.
    public string Inspect(List<Note> notesWithIssue)
    {
        int bps = patternMetadata.bps;
        int pulsesPerScan = bps * pulsesPerBeat;

        // Collect notes of various types for easier enumeration.

        List<Note> chainHeadsAndNodes = new List<Note>();
        List<DragNote> dragNotes = new List<DragNote>();
        List<HoldNote> holdNotes = new List<HoldNote>();
        List<Note> mouseNotes = new List<Note>();
        // Element 0 in each element list should be a head.
        List<List<Note>> repeatSeries = new List<List<Note>>();
        // Indexed by lane.
        List<List<Note>> currentRepeatSeries = new List<List<Note>>();
        for (int i = 0; i < patternMetadata.playableLanes; i++)
        {
            currentRepeatSeries.Add(new List<Note>());
        }
        foreach (Note n in notes)
        {
            if (n.lane < 0 || n.lane >= patternMetadata.playableLanes)
            {
                continue;
            }

            switch (n.type)
            {
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                    chainHeadsAndNodes.Add(n);
                    break;
                case NoteType.Drag:
                    dragNotes.Add(n as DragNote);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    holdNotes.Add(n as HoldNote);
                    break;
            }

            List<Note> series = currentRepeatSeries[n.lane];
            switch (n.type)
            {
                case NoteType.RepeatHead:
                case NoteType.RepeatHeadHold:
                    if (series.Count > 0)
                    {
                        repeatSeries.Add(series);
                    }
                    currentRepeatSeries[n.lane] =
                        new List<Note>() { n };
                    break;
                case NoteType.Repeat:
                case NoteType.RepeatHold:
                    series.Add(n);
                    break;
            }

            switch (n.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.Drag:
                    mouseNotes.Add(n);
                    break;
            }
        }
        foreach (List<Note> s in currentRepeatSeries)
        {
            if (s.Count > 0) repeatSeries.Add(s);
        }

        // Chain heads and nodes.

        if (chainHeadsAndNodes.Count > 0 &&
            chainHeadsAndNodes[0].type == NoteType.ChainNode)
        {
            notesWithIssue.Add(chainHeadsAndNodes[0]);
            return Locale.GetString(
                "pattern_inspection_chain_node_with_no_head");
        }
        for (int i = 0; i < chainHeadsAndNodes.Count; i++)
        {
            if (chainHeadsAndNodes[i].type == NoteType.ChainHead &&
                (i == chainHeadsAndNodes.Count - 1 ||
                chainHeadsAndNodes[i + 1].type == NoteType.ChainHead))
            {
                notesWithIssue.Add(chainHeadsAndNodes[i]);
                return Locale.GetString(
                    "pattern_inspection_chain_head_with_no_node");
            }
            if (chainHeadsAndNodes[i].type == NoteType.ChainNode &&
                Mathf.Abs(chainHeadsAndNodes[i - 1].lane -
                    chainHeadsAndNodes[i].lane) != 1)
            {
                notesWithIssue.Add(chainHeadsAndNodes[i]);
                return Locale.GetString(
                    "pattern_inspection_chain_node_not_in_adjacent_lane");
            }
            if (chainHeadsAndNodes[i].type == NoteType.ChainNode &&
                chainHeadsAndNodes[i - 1].GetScanNumber(bps) !=
                chainHeadsAndNodes[i].GetScanNumber(bps))
            {
                notesWithIssue.Add(chainHeadsAndNodes[i]);
                return Locale.GetString(
                    "pattern_inspection_chain_node_crosses_scans");
            }
        }

        // Drag notes.

        foreach (DragNote n in dragNotes)
        {
            List<FloatPoint> points = n.Interpolate();
            foreach (FloatPoint p in points)
            {
                if (patternMetadata.controlScheme ==
                    ControlScheme.Keys &&
                    Mathf.Abs(p.lane) > Mathf.Epsilon)
                {
                    notesWithIssue.Add(n);
                    return Locale.GetString(
                        "pattern_inspection_drag_leaves_lane_in_keys");
                }
                float lane = p.lane + n.lane;
                if (lane < -0.5f ||
                    lane >= patternMetadata.playableLanes + 0.5f)
                {
                    notesWithIssue.Add(n);
                    return Locale.GetString(
                        "pattern_inspection_drag_leaves_playable_lanes");
                }
            }

            int lastPointPulse = Mathf.FloorToInt(
                points[points.Count - 1].pulse) + n.pulse;
            int startScan = n.pulse / pulsesPerScan;
            // Ending on a scan divider is allowed.
            int endScan = (lastPointPulse - 1) / pulsesPerScan;
            if (startScan != endScan)
            {
                notesWithIssue.Add(n);
                return Locale.GetString(
                    "pattern_inspection_drag_crosses_scans");
            }
        }

        // Holds.

        foreach (HoldNote n in holdNotes)
        {
            if (n.duration >= pulsesPerScan * 2)
            {
                notesWithIssue.Add(n);
                return Locale.GetString(
                    "pattern_inspection_hold_longer_than_two_scans");
            }
        }

        // Repeat series.

        foreach (List<Note> s in repeatSeries)
        {
            if (s[0].type == NoteType.Repeat ||
                s[0].type == NoteType.RepeatHold)
            {
                notesWithIssue.Add(s[0]);
                return Locale.GetString(
                    "pattern_inspection_repeat_note_with_no_head");
            }
            Note head = s[0];
            if (s.Count == 1)
            {
                notesWithIssue.Add(head);
                return Locale.GetString(
                    "pattern_inspection_repeat_head_with_no_repeat");
            }
            for (int i = 1; i < s.Count; i++)
            {
                Note current = s[i];
                int endPulse = current.pulse;
                if (current.type == NoteType.RepeatHold)
                {
                    endPulse += (current as HoldNote).duration;
                }
                if (endPulse - head.pulse >= pulsesPerScan * 2)
                {
                    notesWithIssue.Add(current);
                    return Locale.GetString(
                        "pattern_inspection_repeat_path_longer_than_two_scans");
                }

                Note prev = s[i - 1];
                if (GetRangeBetween(prev, current).Count > 2)
                {
                    notesWithIssue.Add(prev);
                    notesWithIssue.Add(current);
                    return Locale.GetString(
                        "pattern_inspection_repeat_path_covers_notes");
                }
            }
        }

        // Special KM rules.

        if (patternMetadata.controlScheme != ControlScheme.KM)
            return null;

        dragNotes.Sort((DragNote n1, DragNote n2) =>
        {
            return (n1.pulse + n1.Duration()) -
                (n2.pulse + n2.Duration());
        });
        for (int i = 0; i < mouseNotes.Count; i++)
        {
            if (i > 0 &&
                mouseNotes[i].pulse == mouseNotes[i - 1].pulse)
            {
                notesWithIssue.Add(mouseNotes[i - 1]);
                notesWithIssue.Add(mouseNotes[i]);
                return Locale.GetString(
                    "pattern_inspection_mouse_notes_at_same_pulse_in_km");
            }

            // This bit is technically O(n^2) but we should be fine?
            foreach (DragNote d in dragNotes)
            {
                if (d == mouseNotes[i]) continue;
                if (mouseNotes[i].pulse > d.pulse &&
                    mouseNotes[i].pulse <=
                        (d.pulse + d.Duration()))
                {
                    notesWithIssue.Add(mouseNotes[i]);
                    return Locale.GetString(
                        "pattern_inspection_mouse_notes_during_drag_in_km");
                }
            }
        }

        return null;
    }

    // Check if it's valid to add a note of specified type,
    // pulse and lane. A limitation specific to the editor is that
    // lane must be <totalLanes.
    // If ignoredExistingNotes is not empty, will ignore these
    // notes when checking.
    //
    // If valid, sets reason to null and returns true.
    // If invalid, sets reason to a string and returns false.
    public bool CanAddNote(NoteType type,
        int pulse, int lane, int totalLanes,
        HashSet<Note> ignoredExistingNotes,
        out string reason)
    {
        if (ignoredExistingNotes == null)
        {
            ignoredExistingNotes = new HashSet<Note>();
        }

        // Boundary check.
        if (pulse < 0)
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_before_scan_0");
            return false;
        }
        if (lane < 0)
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_above_topmost_lane");
            return false;
        }
        if (lane >= totalLanes)
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_below_bottommost_lane");
            return false;
        }

        // Overlap check.
        Note noteAtSamePulseAndLane = GetNoteAt(pulse, lane);
        if (noteAtSamePulseAndLane != null &&
            !ignoredExistingNotes.Contains(noteAtSamePulseAndLane))
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_on_top_of_existing_note");
            return false;
        }

        // Chain check.
        if (type == NoteType.ChainHead || type == NoteType.ChainNode)
        {
            foreach (Note noteAtPulse in GetViewBetween(pulse, pulse))
            {
                if (ignoredExistingNotes.Contains(noteAtPulse))
                {
                    continue;
                }

                if (noteAtPulse.type == NoteType.ChainHead ||
                    noteAtPulse.type == NoteType.ChainNode)
                {
                    reason = Locale.GetString(
                        "pattern_panel_snackbar_chain_note_at_same_pulse");
                    return false;
                }
            }
        }

        // Hold check.
        Note holdNoteBeforePivot =
            GetClosestNoteBefore(pulse,
            new HashSet<NoteType>()
            {
                NoteType.Hold,
                NoteType.RepeatHeadHold,
                NoteType.RepeatHold
            }, minLaneInclusive: lane, maxLaneInclusive: lane);
        if (holdNoteBeforePivot != null &&
            !ignoredExistingNotes.Contains(holdNoteBeforePivot))
        {
            HoldNote holdNote = holdNoteBeforePivot as HoldNote;
            if (holdNote.pulse + holdNote.duration >= pulse)
            {
                reason = Locale.GetString(
                    "pattern_panel_snackbar_covered_by_hold_note");
                return false;
            }
        }

        reason = null;
        return true;
    }

    public bool CanAddHoldNote(NoteType type,
        int pulse, int lane, int totalLanes, int duration,
        HashSet<Note> ignoredExistingNotes,
        out string reason)
    {
        if (ignoredExistingNotes == null)
        {
            ignoredExistingNotes = new HashSet<Note>();
        }
        if (!CanAddNote(type, pulse, lane, totalLanes,
            ignoredExistingNotes, out reason))
        {
            return false;
        }

        // Additional check for hold notes.
        if (HoldNoteCoversAnotherNote(pulse, lane, duration,
            ignoredExistingNotes))
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_hold_note_covers_other_notes");
            return false;
        }

        reason = null;
        return true;
    }

    public bool CanAdjustHoldNoteDuration(HoldNote holdNote,
        int newDuration, out string reason)
    {
        if (newDuration <= 0)
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_hold_note_zero_length");
            return false;
        }
        if (HoldNoteCoversAnotherNote(
            holdNote.pulse, holdNote.lane,
            newDuration, ignoredExistingNotes: null))
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_hold_note_covers_other_notes");
            return false;
        }
        reason = null;
        return true;
    }

    public bool CanAddDragAnchor(DragNote dragNote,
        float relativePulse, out string reason)
    {
        if (dragNote.nodes.Find((DragNode node) =>
        {
            return Mathf.Abs(node.anchor.pulse - relativePulse) <
                Mathf.Epsilon;
        }) != null)
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_anchor_too_close_to_existing");
            return false;
        }
        reason = null;
        return true;
    }

    public bool CanDeleteDragAnchor(DragNote dragNote,
        int anchorIndex, out string reason)
    {
        if (anchorIndex == 0)
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_cannot_delete_first_anchor");
            return false;
        }
        if (dragNote.nodes.Count == 2)
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_at_least_two_anchors");
            return false;
        }
        reason = null;
        return true;
    }

    public bool CanEditDragNote(DragNote dragNoteAfterEdit,
        out string reason)
    {
        List<FloatPoint> points = dragNoteAfterEdit.Interpolate();
        for (int i = 0; i < points.Count - 1; i++)
        {
            if (points[i + 1].pulse < points[i].pulse)
            {
                reason = Locale.GetString(
                    "pattern_panel_snackbar_drag_flows_left");
                return false;
            }
        }
        reason = null;
        return true;
    }

    // Ignores notes at (pulse, lane), if any.
    public bool HoldNoteCoversAnotherNote(int pulse, int lane,
        int duration,
        HashSet<Note> ignoredExistingNotes)
    {
        if (ignoredExistingNotes == null)
        {
            ignoredExistingNotes = new HashSet<Note>();
        }

        Note noteAfterPivot = GetClosestNoteAfter(
            pulse, types: null,
            minLaneInclusive: lane,
            maxLaneInclusive: lane);
        if (noteAfterPivot != null &&
            !ignoredExistingNotes.Contains(noteAfterPivot))
        {
            if (pulse + duration >= noteAfterPivot.pulse)
            {
                return true;
            }
        }

        return false;
    }
}
