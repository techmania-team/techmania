using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public GameObject vfxPrefab;

    // Non-looping VFX will destroy themselves when done. The looping
    // ones do not, so we track them in these dictionaries and destroy
    // them when the corresponding note resolves.
    private Dictionary<NoteElements, List<GameObject>>
        holdNoteToOngoingHeadVfx;
    private Dictionary<NoteElements, List<GameObject>>
        holdNoteToOngoingTrailVfx;
    private Dictionary<NoteElements, List<GameObject>>
        dragNoteToOngoingVfx;

    private float laneHeight;

    // Start is called before the first frame update
    void Start()
    {
        holdNoteToOngoingHeadVfx =
            new Dictionary<NoteElements, List<GameObject>>();
        holdNoteToOngoingTrailVfx =
            new Dictionary<NoteElements, List<GameObject>>();
        dragNoteToOngoingVfx =
            new Dictionary<NoteElements, List<GameObject>>();
    }

    public void Prepare(float laneHeight)
    {
        ResetSize(laneHeight);
    }

    public void ResetSize(float laneHeight)
    {
        this.laneHeight = laneHeight;
        // Reset size for VFXDrawer?
    }

    private List<GameObject> SpawnVfxAt(Vector3 position,
        List<SpriteSheet> spriteSheetLayers, bool loop = false)
    {
        List<GameObject> layers = new List<GameObject>();
        foreach (SpriteSheet layer in spriteSheetLayers)
        {
            GameObject vfx = Instantiate(vfxPrefab, transform);
            vfx.GetComponent<VFXDrawer>().Initialize(
                position, layer, laneHeight, loop);
            layers.Add(vfx);
        }
        return layers;
    }

    private List<GameObject> SpawnVfxAt(NoteElements elements,
        List<SpriteSheet> spriteSheetLayers, bool loop = false)
    {
        Vector2 screenPoint = elements.templateContainer
            .worldBound.center;
        // Reverse Y coordinate when passing a position to Canvas.
        screenPoint.y = Screen.height - screenPoint.y;
        return SpawnVfxAt(screenPoint,
            spriteSheetLayers, loop);
    }

    public void SpawnOngoingVFX(NoteElements elements,
        Judgement judgement)
    {
        if (judgement == Judgement.Miss ||
            judgement == Judgement.Break)
        {
            return;
        }

        switch (elements.note.type)
        {
            case NoteType.Basic:
            case NoteType.ChainHead:
            case NoteType.ChainNode:
            case NoteType.RepeatHead:
            case NoteType.Repeat:
                // Do nothing. VFX is spawned on resolve.
                break;
            case NoteType.Hold:
                holdNoteToOngoingHeadVfx.Add(elements,
                    SpawnVfxAt(elements,
                        GlobalResource.vfxSkin.holdOngoingHead,
                        loop: true));
                holdNoteToOngoingTrailVfx.Add(elements,
                    SpawnVfxAt(elements,
                        GlobalResource.vfxSkin.holdOngoingTrail,
                        loop: true));
                break;
            case NoteType.Drag:
                dragNoteToOngoingVfx.Add(elements,
                    SpawnVfxAt(elements,
                        GlobalResource.vfxSkin.dragOngoing,
                        loop: true));
                break;
            case NoteType.RepeatHeadHold:
                holdNoteToOngoingHeadVfx.Add(elements,
                    SpawnVfxAt(elements,
                        GlobalResource.vfxSkin.repeatHoldOngoingHead,
                        loop: true));
                holdNoteToOngoingTrailVfx.Add(elements,
                    SpawnVfxAt(elements,
                        GlobalResource.vfxSkin.repeatHoldOngoingTrail,
                        loop: true));
                break;
            case NoteType.RepeatHold:
                // TODO: Spawn the head VFX on repeat head.
                //NoteObject repeatHead = elements
                //    .GetComponent<RepeatNoteAppearanceBase>()
                //    .repeatHead.GetComponent<NoteObject>();
                //holdNoteToOngoingHeadVfx.Add(repeatHead,
                //    SpawnVfxAt(repeatHead,
                //        GlobalResource.vfxSkin.repeatHoldOngoingHead,
                //        loop: true));
                //holdNoteToOngoingTrailVfx.Add(elements,
                //    SpawnVfxAt(elements,
                //        GlobalResource.vfxSkin.repeatHoldOngoingTrail,
                //        loop: true));
                break;
        }
    }

    public void SpawnResolvingVFX(NoteElements elements,
        Judgement judgement)
    {
        // Even if judgement is Miss or Break, we still need
        // to despawn ongoing VFX, if any.

        System.Action<Dictionary<NoteElements, List<GameObject>>,
            NoteElements> despawnVfx =
            (Dictionary<NoteElements, List<GameObject>> dictionary,
            NoteElements elements) =>
        {
            if (!dictionary.ContainsKey(elements)) return;
            dictionary[elements].ForEach(o => Destroy(o));
            dictionary.Remove(elements);
        };

        bool missOrBreak = judgement == Judgement.Miss ||
            judgement == Judgement.Break;

        switch (elements.note.type)
        {
            case NoteType.Basic:
            case NoteType.ChainHead:
            case NoteType.ChainNode:
                switch (judgement)
                {
                    case Judgement.RainbowMax:
                    case Judgement.Max:
                        SpawnVfxAt(elements,
                            GlobalResource.vfxSkin.basicMax);
                        break;
                    case Judgement.Cool:
                        SpawnVfxAt(elements,
                            GlobalResource.vfxSkin.basicCool);
                        break;
                    case Judgement.Good:
                        SpawnVfxAt(elements,
                            GlobalResource.vfxSkin.basicGood);
                        break;
                }
                break;
            case NoteType.Hold:
                despawnVfx(holdNoteToOngoingHeadVfx, elements);
                despawnVfx(holdNoteToOngoingTrailVfx, elements);
                if (!missOrBreak)
                {
                    // TODO: spawn holdComplete
                    //SpawnVfxAt(
                    //    note.GetComponent<NoteAppearance>()
                    //        .GetDurationTrailEndPosition(),
                    //    GlobalResource.vfxSkin.holdComplete);
                }
                break;
            case NoteType.Drag:
                despawnVfx(dragNoteToOngoingVfx, elements);
                if (!missOrBreak)
                {
                    // TODO: spawn dragComplete
                    //SpawnVfxAt(
                    //    note.GetComponent<DragNoteAppearance>()
                    //        .GetCurveEndPosition(),
                    //    GlobalResource.vfxSkin.dragComplete);
                }
                break;
            case NoteType.RepeatHead:
                if (missOrBreak) break;
                SpawnVfxAt(elements,
                    GlobalResource.vfxSkin.repeatHead);
                break;
            case NoteType.Repeat:
                if (missOrBreak) break;
                SpawnVfxAt(elements,
                    GlobalResource.vfxSkin.repeatNote);
                // TODO: spawn repeatHead
                //SpawnVfxAt(
                //    note.GetComponent<RepeatNoteAppearanceBase>()
                //        .repeatHead.GetComponent<NoteObject>(),
                //    GlobalResource.vfxSkin.repeatHead);
                break;
            case NoteType.RepeatHeadHold:
                despawnVfx(holdNoteToOngoingHeadVfx, elements);
                despawnVfx(holdNoteToOngoingTrailVfx, elements);
                if (!missOrBreak)
                {
                    // TODO: spawn repeatHoldComplete
                    //SpawnVfxAt(
                    //    note.GetComponent<NoteAppearance>()
                    //        .GetDurationTrailEndPosition(),
                    //    GlobalResource.vfxSkin.repeatHoldComplete);
                }
                break;
            case NoteType.RepeatHold:
                // TODO: despawn VFX on repeat head
                //NoteObject repeatHeadNote = note
                //    .GetComponent<RepeatNoteAppearanceBase>()
                //    .repeatHead.GetComponent<NoteObject>();
                //if (holdNoteToOngoingHeadVfx
                //    .ContainsKey(repeatHeadNote))
                //{
                //    holdNoteToOngoingHeadVfx[repeatHeadNote].ForEach(
                //        o => Destroy(o));
                //    holdNoteToOngoingHeadVfx.Remove(
                //        repeatHeadNote);
                //}
                despawnVfx(holdNoteToOngoingTrailVfx, elements);
                if (!missOrBreak)
                {
                    // TODO: spawn repeatHoldComplete
                    //SpawnVfxAt(
                    //    note.GetComponent<NoteAppearance>()
                    //        .GetDurationTrailEndPosition(),
                    //    GlobalResource.vfxSkin.repeatHoldComplete);
                }
                break;
        }
    }

    private void Update()
    {
        foreach (KeyValuePair<NoteElements, List<GameObject>> pair in
            holdNoteToOngoingTrailVfx)
        {
            foreach (GameObject o in pair.Value)
            {
                // TODO: move towards trail end
                //o.transform.position =
                //    pair.Key.GetComponent<NoteAppearance>()
                //    .GetOngoingTrailEndPosition();
            }
        }
        foreach (KeyValuePair<NoteElements, List<GameObject>> pair in
            dragNoteToOngoingVfx)
        {
            foreach (GameObject o in pair.Value)
            {
                // TODO: move towards note image
                //o.transform.position =
                //    pair.Key.GetComponent<NoteAppearance>()
                //    .noteImage.transform.position;
            }
        }
    }

    public void Dispose()
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
