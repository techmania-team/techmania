using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrackAndPatternSideSheet : MonoBehaviour
{
    public Action<Setlist.PatternReference> callback;

    private void OnEnable()
    {
        selectTrackLayout.SetActive(true);
        location = Paths.GetTrackRootFolder();
        ShowTracksInCurrentLocation();

        selectPatternLayout.SetActive(false);
    }

    #region Tracks
    [Header("Select track")]
    public GameObject selectTrackLayout;
    public TMP_InputField searchBox;
    public Button goUpButton;
    public TextMeshProUGUI locationDisplay;
    public RectTransform trackContainer;
    public GameObject subfolderPrefab;
    public GameObject trackPrefab;

    private string location;

    private void ShowTracksInCurrentLocation()
    {
        goUpButton.interactable = location != Paths.GetTrackRootFolder();
        locationDisplay.text = System.IO.Path
            .GetFileNameWithoutExtension(location);

        for (int i = 0; i < trackContainer.childCount; i++)
        {
            Destroy(trackContainer.GetChild(i).gameObject);
        }

        List<GlobalResource.Subfolder> subfolders =
            new List<GlobalResource.Subfolder>(
            GlobalResource.GetTrackSubfolders(location));
        subfolders.Sort((GlobalResource.Subfolder s1, 
            GlobalResource.Subfolder s2) =>
            string.Compare(s1.name, s2.name));
        List<GlobalResource.TrackInFolder> tracksInFolder =
            new List<GlobalResource.TrackInFolder>(
            GlobalResource.GetTracksInFolder(location));
        tracksInFolder.Sort((GlobalResource.TrackInFolder t1,
            GlobalResource.TrackInFolder t2) =>
            string.Compare(t1.minimizedTrack.trackMetadata.title,
                t2.minimizedTrack.trackMetadata.title));
        foreach (GlobalResource.Subfolder subfolder in subfolders)
        {
            if (Paths.IsInStreamingAssets(subfolder.fullPath)) continue;
            GameObject button = Instantiate(subfolderPrefab,
                trackContainer);
            button.GetComponent<TrackAndPatternSidesheet
                .TrackSubfolderButton>().SetUp(this, subfolder);
        }
        foreach (GlobalResource.TrackInFolder trackInFolder in
            tracksInFolder)
        {
            if (Paths.IsInStreamingAssets(trackInFolder.folder)) continue;
            GameObject button = Instantiate(trackPrefab,
                trackContainer);
            button.GetComponent<TrackAndPatternSidesheet
                .TrackButton>().SetUp(this, trackInFolder);
        }
    }

    private void ShowSpecificTrackList(
        List<GlobalResource.TrackInFolder> tracksInFolder)
    {
        goUpButton.interactable = false;
        locationDisplay.text = "";

        for (int i = 0; i < trackContainer.childCount; i++)
        {
            Destroy(trackContainer.GetChild(i).gameObject);
        }

        tracksInFolder.Sort((GlobalResource.TrackInFolder t1, 
            GlobalResource.TrackInFolder t2) =>
            string.Compare(t1.minimizedTrack.trackMetadata.title, 
                t2.minimizedTrack.trackMetadata.title));
        foreach (GlobalResource.TrackInFolder trackInFolder in 
            tracksInFolder)
        {
            GameObject button = Instantiate(trackPrefab,
                trackContainer);
            button.GetComponent<TrackAndPatternSidesheet
                .TrackButton>().SetUp(this, trackInFolder);
        }
    }

    public void OnSearchTermChanged(string newTerm)
    {
        newTerm = newTerm.Trim();
        if (newTerm == "")
        {
            location = Paths.GetTrackRootFolder();
            ShowTracksInCurrentLocation();
            return;
        }
        string[] terms = newTerm.Split(' ');

        List<GlobalResource.TrackInFolder> tracksInFolder = 
            new List<GlobalResource.TrackInFolder>();

        Action<string> search = null;
        search = (string folder) =>
        {
            foreach (GlobalResource.Subfolder subfolder in
                GlobalResource.GetTrackSubfolders(folder))
            {
                if (Paths.IsInStreamingAssets(subfolder.fullPath)) 
                    continue;
                search(subfolder.fullPath);
            }
            foreach (GlobalResource.TrackInFolder trackInFolder in
                GlobalResource.GetTracksInFolder(folder))
            {
                if (Paths.IsInStreamingAssets(trackInFolder.folder)) 
                    continue;
                string title = trackInFolder.minimizedTrack
                    .trackMetadata.title;
                bool hasAllTerms = true;
                foreach (string term in terms)
                {
                    if (!title.ToLower().Contains(term.ToLower()))
                    {
                        hasAllTerms = false;
                        break;
                    }
                }
                if (hasAllTerms)
                {
                    tracksInFolder.Add(trackInFolder);
                }
            }
        };
        search(Paths.GetTrackRootFolder());

        ShowSpecificTrackList(tracksInFolder);
    }

    public void OnGoUpButtonClick()
    {
        if (location != Paths.GetTrackRootFolder())
        {
            location = System.IO.Path.GetDirectoryName(location);
            ShowTracksInCurrentLocation();
        }
    }

    public void OnTrackSubfolderButtonClick(string subfolderFullPath)
    {
        location = subfolderFullPath;
        ShowTracksInCurrentLocation();
    }

    public void OnTrackButtonClick(Track minimizedTrack)
    {
        // TODO
    }

    #endregion

    #region Patterns
    [Header("Select pattern")]
    public GameObject selectPatternLayout;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
