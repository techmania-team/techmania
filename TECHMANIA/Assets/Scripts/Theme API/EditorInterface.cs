using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using System.IO;
using System;

namespace ThemeApi
{
    [MoonSharpUserData]
    public class EditorInterface
    {
        #region Launching and exiting
        public void LaunchOnTrack(string trackFolder)
        {
            TopLevelObjects.instance.HideUiDocument();
            TopLevelObjects.instance.editorCanvas.gameObject
                .SetActive(true);
            TopLevelObjects.instance.eventSystem.gameObject
                .SetActive(true);

            // Set EditorContext
            EditorContext.exitCallback = () =>
            {
                TopLevelObjects.instance.ShowUiDocument();
                TopLevelObjects.instance.editorCanvas.gameObject
                    .SetActive(false);
                TopLevelObjects.instance.eventSystem.gameObject
                    .SetActive(false);
                onExit.Function.Call();
            };
            EditorContext.trackPath = Path.Combine(trackFolder,
                Paths.kTrackFilename);
            EditorContext.track = Track.LoadFromFile(
                EditorContext.trackPath) as Track;
            EditorContext.Reset();

            // Show track setup panel
            Panel.current = null;
            PanelTransitioner.TransitionTo(
                TopLevelObjects.instance.trackSetupPanel
                .GetComponent<Panel>(), 
                TransitionToPanel.Direction.Right);
        }

        // In Lua, this function returns 2 values, the Status
        // and newTrackFolder.
        // If successful, this will update the track lists in
        // tm.resources.
        public Status CreateNewTrack(string parentFolder,
            string title, string artist, out string newTrackFolder)
        {
            // Attempt to create track directory. Contains timestamp
            // so collisions are very unlikely.
            string filteredTitle = Paths.
                RemoveCharsNotAllowedOnFileSystem(title);
            string filteredArtist = Paths
                .RemoveCharsNotAllowedOnFileSystem(artist);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            newTrackFolder = Path.Combine(parentFolder,
                $"{filteredArtist} - {filteredTitle} - {timestamp}");
            try
            {
                Directory.CreateDirectory(newTrackFolder);
            }
            catch (Exception e)
            {
                return Status.FromException(e, newTrackFolder);
            }

            // Create empty track.
            Track track = new Track(title, artist);
            string filename = Path.Combine(newTrackFolder, 
                Paths.kTrackFilename);
            try
            {
                track.SaveToFile(filename);
            }
            catch (Exception e)
            {
                return Status.FromException(e, filename);
            }

            // Update in-memory track list.
            GlobalResource.trackList[parentFolder].Add(
                new GlobalResource.TrackInFolder()
            {
                folder = newTrackFolder,
                minimizedTrack = Track.Minimize(track)
            });

            return Status.OKStatus();
        }

        // No parameter.
        // Beware that when this is called, the track lists in
        // tm.resources may have changed.
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