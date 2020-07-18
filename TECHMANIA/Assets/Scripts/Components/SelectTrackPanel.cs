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
        Debug.Log("Title: " + InputDialog.GetValue());
    }
}
