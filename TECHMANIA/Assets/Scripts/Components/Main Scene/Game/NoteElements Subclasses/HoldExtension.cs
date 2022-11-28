using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HoldExtension
{
    private NoteElements noteElements;
    public HoldTrailElements trail { get; private set; }

    public HoldExtension(NoteElements noteElements, int intScan,
        int bps, GameLayout layout)
    {
        this.noteElements = noteElements;
        trail = new HoldTrailElements(noteElements, intScan,
            bps, layout);
        noteElements.holdTrailAndExtensions.RegisterHoldExtension(
            this);
    }

    public void Prepare()
    {
        switch (noteElements.state)
        {
            case NoteElements.State.Inactive:
            case NoteElements.State.Resolved:
            case NoteElements.State.PendingResolve:
                return;
        }
        if (noteElements.note.type == NoteType.Hold)
        {
            trail.SetVisibility(NoteElements.Visibility.Transparent);
        }
        else
        {
            trail.SetVisibility(NoteElements.Visibility.Visible);
        }
    }

    public void Activate()
    {
        switch (noteElements.state)
        {
            case NoteElements.State.Inactive:
            case NoteElements.State.Resolved:
            case NoteElements.State.PendingResolve:
                return;
        }
        trail.SetVisibility(NoteElements.Visibility.Visible);
    }
}
