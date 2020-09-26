using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// TODO: deprecate this.
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
    private int selectedPatternIndex;

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
        TrackMetadata metadata = EditorNavigation.GetCurrentTrack().trackMetadata;
        bool madeChange = false;

        UIUtils.UpdatePropertyInMemory(ref metadata.title,
            title.text, ref madeChange);
        UIUtils.UpdatePropertyInMemory(ref metadata.artist,
            artist.text, ref madeChange);
        UIUtils.UpdatePropertyInMemory(ref metadata.genre,
            genre.text, ref madeChange);

        UIUtils.UpdatePropertyInMemoryFromDropdown(
            ref metadata.previewTrack, previewTrack, ref madeChange);
        UIUtils.ClampInputField(startTime, 0.0, double.MaxValue);
        UIUtils.UpdatePropertyInMemory(ref metadata.previewStartTime,
            startTime.text, ref madeChange);
        UIUtils.ClampInputField(endTime, 0.0, double.MaxValue);
        UIUtils.UpdatePropertyInMemory(ref metadata.previewEndTime,
            endTime.text, ref madeChange);

        UIUtils.UpdatePropertyInMemoryFromDropdown(
            ref metadata.eyecatchImage, eyecatchImage, ref madeChange);
        UIUtils.UpdatePropertyInMemoryFromDropdown(
            ref metadata.backImage, backgroundImage, ref madeChange);
        UIUtils.UpdatePropertyInMemoryFromDropdown(
            ref metadata.bga, backgroundVideo, ref madeChange);
        UIUtils.UpdatePropertyInMemory(ref metadata.bgaOffset,
            videoStartTime.text, ref madeChange);

        if (madeChange)
        {
            EditorNavigation.DoneWithChange();
        }
    }

    public void MemoryToUI()
    {
        TrackMetadata metadata = EditorNavigation.GetCurrentTrack().trackMetadata;

        // Remove all patterns from pattern list, except for template.
        for (int i = 0; i < patternList.transform.childCount; i++)
        {
            GameObject pattern = patternList.transform.GetChild(i).gameObject;
            if (pattern == patternTemplate) continue;
            Destroy(pattern);
        }
        selectedPatternIndex = -1;
        RefreshPatternButtons();

        // Sort patterns.
        EditorNavigation.GetCurrentTrack().SortPatterns();

        // Rebuild pattern list.
        for (int i = 0; i < EditorNavigation.GetCurrentTrack().patterns.Count; i++)
        {
            Pattern p = EditorNavigation.GetCurrentTrack().patterns[i];

            GameObject patternObject = Instantiate(patternTemplate);
            patternObject.name = "Pattern Panel";
            patternObject.transform.SetParent(patternList.transform);
            string textOnObject = $"{p.patternMetadata.patternName}\n" +
                $"<size=16>{p.patternMetadata.controlScheme} {p.patternMetadata.level}</size>";
            patternObject.GetComponentInChildren<Text>().text = textOnObject;
            patternObject.SetActive(true);

            int copyOfI = i;
            patternObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                SelectPattern(copyOfI);
            });
        }
    }

    public void RefreshDropdowns()
    {
        MemoryToUI();
    }

    private void RefreshPatternButtons()
    {
        deleteButton.interactable = selectedPatternIndex >= 0;
        openButton.interactable = selectedPatternIndex >= 0;
    }

    private Transform PatternIndexToTransform(int index)
    {
        // Child #0 is the template?
        return patternList.transform.GetChild(index + 1);
    }

    private void SelectPattern(int index)
    {
        if (selectedPatternIndex >= 0)
        {
            PatternIndexToTransform(selectedPatternIndex)
                .Find("Selection").gameObject.SetActive(false);
        }
        selectedPatternIndex = index;
        if (selectedPatternIndex >= 0)
        {
            PatternIndexToTransform(selectedPatternIndex)
                .Find("Selection").gameObject.SetActive(true);
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
        InputDialog.Show("Pattern name:", InputField.ContentType.Standard);
        yield return new WaitUntil(() => { return InputDialog.IsResolved(); });
        if (InputDialog.GetResult() == InputDialog.Result.Cancelled)
        {
            yield break;
        }
        string name = InputDialog.GetValue();

        Pattern p = new Pattern();
        p.patternMetadata = new PatternMetadata();
        p.patternMetadata.patternName = name;

        EditorNavigation.PrepareForChange();
        EditorNavigation.GetCurrentTrack().patterns.Add(p);
        EditorNavigation.DoneWithChange();

        MemoryToUI();
    }

    public void DeletePattern()
    {
        // This is undoable, so no need for confirmation.
        EditorNavigation.PrepareForChange();
        EditorNavigation.GetCurrentTrack().patterns.RemoveAt(selectedPatternIndex);
        EditorNavigation.DoneWithChange();
        MemoryToUI();
    }

    public void Open()
    {
        if (selectedPatternIndex < 0) return;
        EditorNavigation.SetCurrentPattern(selectedPatternIndex);
        EditorNavigation.GoTo(EditorNavigation.Location.PatternMetadata);
    }
}
