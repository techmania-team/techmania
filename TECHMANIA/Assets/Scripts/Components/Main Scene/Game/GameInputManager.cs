using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public enum InputDevice
{
    Touchscreen,
    Keyboard,
    Mouse
}

public class GameInputManager
{
    private Pattern pattern;
    private ControlScheme scheme;
    private int lanes;

    private GameController controller;
    private GameLayout layout;
    private NoteManager noteManager;
    private GameTimer timer;

    private List<List<KeyCode>> keysForLane;

    public GameInputManager(
        Pattern pattern,
        GameController controller,
        GameLayout layout,
        NoteManager noteManager,
        GameTimer timer)
    {
        this.pattern = pattern;
        scheme = pattern.patternMetadata.controlScheme;
        lanes = pattern.patternMetadata.playableLanes;

        this.controller = controller;
        this.layout = layout;
        this.noteManager = noteManager;
        this.timer = timer;
    }

    public void Prepare()
    {
        // Initialize data structures.
        fingerInLane = new Dictionary<int, int>();
        ongoingNotes = new Dictionary<NoteElements, Judgement>();
        ongoingNoteIsHitOnThisFrame = new
            Dictionary<NoteElements, bool>();
        ongoingNoteLastInput = new Dictionary<NoteElements, float>();

        // Prepare keycodes for keyboard input.
        keysForLane = new List<List<KeyCode>>();
        if (lanes >= 4)
        {
            keysForLane.Add(new List<KeyCode>()
            {
                KeyCode.BackQuote,
                KeyCode.Alpha0,
                KeyCode.Alpha1,
                KeyCode.Alpha2,
                KeyCode.Alpha3,
                KeyCode.Alpha4,
                KeyCode.Alpha5,
                KeyCode.Alpha6,
                KeyCode.Alpha7,
                KeyCode.Alpha8,
                KeyCode.Alpha9,
                KeyCode.Minus,
                KeyCode.Equals,
                KeyCode.KeypadDivide,
                KeyCode.KeypadMultiply,
                KeyCode.KeypadMinus
            });
        }
        if (lanes >= 3)
        {
            keysForLane.Add(new List<KeyCode>()
            {
                KeyCode.Q,
                KeyCode.W,
                KeyCode.E,
                KeyCode.R,
                KeyCode.T,
                KeyCode.Y,
                KeyCode.U,
                KeyCode.I,
                KeyCode.O,
                KeyCode.P,
                KeyCode.LeftBracket,
                KeyCode.RightBracket,
                KeyCode.Backslash,
                KeyCode.Keypad7,
                KeyCode.Keypad8,
                KeyCode.Keypad9
            });
        }
        keysForLane.Add(new List<KeyCode>()
        {
            KeyCode.A,
            KeyCode.S,
            KeyCode.D,
            KeyCode.F,
            KeyCode.G,
            KeyCode.H,
            KeyCode.J,
            KeyCode.K,
            KeyCode.L,
            KeyCode.Semicolon,
            KeyCode.Quote,
            KeyCode.Keypad4,
            KeyCode.Keypad5,
            KeyCode.Keypad6
        });
        keysForLane.Add(new List<KeyCode>()
        {
            KeyCode.Z,
            KeyCode.X,
            KeyCode.C,
            KeyCode.V,
            KeyCode.B,
            KeyCode.N,
            KeyCode.M,
            KeyCode.Comma,
            KeyCode.Period,
            KeyCode.Slash,
            KeyCode.Keypad1,
            KeyCode.Keypad2,
            KeyCode.Keypad3
        });
    }

    public void Dispose()
    {
        keysForLane.Clear();
    }

    #region Update
    public void Update()
    {
        // Input handling gets a bit complicated so here's a graph.
        //
        // Touch/KM                Keys/KM         Timer
        // --------                -------         -----
        // OnFingerDown/Move[0]    OnKeyDown[1]    CheckForBreak[2]
        //     |                       |               |
        // ProcessFingerDown           |               |
        //     |                       |               |
        //     -------------------------               |
        //                |                            |
        //             HitNote                         |
        //              |   |                          |
        //              |   ----------------------------
        //              |                 |
        //       RegisterOngoing       ResolveNote
        //
        // [0] mouse is considered finger #0; finger moving between
        // lanes will cause a new finger down event. Therefore the
        // handling of finger down events is in a separate method.
        // [1] takes a lane number in Keys, works on any lane in KM.
        // [2] the timer will resolve notes as Breaks when the
        // time window to play them has completely passed.
        //
        //
        //
        // Parallel to the above is the handling of ongoing notes.
        //
        // Touch/KM           Keys/KM         Update
        // --------           -------         ------
        // OnFingerHeld       OnKeyHeld[2]    UpdateOngoingNotes[1]
        //      |                 |                    |
        //      -------------------                    |
        //              |                              |
        //        HitOngoingNote[0]                    |
        //              |                              |
        //              --------------------------------
        //                              |
        //                         ResolveNote
        //
        // [0] marks the note as being hit on the current frame, or
        // resolves the note if its duration has passed.
        // [1] after all input is handled, any ongoing note not
        // marked on the current frame will be resolved as Misses.
        // [2] takes a lane number in Keys, works on any lane in KM.

        if (GameController.autoPlay)
        {
            HandleAutoPlay();
        }
        else
        {
            HandleInputByScheme();
            CheckForBreak();
        }
        UpdateOngoingNotes();
    }

