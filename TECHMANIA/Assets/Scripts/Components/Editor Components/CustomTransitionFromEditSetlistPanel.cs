using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CustomTransitionFromEditSetlistPanel : TransitionToPanel
{
    public ConfirmDialog confirmDialog;

    public override void Invoke()
    {
        // TODO: check for the following issues with the setlist:
        // - not enough selectable patterns
        // - not enough hidden patterns
        // - any pattern not found
        // - any pattern with wrong control scheme
        Transition();
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
