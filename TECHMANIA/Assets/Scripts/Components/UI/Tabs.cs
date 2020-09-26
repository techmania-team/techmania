using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Tabs : MonoBehaviour
{
    public Transform tabButtons;
    public Transform tabContents;

    public int CurrentTab { get; private set; }

    // This is fired from buttons only, not on startup.
    public static event UnityAction tabChanged;

    // Start is called before the first frame update
    void Start()
    {
        CurrentTab = 0;
        for (int t = 0; t < tabButtons.childCount; t++)
        {
            ToggleTabButtonBar(t, t == CurrentTab);
            ToggleTabContent(t, t == CurrentTab);

            int tCopy = t;
            tabButtons.GetChild(t).GetComponent<Button>().onClick.AddListener(() =>
            {
                SetTab(tCopy);
            });
        }
    }

    private void ToggleTabButtonBar(int t, bool active)
    {
        tabButtons.GetChild(t).Find("Bar").gameObject.SetActive(active);
    }

    private void ToggleTabContent(int t, bool active)
    {
        tabContents.GetChild(t).gameObject.SetActive(active);
    }

    private void SetTab(int t)
    {
        ToggleTabButtonBar(CurrentTab, false);
        ToggleTabButtonBar(t, true);
        ToggleTabContent(CurrentTab, false);
        ToggleTabContent(t, true);

        CurrentTab = t;

        tabChanged?.Invoke();
    }
}
