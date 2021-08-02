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
                    SelectTrackPanel.ReloadOneTrack(
                        EditorContext.trackFolder);
                    ForceTransition();
                });
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
