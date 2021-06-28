using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// The "main" part is defined in Track.cs.
public partial class Track
{
    private static int ComparePatternLevels(Track t1, Track t2,
        ControlScheme scheme, TrackFilter.SortOrder sortOrder)
    {
        List<int> levelsInT1 = new List<int>();
        List<int> levelsInT2 = new List<int>();
        foreach (Pattern p in t1.patterns)
        {
            if (p.patternMetadata.controlScheme != scheme) continue;
            levelsInT1.Add(p.patternMetadata.level);
        }
        foreach (Pattern p in t2.patterns)
        {
            if (p.patternMetadata.controlScheme != scheme) continue;
            levelsInT2.Add(p.patternMetadata.level);
        }
        if (levelsInT1.Count == 0 && levelsInT2.Count == 0)
        {
            return 0;
        }
        // Tracks without any pattern in the specified scheme
        // are sorted last.
        if (levelsInT1.Count == 0) return 1;
        if (levelsInT2.Count == 0) return -1;
        levelsInT1.Sort();
        levelsInT2.Sort();
        if (sortOrder == TrackFilter.SortOrder.Descending)
        {
            levelsInT1.Reverse();
            levelsInT2.Reverse();
        }

        int count = Math.Min(levelsInT1.Count, levelsInT2.Count);
        for (int i = 0; i < count; i++)
        {
            if (levelsInT1[i] == levelsInT2[i]) continue;

            int compare = levelsInT1[i] - levelsInT2[i];
            if (sortOrder == TrackFilter.SortOrder.Descending)
            {
                compare = -compare;
            }
            return compare;
        }

        // If the common patterns are all identical in level,
        // the track with more patterns is sorted first.
        return levelsInT2.Count - levelsInT1.Count;
    }

    public static int Compare(Track t1, Track t2,
        TrackFilter.SortBasis sortBasis,
        TrackFilter.SortOrder sortOrder)
    {
        int compare = 0;
        switch (sortBasis)
        {
            case TrackFilter.SortBasis.Title:
                compare = string.Compare(t1.trackMetadata.title,
                    t2.trackMetadata.title);
                break;
            case TrackFilter.SortBasis.Artist:
                compare = string.Compare(t1.trackMetadata.artist,
                    t2.trackMetadata.artist);
                break;
            case TrackFilter.SortBasis.Genre:
                compare = string.Compare(t1.trackMetadata.genre,
                    t2.trackMetadata.genre);
                break;
            case TrackFilter.SortBasis.TouchLevel:
                compare = ComparePatternLevels(t1, t2,
                    ControlScheme.Touch, sortOrder);
                break;
            case TrackFilter.SortBasis.KeysLevel:
                compare = ComparePatternLevels(t1, t2,
                    ControlScheme.Keys, sortOrder);
                break;
            case TrackFilter.SortBasis.KMLevel:
                compare = ComparePatternLevels(t1, t2,
                    ControlScheme.KM, sortOrder);
                break;
        }

        if (compare == 0)
        {
            // Use title as the secondary sort basis.
            sortBasis = TrackFilter.SortBasis.Title;
            compare = string.Compare(t1.trackMetadata.title,
                t2.trackMetadata.title);
        }

        switch (sortBasis)
        {
            case TrackFilter.SortBasis.Title:
            case TrackFilter.SortBasis.Artist:
            case TrackFilter.SortBasis.Genre:
                if (sortOrder == TrackFilter.SortOrder.Ascending)
                {
                    return compare;
                }
                else
                {
                    return -compare;
                }
            default:
                // Sort order is taken care of when comparing
                // patterns.
                return compare;
        }
    }
}