using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CustomTransitionFromEditSetlistPanel : TransitionToPanel
{
    public ConfirmDialog confirmDialog;

    public override void Invoke()
    {
        // TODO: check for the number of patterns.
        Transition();
    }

    public void Transition()
    {
        // TODO: Reload setlist into setlist list.

        PanelTransitioner.TransitionTo(null, Direction.Left,
            callbackOnFinish: EditorContext.exitCallback);
    }
}
