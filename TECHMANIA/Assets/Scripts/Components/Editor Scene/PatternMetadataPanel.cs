using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// TODO: deprecate this.
public class PatternMetadataPanel : MonoBehaviour
{
    public InputField nameInput;
    public InputField level;
    public Dropdown controlScheme;
    public Dropdown backingTrack;
    public InputField firstBeatOffset;
    public InputField initialBpm;
    public InputField initialBps;

    public void UIToMemory()
    {
        PatternMetadata metadata = EditorNavigation.GetCurrentPattern().patternMetadata;
        bool madeChange = false;

        UIUtils.UpdatePropertyInMemory(ref metadata.patternName,
            nameInput.text, ref madeChange);
        // UIUtils.ClampInputField(level, Pattern.minLevel, Pattern.maxLevel);
        UIUtils.UpdatePropertyInMemory(ref metadata.level,
            level.text, ref madeChange);

        // Special handling for control scheme
        if ((int)metadata.controlScheme != controlScheme.value)
        {
            if (!madeChange)
            {
                EditorNavigation.PrepareForChange();
                madeChange = true;
            }
            metadata.controlScheme = (ControlScheme)controlScheme.value;
        }

        // UIUtils.UpdatePropertyInMemory(
        //     ref metadata.backingTrack, backingTrack, ref madeChange);

        UIUtils.UpdatePropertyInMemory(
            ref metadata.firstBeatOffset, firstBeatOffset.text, ref madeChange);
        // UIUtils.ClampInputField(initialBpm, Pattern.minBpm, Pattern.maxBpm);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.initBpm, initialBpm.text, ref madeChange);
        // UIUtils.ClampInputField(initialBps, Pattern.minBps, Pattern.maxBps);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.bps, initialBps.text, ref madeChange);

        if (madeChange)
        {
            EditorNavigation.DoneWithChange();
        }
    }
}
