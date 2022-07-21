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
    protected static string currentLocation;
    protected static int selectedCardIndex;
   
    static SelectTrackPanel()
    {
        currentLocation = "";
        selectedCardIndex = -1;
    }

    public static void ResetLocation()
    {
        currentLocation = "";
    }

    public Button backButton;
    public Button upgradeFormatButton;
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
    public GameObject upgradeErrorCardTemplate;
    public GameObject newTrackCard;
    public TextMeshProUGUI trackListBuildingProgress;
    public TextMeshProUGUI trackStatusText;
    public TrackFilterSidesheet trackFilterSidesheet;
    public Panel selectPatternPanel;
    public MessageDialog messageDialog;
    public ConfirmDialog confirmDialog;

    protected Dictionary<GameObject, string> cardToSubfolder;
    protected Dictionary<GameObject,
        GlobalResource.TrackInFolder> cardToTrack;
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

        DiscordController.SetActivity(DiscordActivityType.SelectingTrack);
    }

    protected void OnDisable()
    {
        TrackFilterSidesheet.trackFilterChanged -=
            OnTrackFilterChanged;
    }

    protected IEnumerator Refresh(bool upgradeVersion = false)
    {
        refreshing = true;

        // If player is inside streaming assets track folder in
        // select track panel, move them out to track root folder
        // when they enter editor selct track panel.
        if (ShowNewTrackCard() &&
            Paths.IsInStreamingAssets(currentLocation))
        {
            currentLocation = "";
            selectedCardIndex = -1;
        }

        // Initialization and/or disaster recovery.
        if (currentLocation == "" ||
            !UniversalIO.DirectoryExists(currentLocation))
        {
            currentLocation = Paths.GetTrackRootFolder();
        }

        // Show location.
        if (TrackFilter.instance.showTracksInAllFolders)
        {
            currentLocation = Paths.GetTrackRootFolder();
        }

        locationDisplay.text = Paths.HidePlatformInternalPath(
            currentLocation);

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
            GameObject o = trackGrid.transform.GetChild(i)
                .gameObject;
            if (o == trackCardTemplate) continue;
            if (o == errorCardTemplate) continue;
            if (o == upgradeErrorCardTemplate) continue;
            if (o == newTrackCard) continue;
            Destroy(o);
        }

        if (!GlobalResource.trackList.ContainsKey(currentLocation))
        {
            // Rebuild track list.
            backButton.interactable = false;
            upgradeFormatButton.gameObject.SetActive(false);
            trackFilterButton.interactable = false;
            refreshButton.interactable = false;
            goUpButton.interactable = false;
            newTrackCard.gameObject.SetActive(false);
            trackListBuildingProgress.gameObject.SetActive(true);
            trackStatusText.gameObject.SetActive(false);

            // Launch background worker.
            
            Options.TemporarilyDisableVSync();
            // TODO: GlobalResourceLoader.LoadTrackList
            Options.RestoreVSync();

            trackListBuildingProgress.gameObject.SetActive(false);
            backButton.interactable = true;
            trackFilterButton.interactable = true;
            refreshButton.interactable = true;
        }

        // Enable go up button if applicable.
        goUpButton.interactable = currentLocation !=
            Paths.GetTrackRootFolder();

        // Show upgrade button if any track is outdated.
        upgradeFormatButton.gameObject.SetActive(
            GlobalResource.anyOutdatedTrack);

        // Prepare subfolder list. Make a local copy so we can
        // sort it. This also applies to the track list below.
        List<GlobalResource.TrackSubfolder> subfolders = new List<GlobalResource.TrackSubfolder>();
        if (!TrackFilter.instance.showTracksInAllFolders)
        {
            if (GlobalResource.trackSubfolderList.ContainsKey(currentLocation))
            {
                foreach (GlobalResource.TrackSubfolder s in
                    GlobalResource.trackSubfolderList[currentLocation])
                {
                    // Don't show streaming assets in editor.
                    if (ShowNewTrackCard() &&
                        s.fullPath.Contains(
                            Paths.GetStreamingTrackRootFolder())
                    ) continue;
                    subfolders.Add(s);
                }
            }
            subfolders.Sort((GlobalResource.TrackSubfolder s1, GlobalResource.TrackSubfolder s2) =>
            {
                return string.Compare(s1.fullPath, s2.fullPath);
            });
        }

        // Instantiate subfolder cards.
        cardToSubfolder = new Dictionary<GameObject, string>();
        cardList = new List<GameObject>();
        bool subfolderGridEmpty = true;
        bool trackGridEmpty = true;
        foreach (GlobalResource.TrackSubfolder subfolder in subfolders)
        {
            GameObject card = Instantiate(subfolderCardTemplate,
                subfolderGrid.transform);
            card.name = "Subfolder Card";
            card.GetComponent<SubfolderCard>().Initialize(
                subfolder.name,
                subfolder.eyecatchFullPath);
            card.SetActive(true);
            subfolderGridEmpty = false;

            // Record mapping.
            cardToSubfolder.Add(card, subfolder.fullPath);
            AddToCardList(card);

            // Bind click event.
            card.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnSubfolderCardClick(card);
            });
        }

        // Prepare track list.
        List<GlobalResource.TrackInFolder> tracks = new List<GlobalResource.TrackInFolder>();
        if (TrackFilter.instance.showTracksInAllFolders)
        {
            foreach (List<GlobalResource.TrackInFolder> oneFolder in GlobalResource.trackList.Values)
            {
                foreach (GlobalResource.TrackInFolder t in oneFolder)
                {
                    tracks.Add(t);
                }
            }
        }
        else
        {
            foreach (GlobalResource.TrackInFolder t in GlobalResource.trackList[currentLocation])
            {
                tracks.Add(t);
            }
        }
        tracks.Sort(
            (GlobalResource.TrackInFolder t1, GlobalResource.TrackInFolder t2) =>
            {
                return Track.Compare(t1.minimizedTrack, t2.minimizedTrack,
                    TrackFilter.instance.sortBasis,
                    TrackFilter.instance.sortOrder);
            });

        // Instantiate track cards. Also apply filter.
        cardToTrack = new Dictionary<GameObject, GlobalResource.TrackInFolder>();
        foreach (GlobalResource.TrackInFolder trackInFolder in tracks)
        {
            if (trackFilterSidesheet.searchKeyword != null &&
                trackFilterSidesheet.searchKeyword != "" &&
                !trackInFolder.minimizedTrack.ContainsKeywords(
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
                trackInFolder.minimizedTrack.trackMetadata);
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
        if (GlobalResource.trackWithErrorList.ContainsKey(currentLocation))
        {
            foreach (GlobalResource.TrackWithError error in
                GlobalResource.trackWithErrorList[currentLocation])
            {
                GameObject card = null;
                string key = error.type switch
                {
                    GlobalResource.TrackWithError.Type.Load
                        => "select_track_error_format",
                    GlobalResource.TrackWithError.Type.Upgrade
                        => "select_track_upgrade_error_format",
                    _ => ""
                };
                string message = L10n.
                    GetStringAndFormatIncludingPaths(
                        key,
                        error.trackFile,
                        error.message);

                // Instantiate card.
                GameObject template = error.type switch
                {
                    GlobalResource.TrackWithError.Type.Load => errorCardTemplate,
                    GlobalResource.TrackWithError.Type.Upgrade => 
                        upgradeErrorCardTemplate,
                    _ => null
                };
                card = Instantiate(template, trackGrid.transform);
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
            trackStatusText.text = L10n.GetStringAndFormat(
                "select_track_some_tracks_hidden_text",
                tracks.Count - cardToTrack.Count,
                trackFilterSidesheet.searchKeyword);
        }
        else if (cardToSubfolder.Count + 
            cardToTrack.Count + cardToError.Count == 0)
        {
            trackStatusText.gameObject.SetActive(true);
            trackStatusText.text = L10n.GetString(
                "select_track_no_track_text");
        }
        else
        {
            trackStatusText.gameObject.SetActive(false);
        }

        refreshing = false;

        if (upgradeVersion)
        {
            bool anyUpgradeError = false;
            foreach (List<GlobalResource.TrackWithError> list in GlobalResource.trackWithErrorList.Values)
            {
                foreach (GlobalResource.TrackWithError e in list)
                {
                    if (e.type == GlobalResource.TrackWithError.Type.Upgrade)
                    {
                        anyUpgradeError = true;
                        break;
                    }
                }
                if (anyUpgradeError) break;
            }
            messageDialog.Show(L10n.GetString(
                anyUpgradeError ?
                "select_track_upgrade_complete_with_error_message" :
                "select_track_upgrade_complete_message"));
        }
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
        Selectable down = EventSystem.current
            .currentSelectedGameObject
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

    #region Events from cards and buttons
    public void OnUpgradeFormatButtonClick()
    {
        if (!GlobalResource.anyOutdatedTrack) return;

        confirmDialog.Show(
            L10n.GetString(
                "select_track_upgrade_version_confirmation"),
            L10n.GetString("select_track_upgrade_version_confirm"),
            L10n.GetString("select_track_upgrade_version_cancel"),
            () =>
            {
                GlobalResourceLoader.ClearCachedTrackList();
                StartCoroutine(Refresh(upgradeVersion: true));
            });
    }

    public void OnRefreshButtonClick()
    {
        GlobalResourceLoader.ClearCachedTrackList();
        StartCoroutine(Refresh());
    }

    public void OnGoUpButtonClick()
    {
        Debug.Log(currentLocation);

        currentLocation = Path.GetDirectoryName(currentLocation);
        if (currentLocation.Equals(
            Paths.GetStreamingTrackRootFolder()))
            currentLocation = Paths.GetTrackRootFolder();

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
        GameSetup.trackOptions = Options.instance
            .GetPerTrackOptions(
            cardToTrack[o].minimizedTrack.trackMetadata.guid);
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
