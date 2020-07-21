using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class UIUtils
{
    // Refreshes the option and value of dropdown so:
    // - The options are the file names (directory stripped) in newOptions
    // - The new value points to the same option as before the call if
    //   there is one; "(None)" otherwise
    // - No events are fired
    public static void RefreshFilenameDropdown(Dropdown dropdown, List<string> newOptions)
    {
        const string kNone = "(None)";
        string currentOption = dropdown.options[dropdown.value].text;
        int newValue = 0;

        dropdown.options.Clear();
        dropdown.options.Add(new Dropdown.OptionData(kNone));
        for (int i = 0; i < newOptions.Count; i++)
        {
            string name = new FileInfo(newOptions[i]).Name;
            if (currentOption == name)
            {
                newValue = i + 1;
            }
            dropdown.options.Add(new Dropdown.OptionData(name));
        }

        dropdown.SetValueWithoutNotify(newValue);
    }

    // Update property to be newValue.
    // If the new value is different from the old one,
    // set madeChange to true.
    // If madeChange was false before, also call Navigation.PrepareForChange().
    public static void UpdatePropertyInMemory(
        ref string property, string newValue, ref bool madeChange)
    {
        if (property == newValue)
        {
            return;
        }
        if (!madeChange)
        {
            Navigation.PrepareForChange();
            madeChange = true;
        }
        property = newValue;
    }

    public static void UpdatePropertyInMemoryFromDropdown(ref string property,
        Dropdown dropdown, ref bool madeChange)
    {
        UpdatePropertyInMemory(ref property,
            dropdown.options[dropdown.value].text, ref madeChange);
    }

    public static void UpdatePropertyInMemory(ref double property,
        string newValueString, ref bool madeChange)
    {
        double newValue = double.Parse(newValueString);
        if (property == newValue)
        {
            return;
        }
        if (!madeChange)
        {
            Navigation.PrepareForChange();
            madeChange = true;
        }
        property = newValue;
    }

    public static void UpdatePropertyInMemory(ref int property,
        string newValueString, ref bool madeChange)
    {
        int newValue = int.Parse(newValueString);
        if (property == newValue)
        {
            return;
        }
        if (!madeChange)
        {
            Navigation.PrepareForChange();
            madeChange = true;
        }
        property = newValue;
    }

    public static void MemoryToDropdown(string value, Dropdown dropdown)
    {
        int option = 0;
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (dropdown.options[i].text == value)
            {
                option = i;
                break;
            }
        }
        dropdown.SetValueWithoutNotify(option);
    }
}
