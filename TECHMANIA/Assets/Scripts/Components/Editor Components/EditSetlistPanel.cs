using System.Collections;
using System.Collections.Generic;
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
}
