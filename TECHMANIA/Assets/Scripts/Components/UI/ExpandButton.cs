using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExpandButton : MonoBehaviour
{
    public Sprite expandMoreIcon;
    public Sprite expandLessIcon;
    public Image icon;
    public GameObject target;

    private bool expanded;

    private void Start()
    {
        expanded = false;
        Refresh();
    }

    public void OnClick()
    {
        expanded = !expanded;
        Refresh();
    }

    private void Refresh()
    {
        icon.sprite = expanded ? expandLessIcon : expandMoreIcon;
        target.SetActive(expanded);
    }
}
