using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PatternTimingTab : MonoBehaviour
{
    public TMP_InputField firstBeatOffset;
    public TMP_InputField initialBpm;
    public TMP_InputField bps;
    public Toggle metronome;

    public static event UnityAction TimingUpdated;

    private void OnEnable()
    {
        MemoryToUI();

        EditorContext.UndoInvoked += OnUndoRedo;
        EditorContext.RedoInvoked += OnUndoRedo;
    }

    private void OnDisable()
    {
        Options.instance.SaveToFile(Paths.GetOptionsFilePath());

        EditorContext.UndoInvoked -= OnUndoRedo;
        EditorContext.RedoInvoked -= OnUndoRedo;
    }

    private void OnUndoRedo(EditTransaction transaction)
    {
        foreach (EditOperation op in transaction.ops)
        {
            if (op.type == EditOperation.Type.Metadata)
            {
                MemoryToUI();
                return;
            }
        }
    }

    public void MemoryToUI()
    {
        PatternMetadata m = EditorContext.Pattern.patternMetadata;

        firstBeatOffset.SetTextWithoutNotify(
            m.firstBeatOffset.ToString());
        initialBpm.SetTextWithoutNotify(m.initBpm.ToString());
        bps.SetTextWithoutNotify(m.bps.ToString());

        foreach (TMP_InputField field in new List<TMP_InputField>()
        {
            firstBeatOffset,
            initialBpm,
            bps
        })
        {
            field.GetComponent<MaterialTextField>().RefreshMiniLabel();
        }

        metronome.SetIsOnWithoutNotify(
            Options.instance.editorOptions.metronome);
    }

    public void UIToMemory()
    {
        PatternMetadata m = EditorContext.Pattern.patternMetadata;
        bool madeChange = false;

        UIUtils.UpdateMetadataInMemory(
            ref m.firstBeatOffset, firstBeatOffset.text,
            ref madeChange);
        UIUtils.ClampInputField(initialBpm,
            Pattern.minBpm, float.MaxValue);
        UIUtils.UpdateMetadataInMemory(
            ref m.initBpm, initialBpm.text, ref madeChange);
        UIUtils.ClampInputField(bps, Pattern.minBps, int.MaxValue);
        UIUtils.UpdateMetadataInMemory(
            ref m.bps, bps.text, ref madeChange);
        Options.instance.editorOptions.metronome =
            metronome.isOn;

        if (madeChange)
        {
            TimingUpdated?.Invoke();
        }
    }
}
