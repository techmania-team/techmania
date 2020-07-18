using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SelectTrackPanel : MonoBehaviour
{
    public GridLayoutGroup trackGrid;
    public GameObject trackTemplate;

    // Start is called before the first frame update
    void Start()
    {
        
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
        }
    }
}
