using System.Collections;
using System.Collections.Generic;
using ThemeApi;
using UnityEngine;
using UnityEngine.UIElements;

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

    // To be passed to
    // HoldTrailAndExtensions.GetOngoingTrailEndPosition.
    private GameTimer timer;
    // To query scanline position when placing ongoing VFX.
    private GameLayout layout;

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

    public void Prepare(float laneHeight, GameTimer timer,
        GameLayout layout)
    {
        ResetSize(laneHeight);
        this.timer = timer;
        this.layout = layout;
    }

    public void ResetSize(float laneHeight)
    {
        this.laneHeight = laneHeight;
        // Reset size for VFXDrawer?
    }

    private List<GameObject> SpawnVfxAt(Vector2 viewportPoint,
        List<SpriteSheet> spriteSheetLayers, bool loop = false)
    {
        List<GameObject> layers = new List<GameObject>();
        foreach (SpriteSheet layer in spriteSheetLayers)
        {
            GameObject vfx = Instantiate(vfxPrefab, transform);
            vfx.GetComponent<VFXDrawer>().Initialize(
                viewportPoint, layer, laneHeight, loop);
            layers.Add(vfx);
        }
        return layers;
    }

    private List<GameObject> SpawnVfxAt(
        UnityEngine.UIElements.VisualElement element,
        List<SpriteSheet> spriteSheetLayers, bool loop = false)
    {
        return SpawnVfxAt(
            VisualElementTransform
                .ElementCenterToViewportSpace(element, log:true),
            spriteSheetLayers, loop);
    }

    private List<GameObject> SpawnVfxAt(NoteElements elements,
        List<SpriteSheet> spriteSheetLayers, bool loop = false)
    {
        return SpawnVfxAt(elements.templateContainer, 
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
                // Spawn the head VFX on repeat head.
                NoteElements head = (elements as RepeatNoteElementsBase)
                    .head;
                holdNoteToOngoingHeadVfx.Add(head,
                    SpawnVfxAt(head,
                        GlobalResource.vfxSkin.repeatHoldOngoingHead,
                        loop: true));
                holdNoteToOngoingTrailVfx.Add(elements,
                    SpawnVfxAt(elements,
                        GlobalResource.vfxSkin.repeatHoldOngoingTrail,
                        loop: true));
                break;
        }
    }

    public void SpawnResolvedVFX(NoteElements elements,
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
                    SpawnVfxAt(
                        elements.holdTrailAndExtensions
                        .GetDurationTrailEndPosition(),
                        GlobalResource.vfxSkin.holdComplete);
                }
                break;
            case NoteType.Drag:
                despawnVfx(dragNoteToOngoingVfx, elements);
                if (!missOrBreak)
                {
                    SpawnVfxAt(
                        (elements as DragNoteElements).curveEnd,
                        GlobalResource.vfxSkin.dragComplete);
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
                SpawnVfxAt((elements as RepeatNoteElementsBase).head,
                    GlobalResource.vfxSkin.repeatHead);
                break;
            case NoteType.RepeatHeadHold:
                despawnVfx(holdNoteToOngoingHeadVfx, elements);
                despawnVfx(holdNoteToOngoingTrailVfx, elements);
                if (!missOrBreak)
                {
                    SpawnVfxAt(
                        elements.holdTrailAndExtensions
                        .GetDurationTrailEndPosition(),
                        GlobalResource.vfxSkin.repeatHoldComplete);
                }
                break;
            case NoteType.RepeatHold:
                // Despawn VFX on repeat head.
                NoteElements head = (elements as RepeatNoteElementsBase)
                    .head;
                despawnVfx(holdNoteToOngoingHeadVfx, head);
                despawnVfx(holdNoteToOngoingTrailVfx, elements);
                if (!missOrBreak)
                {
                    SpawnVfxAt(
                        elements.holdTrailAndExtensions
                        .GetDurationTrailEndPosition(),
                        GlobalResource.vfxSkin.repeatHoldComplete);
                }
                break;
        }
    }

    // For use in skin preview and calibration preview.
    // - Only supports note types basic, chain head and chain node
    // - Only supports judgements rainbow MAX, MAX, COOL and GOOD
    public void SpawnOneShotVFX(VisualElement element,
        Judgement judgement)
    {
        switch (judgement)
        {
            case Judgement.RainbowMax:
            case Judgement.Max:
                SpawnVfxAt(element,
                    GlobalResource.vfxSkin.basicMax);
                break;
            case Judgement.Cool:
                SpawnVfxAt(element,
                    GlobalResource.vfxSkin.basicCool);
                break;
            case Judgement.Good:
                SpawnVfxAt(element,
                    GlobalResource.vfxSkin.basicGood);
                break;
        }
    }

    private void Update()
    {
        if (holdNoteToOngoingTrailVfx.Count == 0 &&
            dragNoteToOngoingVfx.Count == 0) return;
        float viewportXOfScanline = layout.GetViewportXOfScanline(
            timer.intScan);

        foreach (KeyValuePair<NoteElements, List<GameObject>> pair in
            holdNoteToOngoingTrailVfx)
        {
            Vector2 ongoingTrailEndPosition =
                VisualElementTransform
                .ElementCenterToViewportSpace(
                    pair.Key.holdTrailAndExtensions
                    .GetOngoingTrailEndPosition(timer.intScan));
            foreach (GameObject o in pair.Value)
            {
                o.GetComponent<VFXDrawer>().SetViewportPoint(
                    new Vector2(viewportXOfScanline,
                    ongoingTrailEndPosition.y));
            }
        }
        foreach (KeyValuePair<NoteElements, List<GameObject>> pair in
            dragNoteToOngoingVfx)
        {
            foreach (GameObject o in pair.Value)
            {
                o.GetComponent<VFXDrawer>().SetViewportPoint(
                    VisualElementTransform
                    .ElementCenterToViewportSpace(
                        pair.Key.noteImage));
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

    public void JumpToScan()
    {
        Dispose();
    }
}
