using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CustomTransitionFromEditSetlistPanel : TransitionToPanel
{
    public ConfirmDialog confirmDialog;

    public override void Invoke()
    {
        if (EditorContext.Dirty)
        {
            confirmDialog.Show(
                L10n.GetString(
                    "edit_setlist_panel_discard_changes_confirmation"),
                L10n.GetString(
                    "edit_setlist_panel_discard_changes_confirm"),
                L10n.GetString(
                    "edit_setlist_panel_discard_changes_cancel"),
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
        // Reload setlist into setlist list.
        string parent = Path.GetDirectoryName(
            EditorContext.setlistFolder);
        foreach (GlobalResource.SetlistInFolder t in
            GlobalResource.setlistList[parent])
        {
            if (t.folder == EditorContext.setlistFolder)
            {
                string setlistPath = Path.Combine(
                    EditorContext.setlistFolder, Paths.kSetlistFilename);
                t.setlist = SetlistBase.LoadFromFile(setlistPath)
                    as Setlist;
                break;
            }
        }

        PanelTransitioner.TransitionTo(null, Direction.Left,
            callbackOnFinish: EditorContext.exitCallback);
    }
}
