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
    public GameObject errorCardTemplate;
    public GameObject noTrackText;
    public SelectPatternDialog selectPatternDialog;
    public MessageDialog messageDialog;

    protected class TrackInFolder
    {
        public string folder;
        public Track track;
    }
    protected Dictionary<GameObject, TrackInFolder> cardToTrack;
    protected Dictionary<GameObject, string> cardToError;

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
            if (o == errorCardTemplate) continue;
            Destroy(o);
        }

        // Rebuild track list.
        cardToTrack = new Dictionary<GameObject, TrackInFolder>();
        cardToError = new Dictionary<GameObject, string>();
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
            string error = null;
            try
            {
                trackBase = TrackBase.LoadFromFile(possibleTrackFile);
                if (!(trackBase is Track))
                {
                    error = "The track was created in an old version and is no longer supported.";
                }
            }
            catch (Exception e)
            {
                error = e.Message;
            }

            GameObject card = null;
            if (error == null)
            {
                Track track = trackBase as Track;

                // Instantiate card.
                card = Instantiate(trackCardTemplate, trackGrid.transform);
                card.name = "Track Card";
                card.SetActive(true);
                card.GetComponent<TrackCard>().Initialize(
                    dir, track.trackMetadata);

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
            else
            {
                error = $"An error occurred when loading {possibleTrackFile}:\n\n"
                    + error;

                // Instantiate card.
                card = Instantiate(errorCardTemplate, trackGrid.transform);
                card.name = "Error Card";
                card.SetActive(true);

                // Record mapping.
                cardToError.Add(card, error);

                // Bind click event.
                card.GetComponent<Button>().onClick.AddListener(() =>
                {
                    OnClickErrorCard(card);
                });
            }

            if (firstCard == null)
            {
                firstCard = card;
                EventSystem.current.SetSelectedGameObject(firstCard);
            }
        }

        noTrackText.SetActive(cardToTrack.Count == 0);
    }

    protected virtual void OnClickCard(GameObject o)
    {
        GameSetup.trackPath = $"{cardToTrack[o].folder}\\{Paths.kTrackFilename}";
        GameSetup.track = TrackBase.LoadFromFile(GameSetup.trackPath) as Track;
        selectPatternDialog.Show();
    }

    protected virtual void OnClickErrorCard(GameObject o)
    {
        string error = cardToError[o];
        messageDialog.Show(error);
    }
}
