using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeysoundSideSheet : MonoBehaviour
{
    public Transform keysoundButtonContainer;
    public GameObject keysoundButtonTemplate;
    public TextMeshProUGUI upcomingKeysoundDisplay;

    public static event UnityAction<List<string>> 
        selectedKeysoundsUpdated;

    // The following lists have the same length.
    private List<string> audioFilesNoFolder;
    private List<GameObject> keysoundButtons;
    private List<bool> selected;

    private const int kInvalid = -1;
    private int upcomingIndex;
    private int lastSelectedIndexWithoutShift;

    public void Initialize()
    {
        // Remove all keysound buttons, except for template.
        for (int i = 0; i < keysoundButtonContainer.childCount; i++)
        {
            GameObject o = keysoundButtonContainer.GetChild(i)
                .gameObject;
            if (o == keysoundButtonTemplate) continue;
            Destroy(o);
        }

        // Rebuild keysound list.
        audioFilesNoFolder = Paths.GetAllAudioFiles(
            EditorContext.trackFolder);
        keysoundButtons = new List<GameObject>();
        selected = new List<bool>();
        for (int i = 0; i < audioFilesNoFolder.Count; i++)
        {
            string file = audioFilesNoFolder[i];
            file = file.Remove(0, EditorContext.trackFolder.Length + 1);
            audioFilesNoFolder[i] = file;

            int iCopy = i;
            GameObject b = Instantiate(keysoundButtonTemplate, 
                keysoundButtonContainer);
            b.GetComponentInChildren<TextMeshProUGUI>().text = file;
            b.GetComponent<KeysoundButton>().clickHandler =
                () => OnKeysoundButtonClick(iCopy);
            b.SetActive(true);
            keysoundButtons.Add(b);
            selected.Add(false);
        }

        upcomingIndex = kInvalid;
        lastSelectedIndexWithoutShift = kInvalid;
        Refresh();
    }

    public void Refresh()
    {
        for (int i = 0; i < audioFilesNoFolder.Count; i++)
        {
            KeysoundButton keysoundButton = keysoundButtons[i]
                .GetComponent<KeysoundButton>();
            keysoundButton.UpdateSelected(selected[i]);
            keysoundButton.UpdateUpcoming(i == upcomingIndex);
        }

        string upcoming = UpcomingKeysound();
        if (upcoming == "")
        {
            upcoming = Locale.GetString(
                "pattern_panel_keysounds_upcoming_none");
        }
        upcomingKeysoundDisplay.text = upcoming;
    }

    public string UpcomingKeysound()
    {
        if (upcomingIndex == kInvalid)
        {
            return "";
        }
        return audioFilesNoFolder[upcomingIndex];
    }

    public void AdvanceUpcoming()
    {
        int startIndex = upcomingIndex + 1;
        if (upcomingIndex == kInvalid) startIndex = 0;
        for (int i = 0; i < selected.Count; i++)
        {
            int index = (startIndex + i) % selected.Count;
            if (selected[index])
            {
                upcomingIndex = index;
                Refresh();
                return;
            }
        }

        upcomingIndex = kInvalid;
        Refresh();
    }

    private void OnKeysoundButtonClick(int index)
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);
        bool ctrl = Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);
        if (shift)
        {
            if (lastSelectedIndexWithoutShift == kInvalid)
            {
                lastSelectedIndexWithoutShift = 0;
            }

            int first = Mathf.Min(lastSelectedIndexWithoutShift, 
                index);
            int last = Mathf.Max(lastSelectedIndexWithoutShift,
                index);
            if (ctrl)
            {
                // Add [first, last] to current selection.
                for (int i = first; i <= last; i++)
                {
                    selected[i] = true;
                }
            }
            else  // !ctrl
            {
                // Overwrite current selection with [first, last].
                for (int i = 0; i < selected.Count; i++)
                {
                    selected[i] = false;
                    if (i >= first && i <= last)
                    {
                        selected[i] = true;
                    }
                }
            }
        }
        else  // !shift
        {
            lastSelectedIndexWithoutShift = index;
            if (ctrl)
            {
                // Toggle [index] in current selection.
                selected[index] = !selected[index];
            }
            else  // !ctrl
            {
                int selectionSize = 0;
                int lastSelected = kInvalid;
                for (int i = 0; i < selected.Count; i++)
                {
                    if (selected[i])
                    {
                        selectionSize++;
                        lastSelected = i;
                    }
                }

                if (selectionSize > 1)
                {
                    // Overwrite current selection with [index].
                    for (int i = 0; i < selected.Count; i++)
                    {
                        selected[i] = i == index;
                    }
                }
                else if (selectionSize == 1 && lastSelected != index)
                {
                    // Overwrite current selection with [index].
                    selected[lastSelected] = false;
                    selected[index] = true;
                }
                else
                {
                    // Toggle [index] in current selection.
                    selected[index] = !selected[index];
                }
            }
        }

        // Reset upcoming index to the first keysound selected,
        // if any.
        upcomingIndex = kInvalid;
        for (int i = 0; i < selected.Count; i++)
        {
            if (selected[i])
            {
                upcomingIndex = i;
                break;
            }
        }

        // Fire event on the updated selection list.
        List<string> selectedList = new List<string>();
        for (int i = 0; i < selected.Count; i++)
        {
            if (selected[i])
            {
                selectedList.Add(audioFilesNoFolder[i]);
            }
        }
        selectedKeysoundsUpdated?.Invoke(selectedList);

        Refresh();
    }
}
