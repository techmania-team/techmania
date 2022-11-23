using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Handles the duration trails and ongoing trails of hold notes,
// repeat head hold notes and repeat hold notes.
//
// Note that each hold extension has its own instance of
// HoldTrailManager.
//
// IMPORTANT: ongoing trail has width 0 if note is not in
// Ongoing state.
public class HoldTrailManager : MonoBehaviour
{
    public RectTransform durationTrail;
    public RectTransform durationTrailEnd;
    public RectTransform ongoingTrail;
    public RectTransform ongoingTrailEnd;

    private Scanline scanlineRef;
    [HideInInspector]
    public NoteAppearance noteRef;

    public NoteType noteType { get; private set; }
    private float durationTrailInitialWidth;
    private bool trailExtendsLeft;

    public void Initialize(NoteAppearance noteRef,
        Scan scanRef, Scanline scanlineRef,
        HoldNote holdNote)
    {
        this.noteRef = noteRef;
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
            durationTrail.localScale = new Vector3(
                durationTrail.localScale.x,
                -durationTrail.localScale.y,
                durationTrail.localScale.z);
            if (ongoingTrail != null)
            {
                ongoingTrail.localRotation =
                    Quaternion.Euler(0f, 0f, 180f);
                ongoingTrail.localScale = new Vector3(
                    ongoingTrail.localScale.x,
                    -ongoingTrail.localScale.y,
                    ongoingTrail.localScale.z);
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
        if (noteType == NoteType.Hold)
        {
            durationTrailScale = GlobalResource.noteSkin
                .holdTrail.scale;
            Rect rect = GlobalResource.noteSkin.holdTrailEnd
                .sprites[0].rect;
            durationTrailEndAspectRatio = rect.width / rect.height;

            ongoingTrailScale = GlobalResource.noteSkin
                .holdOngoingTrail.scale;
        }
        else
        {
            durationTrailScale = GlobalResource.noteSkin
                .repeatHoldTrail.scale;
            Rect rect = GlobalResource.noteSkin.repeatHoldTrailEnd
                .sprites[0].rect;
            durationTrailEndAspectRatio = rect.width / rect.height;
        }

        durationTrail.localScale = new Vector3(
            durationTrail.localScale.x,
            durationTrailScale * durationTrail.localScale.y,
            durationTrail.localScale.z);
        // The trail's scale is applied to the trail end, so
        // compensate for it here.
        durationTrailEnd.localScale = new Vector3(
            durationTrailScale * durationTrailEnd.localScale.x,
            durationTrailEnd.localScale.y,
            durationTrailEnd.localScale.z);
        durationTrailEnd.GetComponent<AspectRatioFitter>()
            .aspectRatio =
            durationTrailEndAspectRatio;
        if (ongoingTrail != null)
        {
            ongoingTrail.localScale = new Vector3(
                ongoingTrail.localScale.x,
                ongoingTrailScale * ongoingTrail.localScale.y,
                ongoingTrail.localScale.z);
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
        }

        Color durationTrailColor = new Color(1f, 1f, 1f,
            noteRef.VisibilityToAlpha(v));
        durationTrail.GetComponent<Image>().color = 
            durationTrailColor;
        durationTrailEnd.GetComponent<Image>().color = 
            durationTrailColor;

        if (ongoingTrail != null)
        {
            Color ongoingTrailColor = new Color(1f, 1f, 1f,
            noteRef.VisibilityToAlpha(v,
            bypassNoteOpacityModifier: true));
            ongoingTrail.GetComponent<Image>().color = 
                ongoingTrailColor;
        }
    }

    public void UpdateTrails(bool ongoing)
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

        // Don't draw ongoing trail if not in ongoing state.
        if (!ongoing)
        {
            width = 0f;
        }

        // Draw ongoing trail on hold notes.
        if (noteType == NoteType.Hold)
        {
            ongoingTrail.sizeDelta = new Vector2(width,
                ongoingTrail.sizeDelta.y);
        }

        // Shorten duration trail regardless of note type.
        durationTrail.anchoredPosition = new Vector2(
            trailExtendsLeft ? -width : width,
            0f);
        durationTrail.sizeDelta = new Vector2(
            durationTrailInitialWidth - width,
            durationTrail.sizeDelta.y);

        UpdateSprites();
    }

    private void UpdateSprites()
    {
        Sprite durationTrailSprite = null;
        Sprite durationTrailEndSprite = null;
        Sprite ongoingTrailSprite = null;
        if (noteType == NoteType.Hold)
        {
            durationTrailSprite = GlobalResource.noteSkin.holdTrail
                .GetSpriteAtFloatIndex(Game.FloatBeat);
            durationTrailEndSprite = GlobalResource.noteSkin
                .holdTrailEnd.GetSpriteAtFloatIndex(
                Game.FloatBeat);
            ongoingTrailSprite = GlobalResource.noteSkin
                .holdOngoingTrail.GetSpriteAtFloatIndex(
                Game.FloatBeat);
        }
        else
        {
            durationTrailSprite = GlobalResource.noteSkin
                .repeatHoldTrail.GetSpriteAtFloatIndex(
                Game.FloatBeat);
            durationTrailEndSprite = 
                GlobalResource.noteSkin.repeatHoldTrailEnd
                .GetSpriteAtFloatIndex(Game.FloatBeat);
        }

        durationTrail.GetComponent<Image>().sprite =
            durationTrailSprite;
        durationTrailEnd.GetComponent<Image>().sprite =
            durationTrailEndSprite;
        if (ongoingTrail != null)
        {
            ongoingTrail.GetComponent<Image>().sprite =
                ongoingTrailSprite;
        }
    }
}
