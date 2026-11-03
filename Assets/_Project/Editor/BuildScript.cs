using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HeroShooter.EditorTools
{
    /// <summary>
    /// Player builds from the command line, which the project never had — S7 was always "File > Build
    /// Profiles by hand". The scene list is read from Project Settings → Build Settings rather than
    /// repeated here, so a scene added there cannot be silently missing from a build.
    ///
    /// Output goes outside the repository: a clean folder is what a grader unzips, and nothing in the game
    /// reads it back.
    /// </summary>
    public static class BuildScript
    {
        const string LinuxOut = "/home/chloe/builds/hero-shooter/linux/HeroShooter.x86_64";
        const string WindowsOut = "/home/chloe/builds/hero-shooter/windows/HeroShooter.exe";

        public static void BuildLinux()
        {
            Build(BuildTarget.StandaloneLinux64, LinuxOut);
        }

        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, WindowsOut);
        }

        static void Build(BuildTarget target, string outputPath)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();

            if (scenes.Length == 0)
            {
                Debug.Log("[Build] FAIL no enabled scenes in Build Settings");
                EditorApplication.Exit(1);
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = BuildOptions.None
            };

            Debug.Log($"[Build] start target={target} scenes={scenes.Length} first={scenes.First()} out={outputPath}");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log($"[Build] result={summary.result} size={summary.totalSize / (1024f * 1024f):0.0}MB time={summary.totalTime} errors={summary.totalErrors} output={outputPath} exists={File.Exists(outputPath)}");

            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
