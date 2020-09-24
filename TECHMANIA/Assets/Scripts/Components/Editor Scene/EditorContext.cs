using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public class EditorContext : MonoBehaviour
{
    public static Track track;
    public static string trackPath;
    public static string trackFolder
    {
        get
        {
            return new FileInfo(trackPath).DirectoryName;
        }
    }

    public static int patternIndex;
    public static Pattern Pattern
    {
        get { return track.patterns[patternIndex]; }
    }

    public static bool dirty;
    public static LimitedStack<Track> undoStack;
    public static LimitedStack<Track> redoStack;

    public static event UnityAction<bool> DirtynessUpdated;
    public static event UnityAction StateUpdated;

    public static void Reset()
    {
        dirty = false;
        undoStack = new LimitedStack<Track>(20);
        redoStack = new LimitedStack<Track>(20);
    }

    // Call this before making any change to track.
    public static void PrepareForChange()
    {
        dirty = true;
        undoStack.Push(track.Clone() as Track);
        redoStack.Clear();
    }    

    public static void DoneWithChange()
    {
        DirtynessUpdated?.Invoke(dirty);
    }

    // May throw exceptions.
    public static void Save()
    {
        track.SaveToFile(trackPath);
        dirty = false;
        DirtynessUpdated?.Invoke(dirty);
    }

    public static void Undo()
    {
        if (undoStack.Empty()) return;
        redoStack.Push(track.Clone() as Track);
        track = undoStack.Pop();
        dirty = true;
        DirtynessUpdated?.Invoke(dirty);
        StateUpdated?.Invoke();
    }

    public static void Redo()
    {
        if (redoStack.Empty()) return;
        undoStack.Push(track.Clone() as Track);
        track = redoStack.Pop();
        dirty = true;
        DirtynessUpdated?.Invoke(dirty);
        StateUpdated?.Invoke();
    }
}
