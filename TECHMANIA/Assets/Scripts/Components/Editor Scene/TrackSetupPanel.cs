using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class TrackSetupPanel : MonoBehaviour
{
    public MessageDialog messageDialog;
    public Tabs tabs;

    #region Refreshing
    private void OnEnable()
    {
        Tabs.tabChanged += Refresh;
        EditorContext.StateUpdated += Refresh;
        EditorContext.DirtynessUpdated += RefreshTitle;
        Refresh();
    }

    private void OnDisable()
    {
        Tabs.tabChanged -= Refresh;
        EditorContext.StateUpdated -= Refresh;
        EditorContext.DirtynessUpdated -= RefreshTitle;
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        switch (tabs.CurrentTab)
        {
            case 0:
                RefreshResourcesTab();
                break;
        }
    }
    #endregion

    #region Top bar
    [Header("Top bar")]
    public TextMeshProUGUI title;

    public void Save()
    {
        EditorContext.Save();
    }

    public void Undo()
    {
        EditorContext.Undo();
    }

    public void Redo()
    {
        EditorContext.Redo();
    }

    private void RefreshTitle(bool dirty)
    {
        string titleText = title.text.TrimEnd('*');
        if (dirty) titleText = titleText + '*';
        title.text = titleText;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                Save();
            }
            if (Input.GetKeyDown(KeyCode.Z))
            {
                Undo();
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
                Redo();
            }
        }
    }
    #endregion

    #region Resources tab
    [Header("Resources tab")]
    public TextMeshProUGUI audioFilesList;
    public TextMeshProUGUI imageFilesList;
    public TextMeshProUGUI videoFilesList;

    public void OnImportButtonClick()
    {
        string trackFolder = EditorContext.trackFolder;

        foreach (string source in SFB.StandaloneFileBrowser.OpenFilePanel(
            "Select resource to import", "", "wav;*.png;*.mp4", multiselect: true))
        {
            FileInfo fileInfo = new FileInfo(source);
            if (fileInfo.DirectoryName == trackFolder) continue;
            string destination = $"{trackFolder}\\{fileInfo.Name}";

            try
            {
                File.Copy(source, destination, overwrite: true);
            }
            catch (Exception e)
            {
                messageDialog.Show(
                    $"An error occurred when copying {source} to {destination}:\n\n"
                    + e.Message);
                return;
            }
        }

        RefreshResourcesTab();
    }

    private string CondenseFileList(List<string> fullPaths)
    {
        string str = "";
        foreach (string file in fullPaths)
        {
            str += new FileInfo(file).Name + "\n";
        }
        return str.TrimEnd('\n');
    }

    public void RefreshResourcesTab()
    {
        string trackFolder = EditorContext.trackFolder;
        audioFilesList.text = CondenseFileList(
            Paths.GetAllAudioFiles(trackFolder));
        imageFilesList.text = CondenseFileList(
            Paths.GetAllImageFiles(trackFolder));
        videoFilesList.text = CondenseFileList(
            Paths.GetAllVideoFiles(trackFolder));
    }
    #endregion
}
