using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RepeatPathExtension : MonoBehaviour
{
    private NoteAppearance noteRef;

    public void Initialize(Scan scanRef, NoteObject noteRef,
        int lastNotePulse)
    {
        this.noteRef = noteRef.GetComponent<NoteAppearance>();

        float startX = GetComponent<RectTransform>()
            .anchoredPosition.x;
        float endX = scanRef.FloatPulseToXPosition(
            lastNotePulse,
            positionEndOfScanOutOfBounds: true,
            positionAfterScanOutOfBounds: true);
        float width = Mathf.Abs(startX - endX);

        RepeatPathManager pathManager =
            GetComponent<RepeatPathManager>();
        pathManager.SetWidth(width, rightToLeft: endX < startX);
        pathManager.InitializeScale();
    }

    public void DrawBeforeRepeatNotes()
    {
        // Hack. Sibling #0 is the scanline, so set as sibling #1.
        transform.SetSiblingIndex(1);
    }

    public void SetExtensionVisibility(
        NoteAppearance.Visibility v)
    {
        GetComponent<RepeatPathManager>().SetVisibility(v);
    }

    public void Activate()
    {
        if (noteRef.state == NoteAppearance.State.Resolved)
            return;
        SetExtensionVisibility(NoteAppearance.Visibility.Visible);
    }

    public void Prepare()
    {
        if (noteRef.state == NoteAppearance.State.Resolved)
            return;
        SetExtensionVisibility(NoteAppearance.Visibility.Visible);
    }

    public void UpdateSprites()
    {
        GetComponent<RepeatPathManager>().UpdateSprites();
    }
}
