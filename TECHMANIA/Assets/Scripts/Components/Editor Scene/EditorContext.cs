using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public class EditorContext : MonoBehaviour
{
    public static TrackV1 track;
    public static string trackPath;
    public static string trackFolder
    {
        get
        {
            return new FileInfo(trackPath).DirectoryName;
        }
    }

    public static int patternIndex;
    public static PatternV1 Pattern
    {
        get { return track.patterns[patternIndex]; }
    }

    public static bool Dirty { get; private set; }
    private static LimitedStack<TrackV1> undoStack;
    private static LimitedStack<TrackV1> redoStack;

    public static event UnityAction<bool> DirtynessUpdated;
    public static event UnityAction UndoRedoStackUpdated;
    public static event UnityAction UndoneOrRedone;

    public static void Reset()
    {
        Dirty = false;
        undoStack = new LimitedStack<TrackV1>(20);
        redoStack = new LimitedStack<TrackV1>(20);
    }

    // Call this before making any change to track.
    public static void PrepareForChange()
    {
        Dirty = true;
        undoStack.Push(track.Clone() as TrackV1);
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
        redoStack.Push(track.Clone() as TrackV1);
        track = undoStack.Pop();
        Dirty = true;
        DirtynessUpdated?.Invoke(Dirty);
        UndoneOrRedone?.Invoke();
        UndoRedoStackUpdated?.Invoke();
    }

    public static void Redo()
    {
        if (redoStack.Empty()) return;
        undoStack.Push(track.Clone() as TrackV1);
        track = redoStack.Pop();
        Dirty = true;
        DirtynessUpdated?.Invoke(Dirty);
        UndoneOrRedone?.Invoke();
        UndoRedoStackUpdated?.Invoke();
    }
}
