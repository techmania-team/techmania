using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class EditSetlistPanel : MonoBehaviour
{
    public MessageDialog messageDialog;
    public ConfirmDialog confirmDialog;

    #region Filename caching
    private List<string> imageFilesCache;

    private void RefreshFilenameCaches()
    {
        imageFilesCache = Paths.GetAllImageFiles(
            EditorContext.setlistFolder);
    }
    #endregion

    #region Refreshing
    private void OnEnable()
    {
        EditorContext.UndoInvoked += OnUndoOrRedo;
        EditorContext.RedoInvoked += OnUndoOrRedo;
        RefreshFilenameCaches();
        Refresh();
    }

    private void OnDisable()
    {
        EditorContext.UndoInvoked -= OnUndoOrRedo;
        EditorContext.RedoInvoked -= OnUndoOrRedo;
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            RefreshFilenameCaches();
            Refresh();
        }
    }

    private void OnUndoOrRedo(EditTransaction t)
    {
        Refresh();
    }

    private void Refresh()
    {
        RefreshResourceSection();
        RefreshMetadataSection();
    }
    #endregion

    #region Resources
    [Header("Resources")]
    public TextMeshProUGUI imageFilesDisplay;

    public void OnImportButtonClick()
    {
        EditorUtilities.ImportResource(new string[]
            { "jpg", "png" },
            copyDestinationFolder: EditorContext.setlistFolder,
            messageDialog, confirmDialog,
            completeCopyCallback: () =>
            {
                RefreshFilenameCaches();
                RefreshResourceSection();
            });
    }

    public void RefreshResourceSection()
    {
        imageFilesDisplay.text = EditorUtilities.CondenseFileList(
            imageFilesCache, EditorContext.setlistFolder);
    }
    #endregion

    #region Metadata
    [Header("Metadata")]
    public TMP_InputField title;
    public TMP_InputField description;
    public TMP_Dropdown controlScheme;
    public TMP_Dropdown backgroundImage;
    public TMP_Dropdown eyecatchImage;
    public EyecatchSelfLoader eyecatchPreview;

    public void RefreshMetadataSection()
    {
        SetlistMetadata metadata = EditorContext.setlist.setlistMetadata;

        title.SetTextWithoutNotify(metadata.title);
        title.GetComponent<MaterialTextField>().RefreshMiniLabel();
        description.SetTextWithoutNotify(metadata.description);
        description.GetComponent<MaterialTextField>().RefreshMiniLabel();

        controlScheme.options.Clear();
        controlScheme.options.Add(new TMP_Dropdown.OptionData(
            L10n.GetString("control_scheme_touch")));
        controlScheme.options.Add(new TMP_Dropdown.OptionData(
            L10n.GetString("control_scheme_keys")));
        controlScheme.options.Add(new TMP_Dropdown.OptionData(
            L10n.GetString("control_scheme_km")));
        controlScheme.SetValueWithoutNotify((int)metadata.controlScheme);

        UIUtils.MemoryToDropdown(backgroundImage, 
            metadata.backImage, imageFilesCache,
            EditorContext.setlistFolder);
        UIUtils.MemoryToDropdown(eyecatchImage,
            metadata.eyecatchImage, imageFilesCache,
            EditorContext.setlistFolder);

        RefreshEyecatchPreview();
    }

    public void OnMetadataUpdated()
    {
        SetlistMetadata metadata = EditorContext.setlist.setlistMetadata;
        bool madeChange = false;

        UIUtils.UpdateSetlistMetadataInMemory(
            ref metadata.title, title.text, ref madeChange);
        UIUtils.UpdateSetlistMetadataInMemory(
            ref metadata.description, description.text, ref madeChange);

        // Special handling for control scheme
        if ((int)metadata.controlScheme != controlScheme.value)
        {
            if (!madeChange)
            {
                EditorContext.PrepareToModifySetlist();
                madeChange = true;
            }
            metadata.controlScheme = (ControlScheme)controlScheme.value;
        }

        UIUtils.UpdateSetlistMetadataInMemory(
            ref metadata.backImage, backgroundImage, ref madeChange);
        UIUtils.UpdateSetlistMetadataInMemory(
            ref metadata.eyecatchImage, eyecatchImage, ref madeChange);
    }

    public void OnEyecatchUpdated()
    {
        RefreshEyecatchPreview();
    }

    public void RefreshEyecatchPreview()
    {
        eyecatchPreview.LoadImage(EditorContext.setlistFolder,
            EditorContext.setlist.setlistMetadata);
    }

    public void OnDeleteSetlistButtonClick()
    {
        confirmDialog.Show(
            L10n.GetStringAndFormatIncludingPaths(
                "edit_setlist_panel_delete_setlist_confirmation",
                EditorContext.setlistFolder),
            L10n.GetString(
                "edit_setlist_panel_delete_setlist_confirm"),
            L10n.GetString(
                "edit_setlist_panel_delete_setlist_cancel"),
            () =>
            {
                // Delete from disk
                Directory.Delete(EditorContext.setlistFolder,
                    recursive: true);

                // Delete from RAM
                string parent = Path.GetDirectoryName(
                    EditorContext.setlistFolder);
                GlobalResource.setlistList[parent].RemoveAll(
                    (GlobalResource.SetlistInFolder t) =>
                    {
                        return t.folder == EditorContext.setlistFolder;
                    });

                GetComponentInChildren<
                    CustomTransitionFromEditSetlistPanel>().
                    Transition();
            });
    }
    #endregion

    #region Selectable patterns
    [Header("Selectable patterns")]
    public TrackAndPatternSideSheet sidesheet;

    public void OnAddSelectablePatternButtonClick()
    {
        sidesheet.GetComponent<Sidesheet>().FadeIn();
    }
    #endregion

    #region Hidden patterns
    #endregion
}
