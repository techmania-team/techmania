using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrackAndPatternSideSheet : MonoBehaviour
{
    public Action<Setlist.PatternReference> callback;
    // If null, will start from select track.
    // If not null, will start from select pattern in the specified
    // track.
    public Setlist.PatternReference startingTrack;

    private void OnEnable()
    {
        if (startingTrack == null)
        {
            // Start from select track
            selectTrackLayout.SetActive(true);
            selectTrackLayout.GetComponent<CanvasGroup>().alpha = 1f;
            location = Paths.GetTrackRootFolder();
            ShowTracksInCurrentLocation();

            selectPatternLayout.SetActive(false);
            selectPatternLayout.GetComponent<CanvasGroup>().alpha = 0f;
        }
        else
        {
            // First, find the starting track
            GlobalResource.TrackInFolder trackInFolder;
            Pattern pattern;
            GlobalResource.SearchForPatternReference(startingTrack,
                out trackInFolder, out pattern);

            // Start from select pattern
            selectTrackLayout.SetActive(false);
            selectTrackLayout.GetComponent<CanvasGroup>().alpha = 0f;
            location = Paths.GoUpFrom(trackInFolder.folder);

            selectPatternLayout.SetActive(true);
            selectPatternLayout.GetComponent<CanvasGroup>().alpha = 1f;
            currentTrack = trackInFolder.minimizedTrack;
            ShowPatternsInCurrentTrack();
        }
        transitioning = false;
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
            GameObject button = Instantiate(subfolderPrefab,
                trackContainer);
            button.GetComponent<TrackAndPatternSidesheet
                .TrackSubfolderButton>().SetUp(this, subfolder);
        }
        foreach (GlobalResource.TrackInFolder trackInFolder in
            tracksInFolder)
        {
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
        if (transitioning) return;

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
                search(subfolder.fullPath);
            }
            foreach (GlobalResource.TrackInFolder trackInFolder in
                GlobalResource.GetTracksInFolder(folder))
            {
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
        if (transitioning) return;

        if (location != Paths.GetTrackRootFolder())
        {
            location = System.IO.Path.GetDirectoryName(location);
            ShowTracksInCurrentLocation();
        }
    }

    public void OnTrackSubfolderButtonClick(string subfolderFullPath)
    {
        if (transitioning) return;

        location = subfolderFullPath;
        ShowTracksInCurrentLocation();
    }

    public void OnTrackButtonClick(Track minimizedTrack)
    {
        if (transitioning) return;

        currentTrack = minimizedTrack;
        ShowPatternsInCurrentTrack();
        StartCoroutine(TrackToPattern());
    }
    #endregion

    #region Transition
    private bool transitioning;

    private IEnumerator TrackToPattern()
    {
        CanvasGroup track = selectTrackLayout.GetComponent<CanvasGroup>();
        CanvasGroup pattern = selectPatternLayout
            .GetComponent<CanvasGroup>();
        transitioning = true;

        const float transitionLength = 0.2f;
        float timer = 0f;
        track.alpha = 1f;
        pattern.alpha = 0f;
        while (timer < transitionLength)
        {
            timer += Time.deltaTime;
            float progress = timer / transitionLength;
            track.alpha = 1f - progress;
            yield return null;
        }
        track.alpha = 0f;
        selectTrackLayout.SetActive(false);
        selectPatternLayout.SetActive(true);
        timer = 0f;
        while (timer < transitionLength)
        {
            timer += Time.deltaTime;
            float progress = timer / transitionLength;
            pattern.alpha = progress;
            yield return null;
        }
        pattern.alpha = 1f;

        transitioning = false;
    }

    private IEnumerator PatternToTrack()
    {
        CanvasGroup track = selectTrackLayout.GetComponent<CanvasGroup>();
        CanvasGroup pattern = selectPatternLayout
            .GetComponent<CanvasGroup>();
        transitioning = true;

        const float transitionLength = 0.2f;
        float timer = 0f;
        track.alpha = 0f;
        pattern.alpha = 1f;
        while (timer < transitionLength)
        {
            timer += Time.deltaTime;
            float progress = timer / transitionLength;
            pattern.alpha = 1f - progress;
            yield return null;
        }
        pattern.alpha = 0f;
        selectPatternLayout.SetActive(false);
        selectTrackLayout.SetActive(true);
        timer = 0f;
        while (timer < transitionLength)
        {
            timer += Time.deltaTime;
            float progress = timer / transitionLength;
            track.alpha = progress;
            yield return null;
        }
        track.alpha = 1f;

        transitioning = false;
    }
    #endregion

    #region Patterns
    [Header("Select pattern")]
    public GameObject selectPatternLayout;
    public RectTransform patternContainer;
    public GameObject patternPrefab;

    private Track currentTrack;

    private void ShowPatternsInCurrentTrack()
    {
        for (int i = 0; i < patternContainer.childCount; i++)
        {
            Destroy(patternContainer.GetChild(i).gameObject);
        }
        foreach (Pattern p in currentTrack.patterns)
        {
            GameObject patternButton = Instantiate(patternPrefab,
                patternContainer);
            patternButton.GetComponent<TrackAndPatternSidesheet
                .PatternButton>().SetUp(this, p);
        }
    }

    public void OnBackToSelectTrackButtonClick()
    {
        if (transitioning) return;
        ShowTracksInCurrentLocation();
        StartCoroutine(PatternToTrack());
    }

    public void OnPatternButtonClick(Pattern pattern)
    {
        if (transitioning) return;

        Setlist.PatternReference reference =
            new Setlist.PatternReference()
        {
            trackTitle = currentTrack.trackMetadata.title,
            trackGuid = currentTrack.trackMetadata.guid,
            patternName = pattern.patternMetadata.patternName,
            patternLevel = pattern.patternMetadata.level,
            patternPlayableLanes = pattern.patternMetadata.playableLanes,
            patternGuid = pattern.patternMetadata.guid
        };
        callback(reference);
        GetComponent<Sidesheet>().FadeOut(silent: true);
    }
    #endregion
}
