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
    private int? newTimeStopPulses;
    private UnityAction<double?, int?> confirmCallback;

    public void Show(BpmEvent currentBpmEvent,
        TimeStop currentTimeStop,
        UnityAction<double?, int?> confirmCallback)
    {
        this.confirmCallback = confirmCallback;
        if (currentBpmEvent == null)
        {
            newBpm = null;
            bpmInputField.text = "";
        }
        else
        {
            newBpm = currentBpmEvent.bpm;
            bpmInputField.text = currentBpmEvent.bpm.ToString();
        }
        if (currentTimeStop == null)
        {
            newTimeStopPulses = null;
            timeStopInputField.text = "";
        }
        else
        {
            newTimeStopPulses = currentTimeStop.duration;
            timeStopInputField.text =
                PulseToBeatsDisplay(currentTimeStop.duration);
        }

        UpdateRadioButtons();
        GetComponent<Dialog>().FadeIn();
    }

    private string PulseToBeatsDisplay(int pulses)
    {
        float beats = (float)pulses / Pattern.pulsesPerBeat;
        return beats.ToString();
    }

    private void UpdateRadioButtons()
    {
        changeBpmButton.SetIsOn(newBpm.HasValue);
        dontChangeBpmButton.SetIsOn(!newBpm.HasValue);

        stopTimeButton.SetIsOn(newTimeStopPulses.HasValue);
        dontStopTimeButton.SetIsOn(!newTimeStopPulses.HasValue);
    }

    public void OnChangeBpmButtonClick()
    {
        newBpm = Pattern.defaultBpm;  // Exact value doesn't matter
        UpdateRadioButtons();
    }

    public void OnDontChangeBpmButtonClick()
    {
        newBpm = null;
        UpdateRadioButtons();
    }

    public void OnStopTimeButtonClick()
    {
        newTimeStopPulses = 0;
        UpdateRadioButtons();
    }

    public void OnDontStopTimeButtonClick()
    {
        newTimeStopPulses = null;
        UpdateRadioButtons();
    }

    public void OnOkButtonClick()
    {
        confirmCallback?.Invoke(newBpm, newTimeStopPulses);
    }

    public void OnBpmInputFieldSelect()
    {
        OnChangeBpmButtonClick();
    }

    public void OnBpmInputFieldEndEdit()
    {
        UIUtils.ClampInputField(bpmInputField,
            Pattern.minBpm, double.MaxValue);
        if (newBpm.HasValue)
        {
            newBpm = double.Parse(bpmInputField.text);
        }
    }

    public void OnTimeStopInputFieldSelect()
    {
        OnStopTimeButtonClick();
    }

    public void OnTimeStopInputFieldEndEdit()
    {
        UIUtils.ClampInputField(timeStopInputField,
            0.0, double.MaxValue);
        if (newTimeStopPulses.HasValue)
        {
            double newTimeStopBeats = double.Parse(
                timeStopInputField.text);
            newTimeStopPulses = Mathf.FloorToInt(
                (float)newTimeStopBeats * Pattern.pulsesPerBeat);
        }
    }
}
