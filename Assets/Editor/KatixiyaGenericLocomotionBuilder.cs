using System.Collections.Generic;
using Config;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class KatixiyaGenericLocomotionBuilder
{
    private const string OutputFolder = "Assets/Config/Characters/1004_Generic";
    private const string ControllerPath = OutputFolder + "/1004_Katixiya_GenericLocomotion.controller";
    private const string LocomotionConfigPath = OutputFolder + "/1004_Katixiya_GenericLocomotionConfig.asset";
    private const string KatixiyaCharacterConfigPath = "Assets/Config/Characters/1004KatixiyaConfig.asset";
    private const string AvatarPath = "Assets/Res/Models/Katixiya/Model/01_Katixiya_Tpose.fbx";

    [MenuItem("Tools/Animation/Katixiya/Create Generic Locomotion Controller")]
    public static void Build()
    {
        EnsureFolder(OutputFolder);

        var controller = CreateControllerAsset();
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Failed", "Failed to create AnimatorController.", "OK");
            return;
        }

        var missingClips = new List<string>();
        BuildStateMachine(controller, missingClips);
        var locomotionConfig = CreateOrUpdateLocomotionConfig(controller);
        TryAssignToCharacterConfig(locomotionConfig);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (missingClips.Count > 0)
        {
            Debug.LogWarning("[KatixiyaGenericLocomotionBuilder] Missing clips: " + string.Join(", ", missingClips));
        }

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(LocomotionConfigPath);
        EditorUtility.DisplayDialog(
            "Done",
            $"Generated Generic locomotion controller:\n{ControllerPath}\n\nGenerated config:\n{LocomotionConfigPath}",
            "OK");
    }

    private static AnimatorController CreateControllerAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsRun", AnimatorControllerParameterType.Bool);
        return controller;
    }

    private static void BuildStateMachine(AnimatorController controller, List<string> missingClips)
    {
        AnimationClip idle = LoadClip("Assets/Res/Animations/1004_Katixiya/InPlace/Idle.anim", missingClips, "Idle");

        var walkClips = new DirectionalClips
        {
            Forward = LoadClip("Assets/Res/Models/Katixiya/fbx/Walk_F.fbx", missingClips, "Walk_F"),
            Backward = LoadClip("Assets/Res/Models/Katixiya/fbx/Walk_B.fbx", missingClips, "Walk_B"),
            LeftForward = LoadClip("Assets/Res/Models/Katixiya/fbx/Walk_LF.fbx", missingClips, "Walk_LF"),
            RightForward = LoadClip("Assets/Res/Models/Katixiya/fbx/Walk_RF.fbx", missingClips, "Walk_RF"),
            LeftBackward = LoadClip("Assets/Res/Models/Katixiya/fbx/Walk_LB.fbx", missingClips, "Walk_LB"),
            RightBackward = LoadClip("Assets/Res/Models/Katixiya/fbx/Walk_RB.fbx", missingClips, "Walk_RB"),
        };

        var runClips = new DirectionalClips
        {
            Forward = LoadClip("Assets/Res/Models/Katixiya/fbx/Run_F.fbx", missingClips, "Run_F"),
            Backward = LoadClip("Assets/Res/Models/Katixiya/fbx/Run_B.fbx", missingClips, "Run_B"),
            LeftForward = LoadClip("Assets/Res/Models/Katixiya/fbx/Run_LF.fbx", missingClips, "Run_LF"),
            RightForward = LoadClip("Assets/Res/Models/Katixiya/fbx/Run_RF.fbx", missingClips, "Run_RF"),
            LeftBackward = LoadClip("Assets/Res/Models/Katixiya/fbx/Run_LB.fbx", missingClips, "Run_LB"),
            RightBackward = LoadClip("Assets/Res/Models/Katixiya/fbx/Run_RB.fbx", missingClips, "Run_RB"),
        };

        var baseLayer = controller.layers[0];
        var stateMachine = baseLayer.stateMachine;

        var locomotionState = stateMachine.AddState("Locomotion");
        stateMachine.defaultState = locomotionState;

        var locomotionTree = new BlendTree
        {
            name = "BT_Locomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false,
        };
        AssetDatabase.AddObjectToAsset(locomotionTree, controller);

        var walkTree = CreateDirectionalTree(controller, "BT_Walk", walkClips);
        var runTree = CreateDirectionalTree(controller, "BT_Run", runClips);

        if (idle != null) locomotionTree.AddChild(idle, 0f);
        if (walkTree != null) locomotionTree.AddChild(walkTree, 0.5f);
        if (runTree != null) locomotionTree.AddChild(runTree, 1f);

        // Fallback when every clip is missing.
        if (locomotionTree.children.Length == 0)
        {
            var fallback = LoadClip("Assets/Res/Animations/1004_Katixiya/InPlace/Idle_Action1.anim", missingClips, "Idle_Action1");
            if (fallback != null)
                locomotionTree.AddChild(fallback, 0f);
        }

        locomotionState.motion = locomotionTree;
    }

    private static BlendTree CreateDirectionalTree(AnimatorController controller, string treeName, DirectionalClips clips)
    {
        var tree = new BlendTree
        {
            name = treeName,
            blendType = BlendTreeType.FreeformDirectional2D,
            blendParameter = "MoveX",
            blendParameterY = "MoveY",
            useAutomaticThresholds = false,
        };
        AssetDatabase.AddObjectToAsset(tree, controller);

        AddDirectionalChild(tree, clips.Forward, 0f, 1f);
        AddDirectionalChild(tree, clips.Backward, 0f, -1f);
        AddDirectionalChild(tree, clips.LeftForward, -0.7f, 0.7f);
        AddDirectionalChild(tree, clips.RightForward, 0.7f, 0.7f);
        AddDirectionalChild(tree, clips.LeftBackward, -0.7f, -0.7f);
        AddDirectionalChild(tree, clips.RightBackward, 0.7f, -0.7f);

        return tree.children.Length > 0 ? tree : null;
    }

    private static void AddDirectionalChild(BlendTree tree, AnimationClip clip, float x, float y)
    {
        if (clip == null)
            return;

        tree.AddChild(clip, new Vector2(x, y));
    }

    private static GenericLocomotionConfig CreateOrUpdateLocomotionConfig(RuntimeAnimatorController controller)
    {
        var config = AssetDatabase.LoadAssetAtPath<GenericLocomotionConfig>(LocomotionConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<GenericLocomotionConfig>();
            AssetDatabase.CreateAsset(config, LocomotionConfigPath);
        }

        config.animatorController = controller;
        config.avatar = LoadAvatar(AvatarPath);
        config.applyRootMotion = false;
        config.walkSpeed = 3.5f;
        config.runSpeed = 6.5f;
        config.rotateSpeed = 12f;
        config.acceleration = 14f;
        config.deceleration = 18f;
        config.gravity = -20f;
        config.animatorDampTime = 0.08f;
        config.speedParam = "Speed";
        config.moveXParam = "MoveX";
        config.moveYParam = "MoveY";
        config.isMovingParam = "IsMoving";
        config.isRunParam = "IsRun";

        EditorUtility.SetDirty(config);
        return config;
    }

    private static void TryAssignToCharacterConfig(GenericLocomotionConfig genericConfig)
    {
        if (genericConfig == null)
            return;

        var characterConfig = AssetDatabase.LoadAssetAtPath<CharacterConfig>(KatixiyaCharacterConfigPath);
        if (characterConfig == null)
            return;

        var serialized = new SerializedObject(characterConfig);
        var field = serialized.FindProperty("GenericLocomotionConfig");
        if (field == null)
            return;

        field.objectReferenceValue = genericConfig;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(characterConfig);
    }

    private static AnimationClip LoadClip(string path, List<string> missingClips, string logicalName)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null && !clip.name.Contains("__preview__"))
            return clip;

        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is AnimationClip animationClip && !animationClip.name.Contains("__preview__"))
                return animationClip;
        }

        missingClips.Add(logicalName);
        return null;
    }

    private static Avatar LoadAvatar(string modelPath)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Avatar avatar)
                return avatar;
        }

        return null;
    }

    private static void EnsureFolder(string targetFolder)
    {
        if (AssetDatabase.IsValidFolder(targetFolder))
            return;

        string[] parts = targetFolder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private struct DirectionalClips
    {
        public AnimationClip Forward;
        public AnimationClip Backward;
        public AnimationClip LeftForward;
        public AnimationClip RightForward;
        public AnimationClip LeftBackward;
        public AnimationClip RightBackward;
    }
}
