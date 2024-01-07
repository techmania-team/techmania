using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PatternPanelPlaybackBar : MonoBehaviour
{
    public PatternPanel panel;
    public GameObject playButton;
    public GameObject stopButton;
    public GameObject audioLoadingIndicator;
    public TextMeshProUGUI timeDisplay;
    public Slider scanlinePositionSlider;
    public Button previewButton;

    public void InternalOnEnable()
    {
        scanlinePositionSlider.SetValueWithoutNotify(0f);
    }

    public void InternalOnDisable() { }

    #region Play and stop
    public void EnablePlayButton(bool enabled)
    {
        playButton.GetComponent<Button>().interactable = enabled;
    }

    public bool playButtonEnabled =>
        playButton.GetComponent<Button>().interactable;

    public void UpdatePlaybackUI()
    {
        if (panel.audioLoaded)
        {
            playButton.SetActive(!panel.isPlaying);
            stopButton.SetActive(panel.isPlaying);
        }
        else
        {
            playButton.SetActive(false);
            stopButton.SetActive(false);
        }
        audioLoadingIndicator.SetActive(!panel.audioLoaded);
        scanlinePositionSlider.interactable = !panel.isPlaying;
        previewButton.interactable = panel.audioLoaded;
    }
    #endregion

    #region Time and scanline
    public void OnScanlinePositionSliderValueChanged(float newValue)
    {
        if (panel.isPlaying) return;

        int totalPulses = panel.workspace.numScans
            * EditorContext.Pattern.patternMetadata.bps
            * Pattern.pulsesPerBeat;
        panel.workspace.scanlineFloatPulse = totalPulses * newValue;
        panel.workspace.ScrollScanlineIntoView();

        RefreshScanlineTimeDisplay();
    }

    private void RefreshScanlineTimeDisplay()
    {
        float scanlineTime = EditorContext.Pattern.PulseToTime(
            (int)panel.workspace.scanlineFloatPulse);
        timeDisplay.text = UIUtils.FormatTime(scanlineTime,
            includeMillisecond: true);
    }
    
    public void Refresh()
    {
        RefreshScanlineTimeDisplay();

        int bps = EditorContext.Pattern.patternMetadata.bps;
        float scanlineNormalizedPosition = 
            panel.workspace.scanlineFloatPulse /
            (panel.workspace.numScans * bps * Pattern.pulsesPerBeat);

        scanlinePositionSlider.SetValueWithoutNotify(
            scanlineNormalizedPosition);
    }
    #endregion

    #region Preview
    public void OnPreviewButtonClick()
    {
        EditorContext.previewStartingScan =
            Mathf.FloorToInt(
                panel.workspace.scanlineFloatPulse /
                Pattern.pulsesPerBeat /
                EditorContext.Pattern.patternMetadata.bps);
        panel.RecordScanlinePulseBeforePreview();
        previewButton.GetComponent<CustomTransitionToEditorPreview>()
            .Invoke();
    }
    #endregion
}
