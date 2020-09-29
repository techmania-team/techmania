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

    public static bool Dirty { get; private set; }
    private static LimitedStack<Track> undoStack;
    private static LimitedStack<Track> redoStack;

    public static event UnityAction<bool> DirtynessUpdated;
    public static event UnityAction UndoRedoStackUpdated;
    public static event UnityAction StateUpdated;

    public static void Reset()
    {
        Dirty = false;
        undoStack = new LimitedStack<Track>(20);
        redoStack = new LimitedStack<Track>(20);
    }

    // Call this before making any change to track.
    public static void PrepareForChange()
    {
        Dirty = true;
        undoStack.Push(track.Clone() as Track);
        redoStack.Clear();
        UndoRedoStackUpdated?.Invoke();
    }    

    public static void DoneWithChange()
    {
        DirtynessUpdated?.Invoke(Dirty);
    }

    // May throw exceptions.
    public static void Save()
    {
        track.SaveToFile(trackPath);
        Dirty = false;
        DirtynessUpdated?.Invoke(Dirty);
    }

    public static bool CanUndo()
    {
        return !undoStack.Empty();
    }

    public static bool CanRedo()
    {
        return !redoStack.Empty();
    }

    public static void Undo()
    {
        if (undoStack.Empty()) return;
        redoStack.Push(track.Clone() as Track);
        track = undoStack.Pop();
        Dirty = true;
        DirtynessUpdated?.Invoke(Dirty);
        StateUpdated?.Invoke();
        UndoRedoStackUpdated?.Invoke();
    }

    public static void Redo()
    {
        if (redoStack.Empty()) return;
        undoStack.Push(track.Clone() as Track);
        track = redoStack.Pop();
        Dirty = true;
        DirtynessUpdated?.Invoke(Dirty);
        StateUpdated?.Invoke();
        UndoRedoStackUpdated?.Invoke();
    }
}
