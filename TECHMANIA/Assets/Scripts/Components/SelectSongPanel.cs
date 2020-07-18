using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SelectSongPanel : MonoBehaviour
{
    public GridLayoutGroup songGrid;
    public GameObject songTemplate;

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
        // Remove all songs from grid, except for template.
        for (int i = 0; i < songGrid.transform.childCount; i++)
        {
            GameObject song = songGrid.transform.GetChild(i).gameObject;
            if (song == songTemplate) continue;
            Destroy(song);
        }

        // Rebuild song list.
        foreach (string dir in Directory.EnumerateDirectories(
            Paths.GetSongFolder()))
        {
            string name = new DirectoryInfo(dir).Name;
            GameObject song = Instantiate(songTemplate);
            song.name = "Song Panel";
            song.transform.SetParent(songGrid.transform);
            song.GetComponentInChildren<Text>().text = name;
            song.SetActive(true);
        }
    }
}
