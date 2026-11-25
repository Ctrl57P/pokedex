using UnityEditor;
using UnityEngine;

// Test build for screenshots on this PC (not part of the deliverable).
public static class LinuxTestBuild
{
    public static void Build()
    {
        var r = BuildPipeline.BuildPlayer(new[] { "Assets/Scenes/Pokedex.unity" }, "Builds/LinuxTest/Pokedex.x86_64",
            BuildTarget.StandaloneLinux64, BuildOptions.None);
        Debug.Log($"[LinuxTestBuild] {r.summary.result}");
    }
}
