using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HoldExtension : MonoBehaviour
{
    public RectTransform durationTrail;
    public RectTransform durationTrailEnd;
    public RectTransform ongoingTrail;
    public RectTransform ongoingTrailEnd;

    private Scan scanRef;
    private Scanline scanlineRef;
    private NoteAppearance noteRef;
    private NoteType noteType;

    public void Initialize(Scan scanRef, Scanline scanlineRef, 
        HoldNote holdNote)
    {
        this.scanRef = scanRef;
        this.scanlineRef = scanlineRef;
        noteType = holdNote.type;

        float startX = GetComponent<RectTransform>()
            .anchoredPosition.x;
        float endX = scanRef.FloatPulseToXPosition(
            holdNote.pulse + holdNote.duration,
            positionEndOfScanOutOfBounds: false,
            positionAfterScanOutOfBounds: true);
        float width = Mathf.Abs(startX - endX);

        durationTrail.sizeDelta = new Vector2(width,
            durationTrail.sizeDelta.y);
        if (endX < startX)
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
    }

    public void RegisterNoteAppearance(NoteAppearance noteRef)
    {
        this.noteRef = noteRef;
    }

    public void SetDurationTrailVisibility(
        NoteAppearance.Visibility v)
    {
        durationTrail.gameObject.SetActive(
            v != NoteAppearance.Visibility.Hidden);
        if (ongoingTrail != null)
        {
            ongoingTrail.gameObject.SetActive(
                v != NoteAppearance.Visibility.Hidden);
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

    public void Activate()
    {
        if (noteRef.state == NoteAppearance.State.Resolved)
            return;
        SetDurationTrailVisibility(
            NoteAppearance.Visibility.Visible);
    }

    public void Prepare()
    {
        if (noteRef.state == NoteAppearance.State.Resolved)
            return;
        if (noteType == NoteType.Hold)
        {
            SetDurationTrailVisibility(
                NoteAppearance.Visibility.Transparent);
        }
        else
        {
            SetDurationTrailVisibility(
                NoteAppearance.Visibility.Visible);
        }
    }

    public void UpdateOngoingTrail()
    {
        float startX = GetComponent<RectTransform>()
            .anchoredPosition.x;
        float endX = scanlineRef.GetComponent<RectTransform>()
            .anchoredPosition.x;
        float width = Mathf.Min(Mathf.Abs(startX - endX),
            durationTrail.sizeDelta.x);

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
            float fullWidth = durationTrail.anchoredPosition.x +
                durationTrail.sizeDelta.x;
            durationTrail.anchoredPosition = new Vector2(
                width, 0f);
            durationTrail.sizeDelta = new Vector2(fullWidth - width,
                durationTrail.sizeDelta.y);
        }
    }
}
