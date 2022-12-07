using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepeatPathExtension
{
    private NoteElements headElements;
    public RepeatPathElements path { get; private set; }

    public RepeatPathExtension(NoteElements headElements, int intScan,
        int bps, GameLayout layout)
    {
        this.headElements = headElements;
        path = new RepeatPathElements(headElements, intScan,
            bps, layout);
        headElements.repeatPathAndExtensions.RegisterPathExtension(
            this);
    }

    public void Prepare()
    {
        if (headElements.state == NoteElements.State.Resolved)
        {
            return;
        }
        path.SetVisibility(NoteElements.Visibility.Visible);
    }

    public void Activate()
    {
        if (headElements.state == NoteElements.State.Resolved)
        {
            return;
        }
        path.SetVisibility(NoteElements.Visibility.Visible);
    }
}
