using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RepeatPathExtension : MonoBehaviour
{
    public RectTransform extension;
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

        extension.sizeDelta = new Vector2(width,
            extension.sizeDelta.y);
        if (endX < startX)
        {
            extension.localRotation =
                Quaternion.Euler(0f, 0f, 180f);
        }

        InitializeScale();
    }

    private void InitializeScale()
    {
        extension.localScale = new Vector3(1f,
            GlobalResource.noteSkin.repeatPath.scale,
            1f);
    }

    public void DrawBeforeRepeatNotes()
    {
        // Hack
        transform.SetSiblingIndex(0);
    }

    public void SetExtensionVisibility(
        NoteAppearance.Visibility v)
    {
        extension.gameObject.SetActive(
            v != NoteAppearance.Visibility.Hidden);
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
        extension.GetComponent<Image>().sprite =
            GlobalResource.noteSkin.repeatPath.GetSpriteForFloatBeat(
                Game.FloatBeat);
    }
}
