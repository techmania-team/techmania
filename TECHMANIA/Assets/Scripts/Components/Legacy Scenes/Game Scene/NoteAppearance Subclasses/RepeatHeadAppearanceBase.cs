using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Contains common logic between RepeatHeadAppearance and
// RepeatHeadHoldAppearance.
public class RepeatHeadAppearanceBase : NoteAppearance
{
    protected override void TypeSpecificResolve()
    {
        state = State.PendingResolve;
        // Only fully resolved when all managed repeat notes
        // get resolved.
        ManagedRepeatNoteResolved();
    }

    protected void SetRepeatPathVisibility(Visibility v)
    {
        GetComponent<RepeatPathManager>().SetVisibility(v);
    }

    protected void SetRepeatPathExtensionVisibility(Visibility v)
    {
        if (repeatPathExtensions == null) return;
        foreach (RepeatPathExtension e in repeatPathExtensions)
        {
            e.SetExtensionVisibility(v);
        }
    }

    protected override void TypeSpecificUpdateState()
    {
        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                SetNoteImageVisibility(Visibility.Hidden,
                    bypassNoteOpacityModifier: true);
                SetFeverOverlayVisibility(Visibility.Hidden,
                    bypassNoteOpacityModifier: true);
                SetRepeatPathVisibility(Visibility.Hidden);
                SetRepeatPathExtensionVisibility(Visibility.Hidden);
                break;
            case State.Prepare:
            case State.Active:
            case State.PendingResolve:
                SetNoteImageVisibility(Visibility.Visible,
                    bypassNoteOpacityModifier: true);
                SetFeverOverlayVisibility(Visibility.Visible,
                    bypassNoteOpacityModifier: true);
                SetRepeatPathVisibility(Visibility.Visible);
                // Not set for extensions: these will be controlled
                // by the scan they belong to.
                break;
            case State.Ongoing:
                // Only applies to repeat head hold.
                SetNoteImageVisibility(Visibility.Visible,
                    bypassNoteOpacityModifier: true);
                SetFeverOverlayVisibility(Visibility.Visible,
                    bypassNoteOpacityModifier: true);
                SetRepeatPathVisibility(Visibility.Visible);
                // Not set for extensions: these will be controlled
                // by the scan they belong to.
                break;
        }
    }

    protected override void TypeSpecificUpdate()
    {
        if (repeatPathExtensions != null)
        {
            foreach (RepeatPathExtension extension in
                repeatPathExtensions)
            {
                extension.UpdateSprites();
            }
        }
    }

    protected override void GetNoteImageScale(
        out float x, out float y)
    {
        x = GlobalResource.noteSkin.repeatHead.scale;
        y = GlobalResource.noteSkin.repeatHead.scale;
    }

    protected override void TypeSpecificInitializeScale()
    {
        GetComponent<RepeatPathManager>().InitializeScale();
    }

    protected override void UpdateSprites()
    {
        noteImage.sprite = GlobalResource.noteSkin.repeatHead
            .GetSpriteAtFloatIndex(Game.FloatBeat);
        GetComponent<RepeatPathManager>().UpdateSprites();
    }

    #region Repeat
    // Repeat heads and repeat hold heads store references to
    // all repeat notes and repeat hold notes after it.
    private List<NoteObject> managedRepeatNotes;
    // Counting backwards because notes are drawn backwards.
    // A value equal to managedRepeatNotes.Count means
    // the head itself.
    private int nextUnresolvedRepeatNoteIndex;
    private List<RepeatPathExtension> repeatPathExtensions;

    public void ManageRepeatNotes(List<NoteObject> repeatNotes)
    {
        // Clone the list because it will be cleared later.
        managedRepeatNotes = new List<NoteObject>(repeatNotes);
        foreach (NoteObject n in managedRepeatNotes)
        {
            n.GetComponent<RepeatNoteAppearanceBase>().repeatHead
                = this;
        }
        TypeSpecificInitialize();
    }

    protected override void TypeSpecificInitialize()
    {
        nextUnresolvedRepeatNoteIndex = managedRepeatNotes.Count;
    }

    public NoteObject GetFirstUnresolvedRepeatNote()
    {
        if (nextUnresolvedRepeatNoteIndex ==
            managedRepeatNotes.Count)
        {
            return GetComponent<NoteObject>();
        }
        else
        {
            return managedRepeatNotes
                [nextUnresolvedRepeatNoteIndex];
        }
    }

    public void ManagedRepeatNoteResolved()
    {
        nextUnresolvedRepeatNoteIndex--;
        if (nextUnresolvedRepeatNoteIndex < 0)
        {
            state = State.Resolved;
            TypeSpecificUpdateState();
        }
    }

    public void DrawRepeatHeadBeforeRepeatNotes()
    {
        // Since notes are drawn from back to front, we look
        // for the 1st note in the same scan, and draw
        // before that one.
        foreach (NoteObject n in managedRepeatNotes)
        {
            if (n.transform.parent == transform.parent)
            {
                transform.SetSiblingIndex(
                    n.transform.GetSiblingIndex());
                return;
            }
        }
    }

    public void DrawRepeatPathTo(int lastRepeatNotePulse,
        bool positionEndOfScanOutOfBounds)
    {
        float startX = GetComponent<RectTransform>()
            .anchoredPosition.x;
        float endX = scanRef.FloatPulseToXPosition(
            lastRepeatNotePulse,
            positionEndOfScanOutOfBounds,
            positionAfterScanOutOfBounds: true);
        float width = Mathf.Abs(startX - endX);

        GetComponent<RepeatPathManager>().SetWidth(
            width, rightToLeft: endX < startX);

        repeatPathExtensions = new List<RepeatPathExtension>();
    }

    public void RegisterRepeatPathExtension(
        RepeatPathExtension extension)
    {
        repeatPathExtensions.Add(extension);
    }

    #endregion
}
