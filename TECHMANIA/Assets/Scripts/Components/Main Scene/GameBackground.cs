using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Manages the BGA and backing track.
public class GameBackground
{
    // null if no backing track.
    public AudioClip backingTrack { get; private set; }
    private AudioSource backingSource => AudioSourceManager.instance
        .backingTrack;

    private VisualElement bgContainer;
    // null if no video.
    public ThemeApi.VideoElement bgaElement { get; private set; }
    private float bgaOffset;

    private PerTrackOptions trackOptions;  // To retrieve brightness
    private bool bgaCovered;

    #region Preparation
    public GameBackground(VisualElement bgContainer,
        PerTrackOptions trackOptions)
    {
        this.bgContainer = bgContainer;
        this.bgContainer.style.unityBackgroundImageTintColor =
            Color.clear;
        this.trackOptions = trackOptions;

        backingTrack = null;
        bgaElement = null;
        bgaCovered = false;
    }

    public void DisplayImage(Texture2D bgImage)
    {
        bgContainer.style.backgroundImage =
            new StyleBackground(bgImage);
        bgContainer.style.unityBackgroundScaleMode =
            new StyleEnum<ScaleMode>(ScaleMode.ScaleAndCrop);
        bgContainer.style.unityBackgroundImageTintColor = Color.white;
    }

    public void SetBackingTrack(AudioClip clip)
    {
        backingTrack = clip;
    }

    public void SetBga(ThemeApi.VideoElement element,
        bool loop,
        float offset)
    {
        bgaElement = element;
        bgaOffset = offset;
        bgaElement.targetElement = new
            ThemeApi.VisualElementWrap(bgContainer);
        bgaElement.isLooping = loop;
    }
    #endregion

    #region States
    public void Begin()
    {
        // BGA will start when base time hits bgaOffset.
        bgaElement?.Pause();
        bgaCovered = true;
        UpdateBgBrightness();
    }

    public void Pause()
    {
        if (backingTrack != null)
        {
            backingSource.Pause();
        }
        bgaElement?.Pause();
    }

    public void Unpause()
    {
        if (backingTrack != null)
        {
            backingSource.UnPause();
        }
        bgaElement?.Unpause();
    }

    public void Conclude()
    {
        backingTrack?.UnloadAudioData();
        bgaElement?.Dispose();
    }
    #endregion

    #region Update
    public void Update(float baseTime)
    {
        UpdateBackingTrack(baseTime);
        UpdateBga(baseTime);
    }

    private void UpdateBackingTrack(float baseTime)
    {
        if (backingTrack == null) return;

        if (baseTime >= 0 && !backingSource.isPlaying)
        {
            AudioSourceManager.instance.PlayBackingTrack(
                backingTrack,
                startTime: baseTime);
        }
        else if (baseTime < 0 && backingSource.isPlaying)
        {
            backingSource.Stop();
        }
    }

    private void UpdateBga(float baseTime)
    {
        if (bgaElement == null) return;

        if (baseTime >= bgaOffset && !bgaElement.isPlaying)
        {
            bgaElement.time = baseTime - bgaOffset;
            bgaElement.Play();
            bgaCovered = false;
            UpdateBgBrightness();
        }
        else if (baseTime < bgaOffset && bgaElement.isPlaying)
        {
            bgaElement.Pause();
            bgaCovered = true;
            UpdateBgBrightness();
        }
    }
    #endregion

    public void UpdateBgBrightness()
    {
        float alpha = (float)trackOptions.backgroundBrightness /
            PerTrackOptions.kMaxBrightness;
        if (bgaCovered) alpha = 0f;
        bgContainer.style.unityBackgroundImageTintColor =
            new StyleColor(new Color(1f, 1f, 1f, alpha));
    }
}