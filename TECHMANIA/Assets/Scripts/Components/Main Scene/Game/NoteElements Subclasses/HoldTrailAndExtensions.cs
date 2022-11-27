using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using State = NoteElements.State;
using Visibility = NoteElements.Visibility;

// Common behavior between hold, repeat head hold and repeat hold
// notes. This manages the trails on the note itself, as well as
// all extensions.
public class HoldTrailAndExtensions
{
    private NoteElements noteElements;
    private HoldTrailElements trail;
    private List<HoldExtension> extensions;

    public HoldTrailAndExtensions(NoteElements noteElements,
        int intScan, int bps, GameLayout layout)
    {
        this.noteElements = noteElements;
        trail = new HoldTrailElements(noteElements, intScan,
            bps, layout);
        extensions = new List<HoldExtension>();
    }

    public void Initialize(TemplateContainer templateContainer)
    {
        trail.Initialize(templateContainer);
        // extensions.ForEach(e => e.Initialize());
    }

    public void InitializeSize()
    {
        trail.InitializeSize();
        // extensions.ForEach(e => e.InitializeSize());
    }

    public void ResetToInactive()
    {
        trail.ResetToInactive();
        // extensions.ForEach(e => e.ResetToInactive());
    }

    public void RegisterHoldExtension(HoldExtension e)
    {
        extensions.Add(e);
        // e.RegisterNoteAppearance(this)
    }

    public void UpdateState(State state)
    {
        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
            case State.PendingResolve:
                SetDurationTrailVisibility(Visibility.Hidden);
                SetHoldExtensionVisibility(Visibility.Hidden);
                break;
            case State.Prepare:
                if (noteElements.note.type == NoteType.Hold)
                {
                    SetDurationTrailVisibility(
                        Visibility.Transparent);
                }
                else
                {
                    SetDurationTrailVisibility(Visibility.Visible);
                }
                // Not set for extensions: these will be controlled
                // by NoteManager.
                break;
            case State.Active:
            case State.Ongoing:
                SetDurationTrailVisibility(Visibility.Visible);
                // Not set for extensions: these will be controlled
                // by NoteManager.
                break;
        }
    }

    private void SetDurationTrailVisibility(Visibility v)
    {
        trail.SetVisibility(v);
    }

    private void SetHoldExtensionVisibility(Visibility v)
    {
        //if (holdExtensions == null) return;
        //foreach (HoldExtension e in holdExtensions)
        //{
        //    e.SetVisibility(v);
        //}
    }

    // Part of NoteElements.UpdateTime. Only called in Ongoing state.
    public void UpdateOngoingTrail(GameTimer timer)
    {
        trail.UpdateTrails(timer);
        //foreach (LegacyHoldExtension e in holdExtensions)
        //{
        //    e.UpdateTrails(state == State.Ongoing);
        //}
    }

    public void UpdateSprites(GameTimer timer)
    {
        trail.UpdateSprites(timer);
        // holdExtensions.ForEach(e => e.UpdateTrails(timer));
    }

    // VFXSpawner calls this to draw completion VFX at the correct
    // position.
    public VisualElement GetDurationTrailEndPosition()
    {
        //if (holdExtensions.Count > 0)
        //{
        //    return holdExtensions[^1].durationTrailEnd.position;
        //}
        //return GetComponent<HoldTrailManager>()
        //    .durationTrailEnd.position;
        return trail.durationTrailEndPosition;
    }

    // VFXSpawner calls this to draw ongoing VFX at the correct
    // position.
    public VisualElement GetOngoingTrailEndPosition()
    {
        //if (holdExtensions.Count == 0 ||
        //    Game.Scan == scanRef.scanNumber)
        //{
        //    return GetComponent<HoldTrailManager>()
        //        .ongoingTrailEnd.position;
        //}
        //else
        //{
        //    int extensionIndex = Game.Scan - scanRef.scanNumber - 1;
        //    extensionIndex = Mathf.Clamp(extensionIndex,
        //        0, holdExtensions.Count - 1);
        //    return holdExtensions[extensionIndex]
        //        .ongoingTrailEnd.position;
        //}
        return trail.ongoingTrailEndPosition;
    }
}
