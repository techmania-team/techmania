using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HoldTrailManager))]
public class LegacyHoldExtension : MonoBehaviour
{
    // If the note is resolved but the Scan asks this extension
    // to Prepare or Activate, do nothing.
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
            noteRef: null,  // Filled later
            scanRef, scanlineRef, holdNote);
    }

    public void SetVisibility(
        NoteAppearance.Visibility v)
    {
        GetComponent<HoldTrailManager>().SetVisibility(v);
    }

    public void UpdateTrails(bool ongoing)
    {
        GetComponent<HoldTrailManager>().UpdateTrails(ongoing);
    }
    #endregion

    public void RegisterNoteAppearance(NoteAppearance noteRef)
    {
        this.noteRef = noteRef;
        GetComponent<HoldTrailManager>().noteRef = noteRef;
    }

    public void Activate()
    {
        if (noteRef.state == NoteAppearance.State.Inactive ||
            noteRef.state == NoteAppearance.State.Resolved ||
            noteRef.state == NoteAppearance.State.PendingResolve)
            return;
        SetVisibility(
            NoteAppearance.Visibility.Visible);
    }

    public void Prepare()
    {
        if (noteRef.state == NoteAppearance.State.Inactive || 
            noteRef.state == NoteAppearance.State.Resolved ||
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
