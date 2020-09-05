using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectTrackPanel : MonoBehaviour
{
    public GridLayoutGroup trackGrid;
    public GameObject trackCardTemplate;
    public GameObject noTrackText;

    protected class TrackInFolder
    {
        public string folder;
        public Track track;
    }
    protected Dictionary<GameObject, TrackInFolder> cardToTrack;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        // Remove all objects from grid, except for template.
        for (int i = 0; i < trackGrid.transform.childCount; i++)
        {
            GameObject o = trackGrid.transform.GetChild(i).gameObject;
            if (o == trackCardTemplate) continue;
            Destroy(o);
        }

        // Rebuild track list.
        cardToTrack = new Dictionary<GameObject, TrackInFolder>();
        GameObject firstCard = null;
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

            // Instantiate card.
            GameObject card = Instantiate(trackCardTemplate, trackGrid.transform);
            card.name = "Track Card";
            string eyecatchPath = "";
            if (track.trackMetadata.eyecatchImage != UIUtils.kNone)
            {
                eyecatchPath = dir + "\\" + track.trackMetadata.eyecatchImage;
            }
            card.SetActive(true);
            card.GetComponent<TrackCard>().Initialize(
                eyecatchPath, track.trackMetadata.title, track.trackMetadata.artist);
            if (firstCard == null)
            {
                firstCard = card;
                EventSystem.current.SetSelectedGameObject(firstCard);
            }

            // Record mapping.
            cardToTrack.Add(card, new TrackInFolder()
            {
                folder = dir,
                track = track
            });

            // Bind click event.
            card.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnClickCard(card);
            });
        }

        noTrackText.SetActive(cardToTrack.Count == 0);
    }

    protected virtual void OnClickCard(GameObject o)
    {
        GameSetup.trackPath = $"{cardToTrack[o].folder}\\{Paths.kTrackFilename}";
        GameSetup.track = TrackBase.LoadFromFile(GameSetup.trackPath) as Track;
        SelectPatternDialog.Show();
    }
}
