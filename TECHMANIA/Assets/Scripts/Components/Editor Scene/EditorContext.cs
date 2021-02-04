using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public class EditOperation
{
    public enum Type
    {
        // This includes:
        // - modifying track metadata
        // - any pattern-level operation (add, delete, duplicate)
        // - modifying pattern metadata
        Metadata,
        // Add, modify or delete.
        BpmEvent,
        AddNote,
        DeleteNote,
        // Any field other than pulse and lane.
        ModifyNote
    }
    public Type type;

    // Metadata

    public string trackSnapsnotBeforeOp;  // Serialized
    public void TakeTrackSnapshot()
    {
        trackSnapsnotBeforeOp = EditorContext.track.Serialize(
            optimizeForSaving: false);
    }
    // It's up to EditorContext to take appropriate snapshot
    // upon undo/redo.

    // BpmEvent

    public List<BpmEvent> bpmEventsBeforeOp;
    public void TakeBpmEventSnapshot()
    {
        bpmEventsBeforeOp = new List<BpmEvent>();
        foreach (BpmEvent e in EditorContext.Pattern.bpmEvents)
        {
            bpmEventsBeforeOp.Add(e.Clone());
        }
    }
    // It's up to EditorContext to clone BPM events upon undo/redo.

    // AddNote

    public Note addedNote;

    // DeleteNote

    public Note deletedNote;

    // ModifyNote

    public Note noteBeforeOp;
    public Note noteAfterOp;
}

// The unit of undo/redo.
public class EditTransaction
{
    public List<EditOperation> ops;
}

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
    private static LimitedStack<EditTransaction> undoStack;
    private static LimitedStack<EditTransaction> redoStack;
    private static EditTransaction currentTransaction;

    public static event UnityAction<bool> DirtynessUpdated;
    public static event UnityAction UndoRedoStackUpdated;
    
    // For types Metadata and BpmEvent, EditorContext has already
    // updated track/pattern before firing these events, listener
    // only needs to update UI.
    //
    // For other types, it's up to the listener to modify pattern
    // and update UI. UndoInvoked listener should reverse any
    // operation they receive; RedoInvoked listener should repeat
    // any operation they receive.
    public static event UnityAction<EditTransaction> UndoInvoked;
    public static event UnityAction<EditTransaction> RedoInvoked;

    public static void Reset()
    {
        Dirty = false;
        undoStack = new LimitedStack<EditTransaction>(20);
        redoStack = new LimitedStack<EditTransaction>(20);
    }

    // TODO: deprecate
    public static void PrepareForChange()
    {
    }    

    // TODO: deprecate
    public static void DoneWithChange()
    {
    }

    #region Transaction and operation APIs
    // Any modification to the track should look like this:
    //
    // EditorContext.BeginTransaction();
    // EditOperation op = EditorContext.BeginOperation(...);
    // MakeChange();
    // RecordBeforeAndAfterStatesInOp();
    // EditOperation op = EditorContext.BeginOperation(...);
    // MakeChange();
    // RecordBeforeAndAfterStatesInOp();
    // EditorContext.EndTransaction();
    //
    // It's up to the caller to record the following in the op:
    // - AddNote: a clone of the added note
    // - DeleteNote: a clone of the deleted note
    // - ModifyNote: clones of note both before and after modification
    public static void BeginTransaction()
    {
        currentTransaction = new EditTransaction();
        currentTransaction.ops = new List<EditOperation>();
    }

    public static void EndTransaction()
    {
        undoStack.Push(currentTransaction);
        currentTransaction = null;
        redoStack.Clear();

        Dirty = true;
        DirtynessUpdated?.Invoke(Dirty);
        UndoRedoStackUpdated?.Invoke();
    }

    // Call this shortcut before making any change to track
    // or pattern metadata. No need to call anything on transactions
    // or operations.
    public static void PrepareToModifyMetadata()
    {
        BeginTransaction();
        BeginOperation(EditOperation.Type.Metadata);
        EndTransaction();
    }

    // Call this shortcut before making any change to BPM events.
    // No need to call anything on transactions or operations.
    public static void PrepareToModifyBpmEvent()
    {
        BeginTransaction();
        BeginOperation(EditOperation.Type.BpmEvent);
        EndTransaction();
    }

    // For types Metadata and BpmEvent, EditorContext will
    // automatically record the current track / list of BPM events.
    //
    // For other types, it's up to the caller to record the
    // before/after state of things.
    public static EditOperation BeginOperation(
        EditOperation.Type type)
    {
        EditOperation op = new EditOperation();
        op.type = type;
        currentTransaction.ops.Add(op);
        switch (type)
        {
            case EditOperation.Type.Metadata:
                op.TakeTrackSnapshot();
                break;
            case EditOperation.Type.BpmEvent:
                op.TakeBpmEventSnapshot();
                break;
        }
        return op;
    }
    #endregion

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

        EditTransaction transaction = undoStack.Pop();
        EditTransaction transactionToRedo = 
            ProcessTransactionAndConvertForOtherStack(transaction);
        redoStack.Push(transactionToRedo);
        UndoRedoStackUpdated?.Invoke();

        UndoInvoked?.Invoke(transaction);

        Dirty = true;
        DirtynessUpdated?.Invoke(Dirty);
    }

    public static void Redo()
    {
        if (redoStack.Empty()) return;

        EditTransaction transaction = redoStack.Pop();
        EditTransaction transactionToUndo =
            ProcessTransactionAndConvertForOtherStack(transaction);
        undoStack.Push(transactionToUndo);
        UndoRedoStackUpdated?.Invoke();

        RedoInvoked?.Invoke(transaction);

        Dirty = true;
        DirtynessUpdated?.Invoke(Dirty);
    }

    private static EditTransaction 
        ProcessTransactionAndConvertForOtherStack(
        EditTransaction input)
    {
        EditTransaction output = new EditTransaction();
        output.ops = new List<EditOperation>();
        foreach (EditOperation op in input.ops)
        {
            switch (op.type)
            {
                case EditOperation.Type.Metadata:
                    {
                        EditOperation convertedOp =
                            new EditOperation();
                        convertedOp.type = op.type;
                        convertedOp.TakeTrackSnapshot();
                        output.ops.Add(convertedOp);

                        track = TrackBase.Deserialize(
                            op.trackSnapsnotBeforeOp) as Track;
                    }
                    break;
                case EditOperation.Type.BpmEvent:
                    {
                        EditOperation convertedOp =
                            new EditOperation();
                        convertedOp.type = op.type;
                        convertedOp.TakeBpmEventSnapshot();
                        output.ops.Add(convertedOp);

                        Pattern.bpmEvents.Clear();
                        foreach (BpmEvent e in op.bpmEventsBeforeOp)
                        {
                            Pattern.bpmEvents.Add(e.Clone());
                        }
                    }
                    break;
                default:
                    output.ops.Add(op);
                    break;
            }
        }
        return output;
    }

    public static void ClearUndoRedoStack()
    {
        undoStack.Clear();
        redoStack.Clear();
        UndoRedoStackUpdated?.Invoke();
    }
}
