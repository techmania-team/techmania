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
    public RectTransform pathToPreviousNote;
    [Header("Hold")]
    public RectTransform durationTrail;
    public RectTransform ongoingTrail;

    private Image feverOverlayImage;
    private Animator feverOverlayAnimator;
    private bool hidden;
    private Scan scanRef;
    private Scanline scanlineRef;

    #region State Interfaces
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
    #endregion

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

    #region Update
    private void Update()
    {
        if (hidden) return;
        if (state == State.Inactive || state == State.Resolved) return;

        if (feverOverlay != null)
        {
            UpdateFeverOverlay();
        }
        if (ongoingTrail != null)
        {
            UpdateOngoingTrail();
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

    private void UpdateOngoingTrail()
    {
        // TODO
    }

    private NoteType GetNoteType()
    {
        return GetComponent<NoteObject>().note.type;
    }

    private void UpdateState()
    {
        // Is the note image visible and targetable?
        if (hidden)
        {
            noteImage.gameObject.SetActive(false);
            if (pathToPreviousNote != null)
            {
                pathToPreviousNote.gameObject.SetActive(false);
            }
            if (durationTrail != null)
            {
                durationTrail.gameObject.SetActive(false);
                ongoingTrail.gameObject.SetActive(false);
            }
            return;
        }

        // TODO: clean this up. Maybe methods for each field?
        // In preparation of practice mode, each case should
        // set every single field.
        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                noteImage.gameObject.SetActive(false);
                if (nextChainNode != null)
                {
                    nextChainNode.GetComponent<NoteAppearance>()
                        .TogglePathToPreviousNote(false);
                }
                if (durationTrail != null)
                {
                    durationTrail.gameObject.SetActive(false);
                    ongoingTrail.gameObject.SetActive(false);
                }
                if (feverOverlayImage)
                {
                    feverOverlayImage.enabled = false;
                }
                break;
            case State.Prepare:
                // TODO: Only the following should be transparent:
                // - Basic Note
                // - Trail of Hold Note
                // - Curve
                Color transparent = new Color(1f, 1f, 1f, 0.6f);
                noteImage.gameObject.SetActive(true);
                if (GetNoteType() == NoteType.Basic)
                {
                    noteImage.color = transparent;
                }
                if (nextChainNode != null)
                {
                    nextChainNode.GetComponent<NoteAppearance>()
                        .TogglePathToPreviousNote(true);
                }
                if (durationTrail != null)
                {
                    durationTrail.gameObject.SetActive(true);
                    durationTrail.GetComponent<Image>().color =            transparent;
                    ongoingTrail.gameObject.SetActive(true);
                    ongoingTrail.GetComponent<Image>().color = 
                        transparent;
                }
                if (feverOverlayImage)
                {
                    feverOverlayImage.enabled = true;
                }
                break;
            case State.Active:
                noteImage.gameObject.SetActive(true);
                noteImage.color = Color.white;
                if (nextChainNode != null)
                {
                    nextChainNode.GetComponent<NoteAppearance>()
                        .TogglePathToPreviousNote(true);
                }
                if (durationTrail != null)
                {
                    durationTrail.gameObject.SetActive(true);
                    durationTrail.GetComponent<Image>().color =            Color.white;
                    ongoingTrail.gameObject.SetActive(true);
                    ongoingTrail.GetComponent<Image>().color =
                        Color.white;
                }
                if (feverOverlayImage)
                {
                    feverOverlayImage.enabled = true;
                }
                break;
        }
    }
    #endregion

    #region Path
    // A little complication here is that, to achieve the correct
    // draw order, each Chain Node draws a path to its previous
    // Chain Head/Node, the same way as in the editor.
    // However, when a Chain Head/Node gets resolved, it should
    // also take away the path pointing to it. Therefore, it's
    // necessary for each Chain Head/Node to be aware of, and
    // eventually control, the next Chain Node.
    private GameObject nextChainNode;
    public void SetNextChainNode(NoteObject nextChainNode)
    {
        this.nextChainNode = null;
        if (nextChainNode != null)
        {
            this.nextChainNode = nextChainNode.gameObject;
            nextChainNode.GetComponent<NoteAppearance>()
                .PointPathTowards(GetComponent<RectTransform>());
            if (GetNoteType() == NoteType.ChainHead)
            {
                UIUtils.RotateToward(
                    noteImage.GetComponent<RectTransform>(),
                    selfPos: GetComponent<RectTransform>()
                        .anchoredPosition,
                    targetPos: nextChainNode
                        .GetComponent<RectTransform>()
                        .anchoredPosition);
            }
        }
    }

    private void PointPathTowards(RectTransform previousNote)
    {
        if (pathToPreviousNote == null) return;
        UIUtils.PointToward(pathToPreviousNote,
            selfPos: GetComponent<RectTransform>().anchoredPosition,
            targetPos: previousNote
                .GetComponent<RectTransform>().anchoredPosition);
    }

    private void TogglePathToPreviousNote(bool active)
    {
        if (pathToPreviousNote == null) return;
        pathToPreviousNote.gameObject.SetActive(active);
    }
    #endregion

    #region Trail
    public void InitializeTrail(Scan scanRef, Scanline scanlineRef)
    {
        this.scanRef = scanRef;
        this.scanlineRef = scanlineRef;

        HoldNote holdNote = GetComponent<NoteObject>().note
            as HoldNote;
        float startX = GetComponent<RectTransform>()
            .anchoredPosition.x;
        float endX = scanRef.FloatPulseToXPosition(
            holdNote.pulse + holdNote.duration);
        float width = Mathf.Abs(startX - endX);

        durationTrail.sizeDelta = new Vector2(width,
            durationTrail.sizeDelta.y);
        if (endX < startX)
        {
            durationTrail.localRotation =
                Quaternion.Euler(0f, 0f, 180f);
        }
        ongoingTrail.sizeDelta = new Vector2(0f,
            ongoingTrail.sizeDelta.y);
    }
    #endregion
}
