using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class TimeEventDialog : MonoBehaviour
{
    public MaterialRadioButton changeBpmButton;
    public TMP_InputField bpmInputField;
    public MaterialRadioButton dontChangeBpmButton;

    public MaterialRadioButton stopTimeButton;
    public TMP_InputField timeStopInputField;
    public MaterialRadioButton dontStopTimeButton;

    private double? newBpm;
    private double? newTimeStop;
    private UnityAction<double?> confirmCallback;

    public void Show(BpmEvent currentEvent,
        UnityAction<double?> confirmCallback)
    {
        this.confirmCallback = confirmCallback;
        if (currentEvent == null)
        {
            newBpm = null;
            bpmInputField.text = "";
            UpdateRadioButtons();
        }
        else
        {
            newBpm = currentEvent.bpm;
            bpmInputField.text = currentEvent.bpm.ToString();
            UpdateRadioButtons();
        }
        GetComponent<Dialog>().FadeIn();
    }

    private void UpdateRadioButtons()
    {
        changeBpmButton.SetIsOn(newBpm.HasValue);
        dontChangeBpmButton.SetIsOn(!newBpm.HasValue);
    }

    public void OnChangeRadioButtonClick()
    {
        newBpm = Pattern.defaultBpm;  // Exact value doesn't matter
        UpdateRadioButtons();
    }

    public void OnNoChangeRadioButtonClick()
    {
        newBpm = null;
        UpdateRadioButtons();
    }

    public void OnOkButtonClick()
    {
        confirmCallback?.Invoke(newBpm);
    }

    public void OnBpmInputFieldSelect()
    {
        OnChangeRadioButtonClick();
    }

    public void OnBpmInputFieldEndEdit()
    {
        UIUtils.ClampInputField(bpmInputField,
            Pattern.minBpm, float.MaxValue);
        if (newBpm.HasValue)
        {
            newBpm = double.Parse(bpmInputField.text);
        }
    }
}
