using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MobileWebBuild
{
    [MenuItem("Tools/Mobile/Build web ZIP")]
    public static void Build()
    {
        BuildProfile profile = AssetDatabase.LoadAssetAtPath<BuildProfile>("Assets/Settings/Build Profiles/Web.asset");
        if (profile == null || EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            throw new BuildFailedException("Select the Web build profile before building.");

        string output = Path.GetFullPath("Builds/Web-Mobile");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions
        {
            buildProfile = profile,
            locationPathName = output,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Web build failed; see the Console.");

        // Package only runtime files; keep generated debug information out of the download.
        string zipPath = Path.GetFullPath("Builds/TopDownShooter-Web-Mobile.zip");
        using (FileStream stream = new FileStream(zipPath, FileMode.Create))
        using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            zip.CreateEntryFromFile(Path.Combine(output, "index.html"), "index.html");
            foreach (string folder in new[] { "Build", "TemplateData", "StreamingAssets" })
            {
                string path = Path.Combine(output, folder);
                if (!Directory.Exists(path)) continue;
                foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    zip.CreateEntryFromFile(file, file.Substring(output.Length + 1).Replace('\\', '/'));
            }
        }
        Debug.Log("Mobile web ZIP ready: " + zipPath);
    }
}
