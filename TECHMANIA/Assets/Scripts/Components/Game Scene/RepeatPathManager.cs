using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RepeatPathManager : MonoBehaviour
{
    public RectTransform path;
    public RectTransform pathEnd;

    public void SetVisibility(NoteAppearance.Visibility v)
    {
        path.gameObject.SetActive(
            v != NoteAppearance.Visibility.Hidden);
    }

    public void InitializeScale()
    {
        path.localScale = new Vector3(
            path.localScale.x,
            path.localScale.y * 
                GlobalResource.noteSkin.repeatPath.scale,
            path.localScale.z);
    }

    public void UpdateSprites()
    {
        path.GetComponent<Image>().sprite =
            GlobalResource.noteSkin.repeatPath
            .GetSpriteForFloatBeat(Game.FloatBeat);
    }

    public void SetWidth(float width, bool rightToLeft)
    {
        path.sizeDelta = new Vector2(width, path.sizeDelta.y);
        if (rightToLeft)
        {
            path.localRotation = Quaternion.Euler(0f, 0f, 180f);
            path.localScale = new Vector3(
                path.localScale.x,
                -path.localScale.y,
                path.localScale.z);
        }
    }
}
