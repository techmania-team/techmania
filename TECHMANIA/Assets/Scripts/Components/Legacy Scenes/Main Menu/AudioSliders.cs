using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSliders : MonoBehaviour
{
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeDisplay;
    public Slider musicVolumeSlider;
    public TextMeshProUGUI musicVolumeDisplay;
    public Slider keysoundVolumeSlider;
    public TextMeshProUGUI keysoundVolumeDisplay;
    public Slider sfxVolumeSlider;
    public TextMeshProUGUI sfxVolumeDisplay;
    public AudioMixer audioMixer;

    private void OnEnable()
    {
        MemoryToUI();
    }

    private void MemoryToUI()
    {
        masterVolumeSlider.SetValueWithoutNotify(
            Options.instance.masterVolumePercent);
        musicVolumeSlider.SetValueWithoutNotify(
            Options.instance.musicVolumePercent);
        keysoundVolumeSlider.SetValueWithoutNotify(
            Options.instance.keysoundVolumePercent);
        sfxVolumeSlider.SetValueWithoutNotify(
            Options.instance.sfxVolumePercent);
        UpdateVolumeDisplay();
    }

    private void UpdateVolumeDisplay()
    {
        masterVolumeDisplay.text =
            Options.instance.masterVolumePercent.ToString();
        musicVolumeDisplay.text = 
            Options.instance.musicVolumePercent.ToString();
        keysoundVolumeDisplay.text = 
            Options.instance.keysoundVolumePercent.ToString();
        sfxVolumeDisplay.text = 
            Options.instance.sfxVolumePercent.ToString();
    }

    public void ApplyVolume()
    {
        audioMixer.SetFloat("MasterVolume", Options.VolumeValueToDb(
            Options.instance.masterVolumePercent));
        audioMixer.SetFloat("MusicVolume", Options.VolumeValueToDb(
            Options.instance.musicVolumePercent));
        audioMixer.SetFloat("KeysoundVolume", Options.VolumeValueToDb(
            Options.instance.keysoundVolumePercent));
        audioMixer.SetFloat("SfxVolume", Options.VolumeValueToDb(
            Options.instance.sfxVolumePercent));
    }

    public void OnVolumeChanged()
    {
        Options.instance.masterVolumePercent =
            Mathf.FloorToInt(masterVolumeSlider.value);
        Options.instance.musicVolumePercent = 
            Mathf.FloorToInt(musicVolumeSlider.value);
        Options.instance.keysoundVolumePercent = 
            Mathf.FloorToInt(keysoundVolumeSlider.value);
        Options.instance.sfxVolumePercent = 
            Mathf.FloorToInt(sfxVolumeSlider.value);

        UpdateVolumeDisplay();
        ApplyVolume();
    }
}
