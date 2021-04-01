using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CustomTransitionFromTrackSetupPanel : TransitionToPanel
{
    public ConfirmDialog confirmDialog;

    public override void Invoke()
    {
        if (EditorContext.Dirty)
        {
            confirmDialog.Show(
                "Unsaved changes to the track will be discarded. Continue?",
                "discard",
                "cancel",
                ForceTransition);
        }
        else
        {
            ForceTransition();
        }
    }

    public void ForceTransition()
    {
        PanelTransitioner.TransitionTo(target, targetAppearsFrom);
    }
}
