using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXSpawner : MonoBehaviour
{
    public GameObject vfxPrefab;

    // Non-looping VFX will destroy themselves when done. The looping
    // ones do not, so we track them in these dictionaries and destroy
    // them when the corresponding note resolves.
    private Dictionary<NoteObject, List<GameObject>>
        holdNoteToOngoingHeadVfx;
    private Dictionary<NoteObject, List<GameObject>> 
        holdNoteToOngoingTrailVfx;
    private Dictionary<NoteObject, List<GameObject>>
        dragNoteToOngoingVfx;

    private void Start()
    {
        holdNoteToOngoingHeadVfx =
            new Dictionary<NoteObject, List<GameObject>>();
        holdNoteToOngoingTrailVfx =
            new Dictionary<NoteObject, List<GameObject>>();
        dragNoteToOngoingVfx =
            new Dictionary<NoteObject, List<GameObject>>();
    }

    private List<GameObject> SpawnVfxAt(Vector3 position,
        List<SpriteSheet> spriteSheetLayers, bool loop = false)
    {
        List<GameObject> layers = new List<GameObject>();
        foreach (SpriteSheet layer in spriteSheetLayers)
        {
            GameObject vfx = Instantiate(vfxPrefab, transform);
            vfx.GetComponent<VFXDrawer>().Initialize(
                position, layer, loop);
            layers.Add(vfx);
        }
        return layers;
    }

    private List<GameObject> SpawnVfxAt(NoteObject note,
        List<SpriteSheet> spriteSheetLayers, bool loop = false)
    {
        return SpawnVfxAt(note.transform.position,
            spriteSheetLayers, loop);
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
                    .GetComponent<RepeatNoteAppearanceBase>()
                    .repeatHead.GetComponent<NoteObject>();
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
                    holdNoteToOngoingHeadVfx[note].ForEach(
                        o => Destroy(o));
                    holdNoteToOngoingHeadVfx.Remove(note);
                }
                if (holdNoteToOngoingTrailVfx.ContainsKey(note))
                {
                    holdNoteToOngoingTrailVfx[note].ForEach(
                        o => Destroy(o));
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
                    dragNoteToOngoingVfx[note].ForEach(
                        o => Destroy(o));
                    dragNoteToOngoingVfx.Remove(note);
                }
                if (judgement != Judgement.Miss &&
                    judgement != Judgement.Break)
                {
                    SpawnVfxAt(
                        note.GetComponent<DragNoteAppearance>()
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
                    note.GetComponent<RepeatNoteAppearanceBase>()
                        .repeatHead.GetComponent<NoteObject>(),
                    GlobalResource.vfxSkin.repeatHead);
                break;
            case NoteType.RepeatHeadHold:
                if (holdNoteToOngoingHeadVfx.ContainsKey(note))
                {
                    holdNoteToOngoingHeadVfx[note].ForEach(
                        o => Destroy(o));
                    holdNoteToOngoingHeadVfx.Remove(note);
                }
                if (holdNoteToOngoingTrailVfx.ContainsKey(note))
                {
                    holdNoteToOngoingTrailVfx[note].ForEach(
                        o => Destroy(o));
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
                    .GetComponent<RepeatNoteAppearanceBase>()
                    .repeatHead.GetComponent<NoteObject>();
                if (holdNoteToOngoingHeadVfx
                    .ContainsKey(repeatHeadNote))
                {
                    holdNoteToOngoingHeadVfx[repeatHeadNote].ForEach(
                        o => Destroy(o));
                    holdNoteToOngoingHeadVfx.Remove(
                        repeatHeadNote);
                }
                if (holdNoteToOngoingTrailVfx.ContainsKey(note))
                {
                    holdNoteToOngoingTrailVfx[note].ForEach(
                        o => Destroy(o));
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
        foreach (KeyValuePair<NoteObject, List<GameObject>> pair in
            holdNoteToOngoingTrailVfx)
        {
            foreach (GameObject o in pair.Value)
            {
                o.transform.position =
                    pair.Key.GetComponent<NoteAppearance>()
                    .GetOngoingTrailEndPosition();
            }
        }
        foreach (KeyValuePair<NoteObject, List<GameObject>> pair in
            dragNoteToOngoingVfx)
        {
            foreach (GameObject o in pair.Value)
            {
                o.transform.position =
                    pair.Key.GetComponent<NoteAppearance>()
                    .noteImage.transform.position;
            }
        }
    }

    public void RemoveAll()
    {
        if (holdNoteToOngoingHeadVfx != null)
        {
            foreach (List<GameObject> list in
                holdNoteToOngoingHeadVfx.Values)
            {
                foreach (GameObject o in list) Destroy(o);
            }
            holdNoteToOngoingHeadVfx.Clear();
        }
        if (holdNoteToOngoingTrailVfx != null)
        {
            foreach (List<GameObject> list in
                holdNoteToOngoingTrailVfx.Values)
            {
                foreach (GameObject o in list) Destroy(o);
            }
            holdNoteToOngoingTrailVfx.Clear();
        }
        if (dragNoteToOngoingVfx != null)
        {
            foreach (List<GameObject> list in
                dragNoteToOngoingVfx.Values)
            {
                foreach (GameObject o in list) Destroy(o);
            }
            dragNoteToOngoingVfx.Clear();
        }
    }
}
