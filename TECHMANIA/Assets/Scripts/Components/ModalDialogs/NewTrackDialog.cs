using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NewTrackDialog : ModalDialog
{
    private static NewTrackDialog instance;
    private static NewTrackDialog GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<Canvas>().GetComponentInChildren<NewTrackDialog>(includeInactive: true);
        }
        return instance;
    }

    public static void Show()
    {
        GetInstance().InternalShow();
    }
    public static bool IsResolved()
    {
        return GetInstance().resolved;
    }
    public static Result GetResult()
    {
        return GetInstance().result;
    }
    public static string GetTitle()
    {
        return GetInstance().titleValue;
    }
    public static string GetArtist()
    {
        return GetInstance().artistValue;
    }

    public InputField titleField;
    public InputField artistField;
    public enum Result
    {
        Cancelled,
        OK
    }
    private Result result;
    private string titleValue;
    private string artistValue;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (titleField.isFocused)
            {
                artistField.ActivateInputField();
            }
            else
            {
                titleField.ActivateInputField();
            }
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            OK();
        }
    }

    private void InternalShow()
    {
        gameObject.SetActive(true);
        titleField.text = "";
        titleField.ActivateInputField();
        artistField.text = "";
        resolved = false;
    }

    public void OK()
    {
        resolved = true;
        result = Result.OK;
        titleValue = titleField.text;
        artistValue = artistField.text;
        gameObject.SetActive(false);
    }

    public void Cancel()
    {
        resolved = true;
        result = Result.Cancelled;
        titleValue = "";
        artistValue = "";
        gameObject.SetActive(false);
    }
}
