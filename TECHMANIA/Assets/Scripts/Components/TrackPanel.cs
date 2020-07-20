using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TrackPanel : MonoBehaviour
{
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
        MemoryToUI();
        ResourcePanel.resourceRefreshed += RefreshDropdowns;
    }

    private void OnDisable()
    {
        ResourcePanel.resourceRefreshed -= RefreshDropdowns;
    }

    private void UpdateProperty(ref string property, string newValue, ref bool madeRecord)
    {
        if (property == newValue)
        {
            return;
        }
        if (!madeRecord)
        {
            Navigation.PrepareForChange();
            madeRecord = true;
        }
        property = newValue;
    }

    public void UIToMemory()
    {
        bool madeChange = false;
        UpdateProperty(ref Navigation.GetCurrentTrack().trackMetadata.title,
            title.text, ref madeChange);
        UpdateProperty(ref Navigation.GetCurrentTrack().trackMetadata.artist,
            artist.text, ref madeChange);
        UpdateProperty(ref Navigation.GetCurrentTrack().trackMetadata.genre,
            genre.text, ref madeChange);

        if (madeChange)
        {
            Navigation.DoneWithChange();
        }
    }

    public void MemoryToUI()
    {
        // This does NOT fire EndEdit events.
        title.text = Navigation.GetCurrentTrack().trackMetadata.title;
        artist.text = Navigation.GetCurrentTrack().trackMetadata.artist;
        genre.text = Navigation.GetCurrentTrack().trackMetadata.genre;
    }

    private void RefreshDropdowns()
    {
        RefreshDropdown(previewTrack, ResourcePanel.GetAudioFiles());
        RefreshDropdown(eyecatchImage, ResourcePanel.GetImageFiles());
        RefreshDropdown(backgroundImage, ResourcePanel.GetImageFiles());
        RefreshDropdown(backgroundVideo, ResourcePanel.GetVideoFiles());
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

        dropdown.value = newValue;
    }
}
