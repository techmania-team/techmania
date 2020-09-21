using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class EditorSelectTrackPanel : SelectTrackPanel
{
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
        GameSetup.trackPath = $"{cardToTrack[o].folder}\\{Paths.kTrackFilename}";
        GameSetup.track = TrackBase.LoadFromFile(GameSetup.trackPath) as Track;
        // TODO: move to next panel
    }

    protected override void OnClickNewTrackCard()
    {
        // TODO: show "new track" dialog
    }

    private IEnumerator InternalNew()
    {
        // Get title and artist.
        NewTrackDialog.Show();
        yield return new WaitUntil(() => { return NewTrackDialog.IsResolved(); });
        if (NewTrackDialog.GetResult() == NewTrackDialog.Result.Cancelled)
        {
            yield break;
        }
        string title = NewTrackDialog.GetTitle();
        string artist = NewTrackDialog.GetArtist();
        string filteredTitle = Paths.FilterString(title);
        string filteredArtist = Paths.FilterString(artist);
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        // Create new directory. Contains timestamp so collisions are
        // very unlikely.
        string newDir = $"{Paths.GetTrackFolder()}\\{filteredArtist} - {filteredTitle} - {timestamp}";
        try
        {
            Directory.CreateDirectory(newDir);
        }
        catch (Exception e)
        {
            // MessageDialog.Show(e.Message);
            yield break;
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
            // MessageDialog.Show(e.Message);
            yield break;
        }

        Refresh();
    }

    // TODO: move this to track setup
    private IEnumerator InternalDelete()
    {
        TrackInFolder trackInFolder = null; // cardToTrack[selectedTrackObject];
        string title = trackInFolder.track.trackMetadata.title;
        string path = trackInFolder.folder;
        ConfirmDialog.Show($"Deleting {title}. This will permanently " +
            $"delele \"{path}\" and everything in it. Are you sure?");
        yield return new WaitUntil(() => { return ConfirmDialog.IsResolved(); });

        if (ConfirmDialog.GetResult() == ConfirmDialog.Result.Cancelled)
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
