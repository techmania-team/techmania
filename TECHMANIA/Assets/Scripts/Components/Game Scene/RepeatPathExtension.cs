using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RepeatPathExtension : MonoBehaviour
{
    public RectTransform extension;

    public void Initialize(Scan scanRef, int lastNotePulse)
    {
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
    }

    public void SetExtensionVisibility(
        NoteAppearance.Visibility v)
    {
        extension.gameObject.SetActive(
            v != NoteAppearance.Visibility.Hidden);
    }

    public void Activate()
    {
        SetExtensionVisibility(NoteAppearance.Visibility.Visible);
    }

    public void Prepare()
    {
        SetExtensionVisibility(NoteAppearance.Visibility.Visible);
    }

    public void Resolve()
    {
        SetExtensionVisibility(NoteAppearance.Visibility.Hidden);
    }
}
