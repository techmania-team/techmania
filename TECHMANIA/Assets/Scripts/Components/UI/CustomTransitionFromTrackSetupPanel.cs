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
                Locale.GetString(
                    "track_setup_discard_changes_confirmation"),
                Locale.GetString(
                    "track_setup_discard_changes_confirm"),
                Locale.GetString(
                    "track_setup_discard_changes_cancel"),
                () =>
                {
                    Transition();
                });
        }
        else
        {
            Transition();
        }
    }

    public void Transition()
    {
        SelectTrackPanel.ReloadOneTrack(EditorContext.trackFolder);
        PanelTransitioner.TransitionTo(target, targetAppearsFrom);
    }
}
