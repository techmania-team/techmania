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
    protected class Subfolder
    {
        public string path;
        public string eyecatchFullPath;
    }
    protected class TrackInFolder
    {
        // The folder that track.tech is in.
        public string folder;
        public Track track;
    }
    protected class ErrorInTrack
    {
        public string trackFile;
        public string message;
    }

    protected static string currentLocation;
    protected static int selectedCardIndex;
    // Cached, keyed by track folder's parent folder.
    protected static Dictionary<string, List<Subfolder>> 
        subfolderList;
    protected static Dictionary<string, List<TrackInFolder>> 
        trackList;
    protected static Dictionary<string, List<ErrorInTrack>>
        errorTrackList;
    static SelectTrackPanel()
    {
        currentLocation = "";
        selectedCardIndex = -1;
        subfolderList = new Dictionary<string, List<Subfolder>>();
        trackList = new Dictionary<string, List<TrackInFolder>>();
        errorTrackList = new Dictionary<string, List<ErrorInTrack>>();
    }

    public static void RemoveCachedLists()
    {
        subfolderList.Clear();
        trackList.Clear();
        errorTrackList.Clear();
    }

    public static void RemoveOneTrack(string trackFolder)
    {
        string parent = new FileInfo(trackFolder).DirectoryName;
        trackList[parent].RemoveAll((TrackInFolder t) =>
        {
            return t.folder == trackFolder;
        });
    }

    public static void ReloadOneTrack(string trackFolder)
    {
        string parent = new FileInfo(trackFolder).DirectoryName;
        foreach (TrackInFolder t in trackList[parent])
        {
            if (t.folder == trackFolder)
            {
                string trackPath = Path.Combine(
                    trackFolder, Paths.kTrackFilename);
                t.track = Track.LoadFromFile(trackPath) as Track;
                break;
            }
        }
    }

    public Button backButton;
    public Button trackFilterButton;
    public Button refreshButton;
    public Button goUpButton;
    public TextMeshProUGUI locationDisplay;
    public ScrollRect scrollRect;
    public GridLayoutGroup subfolderGrid;
    public GameObject subfolderCardTemplate;
    public GridLayoutGroup trackGrid;
    public GameObject trackCardTemplate;
    public GameObject errorCardTemplate;
    public GameObject newTrackCard;
    public TextMeshProUGUI trackListBuildingProgress;
    public TextMeshProUGUI trackStatusText;
    public TrackFilterSidesheet trackFilterSidesheet;
    public Panel selectPatternPanel;
    public MessageDialog messageDialog;

    protected Dictionary<GameObject, string> cardToSubfolder;
    protected Dictionary<GameObject, TrackInFolder> cardToTrack;
    protected Dictionary<GameObject, string> cardToError;
    protected List<GameObject> cardList;

    protected bool refreshing;

    protected void Start()
    {
        // Reset the keyword every time the main menu or editor scene
        // loads.
        trackFilterSidesheet.ResetSearchKeyword();
    }

    protected void OnEnable()
    {
        StartCoroutine(Refresh());
        TrackFilterSidesheet.trackFilterChanged += 
            OnTrackFilterChanged;
    }

    protected void OnDisable()
    {
        TrackFilterSidesheet.trackFilterChanged -=
            OnTrackFilterChanged;
    }

    protected IEnumerator Refresh()
    {
        refreshing = true;

        // Initialization and/or disaster recovery.
        if (currentLocation == "" ||
            !Directory.Exists(currentLocation))
        {
            currentLocation = Paths.GetTrackRootFolder();
        }

        // Show location.
        if (TrackFilter.instance.showTracksInAllFolders)
        {
            currentLocation = Paths.GetTrackRootFolder();
        }
        locationDisplay.text = currentLocation;

        // Activate all grids regardless of content.
        subfolderGrid.gameObject.SetActive(true);
        trackGrid.gameObject.SetActive(true);

        // Remove all objects from grid, except for templates.
        for (int i = 0; i < subfolderGrid.transform.childCount; i++)
        {
            GameObject o = subfolderGrid.transform.GetChild(i)
                .gameObject;
            if (o == subfolderCardTemplate) continue;
            Destroy(o);
        }
        for (int i = 0; i < trackGrid.transform.childCount; i++)
        {
            GameObject o = trackGrid.transform.GetChild(i).gameObject;
            if (o == trackCardTemplate) continue;
            if (o == errorCardTemplate) continue;
            if (o == newTrackCard) continue;
            Destroy(o);
        }

        if (!trackList.ContainsKey(currentLocation))
        {
            backButton.interactable = false;
            trackFilterButton.interactable = false;
            refreshButton.interactable = false;
            goUpButton.interactable = false;
            newTrackCard.gameObject.SetActive(false);
            trackListBuildingProgress.gameObject.SetActive(true);
            trackStatusText.gameObject.SetActive(false);

            // Launch background worker to rebuild track list.
            trackListBuilder = new BackgroundWorker();
            trackListBuilder.DoWork += TrackListBuilderDoWork;
            trackListBuilder.RunWorkerCompleted +=
                TrackListBuilderCompleted;
            builderDone = false;
            builderProgress = "";
            Options.TemporarilyDisableVSync();
            trackListBuilder.RunWorkerAsync();
            do
            {
                trackListBuildingProgress.text =
                    builderProgress;
                yield return null;
            } while (!builderDone);
            Options.RestoreVSync();

            trackListBuildingProgress.gameObject.SetActive(false);
            backButton.interactable = true;
            trackFilterButton.interactable = true;
            refreshButton.interactable = true;
        }

        // Enable go up button if applicable.
        goUpButton.interactable = currentLocation !=
            Paths.GetTrackRootFolder();

        // Prepare subfolder list. Make a local copy so we can
        // sort it. This also applies to the track list below.
        List<Subfolder> subfolders = new List<Subfolder>();
        if (!TrackFilter.instance.showTracksInAllFolders)
        {
            foreach (Subfolder s in subfolderList[currentLocation])
            {
                subfolders.Add(s);
            }
            subfolders.Sort((Subfolder s1, Subfolder s2) =>
            {
                return string.Compare(s1.path, s2.path);
            });
        }

        // Instantiate subfolder cards.
        cardToSubfolder = new Dictionary<GameObject, string>();
        cardList = new List<GameObject>();
        bool subfolderGridEmpty = true;
        bool trackGridEmpty = true;
        foreach (Subfolder subfolder in subfolders)
        {
            GameObject card = Instantiate(subfolderCardTemplate,
                subfolderGrid.transform);
            card.name = "Subfolder Card";
            card.GetComponent<SubfolderCard>().Initialize(
                new DirectoryInfo(subfolder.path).Name,
                subfolder.eyecatchFullPath);
            card.SetActive(true);
            subfolderGridEmpty = false;

            // Record mapping.
            cardToSubfolder.Add(card, subfolder.path);
            AddToCardList(card);

            // Bind click event.
            card.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnSubfolderCardClick(card);
            });
        }

        // Prepare track list.
        List<TrackInFolder> tracks = new List<TrackInFolder>();
        if (TrackFilter.instance.showTracksInAllFolders)
        {
            foreach (List<TrackInFolder> oneFolder in trackList.Values)
            {
                foreach (TrackInFolder t in oneFolder)
                {
                    tracks.Add(t);
                }
            }
        }
        else
        {
            foreach (TrackInFolder t in trackList[currentLocation])
            {
                tracks.Add(t);
            }
        }
        tracks.Sort(
            (TrackInFolder t1, TrackInFolder t2) =>
            {
                return Track.Compare(t1.track, t2.track,
                    TrackFilter.instance.sortBasis,
                    TrackFilter.instance.sortOrder);
            });

        // Instantiate track cards. Also apply filter.
        cardToTrack = new Dictionary<GameObject, TrackInFolder>();
        foreach (TrackInFolder trackInFolder in tracks)
        {
            if (trackFilterSidesheet.searchKeyword != null &&
                trackFilterSidesheet.searchKeyword != "" &&
                !trackInFolder.track.ContainsKeywords(
                    trackFilterSidesheet.searchKeyword))
            {
                // Filtered out.
                continue;
            }

            GameObject card = Instantiate(trackCardTemplate,
                trackGrid.transform);
            card.name = "Track Card";
            card.GetComponent<TrackCard>().Initialize(
                trackInFolder.folder,
                trackInFolder.track.trackMetadata);
            card.SetActive(true);
            trackGridEmpty = false;

            // Record mapping.
            cardToTrack.Add(card, trackInFolder);
            AddToCardList(card);

            // Bind click event.
            card.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnTrackCardClick(card);
            });
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
            trackGridEmpty = false;

            // Record mapping.
            cardToError.Add(card, message);
            AddToCardList(card);

            // Bind click event.
            card.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnErrorCardClick(card);
            });
        }

        if (ShowNewTrackCard())
        {
            newTrackCard.transform.SetAsLastSibling();
            newTrackCard.SetActive(true);
            trackGridEmpty = false;
            newTrackCard.GetComponent<Button>().onClick
                .RemoveAllListeners();
            newTrackCard.GetComponent<Button>().onClick
                .AddListener(() =>
            {
                OnNewTrackCardClick();
            });
            AddToCardList(newTrackCard);
        }

        // Deactivate empty grids.
        subfolderGrid.gameObject.SetActive(!subfolderGridEmpty);
        trackGrid.gameObject.SetActive(!trackGridEmpty);

        // Wait 1 frame to let the grids and scroll rect update
        // their layout.
        yield return null;

        // Restore or initialize selection.
        if (!trackFilterSidesheet.gameObject.activeSelf)
        {
            if (selectedCardIndex >= 0 &&
                selectedCardIndex < cardList.Count)
            {
                SelectCard(selectedCardIndex);
            }
            else if (cardList.Count > 0)
            {
                SelectCard(0);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(
                    backButton.gameObject);
            }
        }

        // Show "no track" message or "tracks hidden" message
        // as necessary.
        if (cardToTrack.Count < tracks.Count)
        {
            trackStatusText.gameObject.SetActive(true);
            trackStatusText.text = Locale.GetStringAndFormat(
                "select_track_some_tracks_hidden_text",
                tracks.Count - cardToTrack.Count,
                trackFilterSidesheet.searchKeyword);
        }
        else if (cardToTrack.Count + cardToError.Count == 0)
        {
            trackStatusText.gameObject.SetActive(true);
            trackStatusText.text = Locale.GetString(
                "select_track_no_track_text");
        }
        else
        {
            trackStatusText.gameObject.SetActive(false);
        }

        refreshing = false;
    }

    private void SelectCard(int index)
    {
        if (index < 0 || index >= cardList.Count) return;
        GameObject card = cardList[index];
        EventSystem.current.SetSelectedGameObject(card);
        card.GetComponent<ScrollIntoViewOnSelect>().ScrollIntoView();
    }

    private void AddToCardList(GameObject card)
    {
        ReportSelectionToSelectTrackPanel reporter =
            card.GetComponent<ReportSelectionToSelectTrackPanel>();
        reporter.Initialize(this, cardList.Count);
        cardList.Add(card);
    }

    protected virtual bool ShowNewTrackCard()
    {
        return false;
    }

    protected void Update()
    {
        // Synchronize alpha with sidesheet because the
        // CanvasGroup on the sidesheet ignores parent.
        if (PanelTransitioner.transitioning &&
            trackFilterSidesheet.gameObject.activeSelf)
        {
            trackFilterSidesheet.GetComponent<CanvasGroup>().alpha
                = GetComponent<CanvasGroup>().alpha;
        }

        // Shortcuts.
        if (refreshing) return;
        if (Input.GetKeyDown(KeyCode.Home))
        {
            MenuSfx.instance.PlaySelectSound();
            SelectCard(0);
        }
        if (Input.GetKeyDown(KeyCode.End))
        {
            MenuSfx.instance.PlaySelectSound();
            SelectCard(cardList.Count - 1);
        }
        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            MenuSfx.instance.PlaySelectSound();
            for (int i = 0; i < 3; i++) MoveSelectionUp();
        }
        if (Input.GetKeyDown(KeyCode.PageDown))
        {
            MenuSfx.instance.PlaySelectSound();
            for (int i = 0; i < 3; i++) MoveSelectionDown();
        }
        bool alt = Input.GetKey(KeyCode.LeftAlt) ||
            Input.GetKey(KeyCode.RightAlt);
        if (alt && Input.GetKeyDown(KeyCode.UpArrow) &&
            goUpButton.interactable)
        {
            OnGoUpButtonClick();
        }
        if (Input.GetKeyDown(KeyCode.F5))
        {
            MenuSfx.instance.PlayClickSound();
            OnRefreshButtonClick();
        }
    }

    private void MoveSelectionUp()
    {
        Selectable up = EventSystem.current.currentSelectedGameObject
            .GetComponent<Selectable>().FindSelectableOnUp();
        int index = cardList.IndexOf(up?.gameObject);
        if (index < 0) return;
        SelectCard(index);
    }

    private void MoveSelectionDown()
    {
        Selectable down = EventSystem.current.currentSelectedGameObject
            .GetComponent<Selectable>().FindSelectableOnDown();
        int index = cardList.IndexOf(down?.gameObject);
        if (index < 0) return;
        SelectCard(index);
    }

    #region Track filter side sheet
    public void OnTrackFilterButtonClick()
    {
        trackFilterSidesheet.GetComponent<Sidesheet>().FadeIn();
    }

    private void OnTrackFilterChanged()
    {
        StartCoroutine(Refresh());
    }
    #endregion

    #region Background worker
    private BackgroundWorker trackListBuilder;
    private string builderProgress;
    private bool builderDone;

    private void TrackListBuilderDoWork(object sender,
        DoWorkEventArgs e)
    {
        RemoveCachedLists();
        subfolderList.Clear();
        trackList.Clear();
        errorTrackList.Clear();

        BuildTrackListFor(Paths.GetTrackRootFolder());
        BuildTrackListFor(Paths.GetStreamingTrackRootFolder());
    }

    private void BuildTrackListFor(string folder)
    {
        subfolderList.Add(folder, new List<Subfolder>());
        trackList.Add(folder, new List<TrackInFolder>());
        errorTrackList.Add(folder, new List<ErrorInTrack>());

        foreach (string file in Directory.EnumerateFiles(
            folder, "*.zip"))
        {
            // Attempt to extract this archive.
            builderProgress = Locale.GetStringAndFormat(
                "select_track_extracting_text", file);
            try
            {
                ExtractZipFile(file);
            }
            catch (Exception ex)
            {
                // Log error and move on.
                Debug.LogError(ex.ToString());
            }
        }

        foreach (string dir in Directory.EnumerateDirectories(
            folder))
        {
            builderProgress = Locale.GetStringAndFormat(
                "select_track_scanning_text", dir);

            // Is there a track?
            string possibleTrackFile = Path.Combine(
                dir, Paths.kTrackFilename);
            if (!File.Exists(possibleTrackFile))
            {
                Subfolder subfolder = new Subfolder()
                {
                    path = dir
                };

                // Look for eyecatch, if any.
                string pngEyecatch = Path.Combine(dir,
                    Paths.kSubfolderEyecatchPngFilename);
                if (File.Exists(pngEyecatch))
                {
                    subfolder.eyecatchFullPath = pngEyecatch;
                }
                string jpgEyecatch = Path.Combine(dir,
                    Paths.kSubfolderEyecatchJpgFilename);
                if (File.Exists(jpgEyecatch))
                {
                    subfolder.eyecatchFullPath = jpgEyecatch;
                }

                // Record as a subfolder.
                if (folder.Equals(Paths.GetStreamingTrackRootFolder())) {
                    subfolderList[Paths.GetTrackRootFolder()].Add(subfolder);
                } else {
                    subfolderList[folder].Add(subfolder);
                }

                // Build recursively.
                BuildTrackListFor(dir);

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
                errorTrackList[folder].Add(new ErrorInTrack()
                {
                    trackFile = possibleTrackFile,
                    message = ex.Message
                });
                continue;
            }

            trackList[folder].Add(new TrackInFolder()
            {
                folder = dir,
                track = track
            });
        }
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

    private void ExtractZipFile(string zipFilename)
    {
        Debug.Log("Extracting: " + zipFilename);

        using (FileStream fileStream = File.OpenRead(zipFilename))
        using (ICSharpCode.SharpZipLib.Zip.ZipFile zipFile = new
            ICSharpCode.SharpZipLib.Zip.ZipFile(fileStream))
        {
            byte[] buffer = new byte[4096];  // Recommended length

            foreach (ICSharpCode.SharpZipLib.Zip.ZipEntry entry in
                zipFile)
            {
                if (string.IsNullOrEmpty(
                    Path.GetDirectoryName(entry.Name)))
                {
                    Debug.Log($"Ignoring due to not being in a folder: {entry.Name} in {zipFilename}");
                    continue;
                }

                if (entry.IsDirectory)
                {
                    Debug.Log($"Ignoring empty folder: {entry.Name} in {zipFilename}");
                    continue;
                }

                string extractedFilename = Path.Combine(
                    currentLocation, entry.Name);
                Debug.Log($"Extracting {entry.Name} in {zipFilename} to: {extractedFilename}");

                Directory.CreateDirectory(Path.GetDirectoryName(
                    extractedFilename));
                using var inputStream = zipFile.GetInputStream(entry);
                using FileStream outputStream = File.Create(
                    extractedFilename);
                ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(
                    inputStream, outputStream, buffer);
            }
        }

        Debug.Log($"Extract successful. Deleting: {zipFilename}");
        File.Delete(zipFilename);
    }
    #endregion

    #region Events from cards and buttons
    public void OnRefreshButtonClick()
    {
        RemoveCachedLists();
        StartCoroutine(Refresh());
    }

    public void OnGoUpButtonClick()
    {
        Debug.Log(currentLocation);

        if (currentLocation.Contains(Paths.GetStreamingTrackRootFolder())) {
            Debug.Log("is sa");
            currentLocation = Paths.GetTrackRootFolder();
        } else {
            currentLocation = new DirectoryInfo(currentLocation)
                .Parent.FullName;
        }
        selectedCardIndex = -1;
        StartCoroutine(Refresh());
    }

    public void OnOpenInExplorerButtonClick()
    {
        Application.OpenURL(currentLocation);
    }

    private void OnSubfolderCardClick(GameObject o)
    {
        currentLocation = cardToSubfolder[o];
        selectedCardIndex = -1;
        StartCoroutine(Refresh());
    }

    protected virtual void OnTrackCardClick(GameObject o)
    {
        GameSetup.trackPath = Path.Combine(cardToTrack[o].folder, 
            Paths.kTrackFilename);
        GameSetup.track = cardToTrack[o].track;
        GameSetup.trackOptions = Options.instance
            .GetPerTrackOptions(GameSetup.track);
        PanelTransitioner.TransitionTo(selectPatternPanel,
            TransitionToPanel.Direction.Right);
    }

    private void OnErrorCardClick(GameObject o)
    {
        string error = cardToError[o];
        messageDialog.Show(error);
    }

    protected virtual void OnNewTrackCardClick()
    {
        throw new NotImplementedException(
            "SelectTrackPanel in the game scene should not show the New Track card.");
    }

    public void OnCardSelected(int cardIndex)
    {
        // Record the index of selected card so we can restore it
        // later. IndexOf return -1 if not found.
        selectedCardIndex = cardIndex;
    }
    #endregion
}
