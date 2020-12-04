using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXSpawner : MonoBehaviour
{
    public GameObject explosionMax;
    public GameObject explosionCool;
    public GameObject explosionGood;
    public GameObject holdOngoingHead;
    public GameObject holdOngoingTrail;
    public GameObject holdComplete;
    public GameObject dragOngoing;
    public GameObject dragComplete;
    public GameObject repeatHead;
    public GameObject repeatNote;
    public GameObject repeatHoldOngoingHead;
    public GameObject repeatHoldOngoingTrail;

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

    private GameObject SpawnPrefabAt(GameObject prefab,
        Vector3 position)
    {
        float size = Scan.laneHeight * 3f;

        GameObject vfx = Instantiate(prefab, transform);
        RectTransform rect = vfx.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.position = position;

        return vfx;
    }

    private GameObject SpawnPrefabAt(
        GameObject prefab, NoteObject note)
    {
        return SpawnPrefabAt(prefab, note.transform.position);
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
                // Do nothing. VFX is spawned on resolve.
                break;
            case NoteType.Hold:
                holdNoteToOngoingHeadVfx.Add(note,
                    SpawnPrefabAt(holdOngoingHead, note));
                holdNoteToOngoingTrailVfx.Add(note,
                    SpawnPrefabAt(holdOngoingTrail, note));
                break;
            case NoteType.Drag:
                dragNoteToOngoingVfx.Add(note,
                    SpawnPrefabAt(dragOngoing, note));
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
                        SpawnPrefabAt(explosionMax, note);
                        break;
                    case Judgement.Cool:
                        SpawnPrefabAt(explosionCool, note);
                        break;
                    case Judgement.Good:
                        SpawnPrefabAt(explosionGood, note);
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
                    SpawnPrefabAt(holdComplete,
                        note.GetComponent<NoteAppearance>()
                        .GetDurationTrailEndPosition());
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
                    SpawnPrefabAt(dragComplete,
                        note.GetComponent<NoteAppearance>()
                        .GetCurveEndPosition());
                }
                break;
            case NoteType.RepeatHead:
                if (judgement == Judgement.Miss ||
                    judgement == Judgement.Break)
                {
                    break;
                }
                SpawnPrefabAt(repeatHead, note);
                break;
            case NoteType.Repeat:
                if (judgement == Judgement.Miss ||
                    judgement == Judgement.Break)
                {
                    break;
                }
                SpawnPrefabAt(repeatNote, note);
                SpawnPrefabAt(repeatHead, 
                    note.GetComponent<NoteAppearance>()
                    .GetRepeatHead().GetComponent<NoteObject>());
                break;
            case NoteType.RepeatHeadHold:
            case NoteType.RepeatHold:
                // TODO
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
