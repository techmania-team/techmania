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
    private ControlScheme scheme;
    private int lanes;

    private GameController controller;
    private GameLayout layout;
    private NoteManager noteManager;
    private GameTimer timer;

    public List<List<KeyCode>> keysForLane { get; private set; }

    public GameInputManager(
        Pattern pattern,
        GameController controller,
        GameLayout layout,
        NoteManager noteManager,
        GameTimer timer)
    {
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
        ongoingNotes = new Dictionary<NoteElements, 
            JudgementAndTimeDifference>();
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

    public void JumpToScan()
    {
        ongoingNotes.Clear();
        ongoingNoteIsHitOnThisFrame.Clear();
        ongoingNoteLastInput.Clear();
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
        //        hitOnThisFrame[0]                    |
        //              |                              |
        //              --------------------------------
        //                              |
        //                         ResolveNote
        //
        // [0] marks the note as being hit on the current frame.
        // [1] after all input is handled, any ongoing note not
        // marked on the current frame will be seen as lost input,
        // eventually resolved as MISS; any ongoing note that
        // finished its duration is resolved with the original
        // judgement on the note head.
        // [2] takes a lane number in Keys, works on any lane in KM.

        if (controller.autoPlay)
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
            if (noteManager.notesInLane[lane].IsEmpty()) continue;
            NoteElements upcoming = noteManager.notesInLane[lane]
                .First() as NoteElements;

            if (timer.gameTime >= upcoming.note.time &&
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
            if (noteManager.notesInLane[lane].IsEmpty()) continue;
            NoteElements upcoming = noteManager.notesInLane[lane]
                .First() as NoteElements;

            if (timer.gameTime > upcoming.note.time
                + upcoming.note.timeWindow[Judgement.Miss]
                + LatencyForNote(upcoming.note) * timer.speed
                &&
                !ongoingNotes.ContainsKey(upcoming))
            {
                controller.ResolveNote(upcoming,
                    JudgementAndTimeDifference.Break());
            }
        }
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
        return controller.autoPlay ? 0f : latencyMs * 0.001f;
    }

    private float TimeDifferenceForNote(Note n)
    {
        float correctTime = n.time + LatencyForNote(n);
        return timer.gameTime - correctTime;
    }

    public static Judgement TimeDifferenceToJudgement(
        Note note, float timeDifference, float speed)
    {
        float absDifference = Mathf.Abs(timeDifference);
        absDifference /= speed;

        foreach (Judgement j in new List<Judgement>{
            Judgement.RainbowMax,
            Judgement.Max,
            Judgement.Cool,
            Judgement.Good,
            Judgement.Miss
        })
        {
            if (absDifference <= note.timeWindow[j])
            {
                return j;
            }
        }

        // Control shouldn't reach here.
        throw new System.Exception($"timeDifference {timeDifference} is outside of all time windows; cannot determine judgement.");
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
        RaycastResult raycastResult = Raycast(screenPoint,
            prioritizeNewNote: false);
        foreach (NoteElements elements in raycastResult.ongoingNotes)
        {
            if (ongoingNoteIsHitOnThisFrame.ContainsKey(elements))
            {
                ongoingNoteIsHitOnThisFrame[elements] = true;
            }
        }
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

    // For repeat note series, the fields point to the repeat note
    // to check, instead of the repeat head.
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

    // For optimization, if prioritizeNewNote, raycast will stop
    // after finding a new note.
    // If !prioritizeNewNote, raycast will ignore all not-ongoing
    // notes.
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
            if (noteToCheck.note.type == NoteType.RepeatHead ||
                noteToCheck.note.type == NoteType.RepeatHeadHold)
            {
                noteToCheck = (noteToCheck as RepeatHeadElementsBase)
                    .GetFirstUnresolvedManagedNote();
            }

            if (ongoingNotes.ContainsKey(noteToCheck))
            {
                result.ongoingNotes.Add(noteToCheck);
                return;
            }

            // If control reaches here, we probably hit a new
            // note, but there are a few more checks.

            // Do we even care about new notes?
            if (!prioritizeNewNote) return;

            // Have we already hit another new note?
            if (result.newNote != null) return;

            // Is the current time within the note's time window?
            float difference = TimeDifferenceForNote(
                noteToCheck.note);
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
        for (int scan = timer.intScan - 2;
            scan <= timer.intScan + 2;
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
    private void OnKeyDownOnLane(int lane)
    {
        if (noteManager.notesInLane[lane].IsEmpty())
        {
            return;
        }

        NoteElements upcoming = noteManager.notesInLane[lane]
            .First() as NoteElements;
        if (ongoingNotes.ContainsKey(upcoming))
        {
            return;
        }

        CheckHitOnKeyboardNote(upcoming);
    }

    private void OnKeyHeldOnLane(int lane)
    {
        NoteElements noteToMark = null;
        foreach (KeyValuePair<NoteElements, bool> pair in
            ongoingNoteIsHitOnThisFrame)
        {
            if (pair.Value == true) continue;
            if (pair.Key.note.lane != lane) continue;
            noteToMark = pair.Key;
            break;
        }
        if (noteToMark != null)
        {
            ongoingNoteIsHitOnThisFrame[noteToMark] = true;
        }
    }

    private void OnKeyDownOnAnyLane()
    {
        // Find the earliest keyboard note.

        List<NoteElements> upcomingInAllLanes = new
            List<NoteElements>();
        int earliestPulse = int.MaxValue;
        foreach (NoteList list in noteManager.keyboardNotesInLane)
        {
            if (list.IsEmpty()) continue;
            NoteElements upcoming = list.First() as NoteElements;
            if (ongoingNotes.ContainsKey(upcoming)) continue;
            upcomingInAllLanes.Add(upcoming);
            if (upcoming.note.pulse < earliestPulse)
            {
                earliestPulse = upcoming.note.pulse;
            }
        }
        if (upcomingInAllLanes.Count == 0)
        {
            return;
        }
        List<NoteElements> earliestUpcoming = new List<NoteElements>();
        foreach (NoteElements elements in upcomingInAllLanes)
        {
            if (elements.note.pulse == earliestPulse)
            {
                earliestUpcoming.Add(elements);
            }
        }

        NoteElements earliest = null;
        if (earliestUpcoming.Count == 1)
        {
            earliest = earliestUpcoming[0];
        }
        else
        {
            // Pick the first note that has no duration, if any.
            foreach (NoteElements n in earliestUpcoming)
            {
                if (n.note.type == NoteType.RepeatHead ||
                    n.note.type == NoteType.Repeat)
                {
                    earliest = n;
                    break;
                }
            }
            if (earliest == null)
            {
                earliest = earliestUpcoming[0];
            }
        }

        CheckHitOnKeyboardNote(earliest);
    }

    private void OnKeyHeldOnAnyLane()
    {
        NoteElements noteToMark = null;
        foreach (KeyValuePair<NoteElements, bool> pair in
            ongoingNoteIsHitOnThisFrame)
        {
            if (pair.Value == true) continue;
            noteToMark = pair.Key;
            break;
        }
        if (noteToMark != null)
        {
            ongoingNoteIsHitOnThisFrame[noteToMark] = true;
        }
    }

    private void CheckHitOnKeyboardNote(NoteElements elements)
    {
        Note note = elements.note;

        // Compare time.
        float difference = TimeDifferenceForNote(note);
        if (Mathf.Abs(difference) >
            note.timeWindow[Judgement.Miss])
        {
            // The keystroke is too early or too late
            // for this note, so it's an empty hit.
            controller.EmptyHitForKeyboard(note);
        }
        else
        {
            // The keystroke lands on this note.
            controller.HitNote(elements, difference);
        }
    }
    #endregion

    #region Ongoing notes
    // Value is the judgement and time difference at note's head.
    public Dictionary<NoteElements, JudgementAndTimeDifference> 
        ongoingNotes { get; private set; }
    private Dictionary<NoteElements, bool> ongoingNoteIsHitOnThisFrame;
    private Dictionary<NoteElements, float> ongoingNoteLastInput;

    public void RegisterOngoingNote(NoteElements elements,
        JudgementAndTimeDifference judgementAndTimeDifference)
    {
        ongoingNotes.Add(elements, judgementAndTimeDifference);
        ongoingNoteIsHitOnThisFrame.Add(elements, true);
    }

    public bool IsOngoing(NoteElements elements)
    {
        return ongoingNotes.ContainsKey(elements);
    }

    private void UpdateOngoingNotes()
    {
        foreach (KeyValuePair<NoteElements, bool> pair in
               ongoingNoteIsHitOnThisFrame)
        {
            NoteElements elements = pair.Key;
            Note note = elements.note;
            bool hit = pair.Value;

            // Has the note's duration finished?
            float latency = LatencyForNote(note);
            float endTime = 0f;
            float gracePeriodLength = 0f;
            if (note is HoldNote)
            {
                HoldNote holdNote = note as HoldNote;
                endTime = holdNote.endTime + latency;
                gracePeriodLength = holdNote.gracePeriodLength;
            }
            else if (note is DragNote)
            {
                DragNote dragNote = note as DragNote;
                endTime = dragNote.endTime + latency;
                gracePeriodLength = dragNote.gracePeriodLength;
            }
            if (timer.gameTime >= endTime)
            {
                // Resolve note.
                controller.ResolveNote(elements,
                    ongoingNotes[elements]);
                ongoingNotes.Remove(elements);
                continue;
            }

            // Update time of last input.
            if (hit)
            {
                // Will create new element if not existing.
                ongoingNoteLastInput[elements] = timer.gameTime;
            }
            else if (!controller.autoPlay)
            {
                float lastInput = ongoingNoteLastInput[elements];
                if (timer.gameTime > lastInput + gracePeriodLength)
                {
                    // No input on this note for too long, resolve
                    // as MISS.
                    controller.ResolveNote(elements,
                        JudgementAndTimeDifference.Miss());
                    controller.StopKeysoundIfPlaying(note);
                    ongoingNotes.Remove(elements);
                    ongoingNoteLastInput.Remove(elements);
                }
            }
        }

        // Prepare for next frame.
        ongoingNoteIsHitOnThisFrame.Clear();
        foreach (NoteElements elements in ongoingNotes.Keys)
        {
            ongoingNoteIsHitOnThisFrame.Add(elements, false);
        }
    }
    #endregion
}