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

    private Image image;
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
        image = GetComponent<Image>();
        feverOverlayAnimator = GetComponentInChildren<Animator>();
        feverOverlayImage = feverOverlayAnimator.GetComponent<Image>();

        state = State.Inactive;
        UpdateState();
    }

    private void Update()
    {
        if (hidden) return;
        if (state == State.Inactive || state == State.Resolved) return;

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
            image.enabled = false;
        }
        else
        {
            switch (state)
            {
                case State.Inactive:
                case State.Resolved:
                    image.enabled = false;
                    feverOverlayImage.enabled = false;
                    break;
                case State.Prepare:
                    image.enabled = true;
                    image.color = new Color(1f, 1f, 1f, 0.5f);
                    image.raycastTarget = false;
                    feverOverlayImage.enabled = true;
                    break;
                case State.Active:
                    image.enabled = true;
                    image.color = Color.white;
                    image.raycastTarget = true;
                    feverOverlayImage.enabled = true;
                    break;
            }
        }
    }
}
