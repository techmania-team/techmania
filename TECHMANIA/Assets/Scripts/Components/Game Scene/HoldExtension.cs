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

    public void Initialize(Scan scanRef, Scanline scanlineRef, 
        HoldNote holdNote)
    {
        this.scanRef = scanRef;
        this.scanlineRef = scanlineRef;

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
            ongoingTrail.localRotation =
                Quaternion.Euler(0f, 0f, 180f);
        }
        ongoingTrail.sizeDelta = new Vector2(0f,
            ongoingTrail.sizeDelta.y);
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
        ongoingTrail.gameObject.SetActive(
            v != NoteAppearance.Visibility.Hidden);
        Color color = (v == NoteAppearance.Visibility.Transparent) ?
            new Color(1f, 1f, 1f, 0.6f) :
            Color.white;
        durationTrail.GetComponent<Image>().color = color;
        ongoingTrail.GetComponent<Image>().color = color;
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
        SetDurationTrailVisibility(
            NoteAppearance.Visibility.Transparent);
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

        ongoingTrail.sizeDelta = new Vector2(width,
            ongoingTrail.sizeDelta.y);
    }
}
