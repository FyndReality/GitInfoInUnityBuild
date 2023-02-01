using System;
using UnityEngine;
using System.Globalization;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEditor.Callbacks;
#endif

namespace FyndReality.Util.Git
{
#if UNITY_EDITOR

/// <summary>
/// Generate temporary files in streaming assets and a resource folder.
/// If build fails, these temporary files are not deleted since Unity does not have an PostProcess event for failure.
/// Temporary files will be updated and deleted on next successful build.
/// 
/// It is therefore recommended to include the following lines in gitignore:
/// <code>
/// Assets/TempGitInfoInBuild.meta
/// Assets/TempGitInfoInBuild/
/// Assets/StreamingAssets/GitInfoInBuild.meta
/// Assets/StreamingAssets/GitInfoInBuild/
/// </code>
/// </summary>
internal class IncludeGitInfoPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Git.GenerateGitInfoFiles();
        AssetDatabase.Refresh();
    }

    //Is only triggered on successful build
    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        Git.DeleteBuildFiles();
        AssetDatabase.Refresh();
    }
}
#endif

/// <summary>
/// Fetches git info on build, and stores it in resources to be available during runtime.
/// Text files are also placed in streaming assets to be available outside of unity.
/// When used in editor it always fetches the latest state of git repo.
/// </summary>
public class Git : MonoBehaviour
{
    // File structure in GitInfoInBuild folders
    private const string fileNameHash = "gitHash";
    private const string fileNameStatus = "gitStatus";
    private const string fileNameBuildTime = "buildTime";

    private const string gitInfoFolderName = "GitInfoInBuild/";
    private const string gitInfoHashPath = gitInfoFolderName + fileNameHash;
    private const string gitInfoStatusPath = gitInfoFolderName + fileNameStatus;
    private const string gitInfoBuildTimePath = gitInfoFolderName + fileNameBuildTime;
    private const string fileExt = ".txt";

    //We only load files from resources once during runtime. (Editor does not use caching) 
    private static bool hasInitialized = false;
    private static string buildHash;
    private static string buildStatus;
    private static string buildTime;

    /// <summary>
    /// The full hash from commit of this build.
    /// </summary>
    public static string Hash
    {
        get
        {
            if (!Application.isPlaying || Application.isEditor)
                return GitCommand.Run(@"rev-parse HEAD");

            InitializeGitInfo();
            return buildHash;
        }
    }

    /// <summary>
    /// The first 8 characters of hash from commit of this build.
    /// </summary>
    public static string HashShort => TruncateString(Hash, 8);

    /// <summary>
    /// A list of all uncommitted or untracked (added) files.
    /// Empty string if git working tree is clean.
    /// </summary>
    public static string Status
    {
        get
        {
            if (!Application.isPlaying || Application.isEditor)
                return GitCommand.Run(@"status --porcelain");

            InitializeGitInfo();
            return buildStatus;
        }
    }

    /// <summary>
    /// Returns the build time as UTC "yyyy/MM/dd HH:mm:ss".
    /// (Not a git command, only the time at build).
    /// Returns current time if called from editor.
    /// </summary>
    public static string BuildTime
    {
        get
        {
            if (!Application.isPlaying || Application.isEditor)
                return DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss", new CultureInfo("en-GB"));

            InitializeGitInfo();
            return buildTime;
        }
    }

