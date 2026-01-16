using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.Linq;

namespace EasyPath.Editor
{
    /// <summary>
    /// Build automation script for Codebuff CLI integration.
    /// Usage: Unity.exe -batchmode -executeMethod EasyPath.Editor.BuildScript.BuildFromCLI -quit
    /// </summary>
    public static class BuildScript
    {
        /// <summary>
        /// Main entry point for CLI builds.
        /// Called via: -executeMethod EasyPath.Editor.BuildScript.BuildFromCLI
        /// </summary>
        public static void BuildFromCLI()
        {
            Debug.Log("[BuildScript] Starting CLI build...");
            
            string[] args = Environment.GetCommandLineArgs();
            BuildTarget target = GetTargetFromArgs(args);
            string outputPath = GetOutputPathFromArgs(args);
            
            Debug.Log($"[BuildScript] Target: {target}");
            Debug.Log($"[BuildScript] Output: {outputPath}");
            
            string[] scenes = GetBuildScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildScript] No scenes found in Build Settings!");
                EditorApplication.Exit(1);
                return;
            }
            
            Debug.Log($"[BuildScript] Scenes: {string.Join(", ", scenes)}");
            
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None
            };
            
            BuildReport report = BuildPipeline.BuildPlayer(options);
            
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] Build SUCCEEDED in {report.summary.totalTime.TotalSeconds:F1}s");
                Debug.Log($"[BuildScript] Size: {report.summary.totalSize / 1024 / 1024:F1} MB");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[BuildScript] Build FAILED: {report.summary.result}");
                foreach (var step in report.steps)
                {
                    foreach (var message in step.messages)
                    {
                        if (message.type == LogType.Error)
                            Debug.LogError($"  {message.content}");
                    }
                }
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Build/Quick Build (Development)")]
        public static void QuickBuild()
        {
            string[] scenes = GetBuildScenes();
            
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/QuickBuild/EasyPathDemo.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };
            
            BuildReport report = BuildPipeline.BuildPlayer(options);
            
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Quick build succeeded! Output: {options.locationPathName}");
            }
            else
            {
                Debug.LogError("Quick build failed!");
            }
        }

        [MenuItem("Build/Release Build")]
        public static void ReleaseBuild()
        {
            string[] scenes = GetBuildScenes();
            
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/Release/EasyPathDemo.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            
            BuildReport report = BuildPipeline.BuildPlayer(options);
            
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Release build succeeded! Output: {options.locationPathName}");
                Debug.Log($"Size: {report.summary.totalSize / 1024 / 1024:F1} MB");
            }
            else
            {
                Debug.LogError("Release build failed!");
            }
        }

        private static string[] GetBuildScenes()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
        }

        private static BuildTarget GetTargetFromArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-buildTarget" && i + 1 < args.Length)
                {
                    string targetStr = args[i + 1];
                    switch (targetStr.ToLower())
                    {
                        case "win64":
                        case "standalonewindows64":
                            return BuildTarget.StandaloneWindows64;
                        case "win32":
                        case "standalonewindows":
                            return BuildTarget.StandaloneWindows;
                        case "linux64":
                        case "standalonelinux64":
                            return BuildTarget.StandaloneLinux64;
                        case "osx":
                        case "standaloneosx":
                            return BuildTarget.StandaloneOSX;
                        case "webgl":
                            return BuildTarget.WebGL;
                        case "android":
                            return BuildTarget.Android;
                        case "ios":
                            return BuildTarget.iOS;
                    }
                }
            }
            return BuildTarget.StandaloneWindows64;
        }

        private static string GetOutputPathFromArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-outputPath" && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }
            return "Builds/Game.exe";
        }
    }
}
