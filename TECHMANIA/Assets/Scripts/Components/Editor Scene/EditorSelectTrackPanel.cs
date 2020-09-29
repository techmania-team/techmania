using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class EditorSelectTrackPanel : SelectTrackPanel
{
    public NewTrackDialog newTrackDialog;
    public Panel trackSetupPanel;

    private void OnEnable()
    {
        Refresh();
    }

    protected override bool ShowNewTrackCard()
    {
        return true;
    }

    protected override void OnClickCard(GameObject o)
    {
        EditorContext.Reset();
        EditorContext.trackPath = $"{cardToTrack[o].folder}\\{Paths.kTrackFilename}";
        EditorContext.track = TrackBase.LoadFromFile(EditorContext.trackPath) as Track;
        PanelTransitioner.TransitionTo(trackSetupPanel, TransitionToPanel.Direction.Right);
    }

    protected override void OnClickNewTrackCard()
    {
        newTrackDialog.Show(createCallback: (string title, string artist) =>
        {
            OnCreateButtonClick(title, artist);
        });
    }

    private void OnCreateButtonClick(string title, string artist)
    {
        // Attempt to create track directory. Contains timestamp
        // so collisions are very unlikely.
        string filteredTitle = Paths.FilterString(title);
        string filteredArtist = Paths.FilterString(artist);
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        string newDir = $"{Paths.GetTrackFolder()}\\{filteredArtist} - {filteredTitle} - {timestamp}";
        try
        {
            Directory.CreateDirectory(newDir);
        }
        catch (Exception e)
        {
            messageDialog.Show($"An error occurred when " +
                $"creating {newDir}:\n\n{e.Message}");
            return;
        }

        // Create empty track.
        Track track = new Track(title, artist);
        string filename = $"{newDir}\\{Paths.kTrackFilename}";
        try
        {
            track.SaveToFile(filename);
        }
        catch (Exception e)
        {
            messageDialog.Show($"An error occurred when " +
                $"writing to {filename}:\n\n{e.Message}");
            return;
        }

        EditorContext.Reset();
        EditorContext.trackPath = filename;
        EditorContext.track = track;
        PanelTransitioner.TransitionTo(trackSetupPanel, TransitionToPanel.Direction.Right);
    }

    // TODO: move this to track setup
    private IEnumerator InternalDelete()
    {
        TrackInFolder trackInFolder = null; // cardToTrack[selectedTrackObject];
        string title = trackInFolder.track.trackMetadata.title;
        string path = trackInFolder.folder;
        // ConfirmDialog.Show($"Deleting {title}. This will permanently " +
        //     $"delele \"{path}\" and everything in it. Are you sure?");
        // yield return new WaitUntil(() => { return ConfirmDialog.IsResolved(); });

        // if (ConfirmDialog.GetResult() == ConfirmDialog.Result.Cancelled)
        {
            yield break;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (Exception e)
        {
            // MessageDialog.Show(e.Message);
        }

        Refresh();
    }
}
