using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// TODO: deprecate this.
public class ResourcePanel : MonoBehaviour
{
    private static ResourcePanel instance;
    private static ResourcePanel GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<Canvas>().GetComponentInChildren<ResourcePanel>();
        }
        return instance;
    }

    public Text list;

    // These all contain full paths.
    private List<string> audioFiles;
    private List<string> imageFiles;
    private List<string> videoFiles;

    public static event UnityAction resourceRefreshed;

    public static List<string> GetAudioFiles()
    {
        return GetInstance().audioFiles;
    }
    public static List<string> GetImageFiles()
    {
        return GetInstance().imageFiles;
    }
    public static List<string> GetVideoFiles()
    {
        return GetInstance().videoFiles;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        resourceRefreshed?.Invoke();
    }

    public void Import()
    {
        
    }
}
