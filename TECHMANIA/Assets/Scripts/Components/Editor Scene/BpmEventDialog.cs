using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class BpmEventDialog : MonoBehaviour
{
    public MaterialRadioButton changeRadioButton;
    public TMP_InputField bpmInputField;
    public MaterialRadioButton noChangeRadioButton;

    private double? newBpm;
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
        changeRadioButton.SetIsOn(newBpm.HasValue);
        noChangeRadioButton.SetIsOn(!newBpm.HasValue);
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
