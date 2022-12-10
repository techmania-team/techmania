using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Manages the BGA and backing track.
public class GameBackground
{
    private Pattern pattern;

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

    private int playableLanes;  // To find hidden lanes
    private NoteManager noteManager;
    private KeysoundPlayer keysoundPlayer;

    #region Preparation
    public GameBackground(Pattern pattern,
        VisualElement bgContainer,
        PerTrackOptions trackOptions)
    {
        this.pattern = pattern;
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
        bgContainer.style.backgroundSize = new StyleBackgroundSize(
            new BackgroundSize(BackgroundSizeType.Cover));
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

    public void SetNoteManager(NoteManager noteManager,
        KeysoundPlayer keysoundPlayer,
        int playableLanes)
    {
        this.noteManager = noteManager;
        this.keysoundPlayer = keysoundPlayer;
        this.playableLanes = playableLanes;
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
    public void Update(float prevFrameBaseTime, float baseTime)
    {
        UpdateBackingTrack(prevFrameBaseTime, baseTime);
        UpdateBga(prevFrameBaseTime, baseTime);
        UpdateHiddenNotes(baseTime);
    }

    private void UpdateBackingTrack(float prevFrameBaseTime,
        float baseTime)
    {
        if (backingTrack == null) return;

        if (prevFrameBaseTime < 0f && baseTime >= 0f)
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

    private void UpdateBga(float prevFrameBaseTime, float baseTime)
    {
        if (bgaElement == null) return;

        if (prevFrameBaseTime < bgaOffset && baseTime >= bgaOffset)
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

    private void UpdateHiddenNotes(float baseTime)
    {
        for (int lane = playableLanes; lane < Pattern.kMaxLane; lane++)
        {
            if (noteManager.notesInLane[lane].Count == 0) continue;
            NoteElements upcomingNote = noteManager.notesInLane[lane]
                .First() as NoteElements;
            if (baseTime < upcomingNote.note.time) continue;

            bool musicChannel = pattern.ShouldPlayInMusicChannel(
                upcomingNote.note.lane);
            keysoundPlayer.Play(upcomingNote.note,
                hidden: musicChannel, emptyHit: false);
            noteManager.ResolveNote(upcomingNote);
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
