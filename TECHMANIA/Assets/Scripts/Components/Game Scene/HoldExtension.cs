using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HoldTrailManager))]
public class HoldExtension : MonoBehaviour
{
    private NoteAppearance noteRef;

    #region HoldTrailManager wrapper
    public RectTransform durationTrailEnd =>
        GetComponent<HoldTrailManager>().durationTrailEnd;
    public RectTransform ongoingTrailEnd =>
        GetComponent<HoldTrailManager>().ongoingTrailEnd;

    public void Initialize(Scan scanRef, Scanline scanlineRef, 
        HoldNote holdNote)
    {
        GetComponent<HoldTrailManager>().Initialize(
            scanRef, scanlineRef, holdNote);
    }

    public void SetVisibility(
        NoteAppearance.Visibility v)
    {
        GetComponent<HoldTrailManager>().SetVisibility(v);
    }

    public void UpdateTrails()
    {
        GetComponent<HoldTrailManager>().UpdateTrails();
    }
    #endregion

    public void RegisterNoteAppearance(NoteAppearance noteRef)
    {
        this.noteRef = noteRef;
    }

    public void Activate()
    {
        if (noteRef.state == NoteAppearance.State.Resolved ||
            noteRef.state == NoteAppearance.State.PendingResolve)
            return;
        SetVisibility(
            NoteAppearance.Visibility.Visible);
    }

    public void Prepare()
    {
        if (noteRef.state == NoteAppearance.State.Resolved ||
            noteRef.state == NoteAppearance.State.PendingResolve)
            return;
        if (noteRef.GetNoteType() == NoteType.Hold)
        {
            SetVisibility(
                NoteAppearance.Visibility.Transparent);
        }
        else
        {
            SetVisibility(
                NoteAppearance.Visibility.Visible);
        }
    }
}
