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
           Options.instance.masterVolume);
        musicVolumeSlider.SetValueWithoutNotify(
            Options.instance.musicVolume);
        keysoundVolumeSlider.SetValueWithoutNotify(
            Options.instance.keysoundVolume);
        sfxVolumeSlider.SetValueWithoutNotify(
            Options.instance.sfxVolume);
        UpdateVolumeDisplay();
    }

    private void UpdateVolumeDisplay()
    {
        masterVolumeDisplay.text = VolumeValueToDisplay(
            Options.instance.masterVolume);
        musicVolumeDisplay.text = VolumeValueToDisplay(
            Options.instance.musicVolume);
        keysoundVolumeDisplay.text = VolumeValueToDisplay(
            Options.instance.keysoundVolume);
        sfxVolumeDisplay.text = VolumeValueToDisplay(
            Options.instance.sfxVolume);
    }

    private float VolumeValueToDb(float volume)
    {
        return (Mathf.Pow(volume, 0.25f) - 1f) * 80f;
    }

    private string VolumeValueToDisplay(float volume)
    {
        return Mathf.RoundToInt(volume * 100f).ToString();
    }

    public void ApplyVolume()
    {
        audioMixer.SetFloat("MasterVolume", VolumeValueToDb(
            Options.instance.masterVolume));
        audioMixer.SetFloat("MusicVolume", VolumeValueToDb(
            Options.instance.musicVolume));
        audioMixer.SetFloat("KeysoundVolume", VolumeValueToDb(
            Options.instance.keysoundVolume));
        audioMixer.SetFloat("SfxVolume", VolumeValueToDb(
            Options.instance.sfxVolume));
    }

    public void OnVolumeChanged()
    {
        Options.instance.masterVolume = masterVolumeSlider.value;
        Options.instance.musicVolume = musicVolumeSlider.value;
        Options.instance.keysoundVolume = keysoundVolumeSlider.value;
        Options.instance.sfxVolume = sfxVolumeSlider.value;

        UpdateVolumeDisplay();
        ApplyVolume();
    }
}
