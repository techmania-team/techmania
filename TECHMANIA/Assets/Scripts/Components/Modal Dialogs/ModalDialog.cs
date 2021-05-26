using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModalDialog : MonoBehaviour
{
    [HideInInspector]
    public bool resolved;

    private static ModalDialog instance;
    private static ModalDialog GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<Canvas>()
                .GetComponentInChildren<ModalDialog>(
                includeInactive: true);
        }
        return instance;
    }

    public static bool IsAnyModalDialogActive()
    {
        for (int i = 0; i < GetInstance().transform.childCount; i++)
        {
            if (GetInstance().transform.GetChild(i).gameObject.activeSelf)
            {
                return true;
            }
        }
        return false;
    }
}
