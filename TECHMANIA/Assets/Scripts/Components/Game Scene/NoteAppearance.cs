using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Different from the editor, here the notes do not report click
// and drag events on themselves. Instead, the Game component
// performs ray tracing on all clicks and touches. This is so that
// when a held down click/touch enters another lane, Game can
// handle that has a new click/touch. This is necessary for chain
// notes.
//
// TODO: Add a Playing state for notes with a duration.
public class NoteAppearance : MonoBehaviour
{
    private enum State
    {
        Inactive,  // Note has not appeared yet; starting state
        Prepare,  // Note is 50% transparent
        Active,  // Note is opaque and can be played
        Resolved  // Note is resolved and no longer visible
    }
    private State state;

    public Image noteImage;
    public GameObject feverOverlay;
    [Header("Chain")]
    public RectTransform pathToNextChainNode;

    private Image feverOverlayImage;
    private Animator feverOverlayAnimator;
    private bool hidden;

    public void SetHidden(bool hidden)
    {
        this.hidden = hidden;
    }

    public void Prepare()
    {
        state = State.Prepare;
        UpdateState();
    }

    public void Activate()
    {
        state = State.Active;
        UpdateState();
    }

    public void Resolve()
    {
        state = State.Resolved;
        UpdateState();
    }

    private void Start()
    {
        if (feverOverlay != null)
        {
            feverOverlayAnimator =
                feverOverlay.GetComponent<Animator>();
            feverOverlayImage = feverOverlay.GetComponent<Image>();
        }

        state = State.Inactive;
        UpdateState();
    }

    private void Update()
    {
        if (hidden) return;
        if (state == State.Inactive || state == State.Resolved) return;

        if (feverOverlay != null)
        {
            UpdateFeverOverlay();
        }
    }

    private void UpdateFeverOverlay()
    {
        if (Game.feverState == Game.FeverState.Active)
        {
            if (!feverOverlayAnimator.enabled)
            {
                feverOverlayAnimator.enabled = true;
                feverOverlayImage.color = Color.white;
            }
            else if (Game.feverAmount < 0.1f)
            {
                feverOverlayImage.color = new Color(
                    1f, 1f, 1f, Game.feverAmount * 10f);
            }
        }
        else
        {
            if (feverOverlayAnimator.enabled)
            {
                feverOverlayAnimator.enabled = false;
                feverOverlayImage.color = Color.clear;
            }
        }
    }

    private void UpdateState()
    {
        // Is the note image visible and targetable?
        if (hidden)
        {
            noteImage.enabled = false;
        }
        else
        {
            switch (state)
            {
                case State.Inactive:
                case State.Resolved:
                    noteImage.enabled = false;
                    if (feverOverlayImage)
                    {
                        feverOverlayImage.enabled = false;
                    }
                    break;
                case State.Prepare:
                    noteImage.enabled = true;
                    noteImage.color = new Color(1f, 1f, 1f, 0.5f);
                    noteImage.raycastTarget = false;
                    if (feverOverlayImage)
                    {
                        feverOverlayImage.enabled = true;
                    }
                    break;
                case State.Active:
                    noteImage.enabled = true;
                    noteImage.color = Color.white;
                    noteImage.raycastTarget = true;
                    if (feverOverlayImage)
                    {
                        feverOverlayImage.enabled = true;
                    }
                    break;
            }
        }
    }
}
