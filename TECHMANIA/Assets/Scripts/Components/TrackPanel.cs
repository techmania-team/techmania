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
        // MemoryToUI();
        ResourcePanel.resourceRefreshed += RefreshDropdowns;
    }

    private void OnDisable()
    {
        ResourcePanel.resourceRefreshed -= RefreshDropdowns;
    }

    private void UpdateProperty(ref string property, string newValue, ref bool madeChange)
    {
        if (property == newValue)
        {
            return;
        }
        if (!madeChange)
        {
            Navigation.PrepareForChange();
            madeChange = true;
        }
        property = newValue;
    }

    private void UpdatePropertyFromDropdown(ref string property,
        Dropdown dropdown, ref bool madeChange)
    {
        UpdateProperty(ref property, dropdown.options[dropdown.value].text, ref madeChange);
    }

    private void UpdateProperty(ref double property, string newValueString, ref bool madeChange)
    {
        double newValue = double.Parse(newValueString);
        if (property == newValue)
        {
            return;
        }
        if (!madeChange)
        {
            Navigation.PrepareForChange();
            madeChange = true;
        }
        property = newValue;
    }

    public void UIToMemory()
    {
        TrackMetadata metadata = Navigation.GetCurrentTrack().trackMetadata;
        bool madeChange = false;

        UpdateProperty(ref metadata.title, title.text, ref madeChange);
        UpdateProperty(ref metadata.artist, artist.text, ref madeChange);
        UpdateProperty(ref metadata.genre, genre.text, ref madeChange);

        UpdatePropertyFromDropdown(ref metadata.previewTrack, previewTrack, ref madeChange);
        UpdateProperty(ref metadata.previewStartTime, startTime.text, ref madeChange);
        UpdateProperty(ref metadata.previewEndTime, endTime.text, ref madeChange);

        UpdatePropertyFromDropdown(ref metadata.eyecatchImage, eyecatchImage, ref madeChange);
        UpdatePropertyFromDropdown(ref metadata.backImage, backgroundImage, ref madeChange);
        UpdatePropertyFromDropdown(ref metadata.bga, backgroundVideo, ref madeChange);
        UpdateProperty(ref metadata.bgaStartTime, videoStartTime.text, ref madeChange);

        if (madeChange)
        {
            Navigation.DoneWithChange();
        }
    }

    private void MemoryToDropdown(string value, Dropdown dropdown)
    {
        int option = 0;
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (dropdown.options[i].text == value)
            {
                option = i;
                break;
            }
        }
        dropdown.SetValueWithoutNotify(option);
    }

    public void MemoryToUI()
    {
        TrackMetadata metadata = Navigation.GetCurrentTrack().trackMetadata;

        // Editing a InputField's text does not fire EndEdit events.
        title.text = metadata.title;
        artist.text = metadata.artist;
        genre.text = metadata.genre;

        MemoryToDropdown(metadata.previewTrack, previewTrack);
        startTime.text = metadata.previewStartTime.ToString();
        endTime.text = metadata.previewEndTime.ToString();

        MemoryToDropdown(metadata.eyecatchImage, eyecatchImage);
        MemoryToDropdown(metadata.backImage, backgroundImage);
        MemoryToDropdown(metadata.bga, backgroundVideo);
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
        RefreshDropdown(previewTrack, ResourcePanel.GetAudioFiles());
        RefreshDropdown(eyecatchImage, ResourcePanel.GetImageFiles());
        RefreshDropdown(backgroundImage, ResourcePanel.GetImageFiles());
        RefreshDropdown(backgroundVideo, ResourcePanel.GetVideoFiles());
        MemoryToUI();
    }

    private void RefreshDropdown(Dropdown dropdown, List<string> newOptions)
    {
        const string kNone = "(None)";
        string currentOption = dropdown.options[dropdown.value].text;
        int newValue = 0;

        dropdown.options.Clear();
        dropdown.options.Add(new Dropdown.OptionData(kNone));
        for (int i = 0; i < newOptions.Count; i++)
        {
            string name = new FileInfo(newOptions[i]).Name;
            if (currentOption == name)
            {
                newValue = i + 1;
            }
            dropdown.options.Add(new Dropdown.OptionData(name));
        }

        dropdown.SetValueWithoutNotify(newValue);
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
}
