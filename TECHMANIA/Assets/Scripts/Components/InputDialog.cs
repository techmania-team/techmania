using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputDialog : ModalDialog
{
    private static InputDialog instance;
    private static InputDialog GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<Canvas>().GetComponentInChildren<InputDialog>(includeInactive: true);
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
    public static Result GetResult()
    {
        return GetInstance().result;
    }
    public static string GetValue()
    {
        return GetInstance().value;
    }

    public Text promptText;
    public InputField inputField;
    public enum Result
    {
        Cancelled,
        OK
    }
    private Result result;
    private string value;  // Only meaningful if state is OK

    private void InternalShow(string prompt)
    {
        promptText.text = prompt;
        inputField.text = "";
        resolved = false;
        gameObject.SetActive(true);
    }

    public void OK()
    {
        resolved = true;
        result = Result.OK;
        value = inputField.text;
        gameObject.SetActive(false);
    }

    public void Cancel()
    {
        resolved = true;
        result = Result.Cancelled;
        value = "";
        gameObject.SetActive(false);
    }
}
