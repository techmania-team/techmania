using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmDialog : ModalDialog
{
    private static ConfirmDialog instance;
    private static ConfirmDialog GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<Canvas>().GetComponentInChildren<ConfirmDialog>(includeInactive: true);
        }
        return instance;
    }

    public static void Show(string message)
    {
        GetInstance().InternalShow(message);
    }
    public static bool IsResolved()
    {
        return GetInstance().resolved;
    }
    public static Result GetResult()
    {
        return GetInstance().result;
    }

    public Text messageText;
    public enum Result
    {
        Cancelled,
        OK
    }
    private Result result;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            OK();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
        }
    }

    private void InternalShow(string message)
    {
        gameObject.SetActive(true);
        messageText.text = message;
        resolved = false;
    }

    public void OK()
    {
        resolved = true;
        result = Result.OK;
        gameObject.SetActive(false);
    }

    public void Cancel()
    {
        resolved = true;
        result = Result.Cancelled;
        gameObject.SetActive(false);
    }
}
