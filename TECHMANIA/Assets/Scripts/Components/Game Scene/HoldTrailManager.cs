using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Common routines between NoteAppearance and HoldExtension.
// This handles the initialization, visibility and
// updating of duration trails and ongoing trails.
//
// Works for both hold notes and repeat hold notes.

public class HoldTrailManager : MonoBehaviour
{
    public RectTransform durationTrail;
    public RectTransform durationTrailEnd;
    public RectTransform ongoingTrail;
    public RectTransform ongoingTrailEnd;

    private Scanline scanlineRef;
    public NoteType noteType { get; private set; }
    private float durationTrailInitialWidth;
    private bool trailExtendsLeft;

    public void Initialize(Scan scanRef, Scanline scanlineRef,
        HoldNote holdNote)
    {
        this.scanlineRef = scanlineRef;
        noteType = holdNote.type;

        float startX = GetComponent<RectTransform>()
            .anchoredPosition.x;
        float endX = scanRef.FloatPulseToXPosition(
            holdNote.pulse + holdNote.duration,
            positionEndOfScanOutOfBounds: false,
            positionAfterScanOutOfBounds: true);
        trailExtendsLeft = endX < startX;
        durationTrailInitialWidth = Mathf.Abs(startX - endX);

        durationTrail.sizeDelta = new Vector2(
            durationTrailInitialWidth,
            durationTrail.sizeDelta.y);
        if (trailExtendsLeft)
        {
            durationTrail.localRotation =
                Quaternion.Euler(0f, 0f, 180f);
            if (ongoingTrail != null)
            {
                ongoingTrail.localRotation =
                    Quaternion.Euler(0f, 0f, 180f);
            }
        }
        if (ongoingTrail != null)
        {
            ongoingTrail.sizeDelta = new Vector2(0f,
                ongoingTrail.sizeDelta.y);
        }

        InitializeScale();
    }

    private void InitializeScale()
    {
        float durationTrailScale = 1f;
        float ongoingTrailScale = 1f;
        float durationTrailEndAspectRatio = 1f;
        float ongoingTrailEndAspectRatio = 1f;
        if (noteType == NoteType.Hold)
        {
            durationTrailScale = GlobalResource.noteSkin.holdTrail.scale;
            Rect rect = GlobalResource.noteSkin.holdTrailEnd
                .sprites[0].rect;
            durationTrailEndAspectRatio = rect.width / rect.height;

            ongoingTrailScale = GlobalResource.noteSkin
                .holdOngoingTrail.scale;
            rect = GlobalResource.noteSkin.holdOngoingTrailEnd
                .sprites[0].rect;
            ongoingTrailEndAspectRatio = rect.width / rect.height;

        }
        else
        {
            durationTrailScale = GlobalResource.noteSkin
                .repeatHoldTrail.scale;
            Rect rect = GlobalResource.noteSkin.repeatHoldTrailEnd
                .sprites[0].rect;
            durationTrailEndAspectRatio = rect.width / rect.height;
        }

        durationTrail.localScale = new Vector3(1f,
            durationTrailScale,
            1f);
        // The trail's scale is applied to the trail end, so
        // compensate for it here.
        durationTrailEnd.localScale = new Vector3(
            durationTrailScale, 1f, 1f);
        durationTrailEnd.GetComponent<AspectRatioFitter>().aspectRatio =
            durationTrailEndAspectRatio;
        if (ongoingTrail != null)
        {
            ongoingTrail.localScale = new Vector3(1f,
                ongoingTrailScale,
                1f);
            ongoingTrailEnd.localScale = new Vector3(
                ongoingTrailScale, 1f, 1f);
            ongoingTrailEnd.GetComponent<AspectRatioFitter>()
                .aspectRatio = ongoingTrailEndAspectRatio;
        }
    }

    public void SetVisibility(NoteAppearance.Visibility v)
    {
        bool active = v != NoteAppearance.Visibility.Hidden;
        durationTrail.gameObject.SetActive(active);
        durationTrailEnd.gameObject.SetActive(active);
        if (ongoingTrail != null)
        {
            ongoingTrail.gameObject.SetActive(active);
            ongoingTrailEnd.gameObject.SetActive(active);
        }

        Color color = (v == NoteAppearance.Visibility.Transparent) ?
            new Color(1f, 1f, 1f, 0.6f) :
            Color.white;
        durationTrail.GetComponent<Image>().color = color;
        if (ongoingTrail != null)
        {
            ongoingTrail.GetComponent<Image>().color = color;
        }
    }

    public void UpdateTrails()
    {
        float startX = GetComponent<RectTransform>()
            .anchoredPosition.x;
        float endX = scanlineRef.GetComponent<RectTransform>()
            .anchoredPosition.x;
        float width = Mathf.Min(Mathf.Abs(startX - endX),
            durationTrailInitialWidth);

        // Override width to 0 if the scanline is on the wrong side.
        float durationTrailDirection =
            durationTrailEnd.transform.position.x -
            transform.position.x;
        float scanlineDirection =
            scanlineRef.transform.position.x -
            transform.position.x;
        if (durationTrailDirection * scanlineDirection < 0f)
        {
            width = 0f;
        }

        if (noteType == NoteType.Hold)
        {
            ongoingTrail.sizeDelta = new Vector2(width,
                ongoingTrail.sizeDelta.y);
        }
        else
        {
            durationTrail.anchoredPosition = new Vector2(
                trailExtendsLeft ? -width : width,
                0f);
            durationTrail.sizeDelta = new Vector2(
                durationTrailInitialWidth - width,
                durationTrail.sizeDelta.y);
        }

        UpdateSprites();
    }

    public void UpdateSprites()
    {
        Sprite durationTrailSprite = null;
        Sprite durationTrailEndSprite = null;
        Sprite ongoingTrailSprite = null;
        Sprite ongoingTrailEndSprite = null;
        if (noteType == NoteType.Hold)
        {
            durationTrailSprite = GlobalResource.noteSkin.holdTrail
                .GetSpriteForFloatBeat(Game.Time);
            durationTrailEndSprite = GlobalResource.noteSkin.holdTrailEnd
                .GetSpriteForFloatBeat(Game.Time);
            ongoingTrailSprite = GlobalResource.noteSkin.holdOngoingTrail
                .GetSpriteForFloatBeat(Game.Time);
            ongoingTrailEndSprite = 
                GlobalResource.noteSkin.holdOngoingTrailEnd
                .GetSpriteForFloatBeat(Game.Time);
        }
        else
        {
            durationTrailSprite = GlobalResource.noteSkin.repeatHoldTrail
                .GetSpriteForFloatBeat(Game.Time);
            durationTrailEndSprite = 
                GlobalResource.noteSkin.repeatHoldTrailEnd
                .GetSpriteForFloatBeat(Game.Time);
        }

        durationTrail.GetComponent<Image>().sprite =
            durationTrailSprite;
        durationTrailEnd.GetComponent<Image>().sprite =
            durationTrailEndSprite;
        if (ongoingTrail != null)
        {
            ongoingTrail.GetComponent<Image>().sprite =
                ongoingTrailSprite;
            ongoingTrailEnd.GetComponent<Image>().sprite =
                ongoingTrailEndSprite;
        }
    }
}
