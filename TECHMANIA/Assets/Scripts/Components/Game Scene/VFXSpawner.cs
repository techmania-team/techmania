using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXSpawner : MonoBehaviour
{
    public GameObject vfxPrefab;

    private Dictionary<NoteObject, GameObject> 
        holdNoteToOngoingHeadVfx;
    private Dictionary<NoteObject, GameObject> 
        holdNoteToOngoingTrailVfx;
    private Dictionary<NoteObject, GameObject>
        dragNoteToOngoingVfx;

    private void Start()
    {
        holdNoteToOngoingHeadVfx =
            new Dictionary<NoteObject, GameObject>();
        holdNoteToOngoingTrailVfx =
            new Dictionary<NoteObject, GameObject>();
        dragNoteToOngoingVfx =
            new Dictionary<NoteObject, GameObject>();
    }

    private GameObject SpawnVfxAt(Vector3 position,
        SpriteSheetForVfx spriteSheet, bool loop = false)
    {
        GameObject vfx = Instantiate(vfxPrefab, transform);
        vfx.GetComponent<VFXDrawer>().Initialize(
            position, spriteSheet, loop);

        return vfx;
    }

    private GameObject SpawnVfxAt(NoteObject note,
        SpriteSheetForVfx spriteSheet, bool loop = false)
    {
        return SpawnVfxAt(note.transform.position,
            spriteSheet, loop);
    }

    public void SpawnVFXOnHit(NoteObject note, Judgement judgement)
    {
        // Judgement should never be Break here.
        if (judgement == Judgement.Miss) return;

        switch (note.note.type)
        {
            case NoteType.Basic:
            case NoteType.ChainHead:
            case NoteType.ChainNode:
            case NoteType.RepeatHead:
            case NoteType.Repeat:
                // Do nothing. VFX is spawned on resolve.
                break;
            case NoteType.Hold:
                holdNoteToOngoingHeadVfx.Add(note,
                    SpawnVfxAt(note,
                        GlobalResource.vfxSkin.holdOngoingHead,
                        loop: true));
                holdNoteToOngoingTrailVfx.Add(note,
                    SpawnVfxAt(note,
                        GlobalResource.vfxSkin.holdOngoingTrail,
                        loop: true));
                break;
            case NoteType.Drag:
                dragNoteToOngoingVfx.Add(note,
                    SpawnVfxAt(note,
                        GlobalResource.vfxSkin.dragOngoing,
                        loop: true));
                break;
            case NoteType.RepeatHeadHold:
                holdNoteToOngoingHeadVfx.Add(note,
                    SpawnVfxAt(note,
                        GlobalResource.vfxSkin.repeatHoldOngoingHead,
                        loop: true));
                holdNoteToOngoingTrailVfx.Add(note,
                    SpawnVfxAt(note,
                        GlobalResource.vfxSkin.repeatHoldOngoingTrail,
                        loop: true));
                break;
            case NoteType.RepeatHold:
                // Spawn the head VFX on repeat head.
                NoteObject repeatHead = note
                    .GetComponent<NoteAppearance>()
                    .GetRepeatHead().GetComponent<NoteObject>();
                holdNoteToOngoingHeadVfx.Add(repeatHead,
                    SpawnVfxAt(repeatHead,
                        GlobalResource.vfxSkin.repeatHoldOngoingHead,
                        loop: true));
                holdNoteToOngoingTrailVfx.Add(note,
                    SpawnVfxAt(note,
                        GlobalResource.vfxSkin.repeatHoldOngoingTrail,
                        loop: true));
                break;
        }
    }

    public void SpawnVFXOnResolve(NoteObject note,
        Judgement judgement)
    {
        // Even if judgement is Miss or Break, we still need
        // to despawn ongoing VFX, if any.

        switch (note.note.type)
        {
            case NoteType.Basic:
            case NoteType.ChainHead:
            case NoteType.ChainNode:
                switch (judgement)
                {
                    case Judgement.RainbowMax:
                    case Judgement.Max:
                        SpawnVfxAt(note,
                            GlobalResource.vfxSkin.basicMax);
                        break;
                    case Judgement.Cool:
                        SpawnVfxAt(note,
                            GlobalResource.vfxSkin.basicCool);
                        break;
                    case Judgement.Good:
                        SpawnVfxAt(note,
                            GlobalResource.vfxSkin.basicGood);
                        break;
                }
                break;
            case NoteType.Hold:
                if (holdNoteToOngoingHeadVfx.ContainsKey(note))
                {
                    Destroy(holdNoteToOngoingHeadVfx[note]);
                    holdNoteToOngoingHeadVfx.Remove(note);
                }
                if (holdNoteToOngoingTrailVfx.ContainsKey(note))
                {
                    Destroy(holdNoteToOngoingTrailVfx[note]);
                    holdNoteToOngoingTrailVfx.Remove(note);
                }
                if (judgement != Judgement.Miss &&
                    judgement != Judgement.Break)
                {
                    SpawnVfxAt(
                        note.GetComponent<NoteAppearance>()
                            .GetDurationTrailEndPosition(),
                        GlobalResource.vfxSkin.holdComplete);
                }
                break;
            case NoteType.Drag:
                if (dragNoteToOngoingVfx.ContainsKey(note))
                {
                    Destroy(dragNoteToOngoingVfx[note]);
                    dragNoteToOngoingVfx.Remove(note);
                }
                if (judgement != Judgement.Miss &&
                    judgement != Judgement.Break)
                {
                    SpawnVfxAt(
                        note.GetComponent<NoteAppearance>()
                            .GetCurveEndPosition(),
                        GlobalResource.vfxSkin.dragComplete);
                }
                break;
            case NoteType.RepeatHead:
                if (judgement == Judgement.Miss ||
                    judgement == Judgement.Break)
                {
                    break;
                }
                SpawnVfxAt(note,
                    GlobalResource.vfxSkin.repeatHead);
                break;
            case NoteType.Repeat:
                if (judgement == Judgement.Miss ||
                    judgement == Judgement.Break)
                {
                    break;
                }
                SpawnVfxAt(note,
                    GlobalResource.vfxSkin.repeatNote);
                SpawnVfxAt( 
                    note.GetComponent<NoteAppearance>()
                        .GetRepeatHead().GetComponent<NoteObject>(),
                    GlobalResource.vfxSkin.repeatHead);
                break;
            case NoteType.RepeatHeadHold:
                if (holdNoteToOngoingHeadVfx.ContainsKey(note))
                {
                    Destroy(holdNoteToOngoingHeadVfx[note]);
                    holdNoteToOngoingHeadVfx.Remove(note);
                }
                if (holdNoteToOngoingTrailVfx.ContainsKey(note))
                {
                    Destroy(holdNoteToOngoingTrailVfx[note]);
                    holdNoteToOngoingTrailVfx.Remove(note);
                }
                if (judgement != Judgement.Miss &&
                    judgement != Judgement.Break)
                {
                    SpawnVfxAt(
                        note.GetComponent<NoteAppearance>()
                            .GetDurationTrailEndPosition(),
                        GlobalResource.vfxSkin.repeatHoldComplete);
                }
                break;
            case NoteType.RepeatHold:
                NoteObject repeatHeadNote = note
                    .GetComponent<NoteAppearance>()
                    .GetRepeatHead().GetComponent<NoteObject>();
                if (holdNoteToOngoingHeadVfx
                    .ContainsKey(repeatHeadNote))
                {
                    Destroy(holdNoteToOngoingHeadVfx
                        [repeatHeadNote]);
                    holdNoteToOngoingHeadVfx.Remove(
                        repeatHeadNote);
                }
                if (holdNoteToOngoingTrailVfx.ContainsKey(note))
                {
                    Destroy(holdNoteToOngoingTrailVfx[note]);
                    holdNoteToOngoingTrailVfx.Remove(note);
                }
                if (judgement != Judgement.Miss &&
                    judgement != Judgement.Break)
                {
                    SpawnVfxAt(
                        note.GetComponent<NoteAppearance>()
                            .GetDurationTrailEndPosition(),
                        GlobalResource.vfxSkin.repeatHoldComplete);
                }
                break;
        }
    }

    private void Update()
    {
        foreach (KeyValuePair<NoteObject, GameObject> pair in
            holdNoteToOngoingTrailVfx)
        {
            pair.Value.transform.position =
                pair.Key.GetComponent<NoteAppearance>()
                .GetOngoingTrailEndPosition();
        }
        foreach (KeyValuePair<NoteObject, GameObject> pair in
            dragNoteToOngoingVfx)
        {
            pair.Value.transform.position =
                pair.Key.GetComponent<NoteAppearance>()
                .noteImage.transform.position;
        }
    }
}
