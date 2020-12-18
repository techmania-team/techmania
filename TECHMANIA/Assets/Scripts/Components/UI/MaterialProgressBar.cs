using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialProgressBar : MonoBehaviour
{
    public RectTransform fill;

    public void SetValue(float value)
    {
        fill.anchorMax = new Vector2(value, 1f);
    }
}
