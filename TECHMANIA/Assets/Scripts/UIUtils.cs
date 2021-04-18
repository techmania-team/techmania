using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIUtils
{
    public static string NoneOptionInDropdowns()
    {
        return Locale.GetString("none_option_in_dropdowns");
    }

    #region ClampInputField
    public static void ClampInputField(TMP_InputField field,
        double min, double max)
    {
        double value = 0;
        bool clamped = false;
        if (!double.TryParse(field.text, out value))
        {
            clamped = true;
        }
        
        if (value < min)
        {
            clamped = true;
            value = min;
        }
        if (value > max)
        {
            clamped = true;
            value = max;
        }
        
        if (clamped)
        {
            field.text = value.ToString();
        }
    }

    public static void ClampInputField(TMP_InputField field,
        int min, int max)
    {
        int value = 0;
        bool clamped = false;
        if (!int.TryParse(field.text, out value))
        {
            clamped = true;
        }

        if (value < min)
        {
            clamped = true;
            value = min;
        }
        if (value > max)
        {
            clamped = true;
            value = max;
        }

        if (clamped)
        {
            field.text = value.ToString();
        }
    }
    #endregion

    #region UpdateMetadataInMemory
    // Update property to be newValue.
    // - If the new value is different from the old one,
    //   set madeChange to true.
    // - If madeChange was false before, also call 
    //   EditorContext.PrepareToModifyMetadata().
    public static void UpdateMetadataInMemory(
        ref string property, string newValue, ref bool madeChange)
    {
        if (property == newValue)
        {
            return;
        }
        if (!madeChange)
        {
            EditorContext.PrepareToModifyMetadata();
            madeChange = true;
        }
        property = newValue;
    }

    public static void UpdateMetadataInMemory(ref double property,
        string newValueString, ref bool madeChange)
    {
        double newValue = double.Parse(newValueString);
        if (property == newValue)
        {
            return;
        }
        if (!madeChange)
        {
            EditorContext.PrepareToModifyMetadata();
            madeChange = true;
        }
        property = newValue;
    }

    public static void UpdateMetadataInMemory(ref int property,
        string newValueString, ref bool madeChange)
    {
        int newValue = int.Parse(newValueString);
        if (property == newValue)
        {
            return;
        }
        if (!madeChange)
        {
            EditorContext.PrepareToModifyMetadata();
            madeChange = true;
        }
        property = newValue;
    }

    public static void UpdateMetadataInMemory(ref string property,
        TMP_Dropdown dropdown, ref bool madeChange)
    {
        string newValueString = dropdown.options[dropdown.value].text;
        if (newValueString == NoneOptionInDropdowns() &&
            dropdown.value == 0)
        {
            newValueString = "";
        }
        UpdateMetadataInMemory(ref property,
            newValueString, ref madeChange);
    }

    public static void UpdateMetadataInMemory(ref bool property,
        bool newValue, ref bool madeChange)
    {
        if (property == newValue) return;
        if (!madeChange)
        {
            EditorContext.PrepareToModifyMetadata();
            madeChange = true;
        }
        property = newValue;
    }
    #endregion

    #region MemoryToDropdown
    // Refreshes the option and value of dropdown so:
    // - The options are the file names (directory stripped)
    //   in allOptions
    // - The new value points to currentOption if it as among
    //   allOptions; "(None)" otherwise
    // - No events are fired
    public static void MemoryToDropdown(TMP_Dropdown dropdown,
        string currentOption, List<string> allOptions)
    {
        int value = 0;

        dropdown.options.Clear();
        dropdown.options.Add(new TMP_Dropdown.OptionData(
            NoneOptionInDropdowns()));
        for (int i = 0; i < allOptions.Count; i++)
        {
            string name = new FileInfo(allOptions[i]).Name;
            if (currentOption == name)
            {
                value = i + 1;
            }
            dropdown.options.Add(new TMP_Dropdown.OptionData(name));
        }

        dropdown.SetValueWithoutNotify(value);
        dropdown.RefreshShownValue();
    }

    // Updates the value of dropdown so the new value points to
    // currentOptions if it is among the dropdown's options.
    public static void MemoryToDropdown(TMP_Dropdown dropdown,
        string currentOption, int defaultValue = 0)
    {
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (dropdown.options[i].text == currentOption)
            {
                dropdown.SetValueWithoutNotify(i);
                dropdown.RefreshShownValue();
                return;
            }
        }
        dropdown.SetValueWithoutNotify(defaultValue);
        dropdown.RefreshShownValue();
    }
    #endregion

    public static string StripAudioExtension(string filename)
    {
        return filename.Replace(".wav", "").Replace(".ogg", "");
    }

    #region Rotate/Point Toward
    public static void RotateToward(RectTransform self,
        Vector2 selfPos, Vector2 targetPos)
    {
        float deltaY = targetPos.y - selfPos.y;
        float deltaX = targetPos.x - selfPos.x;
        if (Mathf.Abs(deltaY) < Mathf.Epsilon &&
            Mathf.Abs(deltaX) < Mathf.Epsilon)
        {
            // Do nothing.
            return;
        }

        float angleInRadian = Mathf.Atan2(deltaY, deltaX);
        self.localRotation = Quaternion.Euler(0f, 0f,
            angleInRadian * Mathf.Rad2Deg);
    }

    public static void RotateToward(RectTransform self,
        RectTransform target)
    {
        Vector2 selfPos = self.anchoredPosition;
        Vector2 targetPos = target.anchoredPosition;
        RotateToward(self, selfPos, targetPos);
    }

    public static void PointToward(RectTransform self,
        Vector2 selfPos, Vector2 targetPos)
    {
        float distance = Vector2.Distance(selfPos, targetPos);
        float angleInRadian = Mathf.Atan2(targetPos.y - selfPos.y,
            targetPos.x - selfPos.x);
        self.sizeDelta = new Vector2(distance, self.sizeDelta.y);
        self.localRotation = Quaternion.Euler(0f, 0f,
            angleInRadian * Mathf.Rad2Deg);
    }

    public static void PointToward(RectTransform self,
        RectTransform target)
    {
        Vector2 selfPos = self.anchoredPosition;
        Vector2 targetPos = target.anchoredPosition;
        PointToward(self, selfPos, targetPos);
    }
    #endregion
}
