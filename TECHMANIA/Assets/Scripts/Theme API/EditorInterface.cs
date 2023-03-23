using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    [MoonSharpUserData]
    public class EditorInterface
    {
        #region Launching and exiting
        public void LaunchOnTrack(string trackFolder)
        {
            TopLevelObjects.instance.mainUiDocument.gameObject
                .SetActive(false);
            TopLevelObjects.instance.editorCanvas.gameObject
                .SetActive(true);
            // TODO: set EditorContext
            // TODO: show select track panel
        }

        // In Lua, this function returns 2 values, the Status
        // and newTrackFolder.
        public Status CreateNewTrack(string parentFolder,
            string title, string artist, out string newTrackFolder)
        {
            newTrackFolder = "";
            return Status.OKStatus();
        }

        public DynValue onExit;
        #endregion

        #region Editor preview
        // When user enters editor preview, editor will fade out,
        // then call this callback. Parameter: to be determined.
        public DynValue onPreview;

        public void ReturnFromPreview()
        {

        }
        #endregion
    }
}