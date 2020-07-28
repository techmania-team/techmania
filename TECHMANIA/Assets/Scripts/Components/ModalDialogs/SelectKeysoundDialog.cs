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
    private HashSet<GameObject> emptyKeysoundObjects;
    private const string kEmptyKeysoundDisplayText = "(None)";

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
        promptText.text = prompt;
        resolved = false;

        emptyKeysoundObjects = new HashSet<GameObject>();
        BuildLists(initialSelection);
    }

    private void BuildLists(List<string> initialSelection)
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

        // The available list starts with empty.
        GameObject emptyKeysoundObject = InstantiateKeysoundObject(
            availableTemplate, availableList, "");
        emptyKeysoundObject.GetComponent<Button>().onClick.AddListener(() =>
        {
            Select("");
        });

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
        foreach (string sound in initialSelection)
        {
            GameObject keysoundObject = InstantiateKeysoundObject(
                selectedTemplate, selectedList, sound);

            keysoundObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                Unselect(keysoundObject);
            });
        }
    }

    private GameObject InstantiateKeysoundObject(GameObject template,
        VerticalLayoutGroup group, string text)
    {
        string displayText = (text == "") ?
            kEmptyKeysoundDisplayText : text;
        GameObject keysoundObject = Instantiate(
                template, group.transform);
        keysoundObject.GetComponentInChildren<Text>().text = displayText;
        keysoundObject.SetActive(true);

        if (text == "")
        {
            emptyKeysoundObjects.Add(keysoundObject);
        }

        return keysoundObject;
    }

    private void Select(string keysound)
    {
        GameObject keysoundObject = InstantiateKeysoundObject(
            selectedTemplate, selectedList, keysound);
        keysoundObject.GetComponent<Button>().onClick.AddListener(() =>
        {
            Unselect(keysoundObject);
        });
    }

    private void Unselect(GameObject keysoundObject)
    {
        if (emptyKeysoundObjects.Contains(keysoundObject))
        {
            emptyKeysoundObjects.Remove(keysoundObject);
        }
        Destroy(keysoundObject);
    }

    public void OK()
    {
        selectedKeysounds = new List<string>();
        for (int i = 0; i < selectedList.transform.childCount; i++)
        {
            GameObject keysoundObject = selectedList.transform.GetChild(i).gameObject;
            if (keysoundObject == selectedTemplate) continue;
            if (emptyKeysoundObjects.Contains(keysoundObject))
            {
                selectedKeysounds.Add("");
            }
            else
            {
                selectedKeysounds.Add(keysoundObject.GetComponentInChildren<Text>().text);
            }
        }
        if (selectedKeysounds.Count == 0)
        {
            selectedKeysounds.Add("");
        }
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
