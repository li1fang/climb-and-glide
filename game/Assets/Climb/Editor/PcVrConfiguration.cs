using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace Climb.EditorTools
{
    public static class PcVrConfiguration
    {
        public static void Configure(bool includeLoader = true)
        {
            var settings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>("Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            if (settings == null) throw new System.InvalidOperationException("XR settings asset missing");
            if (!settings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Standalone))
                settings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Standalone);
            var general = settings.SettingsForBuildTarget(BuildTargetGroup.Standalone);
            general.InitManagerOnStart = false; // --vr opts in; desktop trials never initialize XR.
            general.Manager.automaticLoading = false;
            general.Manager.automaticRunning = false;
            if (includeLoader && !XRPackageMetadataStore.AssignLoader(general.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Standalone))
                throw new System.InvalidOperationException("Could not assign OpenXR loader");
            if (!includeLoader) XRPackageMetadataStore.RemoveLoader(general.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Standalone);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
            var openxr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            openxr.GetFeature<OculusTouchControllerProfile>().enabled = true;
            openxr.GetFeature<MetaQuestTouchProControllerProfile>().enabled = true;
            // Compatibility baseline for the existing graybox shader/TextMesh.
            openxr.renderMode = OpenXRSettings.RenderMode.MultiPass;
            EditorUtility.SetDirty(settings); EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(general.Manager); EditorUtility.SetDirty(openxr);
            const string path = "Assets/Climb/Resources/PcVrActions.asset";
            if (AssetDatabase.LoadAssetAtPath<InputActionAsset>(path) == null) {
                var asset = ScriptableObject.CreateInstance<InputActionAsset>();
                var map = new InputActionMap("Flight"); asset.AddActionMap(map);
                Add(map,"LeftPull","LeftHand","trigger",InputActionType.Value);
                Add(map,"RightPull","RightHand","trigger",InputActionType.Value);
                Add(map,"Accept","RightHand","primaryButton",InputActionType.Button);
                Add(map,"Pause","RightHand","secondaryButton",InputActionType.Button);
                Add(map,"Target","LeftHand","primaryButton",InputActionType.Button);
                Add(map,"Reset","LeftHand","secondaryButton",InputActionType.Button);
                AssetDatabase.CreateAsset(asset,path);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("PCVR configuration: loader included="+includeLoader+", manual startup, Touch + Touch Pro, MultiPass, action asset");
        }
        private static void Add(InputActionMap map,string name,string hand,string control,InputActionType type)
        {
            var action=map.AddAction(name,type);
            foreach(var layout in new[]{"OculusTouchController","QuestProTouchController"})
                action.AddBinding("<"+layout+">{"+hand+"}/"+control);
        }
    }
}
