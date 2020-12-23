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
    public GameObject newTrackCard;
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

    protected void Refresh()
    {
        // Remove all objects from grid, except for templates.
        for (int i = 0; i < trackGrid.transform.childCount; i++)
        {
            GameObject o = trackGrid.transform.GetChild(i).gameObject;
            if (o == trackCardTemplate) continue;
            if (o == errorCardTemplate) continue;
            if (o == newTrackCard) continue;
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
                card = Instantiate(trackCardTemplate, 
                    trackGrid.transform);
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
                card = Instantiate(errorCardTemplate, 
                    trackGrid.transform);
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
            }
        }

        if (ShowNewTrackCard())
        {
            newTrackCard.transform.SetAsLastSibling();
            newTrackCard.SetActive(true);
            newTrackCard.GetComponent<Button>().onClick
                .RemoveAllListeners();
            newTrackCard.GetComponent<Button>().onClick
                .AddListener(() =>
            {
                OnClickNewTrackCard();
            });

            if (firstCard == null)
            {
                firstCard = newTrackCard;
            }
        }

        EventSystem.current.SetSelectedGameObject(firstCard);
        noTrackText.SetActive(firstCard == null);
    }

    protected virtual bool ShowNewTrackCard()
    {
        return false;
    }

    protected virtual void OnClickCard(GameObject o)
    {
        GameSetup.trackPath = $"{cardToTrack[o].folder}\\{Paths.kTrackFilename}";
        GameSetup.track = TrackBase.LoadFromFile(
            GameSetup.trackPath) as Track;
        selectPatternDialog.Show();
    }

    private void OnClickErrorCard(GameObject o)
    {
        string error = cardToError[o];
        messageDialog.Show(error);
    }

    protected virtual void OnClickNewTrackCard()
    {
        throw new NotImplementedException(
            "SelectTrackPanel in the game scene should not show the New Track card.");
    }
}
