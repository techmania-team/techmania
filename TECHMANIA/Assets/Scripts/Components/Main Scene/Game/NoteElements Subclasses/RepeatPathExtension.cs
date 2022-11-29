using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepeatPathExtension
{
    private NoteElements noteElements;
    public RepeatPathElements path { get; private set; }

    public RepeatPathExtension(NoteElements noteElements, int intScan,
        int bps, GameLayout layout)
    {
        this.noteElements = noteElements;
        path = new RepeatPathElements(noteElements, intScan,
            bps, layout);
        noteElements.repeatPathAndExtensions.RegisterPathExtension(
            this);
    }

    public void Prepare()
    {
        if (noteElements.state == NoteElements.State.Resolved)
        {
            return;
        }
        path.SetVisibility(NoteElements.Visibility.Visible);
    }

    public void Activate()
    {
        if (noteElements.state == NoteElements.State.Resolved)
        {
            return;
        }
        path.SetVisibility(NoteElements.Visibility.Visible);
    }
}
