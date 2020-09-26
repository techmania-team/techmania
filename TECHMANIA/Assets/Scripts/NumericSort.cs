using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class NumericSort
{
    // Based on https://stackoverflow.com/a/22323356
    public static IEnumerable<string> Sort(IEnumerable<string> items, StringComparer stringComparer = null)
    {
        var regex = new Regex(@"\d+", RegexOptions.Compiled);

        int maxDigits = items.SelectMany(
                i => regex.Matches(i)
                        .Cast<Match>()
                        .Select(digitChunk => (int?)digitChunk.Value.Length)
            )
            .Max() ?? 0;

        return items.OrderBy(i => regex.Replace(
            i, match => match.Value.PadLeft(maxDigits, '0')), stringComparer ?? StringComparer.CurrentCulture);
    }
}
