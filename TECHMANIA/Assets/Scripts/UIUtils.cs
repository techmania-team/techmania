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
            string name = Paths.RelativePath(EditorContext.trackFolder, allOptions[i]);
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

    public static void UseDefaultCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public static string FormatTime(float time,
        bool includeMillisecond)
    {
        bool negative = time < 0f;
        time = Mathf.Abs(time);
        int minute = Mathf.FloorToInt(time / 60f);
        time -= minute * 60f;
        int second = Mathf.FloorToInt(time);
        time -= second;
        int milliSecond = Mathf.FloorToInt(time * 1000f);

        string sign = negative ? "-" : "";
        if (includeMillisecond)
        {
            return $"{sign}{minute}:{second:D2}.{milliSecond:D3}";
        }
        else
        {
            return $"{sign}{minute}:{second:D2}";
        }
    }

    public static void InitializeDropdownWithLocalizedOptions(
        TMP_Dropdown dropdown, params string[] optionKeys)
    {
        dropdown.ClearOptions();
        foreach (string key in optionKeys)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(
                Locale.GetString(key)));
        }
    }

    public static void SetSpriteAndAspectRatio(Image image,
        Sprite sprite)
    {
        image.sprite = sprite;
        image.GetComponent<AspectRatioFitter>().aspectRatio =
            sprite.rect.width / sprite.rect.height;
    }

    #region Scroll into view
    // Scroll scrollRect so that rect is fully visible. If it
    // already is fully visible, do nothing.
    //
    // normalizedMargin: scroll so that there's this much margin
    // between rect's edge and scrollRect's edge.
    //
    // viewRectAsPoint: ignore the size of rect and view it as a point.
    public static void ScrollIntoView(
        RectTransform rect, ScrollRect scrollRect,
        float normalizedMargin, bool viewRectAsPoint,
        bool horizontal, bool vertical)
    {
        float minX, maxX, minY, maxY;
        GetMinMaxXY(rect, out minX, out maxX,
            out minY, out maxY);
        InnerScrollIntoView(
            minX, maxX, minY, maxY, scrollRect,
            normalizedMargin, viewRectAsPoint,
            horizontal, vertical);
    }

    public static void ScrollIntoView(
        Vector2 positionInWorld, ScrollRect scrollRect,
        float normalizedMargin, bool viewRectAsPoint,
        bool horizontal, bool vertical)
    {
        float minX = positionInWorld.x,
            maxX = positionInWorld.x,
            minY = positionInWorld.y,
            maxY = positionInWorld.y;
        InnerScrollIntoView(
            minX, maxX, minY, maxY, scrollRect,
            normalizedMargin, viewRectAsPoint,
            horizontal, vertical);
    }

    private static void InnerScrollIntoView(
        float minX, float maxX, float minY, float maxY,
        ScrollRect scrollRect,
        float normalizedMargin, bool viewRectAsPoint,
        bool horizontal, bool vertical)
    {
        RectTransform viewPort = scrollRect.viewport;
        RectTransform content = scrollRect.content;
        
        float viewMinX, viewMaxX, viewMinY, viewMaxY;
        float contentMinX, contentMaxX, contentMinY, contentMaxY;

        GetMinMaxXY(viewPort, out viewMinX, out viewMaxX,
            out viewMinY, out viewMaxY);
        GetMinMaxXY(content, out contentMinX, out contentMaxX,
            out contentMinY, out contentMaxY);

        if (viewRectAsPoint)
        {
            minX = (minX + maxX) * 0.5f;
            maxX = minX;
            minY = (minY + maxY) * 0.5f;
            maxY = minY;
        }

        float viewWidth = viewMaxX - viewMinX;
        float viewHeight = viewMaxY - viewMinY;
        float contentWidth = contentMaxX - contentMinX;
        float contentHeight = contentMaxY - contentMinY;

        if (horizontal)
        {
            float horizontalPosition =
                scrollRect.horizontalNormalizedPosition;
            if (maxX > viewMaxX)
            {
                horizontalPosition =
                    (maxX - contentMinX -
                        (1f - normalizedMargin) * viewWidth) /
                    (contentWidth - viewWidth);
            }
            else if (minX < viewMinX)
            {
                horizontalPosition =
                    (minX - contentMinX -
                        normalizedMargin * viewWidth) /
                    (contentWidth - viewWidth);
            }
            scrollRect.horizontalNormalizedPosition =
                Mathf.Clamp01(horizontalPosition);
        }
        if (vertical)
        {
            float verticalPosition =
                scrollRect.verticalNormalizedPosition;
            if (maxY > viewMaxY)
            {
                verticalPosition =
                    (maxY - contentMinY -
                        (1f - normalizedMargin) * viewHeight) /
                    (contentHeight - viewHeight);
            }
            else if (minY < viewMinY)
            {
                verticalPosition =
                    (minY - contentMinY -
                        normalizedMargin * viewHeight) /
                    (contentHeight - viewHeight);
            }
            scrollRect.verticalNormalizedPosition =
                Mathf.Clamp01(verticalPosition);
        }
    }

    private static void GetMinMaxXY(RectTransform r,
        out float minX, out float maxX,
        out float minY, out float maxY)
    {
        Vector3[] corners = new Vector3[4];
        r.GetWorldCorners(corners);
        minX = float.MaxValue;
        maxX = float.MinValue;
        minY = float.MaxValue;
        maxY = float.MinValue;
        foreach (Vector3 c in corners)
        {
            if (c.x < minX) minX = c.x;
            if (c.x > maxX) maxX = c.x;
            if (c.y < minY) minY = c.y;
            if (c.y > maxY) maxY = c.y;
        }
    }
    #endregion
}
