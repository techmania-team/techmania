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
    private int intScan;
    private HoldTrailElements trail;
    private List<HoldExtension> extensions;

    public HoldTrailAndExtensions(NoteElements noteElements,
        int intScan, int bps, GameLayout layout)
    {
        this.noteElements = noteElements;
        this.intScan = intScan;
        trail = new HoldTrailElements(noteElements, intScan,
            bps, layout);
        extensions = new List<HoldExtension>();
    }

    public void Initialize(TemplateContainer templateContainer)
    {
        trail.Initialize(templateContainer);
        // At this point extensions are not spawned yet. They will
        // be initialized when spawned.
    }

    public void InitializeSize()
    {
        trail.InitializeSize();
        extensions.ForEach(e => e.trail.InitializeSize());
    }

    public void ResetToInactive()
    {
        trail.ResetToInactive();
        extensions.ForEach(e => e.trail.ResetToInactive());
    }

    public void RegisterHoldExtension(HoldExtension e)
    {
        extensions.Add(e);
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
        extensions.ForEach(e => e.trail.SetVisibility(v));
    }

    // Part of NoteElements.UpdateTime. Only called in Ongoing state.
    public void UpdateOngoingTrail(GameTimer timer)
    {
        trail.UpdateTrails(timer);
        extensions.ForEach(e => e.trail.UpdateTrails(timer));
    }

    public void UpdateSprites(GameTimer timer)
    {
        trail.UpdateSprites(timer);
        extensions.ForEach(e => e.trail.UpdateSprites(timer));
    }

    // VFXSpawner calls this to draw completion VFX at the correct
    // position.
    public VisualElement GetDurationTrailEndPosition()
    {
        if (extensions.Count > 0)
        {
            return extensions[^1].trail.durationTrailEndPosition;
        }
        else
        {
            return trail.durationTrailEndPosition;
        }
    }

    // VFXSpawner calls this to draw ongoing VFX at the correct
    // position.
    public VisualElement GetOngoingTrailEndPosition(int currentIntScan)
    {
        if (extensions.Count == 0 ||
            currentIntScan == intScan)
        {
            return trail.ongoingTrailEndPosition;
        }
        else
        {
            int extensionIndex = currentIntScan - intScan - 1;
            return extensions[extensionIndex].trail
                .ongoingTrailEndPosition;
        }
    }
}
