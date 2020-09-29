using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TransitionToPanelWhenNotDirty : TransitionToPanel
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
                () =>
                {
                    PanelTransitioner.TransitionTo(target, targetAppearsFrom);
                });
        }
        else
        {
            PanelTransitioner.TransitionTo(target, targetAppearsFrom);
        }
    }
}
