using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;

public class BuildScript
{
    public static void BuildIOS()
    {
        PlayerSettings.applicationIdentifier = "com.harypary.tofupuyopuyo";
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.iOS.buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "1";

        // Set 1024x1024 App Store icon
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/AppIcon.png");
        if (icon != null)
        {
            var kind = UnityEditor.iOS.iOSPlatformIconKind.Application;
            var icons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.iOS, kind);
            for (int i = 0; i < icons.Length; i++)
                icons[i].SetTexture(icon);
            PlayerSettings.SetPlatformIcons(BuildTargetGroup.iOS, kind, icons);
        }

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
