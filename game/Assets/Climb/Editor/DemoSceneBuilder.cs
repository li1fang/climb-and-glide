using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Climb.Demo;

namespace Climb.EditorTools
{
    public static class DemoSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ClimbDemo.unity";

        [MenuItem("Climb/Create or Open Demo Scene")]
        public static void CreateOrOpen()
        {
            // Never discard an unrelated scene with unsaved work.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            CreateScene();
        }

        public static void PrepareForBatch(bool includeXr = false)
        {
            // Resolve package settings before BuildPipeline enters its immutable build phase.
            UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(BuildTargetGroup.Standalone);
            PcVrConfiguration.Configure(includeXr);
            AssetDatabase.SaveAssets();
            if (!File.Exists(ScenePath)) CreateScene();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Climb Demo").AddComponent<ClimbDemoController>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.Refresh();
            Debug.Log("Demo scene prepared. Synthetic controls only; no device connection is performed.");
        }

        [MenuItem("Climb/Build Windows Demo")]
        public static void BuildWindows()
        {
            BuildWindowsTarget(false);
        }

        [MenuItem("Climb/Build PC VR Test")]
        public static void BuildPcVr() { BuildWindowsTarget(true); }

        public static void BuildDesktopAndPcVr() { BuildWindowsTarget(false); BuildWindowsTarget(true); }

        private static void BuildWindowsTarget(bool vr)
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareForBatch(vr);
            string folder=vr?"Builds/WindowsVR":"Builds/Windows";
            Directory.CreateDirectory(folder);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = folder+"/ClimbDemo.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("Windows build failed: " + report.summary.result);
        }
    }
}
