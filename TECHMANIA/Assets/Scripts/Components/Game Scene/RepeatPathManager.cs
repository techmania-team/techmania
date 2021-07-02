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

        pathEnd.localScale = new Vector3(
            pathEnd.localScale.x *
                GlobalResource.noteSkin.repeatPath.scale,
            pathEnd.localScale.y,
            pathEnd.localScale.z);
        Rect rect = GlobalResource.noteSkin.repeatPathEnd
            .sprites[0].rect;
        pathEnd.GetComponent<AspectRatioFitter>()
            .aspectRatio = rect.width / rect.height;
    }

    public void UpdateSprites()
    {
        path.GetComponent<Image>().sprite =
            GlobalResource.noteSkin.repeatPath
            .GetSpriteAtFloatIndex(Game.FloatBeat);
        pathEnd.GetComponent<Image>().sprite =
            GlobalResource.noteSkin.repeatPathEnd
            .GetSpriteAtFloatIndex(Game.FloatBeat);
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
