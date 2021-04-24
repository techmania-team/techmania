using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectTrackPanel : MonoBehaviour
{
    protected class TrackInFolder
    {
        public string folder;
        public Track track;
    }
    protected class ErrorInTrack
    {
        public string trackFile;
        public string message;
    }

    protected static string currentLocation;
    // Cached, keyed by track folder's parent folder.
    protected static Dictionary<string, List<string>> subfolderList;
    protected static Dictionary<string, List<TrackInFolder>> trackList;
    protected static Dictionary<string, List<ErrorInTrack>>
        errorTrackList;
    static SelectTrackPanel()
    {
        currentLocation = Paths.GetTrackRootFolder();
        subfolderList = new Dictionary<string, List<string>>();
        trackList = new Dictionary<string, List<TrackInFolder>>();
        errorTrackList = new Dictionary<string, List<ErrorInTrack>>();
    }

    public static void RemoveCachedListsAtCurrentLocation()
    {
        subfolderList.Remove(currentLocation);
        trackList.Remove(currentLocation);
        errorTrackList.Remove(currentLocation);
    }

    public Button backButton;
    public Button refreshButton;
    public GridLayoutGroup trackGrid;
    public TextMeshProUGUI locationDisplay;
    public GameObject goUpCard;
    public GameObject subfolderCardTemplate;
    public GameObject trackCardTemplate;
    public GameObject errorCardTemplate;
    public GameObject newTrackCard;
    public TextMeshProUGUI trackListBuildingProgress;
    public GameObject noTrackText;
    public SelectPatternDialog selectPatternDialog;
    public MessageDialog messageDialog;

    protected Dictionary<GameObject, string> cardToSubfolder;
    protected Dictionary<GameObject, TrackInFolder> cardToTrack;
    protected Dictionary<GameObject, string> cardToError;

    private void OnEnable()
    {
        StartCoroutine(Refresh());
    }

    protected IEnumerator Refresh()
    {
        // Disaster recovery.
        if (!Directory.Exists(currentLocation))
        {
            currentLocation = Paths.GetTrackRootFolder();
        }

        // Show location.
        locationDisplay.text = currentLocation;

        // Remove all objects from grid, except for templates.
        for (int i = 0; i < trackGrid.transform.childCount; i++)
        {
            GameObject o = trackGrid.transform.GetChild(i).gameObject;
            if (o == goUpCard) continue;
            if (o == subfolderCardTemplate) continue;
            if (o == trackCardTemplate) continue;
            if (o == errorCardTemplate) continue;
            if (o == newTrackCard) continue;
            Destroy(o);
        }

        if (!trackList.ContainsKey(currentLocation))
        {
            backButton.interactable = false;
            refreshButton.interactable = false;
            goUpCard.gameObject.SetActive(false);
            newTrackCard.gameObject.SetActive(false);
            trackListBuildingProgress.gameObject.SetActive(true);

            // Launch background worker to rebuild track list.
            trackListBuilder = new BackgroundWorker();
            trackListBuilder.DoWork += TrackListBuilderDoWork;
            trackListBuilder.RunWorkerCompleted +=
                TrackListBuilderCompleted;
            builderDone = false;
            builderProgress = "";
            trackListBuilder.RunWorkerAsync();
            do
            {
                trackListBuildingProgress.text =
                    builderProgress;
                yield return null;
            } while (!builderDone);

            trackListBuildingProgress.gameObject.SetActive(false);
            refreshButton.interactable = true;
            backButton.interactable = true;
        }

        // Show go up card if applicable.
        goUpCard.SetActive(currentLocation != Paths.GetTrackRootFolder());

        // Instantiate subfolder cards.
        cardToSubfolder = new Dictionary<GameObject, string>();
        GameObject firstCard = null;
        foreach (string subfolder in subfolderList[currentLocation])
        {
            GameObject card = Instantiate(subfolderCardTemplate,
                trackGrid.transform);
            card.name = "Subfolder Card";
            card.GetComponent<SubfolderCard>().Initialize(
                new DirectoryInfo(subfolder).Name);
            card.SetActive(true);

            // Record mapping.
            cardToSubfolder.Add(card, subfolder);

            // Bind click event.
            card.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnClickSubfolderCard(card);
            });

            if (firstCard == null)
            {
                firstCard = card;
            }
        }

        // Instantiate track cards.
        cardToTrack = new Dictionary<GameObject, TrackInFolder>();
        foreach (TrackInFolder trackInFolder in 
            trackList[currentLocation])
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
        cardToError = new Dictionary<GameObject, string>();
        foreach (ErrorInTrack error in 
            errorTrackList[currentLocation])
        {
            GameObject card = null;
            string message = Locale.GetStringAndFormat(
                "select_track_error_format",
                error.trackFile,
                error.message);

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

        if (firstCard == null && goUpCard.activeSelf)
        {
            firstCard = goUpCard;
        }
        EventSystem.current.SetSelectedGameObject(firstCard);
        noTrackText.SetActive(firstCard == null);
    }

    protected virtual bool ShowNewTrackCard()
    {
        return false;
    }

    #region Background worker
    private BackgroundWorker trackListBuilder;
    private string builderProgress;
    private bool builderDone;

    private void TrackListBuilderDoWork(object sender,
        DoWorkEventArgs e)
    {
        RemoveCachedListsAtCurrentLocation();
        subfolderList[currentLocation] = new List<string>();
        trackList[currentLocation] = new List<TrackInFolder>();
        errorTrackList[currentLocation] = new List<ErrorInTrack>();

        foreach (string dir in Directory.EnumerateDirectories(
            currentLocation))
        {
            builderProgress = Locale.GetStringAndFormat(
                "select_track_scanning_text", dir);

            // Is there a track?
            string possibleTrackFile = Path.Combine(
                dir, Paths.kTrackFilename);
            if (!File.Exists(possibleTrackFile))
            {
                // Record as a subfolder.
                subfolderList[currentLocation].Add(dir);
                continue;
            }

            // Attempt to load track.
            Track track = null;
            try
            {
                track = Track.LoadFromFile(possibleTrackFile) as Track;
            }
            catch (Exception ex)
            {
                errorTrackList[currentLocation].Add(new ErrorInTrack()
                {
                    trackFile = possibleTrackFile,
                    message = ex.Message
                });
                continue;
            }

            trackList[currentLocation].Add(new TrackInFolder()
            {
                folder = dir,
                track = track
            });
        }

        trackList[currentLocation].Sort(
            (TrackInFolder t1, TrackInFolder t2) =>
            {
                return string.Compare(t1.track.trackMetadata.title,
                    t2.track.trackMetadata.title);
            });
    }

    private void TrackListBuilderCompleted(object sender, 
        RunWorkerCompletedEventArgs e)
    {
        builderDone = true;
        if (e.Error != null)
        {
            Debug.LogError(e.Error);
        }
    }
    #endregion

    #region Clicks on cards
    public void OnRefreshButtonClick()
    {
        RemoveCachedListsAtCurrentLocation();
        StartCoroutine(Refresh());
    }

    public void OnClickGoUpCard()
    {
        currentLocation = new DirectoryInfo(currentLocation).Parent.FullName;
        StartCoroutine(Refresh());
    }

    private void OnClickSubfolderCard(GameObject o)
    {
        currentLocation = cardToSubfolder[o];
        StartCoroutine(Refresh());
    }

    protected virtual void OnClickCard(GameObject o)
    {
        GameSetup.trackPath = Path.Combine(cardToTrack[o].folder, 
            Paths.kTrackFilename);
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
    #endregion
}