    private void HandleInputByScheme()
    {
        switch (scheme)
        {
            case ControlScheme.Touch:
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    switch (t.phase)
                    {
                        case TouchPhase.Began:
                            OnFingerDown(t.fingerId, t.position);
                            break;
                        case TouchPhase.Moved:
                            OnFingerMove(t.fingerId, t.position);
                            OnFingerHeld(t.position);
                            break;
                        case TouchPhase.Stationary:
                            OnFingerHeld(t.position);
                            break;
                        case TouchPhase.Canceled:
                        case TouchPhase.Ended:
                            OnFingerUp(t.fingerId);
                            break;
                    }
                }
                break;
            case ControlScheme.Keys:
                for (int lane = 0; lane < lanes; lane++)
                {
                    foreach (KeyCode key in keysForLane[lane])
                    {
                        if (Input.GetKeyDown(key))
                        {
                            OnKeyDownOnLane(lane);
                        }
                        if (Input.GetKey(key))
                        {
                            OnKeyHeldOnLane(lane);
                        }
                    }
                }
                break;
            case ControlScheme.KM:
                if (Input.GetMouseButtonDown(0) ||
                    Input.GetMouseButtonDown(1) ||
                    Input.GetMouseButtonDown(2))
                {
                    OnFingerDown(0, Input.mousePosition);
                }
                if (Input.GetMouseButton(0) ||
                    Input.GetMouseButton(1) ||
                    Input.GetMouseButton(2))
                {
                    OnFingerMove(0, Input.mousePosition);
                    OnFingerHeld(Input.mousePosition);
                }
                if (Input.GetMouseButtonUp(0) ||
                    Input.GetMouseButtonUp(1) ||
                    Input.GetMouseButtonUp(2))
                {
                    OnFingerUp(0);
                }
                for (int lane = 0; lane < lanes; lane++)
                {
                    foreach (KeyCode key in keysForLane[lane])
                    {
                        if (Input.GetKeyDown(key))
                        {
                            OnKeyDownOnAnyLane();
                        }
                        if (Input.GetKey(key))
                        {
                            OnKeyHeldOnAnyLane();
                        }
                    }
                }
                break;
        }
    }

    private void HandleAutoPlay()
    {
        for (int lane = 0; lane < lanes; lane++)
        {
            if (noteManager.notesInLane[lane].Count == 0) continue;
            NoteElements upcoming = noteManager.notesInLane[lane]
                .First() as NoteElements;

            if (timer.GameTime >= upcoming.note.time &&
                !ongoingNotes.ContainsKey(upcoming))
            {
                controller.HitNote(upcoming, 0f);
            }
        }
    }

    private void CheckForBreak()
    {
        for (int lane = 0; lane < lanes; lane++)
        {
            if (noteManager.notesInLane[lane].Count == 0) continue;
            NoteElements upcoming = noteManager.notesInLane[lane]
                .First() as NoteElements;

            
            if (timer.GameTime > upcoming.note.time
                + upcoming.note.timeWindow[Judgement.Miss]
                + LatencyForNote(upcoming.note) * timer.speed
                &&
                !ongoingNotes.ContainsKey(upcoming))
            {
                controller.ResolveNote(upcoming, Judgement.Break);
            }
        }
    }

    private void UpdateOngoingNotes()
    {
        
    }
    #endregion

    #region Timing calculation
    private InputDevice DeviceForNote(Note n)
    {
        if (scheme == ControlScheme.Touch)
        {
            return InputDevice.Touchscreen;
        }
        if (scheme == ControlScheme.Keys)
        {
            return InputDevice.Keyboard;
        }

        switch (n.type)
        {
            case NoteType.Basic:
            case NoteType.ChainHead:
            case NoteType.ChainNode:
            case NoteType.Drag:
                return InputDevice.Mouse;
            default:
                return InputDevice.Keyboard;
        }
    }

    private float LatencyForNote(Note n)
    {
        int latencyMs = Options.instance.GetLatencyForDevice(
            DeviceForNote(n));
        return GameController.autoPlay ? 0f : latencyMs * 0.001f;
    }
    #endregion

    #region Finger / mouse events
    private Dictionary<int, int> fingerInLane;

    private void OnFingerDown(int finger, Vector2 screenPoint)
    {
        int lane = layout.ScreenPointToLaneNumber(screenPoint);
        if (fingerInLane.ContainsKey(finger))
        {
            fingerInLane[finger] = lane;
        }
        else
        {
            fingerInLane.Add(finger, lane);
        }
        ProcessFingerDown(lane, screenPoint);
    }

    // Meant for ongoing notes; doesn't care which finger.
    private void OnFingerHeld(Vector2 screenPoint)
    {
        // TODO
    }

    // Fires additional finger down events if the finger moved
    // between lanes.
    private void OnFingerMove(int finger, Vector2 screenPoint)
    {
        if (!fingerInLane.ContainsKey(finger))
        {
            OnFingerDown(finger, screenPoint);
            return;
        }

        int lane = layout.ScreenPointToLaneNumber(screenPoint);
        if (fingerInLane[finger] != lane)
        {
            ProcessFingerDown(lane, screenPoint);
            fingerInLane[finger] = lane;
        }
    }

    private void OnFingerUp(int finger)
    {
        fingerInLane.Remove(finger);
    }

    private void ProcessFingerDown(int lane, Vector2 screenPoint)
    {
        RaycastResult raycastResult = Raycast(screenPoint,
            prioritizeNewNote: true);
        if (raycastResult.newNote != null)
        {
            // Hit a new note, so just process it.
            controller.HitNote(raycastResult.newNote, 
                raycastResult.newNoteTimeDifference);
        }
        else if (raycastResult.ongoingNotes.Count > 0)
        {
            // Hit no new note but at least 1 ongoing note, this
            // will be processed by OnFingerHeld. Do nothing here.
            return;
        }
        else
        {
            // Hit nothing - empty hit.
            controller.EmptyHitForFinger(lane);
        }
    }

    private class RaycastResult
    {
        // If a point hits multiple new notes, this is the one
        // with smallest pulse.
        public NoteElements newNote = null;
        public float newNoteTimeDifference = 0f;
        // All ongoing notes that the point hits.
        public List<NoteElements> ongoingNotes =
            new List<NoteElements>();
    }

    // If prioritizeNewNote, raycast will stop after finding
    // a new note.
    private RaycastResult Raycast(Vector2 screenPoint,
        bool prioritizeNewNote)
    {
        RaycastResult result = new RaycastResult();
        System.Action<NoteElements> raycastOnNote =
            (NoteElements elements) =>
        {
            if (elements.hitbox == null) return;
            if (elements.hitbox.pickingMode == PickingMode.Ignore)
            {
                return;
            }
            if (!ThemeApi.VisualElementTransform
                .ElementContainsPointInScreenSpace(
                elements.hitbox, screenPoint))
            {
                return;
            }
            if (scheme == ControlScheme.KM)
            {
                if (elements.note.type == NoteType.Hold ||
                    elements.note.type == NoteType.RepeatHead ||
                    elements.note.type == NoteType.RepeatHeadHold ||
                    elements.note.type == NoteType.Repeat ||
                    elements.note.type == NoteType.RepeatHold)
                {
                    return;
                }
            }

            NoteElements noteToCheck = elements;
            // TODO: get the correct note to check for
            // repeat heads
            if (ongoingNotes.ContainsKey(noteToCheck))
            {
                result.ongoingNotes.Add(noteToCheck);
                return;
            }

            // If control reaches here, we probably hit a new
            // note, but there are a few more checks.

            // Have we already hit another new note?
            if (result.newNote != null) return;

            // Is the current time within the note's time window?
            float correctTime = noteToCheck.note.time
                + LatencyForNote(noteToCheck.note);
            float difference = timer.GameTime - correctTime;
            if (Mathf.Abs(difference) >
                noteToCheck.note.timeWindow[Judgement.Miss])
            {
                return;
            }

            // All checks pass, we hit this note.
            result.newNote = noteToCheck;
            result.newNoteTimeDifference = difference;
        };

        // Raycast on notes in the surrounding scans.
        for (int scan = timer.IntScan - 2;
            scan <= timer.IntScan + 2;
            scan++)
        {
            if (!noteManager.notesInScan.ContainsKey(scan))
            {
                continue;
            }
            foreach (NoteElements elements in
                noteManager.notesInScan[scan])
            {
                raycastOnNote(elements);
                if (prioritizeNewNote && result.newNote != null)
                {
                    break;
                }
            }
            if (prioritizeNewNote && result.newNote != null)
            {
                break;
            }
        }

        return result;
    }
    #endregion

    #region Keyboard events
    public void OnKeyDownOnLane(int lane)
    {

    }

    public void OnKeyHeldOnLane(int lane)
    {

    }

    public void OnKeyDownOnAnyLane()
    {

    }

    public void OnKeyHeldOnAnyLane()
    {

    }
    #endregion

    #region Ongoing notes
    // Value is the judgement at note's head.
    private Dictionary<NoteElements, Judgement> ongoingNotes;
    private Dictionary<NoteElements, bool> ongoingNoteIsHitOnThisFrame;
    private Dictionary<NoteElements, float> ongoingNoteLastInput;

    public void RegisterOngoingNote(NoteElements elements,
        Judgement judgement)
    {
        ongoingNotes.Add(elements, judgement);
        ongoingNoteIsHitOnThisFrame.Add(elements, true);
    }

    public bool IsOngoing(NoteElements elements)
    {
        return ongoingNotes.ContainsKey(elements);
    }
    #endregion
}
