using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SelectTrackPanel : MonoBehaviour
{
    public GridLayoutGroup trackGrid;
    public GameObject trackTemplate;
    public Button deleteButton;
    public Button openButton;

    private GameObject selectedTrack;

    // Start is called before the first frame update
    void Start()
    {
        
        Refresh();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Refresh()
    {
        // Remove all tracks from grid, except for template.
        for (int i = 0; i < trackGrid.transform.childCount; i++)
        {
            GameObject track = trackGrid.transform.GetChild(i).gameObject;
            if (track == trackTemplate) continue;
            Destroy(track);
        }
        selectedTrack = null;
        RefreshButtons();

        // Rebuild track list.
        foreach (string dir in Directory.EnumerateDirectories(
            Paths.GetTrackFolder()))
        {
            string name = new DirectoryInfo(dir).Name;
            GameObject track = Instantiate(trackTemplate);
            track.name = "Track Panel";
            track.transform.SetParent(trackGrid.transform);
            track.GetComponentInChildren<Text>().text = name;
            track.SetActive(true);

            // Bind click event.
            // TODO: double click to open?
            track.GetComponent<Button>().onClick.AddListener(() =>
            {
                Select(track);
            });
        }
    }

    private void RefreshButtons()
    {
        deleteButton.interactable = selectedTrack != null;
        openButton.interactable = selectedTrack != null;
    }

    private void Select(GameObject track)
    {
        if (selectedTrack != null)
        {
            selectedTrack.transform.Find("Selection").gameObject.SetActive(false);
        }
        selectedTrack = track;
        RefreshButtons();
        selectedTrack.transform.Find("Selection").gameObject.SetActive(true);
    }

    public void New()
    {
        StartCoroutine(InternalNew());
    }

    private IEnumerator InternalNew()
    {
        InputDialog.Show("Title:");
        yield return new WaitUntil(() => { return InputDialog.IsResolved(); });
        if (InputDialog.GetResult() == InputDialog.Result.Cancelled)
        {
            yield break;
        }
        string title = InputDialog.GetValue();
        if (title.Length == 0)
        {
            yield break;
        }

        string invalidChars = "\\/*:?\"<>|";
        foreach (char invalidChar in invalidChars)
        {
            if (title.Contains(invalidChar.ToString()))
            {
                MessageDialog.Show($"Title cannot contain these characters:\n{invalidChars}");
                yield break;
            }
        }

        string newDir = $"{Paths.GetTrackFolder()}\\{title}";
        try
        {
            Directory.CreateDirectory(newDir);
        }
        catch (Exception e)
        {
            MessageDialog.Show(e.Message);
            yield break;
        }

        Refresh();
    }

    public void Delete()
    {
        if (selectedTrack == null) return;
        StartCoroutine(InternalDelete());
    }

    private IEnumerator InternalDelete()
    {
        string title = selectedTrack.GetComponentInChildren<Text>().text;
        string path = $"{Paths.GetTrackFolder()}\\{title}";
        ConfirmDialog.Show($"Deleting {title}. This will permanently delele {path} and everything in it. Are you sure?");
        yield return new WaitUntil(() => { return ConfirmDialog.IsResolved(); });

        if (ConfirmDialog.GetResult() == ConfirmDialog.Result.Cancelled)
        {
            yield break;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (Exception e)
        {
            MessageDialog.Show(e.Message);
        }

        Refresh();
    }
}
