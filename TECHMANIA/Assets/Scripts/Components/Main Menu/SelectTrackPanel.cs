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
    protected class ErrorInFolder
    {
        public string trackFile;
        public string message;
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

        // Build and sort the list of all tracks.
        List<TrackInFolder> allTracks = new List<TrackInFolder>();
        List<ErrorInFolder> allErrors = new List<ErrorInFolder>();
        foreach (string dir in Directory.EnumerateDirectories(
            Paths.GetTrackFolder()))
        {
            // Is there a track?
            string possibleTrackFile = Path.Combine(
                dir, Paths.kTrackFilename);
            if (!File.Exists(possibleTrackFile))
            {
                continue;
            }

            // Attempt to load track.
            Track track = null;
            try
            {
                track = Track.LoadFromFile(possibleTrackFile) as Track;
            }
            catch (Exception e)
            {
                allErrors.Add(new ErrorInFolder()
                {
                    trackFile = possibleTrackFile,
                    message = e.Message
                });
                continue;
            }

            allTracks.Add(new TrackInFolder()
            {
                folder = dir,
                track = track
            });
        }

        allTracks.Sort((TrackInFolder t1, TrackInFolder t2) =>
        {
            return string.Compare(t1.track.trackMetadata.title,
                t2.track.trackMetadata.title);
        });

        // Instantiate track cards.
        cardToTrack = new Dictionary<GameObject, TrackInFolder>();
        cardToError = new Dictionary<GameObject, string>();
        GameObject firstCard = null;
        foreach (TrackInFolder trackInFolder in allTracks)
        {
            GameObject card = Instantiate(trackCardTemplate,
                trackGrid.transform);
            card.name = "Track Card";
            card.GetComponent<TrackCard>().Initialize(
                trackInFolder.folder,
                trackInFolder.track.trackMetadata);
            card.SetActive(true);

            // Record mapping.
            cardToTrack.Add(card, trackInFolder);

            // Bind click event.
            card.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnClickCard(card);
            });

            if (firstCard == null)
            {
                firstCard = card;
            }
        }

        // Instantiate error cards.
        foreach (ErrorInFolder error in allErrors)
        {
            GameObject card = null;
            string message = $"An error occurred when loading {error.trackFile}:\n\n{error.message}";

            // Instantiate card.
            card = Instantiate(errorCardTemplate, 
                trackGrid.transform);
            card.name = "Error Card";
            card.SetActive(true);

            // Record mapping.
            cardToError.Add(card, message);

            // Bind click event.
            card.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnClickErrorCard(card);
            });

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
        GameSetup.trackPath = Path.Combine(cardToTrack[o].folder, Paths.kTrackFilename);
        GameSetup.track = cardToTrack[o].track;
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
