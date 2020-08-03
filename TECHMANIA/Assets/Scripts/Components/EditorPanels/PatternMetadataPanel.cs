using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class PatternMetadataPanel : MonoBehaviour
{
    public InputField nameInput;
    public InputField level;
    public Dropdown controlScheme;
    public Dropdown backingTrack;
    public InputField firstBeatOffset;
    public InputField initialBpm;
    public InputField initialBps;

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
        PatternMetadata metadata = Navigation.GetCurrentPattern().patternMetadata;
        bool madeChange = false;

        UIUtils.UpdatePropertyInMemory(ref metadata.patternName,
            nameInput.text, ref madeChange);
        UIUtils.ClampInputField(level, Pattern.minLevel, Pattern.maxLevel);
        UIUtils.UpdatePropertyInMemory(ref metadata.level,
            level.text, ref madeChange);

        // Special handling for control scheme
        if ((int)metadata.controlScheme != controlScheme.value)
        {
            if (!madeChange)
            {
                Navigation.PrepareForChange();
                madeChange = true;
            }
            metadata.controlScheme = (ControlScheme)controlScheme.value;
        }

        UIUtils.UpdatePropertyInMemoryFromDropdown(
            ref metadata.backingTrack, backingTrack, ref madeChange);

        UIUtils.UpdatePropertyInMemory(
            ref metadata.firstBeatOffset, firstBeatOffset.text, ref madeChange);
        UIUtils.ClampInputField(initialBpm, Pattern.minBpm, Pattern.maxBpm);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.initBpm, initialBpm.text, ref madeChange);
        UIUtils.ClampInputField(initialBps, Pattern.minBps, Pattern.maxBps);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.bps, initialBps.text, ref madeChange);

        if (madeChange)
        {
            Navigation.DoneWithChange();
        }
    }

    public void MemoryToUI()
    {
        PatternMetadata metadata = Navigation.GetCurrentPattern().patternMetadata;

        nameInput.text = metadata.patternName;
        level.text = metadata.level.ToString();
        controlScheme.SetValueWithoutNotify((int)metadata.controlScheme);

        UIUtils.MemoryToDropdown(metadata.backingTrack, backingTrack);

        firstBeatOffset.text = metadata.firstBeatOffset.ToString();
        initialBpm.text = metadata.initBpm.ToString();
        initialBps.text = metadata.bps.ToString();
    }

    public void RefreshDropdowns()
    {
        UIUtils.RefreshFilenameDropdown(backingTrack, ResourcePanel.GetAudioFiles());
        MemoryToUI();
    }

    public void Edit()
    {
        Navigation.GoTo(Navigation.Location.Pattern);
    }
}
