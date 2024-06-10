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
            // Statistics
            StatsMaintainer.instance?.OnEnterEditor();

            // Set EditorContext
            EditorContext.previewCallback = () =>
            {
                EditorContext.inPreview = true;

                TopLevelObjects.instance.ShowUiDocument();
                TopLevelObjects.instance.editorCanvas.gameObject
                    .SetActive(false);
                TopLevelObjects.instance.eventSystem.gameObject
                    .SetActive(false);
                onPreview.Function.Call(
                    EditorContext.trackFolder,
                    EditorContext.track,
                    EditorContext.Pattern,
                    EditorContext.previewStartingScan);
            };
            EditorContext.exitCallback = () =>
            {
                StatsMaintainer.instance?.OnLeaveEditor();
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
            TopLevelObjects.instance.HideUiDocument();
            TopLevelObjects.instance.editorCanvas.gameObject
                .SetActive(true);
            TopLevelObjects.instance.eventSystem.gameObject
                .SetActive(true);
            PanelTransitioner.TransitionTo(
                TopLevelObjects.instance.trackSetupPanel
                .GetComponent<Panel>(), 
                TransitionToPanel.Direction.Right);
        }

        public void LaunchOnSetlist(string setlistFolder)
        {
            // Statistics
            StatsMaintainer.instance?.OnEnterEditor();

            // Set EditorContext
            EditorContext.exitCallback = () =>
            {
                StatsMaintainer.instance?.OnLeaveEditor();
                TopLevelObjects.instance.ShowUiDocument();
                TopLevelObjects.instance.editorCanvas.gameObject
                    .SetActive(false);
                TopLevelObjects.instance.eventSystem.gameObject
                    .SetActive(false);
                onExit.Function.Call();
            };
            EditorContext.setlistPath = Path.Combine(setlistFolder, 
                Paths.kSetlistFilename);
            EditorContext.setlist = Setlist.LoadFromFile(
                EditorContext.setlistPath) as Setlist;
            EditorContext.Reset();

            // Show track setup panel
            Panel.current = null;
            TopLevelObjects.instance.HideUiDocument();
            TopLevelObjects.instance.editorCanvas.gameObject
                .SetActive(true);
            TopLevelObjects.instance.eventSystem.gameObject
                .SetActive(true);
            PanelTransitioner.TransitionTo(
                TopLevelObjects.instance.editSetlistPanel
                .GetComponent<Panel>(),
                TransitionToPanel.Direction.Right);
        }

        // Contains timestamp so collisions are very unlikely.
        public static string TrackToDirectoryName(
            string title, string artist)
        {
            string filteredTitle = Paths.
                RemoveCharsNotAllowedOnFileSystem(title);
            string filteredArtist = Paths
                .RemoveCharsNotAllowedOnFileSystem(artist);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            return $"{filteredArtist} - {filteredTitle} - {timestamp}";
        }

        public static string SetlistToDirectoryName(
            string title)
        {
            string filteredTitle = Paths.
                RemoveCharsNotAllowedOnFileSystem(title);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{timestamp} - {filteredTitle}";
        }

        // In Lua, this function returns 2 values, the Status
        // and newTrackFolder.
        // If successful, this will update the track lists in
        // tm.resources.
        public Status CreateNewTrack(string parentFolder,
            string title, string artist, out string newTrackFolder)
        {
            // Attempt to create track directory.
            newTrackFolder = Path.Combine(parentFolder,
                TrackToDirectoryName(title, artist));
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

        // In Lua, this function returns 2 values, the Status
        // and newSetlistFolder.
        // If successful, this will update the setlist lists in
        // tm.resources.
        public Status CreateNewSetlist(string parentFolder,
            string title, out string newSetlistFolder)
        {
            // Attempt to create setlist directory.
            newSetlistFolder = Path.Combine(parentFolder,
                SetlistToDirectoryName(title));
            try
            {
                Directory.CreateDirectory(newSetlistFolder);
            }
            catch (Exception e)
            {
                return Status.FromException(e, newSetlistFolder);
            }

            // Create empty setlist.
            Setlist setlist = new Setlist(title);
            string filename = Path.Combine(newSetlistFolder,
                Paths.kSetlistFilename);
            try
            {
                setlist.SaveToFile(filename);
            }
            catch (Exception e)
            {
                return Status.FromException(e, filename);
            }

            // Update in-memory setlist list.
            GlobalResource.setlistList[parentFolder].Add(
                new GlobalResource.SetlistInFolder()
                {
                    folder = newSetlistFolder,
                    setlist = setlist
                });
            return Status.OKStatus();
        }

        // No parameter.
        // Beware that when this is called, the track lists and
        // setlist lists in tm.resources may have changed.
        public DynValue onExit;
        #endregion

        #region Editor preview
        // When user enters editor preview, editor will fade out,
        // then call this callback.
        //
        // Parameters:
        // - Track folder as string
        // - Track
        // - Pattern
        // - Starting scan
        //
        // These parameters are only for informational purposes.
        // Theme API will set up the game correctly; the theme doesn't
        // have to do anything about them.
        public DynValue onPreview;

        public void ReturnFromPreview()
        {
            EditorContext.inPreview = false;

            TopLevelObjects.instance.HideUiDocument();
            TopLevelObjects.instance.editorCanvas.gameObject
                .SetActive(true);
            TopLevelObjects.instance.eventSystem.gameObject
                .SetActive(true);
            PanelTransitioner.TransitionTo(
                TopLevelObjects.instance.patternPanel
                    .GetComponent<Panel>(),
                TransitionToPanel.Direction.Left);
        }
        #endregion
    }
}