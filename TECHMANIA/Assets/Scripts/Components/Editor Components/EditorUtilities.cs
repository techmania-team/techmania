using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Common utilities between track setup panel and edit setlist panel.
public class EditorUtilities
{
    public static void ImportResource(string[] supportedFormats,
        string copyDestinationFolder,
        MessageDialog messageDialog,
        ConfirmDialog confirmDialog,
        Action completeCopyCallback)
    {
        SFB.ExtensionFilter[] extensionFilters =
            new SFB.ExtensionFilter[2];
        extensionFilters[0] = new SFB.ExtensionFilter(
            L10n.GetString(
                "import_resource_dialog_supported_formats"),
            supportedFormats);
        extensionFilters[1] = new SFB.ExtensionFilter(
            L10n.GetString(
                "import_resource_dialog_all_files"),
            new string[] { "*" });
        string[] sources = SFB.StandaloneFileBrowser.OpenFilePanel(
            L10n.GetString(
                "import_resource_dialog_title"),
            "",
            extensionFilters, multiselect: true);

        List<Tuple<string, string>> pairs = new List<Tuple<string, string>>();
        List<string> filesToBeOverwritten = new List<string>();
        foreach (string source in sources)
        {
            FileInfo fileInfo = new FileInfo(source);
            if (fileInfo.DirectoryName == copyDestinationFolder) 
                continue;
            string destination = Path.Combine(copyDestinationFolder,
                fileInfo.Name);

            if (File.Exists(destination))
            {
                filesToBeOverwritten.Add(fileInfo.Name);
            }
            pairs.Add(new Tuple<string, string>(source, destination));
        }

        if (filesToBeOverwritten.Count > 0)
        {
            string fileList = "";
            for (int i = 0; i < filesToBeOverwritten.Count; i++)
            {
                if (i == 10)
                {
                    fileList += "\n";
                    fileList += L10n.GetStringAndFormat(
                        "import_resource_overwrite_omitted_files",
                        filesToBeOverwritten.Count - 10);
                    break;
                }
                else
                {
                    if (fileList != "") fileList += "\n";
                    fileList += Paths.HidePlatformInternalPath(filesToBeOverwritten[i]);
                }
            }
            confirmDialog.Show(
                L10n.GetStringAndFormat(
                    "import_resource_overwrite_warning",
                    fileList),
                L10n.GetString(
                    "import_resource_overwrite_confirm"),
                L10n.GetString(
                    "import_resource_overwrite_cancel"),
                () =>
                {
                    StartCopy(pairs, messageDialog, 
                        completeCopyCallback);
                });
        }
        else
        {
            StartCopy(pairs, messageDialog, completeCopyCallback);
        }
    }

    private static void StartCopy(List<Tuple<string, string>> pairs,
        MessageDialog messageDialog,
        Action completeCopyCallback)
    {
        foreach (Tuple<string, string> pair in pairs)
        {
            try
            {
                File.Copy(pair.Item1, pair.Item2, overwrite: true);
            }
            catch (Exception e)
            {
                messageDialog.Show(
                    L10n.GetStringAndFormatIncludingPaths(
                        "import_resource_copy_file_error_format",
                        pair.Item1,
                        pair.Item2,
                        e.Message));
                break;
            }
        }

        completeCopyCallback();
    }

    public static string CondenseFileList(List<string> fullPaths,
        string folder)
    {
        string str = "";
        foreach (string file in fullPaths)
        {
            str += Paths.RelativePath(folder, file) + "\n";
        }
        return str.TrimEnd('\n');
    }
}
