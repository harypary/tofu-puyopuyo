using UnityEditor;
using UnityEditor.Build.Reporting;
using System;

public class BuildScript
{
    public static void BuildIOS()
    {
        PlayerSettings.applicationIdentifier = "com.harypary.tofupuyopuyo";
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.iOS.buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "1";

        var options = new BuildPlayerOptions
        {
            scenes    = GetEnabledScenes(),
            locationPathName = "Builds/iOS",
            target    = BuildTarget.iOS,
            options   = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Build failed: " + report.summary.result);
    }

    static string[] GetEnabledScenes()
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled) list.Add(s.path);
        return list.ToArray();
    }
}
