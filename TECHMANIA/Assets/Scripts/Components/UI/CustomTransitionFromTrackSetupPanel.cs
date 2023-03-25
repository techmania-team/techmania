using System.Collections;
using System.Collections.Generic;
using System.IO;
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
                L10n.GetString(
                    "track_setup_discard_changes_confirmation"),
                L10n.GetString(
                    "track_setup_discard_changes_confirm"),
                L10n.GetString(
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
        // Reload track into track list.
        string parent = Path.GetDirectoryName(EditorContext.trackFolder);
        foreach (GlobalResource.TrackInFolder t in
            GlobalResource.trackList[parent])
        {
            if (t.folder == EditorContext.trackFolder)
            {
                string trackPath = Path.Combine(
                    EditorContext.trackFolder, Paths.kTrackFilename);
                t.minimizedTrack = Track.LoadFromFile(trackPath)
                    as Track;
                t.minimizedTrack = Track.Minimize(t.minimizedTrack);
                break;
            }
        }

        PanelTransitioner.TransitionTo(null, Direction.Left,
            callbackOnFinish: EditorContext.exitCallback);
    }
}
