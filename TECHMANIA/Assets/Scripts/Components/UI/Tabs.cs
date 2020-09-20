using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tabs : MonoBehaviour
{
    public Transform tabButtons;
    public Transform tabContents;

    private int currentTab;

    // Start is called before the first frame update
    void Start()
    {
        currentTab = 0;
        for (int t = 0; t < tabButtons.childCount; t++)
        {
            ToggleTabButtonBar(t, t == currentTab);
            ToggleTabContent(t, t == currentTab);

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
        ToggleTabButtonBar(currentTab, false);
        ToggleTabButtonBar(t, true);
        ToggleTabContent(currentTab, false);
        ToggleTabContent(t, true);

        currentTab = t;
    }
}
