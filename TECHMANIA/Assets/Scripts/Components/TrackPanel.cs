using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TrackPanel : MonoBehaviour
{
    [Header("Metadata")]
    public InputField title;
    public InputField artist;
    public InputField genre;
    public Dropdown previewTrack;
    public InputField startTime;
    public InputField endTime;
    public Dropdown eyecatchImage;
    public Dropdown backgroundImage;
    public Dropdown backgroundVideo;
    public InputField videoStartTime;

    [Header("Patterns")]
    public VerticalLayoutGroup patternList;
    public GameObject patternTemplate;
    public Button deleteButton;
    public Button openButton;
    private Dictionary<GameObject, Pattern> objectToPattern;
    private GameObject selectedPatternObject;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        ResourcePanel.resourceRefreshed += RefreshDropdowns;
    }

    private void OnDisable()
    {
        ResourcePanel.resourceRefreshed -= RefreshDropdowns;
    }

    public void UIToMemory()
    {
        TrackMetadata metadata = Navigation.GetCurrentTrack().trackMetadata;
        bool madeChange = false;

        UIUtils.UpdatePropertyInMemory(ref metadata.title,
            title.text, ref madeChange);
        UIUtils.UpdatePropertyInMemory(ref metadata.artist,
            artist.text, ref madeChange);
        UIUtils.UpdatePropertyInMemory(ref metadata.genre,
            genre.text, ref madeChange);

        UIUtils.UpdatePropertyInMemoryFromDropdown(
            ref metadata.previewTrack, previewTrack, ref madeChange);
        UIUtils.UpdatePropertyInMemory(ref metadata.previewStartTime,
            startTime.text, ref madeChange);
        UIUtils.UpdatePropertyInMemory(ref metadata.previewEndTime,
            endTime.text, ref madeChange);

        UIUtils.UpdatePropertyInMemoryFromDropdown(
            ref metadata.eyecatchImage, eyecatchImage, ref madeChange);
        UIUtils.UpdatePropertyInMemoryFromDropdown(
            ref metadata.backImage, backgroundImage, ref madeChange);
        UIUtils.UpdatePropertyInMemoryFromDropdown(
            ref metadata.bga, backgroundVideo, ref madeChange);
        UIUtils.UpdatePropertyInMemory(ref metadata.bgaStartTime,
            videoStartTime.text, ref madeChange);

        if (madeChange)
        {
            Navigation.DoneWithChange();
        }
    }

    public void MemoryToUI()
    {
        TrackMetadata metadata = Navigation.GetCurrentTrack().trackMetadata;

        // Editing a InputField's text does not fire EndEdit events.
        title.text = metadata.title;
        artist.text = metadata.artist;
        genre.text = metadata.genre;

        UIUtils.MemoryToDropdown(metadata.previewTrack, previewTrack);
        startTime.text = metadata.previewStartTime.ToString();
        endTime.text = metadata.previewEndTime.ToString();

        UIUtils.MemoryToDropdown(metadata.eyecatchImage, eyecatchImage);
        UIUtils.MemoryToDropdown(metadata.backImage, backgroundImage);
        UIUtils.MemoryToDropdown(metadata.bga, backgroundVideo);
        videoStartTime.text = metadata.bgaStartTime.ToString();

        // Remove all patterns from pattern list, except for template.
        for (int i = 0; i < patternList.transform.childCount; i++)
        {
            GameObject pattern = patternList.transform.GetChild(i).gameObject;
            if (pattern == patternTemplate) continue;
            Destroy(pattern);
        }
        selectedPatternObject = null;
        RefreshPatternButtons();

        // Rebuild pattern list.
        objectToPattern = new Dictionary<GameObject, Pattern>();
        foreach (Pattern p in Navigation.GetCurrentTrack().patterns)
        {
            GameObject patternObject = Instantiate(patternTemplate);
            patternObject.name = "Pattern Panel";
            patternObject.transform.SetParent(patternList.transform);
            string textOnObject = $"{p.patternMetadata.patternName}\n" +
                $"<size=16>{p.patternMetadata.controlScheme} {p.patternMetadata.level}</size>";
            patternObject.GetComponentInChildren<Text>().text = textOnObject;
            patternObject.SetActive(true);
            objectToPattern.Add(patternObject, p);

            // TODO: double click to open?
            patternObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                SelectPattern(patternObject);
            });
        }
    }

    private void RefreshDropdowns()
    {
        UIUtils.RefreshFilenameDropdown(previewTrack, ResourcePanel.GetAudioFiles());
        UIUtils.RefreshFilenameDropdown(eyecatchImage, ResourcePanel.GetImageFiles());
        UIUtils.RefreshFilenameDropdown(backgroundImage, ResourcePanel.GetImageFiles());
        UIUtils.RefreshFilenameDropdown(backgroundVideo, ResourcePanel.GetVideoFiles());
        MemoryToUI();
    }

    private void RefreshPatternButtons()
    {
        deleteButton.interactable = selectedPatternObject != null;
        openButton.interactable = selectedPatternObject != null;
    }

    private void SelectPattern(GameObject patternObject)
    {
        if (selectedPatternObject != null)
        {
            selectedPatternObject.transform.Find("Selection").gameObject.SetActive(false);
        }
        if (!objectToPattern.ContainsKey(patternObject))
        {
            selectedPatternObject = null;
        }
        else
        {
            selectedPatternObject = patternObject;
            selectedPatternObject.transform.Find("Selection").gameObject.SetActive(true);
        }
        RefreshPatternButtons();
    }

    public void NewPattern()
    {
        StartCoroutine(InternalNewPattern());
    }

    private IEnumerator InternalNewPattern()
    {
        // Get pattern name.
        InputDialog.Show("Pattern name:");
        yield return new WaitUntil(() => { return InputDialog.IsResolved(); });
        if (InputDialog.GetResult() == InputDialog.Result.Cancelled)
        {
            yield break;
        }
        string name = InputDialog.GetValue();

        Pattern p = new Pattern();
        p.patternMetadata = new PatternMetadata();
        p.patternMetadata.patternName = name;

        Navigation.PrepareForChange();
        Navigation.GetCurrentTrack().patterns.Add(p);
        Navigation.DoneWithChange();

        MemoryToUI();
    }

    public void DeletePattern()
    {
        // This is undoable, so no need for confirmation.
        if (!objectToPattern.ContainsKey(selectedPatternObject))
        {
            return;
        }
        Navigation.PrepareForChange();
        Navigation.GetCurrentTrack().patterns.Remove(
            objectToPattern[selectedPatternObject]);
        Navigation.DoneWithChange();
        MemoryToUI();
    }

    public void Open()
    {
        if (selectedPatternObject == null) return;
        Navigation.SetCurrentPattern(objectToPattern[selectedPatternObject]);
        Navigation.GoTo(Navigation.Location.PatternMetadata);
    }
}
