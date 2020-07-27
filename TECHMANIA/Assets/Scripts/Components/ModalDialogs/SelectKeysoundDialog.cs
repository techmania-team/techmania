using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectKeysoundDialog : ModalDialog
{
    private static SelectKeysoundDialog instance;
    private static SelectKeysoundDialog GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<Canvas>().GetComponentInChildren<SelectKeysoundDialog>(includeInactive: true);
        }
        return instance;
    }

    public static void Show(string prompt, List<string> initialSelection)
    {
        GetInstance().InternalShow(prompt, initialSelection);
    }
    public static bool IsResolved()
    {
        return GetInstance().resolved;
    }
    public static Result GetResult()
    {
        return GetInstance().result;
    }
    public static List<string> GetSelectedKeysounds()
    {
        return GetInstance().selectedKeysounds;
    }

    public Text promptText;
    public VerticalLayoutGroup availableList;
    public GameObject availableTemplate;
    public VerticalLayoutGroup selectedList;
    public GameObject selectedTemplate;

    public enum Result
    {
        Cancelled,
        OK
    }
    private Result result;
    private List<string> selectedKeysounds;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            OK();
        }
    }

    private void InternalShow(string prompt, List<string> initialSelection)
    {
        gameObject.SetActive(true);
        promptText.text = "";
        // Clone initialSelection so to not modify the caller.
        selectedKeysounds = new List<string>();
        foreach (string keysound in initialSelection)
        {
            selectedKeysounds.Add(keysound);
        }
        resolved = false;

        BuildLists();
    }

    private void BuildLists()
    {
        // Clear both lists.
        for (int i = 0; i < availableList.transform.childCount; i++)
        {
            GameObject keysoundObject = availableList.transform.GetChild(i).gameObject;
            if (keysoundObject == availableTemplate) continue;
            Destroy(keysoundObject);
        }
        for (int i = 0; i < selectedList.transform.childCount; i++)
        {
            GameObject keysoundObject = selectedList.transform.GetChild(i).gameObject;
            if (keysoundObject == selectedTemplate) continue;
            Destroy(keysoundObject);
        }

        // Build available list while enumerating sound files.
        string folder = new FileInfo(Navigation.GetCurrentTrackPath()).DirectoryName;

        foreach (string file in Directory.EnumerateFiles(folder, "*.wav"))
        {
            string filename = new FileInfo(file).Name;
            GameObject keysoundObject = InstantiateKeysoundObject(
                availableTemplate, availableList, filename);

            keysoundObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                Select(filename);
            });
        }

        // Build selected list.
        for (int i = 0; i < selectedKeysounds.Count; i++)
        {
            string sound = selectedKeysounds[i];
            GameObject keysoundObject = InstantiateKeysoundObject(
                selectedTemplate, selectedList, sound);

            int copyOfI = i;
            keysoundObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                Unselect(keysoundObject, copyOfI);
            });
        }
    }

    private GameObject InstantiateKeysoundObject(GameObject template,
        VerticalLayoutGroup group, string text)
    {
        GameObject keysoundObject = Instantiate(
                template, group.transform);
        keysoundObject.GetComponentInChildren<Text>().text = text;
        keysoundObject.SetActive(true);

        return keysoundObject;
    }

    private void Select(string keysound)
    {
        // Add to list
        selectedKeysounds.Add(keysound);

        // Add to UI
        InstantiateKeysoundObject(selectedTemplate,
            selectedList, keysound);
    }

    private void Unselect(GameObject keysoundObject, int index)
    {
        // Remove from list
        selectedKeysounds.RemoveAt(index);

        // Remove from UI
        Destroy(keysoundObject);
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
