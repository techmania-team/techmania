using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SelectTrackPanel : MonoBehaviour
{
    public GridLayoutGroup trackGrid;
    public GameObject trackTemplate;

    protected class TrackInFolder
    {
        public string folder;
        public Track track;
    }
    protected Dictionary<GameObject, TrackInFolder> objectToTrack;

    private void OnEnable()
    {
        Refresh();

        GameSetup.noFail = false;
        GameSetup.autoPlay = false;
    }

    public void Refresh()
    {
        // Remove all tracks from grid, except for template.
        for (int i = 0; i < trackGrid.transform.childCount; i++)
        {
            GameObject track = trackGrid.transform.GetChild(i).gameObject;
            if (track == trackTemplate) continue;
            Destroy(track);
        }

        // Rebuild track list.
        objectToTrack = new Dictionary<GameObject, TrackInFolder>();
        foreach (string dir in Directory.EnumerateDirectories(
            Paths.GetTrackFolder()))
        {
            // Is there a track?
            string possibleTrackFile = $"{dir}\\{Paths.kTrackFilename}";
            if (!File.Exists(possibleTrackFile))
            {
                continue;
            }

            // Attempt to load track.
            TrackBase trackBase = null;
            try
            {
                trackBase = TrackBase.LoadFromFile(possibleTrackFile);
            }
            catch (Exception)
            {
                continue;
            }
            if (!(trackBase is Track))
            {
                continue;
            }
            Track track = trackBase as Track;

            // Instantiate track representation.
            GameObject trackObject = Instantiate(trackTemplate, trackGrid.transform);
            trackObject.name = "Track Panel";
            string textOnObject = $"<b>{track.trackMetadata.title}</b>\n" +
                $"<size=16>{track.trackMetadata.artist}</size>";
            trackObject.GetComponentInChildren<Text>().text = textOnObject;
            trackObject.SetActive(true);

            // Load eyecatch image.
            if (track.trackMetadata.eyecatchImage != UIUtils.kNone)
            {
                string eyecatchPath = dir + "\\" + track.trackMetadata.eyecatchImage;
                trackObject.GetComponentInChildren<EyecatchSelfLoader>().LoadImage(
                    eyecatchPath);
            }

            // Record mapping.
            objectToTrack.Add(trackObject, new TrackInFolder()
            {
                folder = dir,
                track = track
            });

            // Bind click event.
            // TODO: double click to open?
            trackObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnClickTrackObject(trackObject);
            });
        }
    }

    protected virtual void OnClickTrackObject(GameObject o)
    {
        GameSetup.trackPath = $"{objectToTrack[o].folder}\\{Paths.kTrackFilename}";
        GameSetup.track = TrackBase.LoadFromFile(GameSetup.trackPath) as Track;
        SelectPatternDialog.Show();
    }
}
