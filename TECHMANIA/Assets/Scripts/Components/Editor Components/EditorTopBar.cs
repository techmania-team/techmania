using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditorTopBar : MonoBehaviour
{
    public TextMeshProUGUI title;
    public Button undoButton;
    public Button redoButton;

    private DateTime lastAutoSaveTime;

    private void OnEnable()
    {
        EditorContext.DirtynessUpdated += RefreshTitle;
        EditorContext.UndoRedoStackUpdated += RefreshUndoRedoButtons;
        RefreshTitle(EditorContext.Dirty);
        RefreshUndoRedoButtons();
        lastAutoSaveTime = DateTime.Now;
    }

    private void OnDisable()
    {
        EditorContext.DirtynessUpdated -= RefreshTitle;
        EditorContext.UndoRedoStackUpdated -= RefreshUndoRedoButtons;
    }

    public void Save()
    {
        if (GetComponentInParent<TrackSetupPanel>() != null ||
            GetComponentInParent<PatternPanel>() != null)
        {
            EditorContext.SaveTrack();
        }
        if (GetComponentInParent<EditSetlistPanel>() != null)
        {
            EditorContext.SaveSetlist();
        }
        lastAutoSaveTime = DateTime.Now;
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

    private void RefreshUndoRedoButtons()
    {
        undoButton.interactable = EditorContext.CanUndo();
        redoButton.interactable = EditorContext.CanRedo();
    }

    // Update is called once per frame
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

        if (Options.instance.editorOptions.autoSave)
        {
            if (DateTime.Now - lastAutoSaveTime >
                TimeSpan.FromMinutes(5))
            {
                Debug.Log("Auto saving.");
                Save();
            }
        }
    }
}
