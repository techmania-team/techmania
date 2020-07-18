using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageDialog : ModalDialog
{
    private static MessageDialog instance;
    private static MessageDialog GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<Canvas>().GetComponentInChildren<MessageDialog>(includeInactive: true);
        }
        return instance;
    }

    public static void Show(string prompt)
    {
        GetInstance().InternalShow(prompt);
    }
    public static bool IsResolved()
    {
        return GetInstance().resolved;
    }

    public Text messageText;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Escape))
        {
            OK();
        }
    }

    private void InternalShow(string prompt)
    {
        messageText.text = prompt;
        resolved = false;
        gameObject.SetActive(true);
    }

    public void OK()
    {
        resolved = true;
        gameObject.SetActive(false);
    }
}
