using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

// String table format
//
// First row: header
// Second row: language names
// Subsequent rows:
//   Field 0 - key
//   Field 1 - comment
//   Same field as the locale in header - string content

public class Locale
{
    public static event UnityAction LocaleChanged;
    public static Dictionary<string, string> localeToLanguageName;
    private static Dictionary<string, string> currentStringTable;
    private static Dictionary<string, string> fallbackStringTable;

    public static void Load(TextAsset stringTable, string locale)
    {
        NReco.Csv.CsvReader csvReader = new NReco.Csv.CsvReader(
            new StringReader(stringTable.text));
        List<string> locales = new List<string>();
        int localeIndex = 0;
        int fallbackLocaleIndex = 0;

        // Header
        csvReader.Read();
        for (int i = 2; i < csvReader.FieldsCount; i++)
        {
            locales.Add(csvReader[i]);
            if (csvReader[i] == locale) localeIndex = i;
            if (csvReader[i] == "en") fallbackLocaleIndex = i;
        }

        // Language names
        csvReader.Read();
        localeToLanguageName = new Dictionary<string, string>();
        for (int i = 2; i < csvReader.FieldsCount; i++)
        {
            localeToLanguageName.Add(locales[i - 2],
                csvReader[i]);
        }

        if (localeIndex == 0)
        {
            throw new System.Exception("Locale not found: " + locale);
        }

        // String table
        currentStringTable = new Dictionary<string, string>();
        fallbackStringTable = new Dictionary<string, string>();
        while (csvReader.Read())
        {
            currentStringTable.Add(csvReader[0],
                csvReader[localeIndex]);
            fallbackStringTable.Add(csvReader[0],
                csvReader[fallbackLocaleIndex]);
        }

        LocaleChanged?.Invoke();
    }

    public static string GetString(string key)
    {
        if (currentStringTable != null &&
            currentStringTable.ContainsKey(key))
        {
            return currentStringTable[key];
        }
        if (fallbackStringTable == null) return "";
        return fallbackStringTable[key];
    }
}