    /// <summary>
    /// Only trigger Resources.Load the first time a git info field is requested.
    /// </summary>
    private static void InitializeGitInfo()
    {
        if (hasInitialized)
            return;

        TextAsset textAssetHash = Resources.Load(gitInfoHashPath) as TextAsset;
        if (textAssetHash == null)
        {
            Debug.LogError("Could not get a build git hash");
            buildHash = "0";
        }
        else if (string.IsNullOrEmpty(textAssetHash.text))
        {
            Debug.LogError("Build git hash text is empty string");
            buildHash = "";
        }
        else
        {
            buildHash = textAssetHash.text;
        }

        TextAsset textAssetStatus = Resources.Load(gitInfoStatusPath) as TextAsset;
        if (textAssetStatus == null)
        {
            Debug.LogError("Could not get a build git status");
            buildStatus = "Missing build git status";
        }
        else if (string.IsNullOrEmpty(textAssetStatus.text))
        {
            buildStatus = "";
        }
        else
        {
            buildStatus = textAssetStatus.text;
        }

        TextAsset textAssetBuildTime = Resources.Load(gitInfoBuildTimePath) as TextAsset;
        if (textAssetBuildTime == null)
        {
            Debug.LogError("Could not get a build time");
            buildTime = "Missing build time";
        }
        else if (string.IsNullOrEmpty(textAssetBuildTime.text))
        {
            Debug.LogError("BuildTime is empty");
            buildTime = "";
        }
        else
        {
            buildTime = textAssetBuildTime.text;
        }

        hasInitialized = true;
    }

    // Folder Path that will include resources folder with git info.
    // Deleted after build, (is not deleted if build fails).
    private static readonly string resourcesTempFolderToDelete =
        Application.dataPath + "/TempGitInfoInBuild/";

    // Folder Path for git info in streaming assets, but it is not needed to display git info during runtime.
    // This makes git info files accessable without starting the program. 
    // Deleted after build. (Is not deleted if build fails).
    private static readonly string streamingAssetsTempFolderToDelete =
        Application.dataPath + "/StreamingAssets/";

    /// <summary>
    /// Read git info and saves it into resources and streaming assets folder.
    /// </summary>
    internal static void GenerateGitInfoFiles()
    {
        var gitHash = Git.Hash ?? "";
        var gitStatus = Git.Status ?? "";
        var gitBuildTime = Git.BuildTime ?? "";

        if (string.IsNullOrEmpty(gitHash))
            Debug.LogWarning("No git hash included in build.");
        if (string.IsNullOrEmpty(gitBuildTime))
            Debug.LogWarning("No git build time included in build.");
        if (string.IsNullOrEmpty(gitStatus) == false)
            Debug.LogWarning("Git status is not empty, you started a build with uncommitted changes.");

        var resourcesFolder = resourcesTempFolderToDelete + "Resources/";

        Directory.CreateDirectory(resourcesFolder + gitInfoFolderName);
        File.WriteAllText(resourcesFolder + gitInfoHashPath + fileExt, gitHash);
        File.WriteAllText(resourcesFolder + gitInfoStatusPath + fileExt, gitStatus);
        File.WriteAllText(resourcesFolder + gitInfoBuildTimePath + fileExt, gitBuildTime);

        Directory.CreateDirectory(streamingAssetsTempFolderToDelete + gitInfoFolderName);
        File.WriteAllText(streamingAssetsTempFolderToDelete + gitInfoHashPath + fileExt, gitHash);
        File.WriteAllText(streamingAssetsTempFolderToDelete + gitInfoStatusPath + fileExt, gitStatus);
        File.WriteAllText(streamingAssetsTempFolderToDelete + gitInfoBuildTimePath + fileExt, gitBuildTime);
    }

    /// <summary>
    /// Delete folders and meta files generated during PreProcess build.
    /// </summary>
    internal static void DeleteBuildFiles()
    {
        Directory.Delete(streamingAssetsTempFolderToDelete + gitInfoFolderName, true);
        File.Delete(Path.GetDirectoryName(streamingAssetsTempFolderToDelete + gitInfoFolderName) + ".meta");

        Directory.Delete(resourcesTempFolderToDelete, true);
        File.Delete(Path.GetDirectoryName(resourcesTempFolderToDelete) + ".meta");
    }

    /// <summary>
    /// Get n first characters in string, if possible.
    /// Returns empty string "" if given string is null.
    /// </summary>
    private static string TruncateString(string value, int nChar)
    {
        if (value == null)
            return "";
        return nChar >= value.Length ? value : value.Substring(0, nChar);
    }
}
}