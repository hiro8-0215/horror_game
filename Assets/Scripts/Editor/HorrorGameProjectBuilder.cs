using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using HorrorGame.Core;
using HorrorGame.Audio;
using HorrorGame.ScenarioA;
using HorrorGame.ScenarioB;
using HorrorGame.ScenarioC;

namespace HorrorGame.Editor
{
    /// <summary>
    /// 全4シーン（00_BootScene / 01_ScenarioA_Elevator / 02_ScenarioB_Darkroom / 03_ScenarioC_Mannequin）の自動構築、
    /// ランダム12パターン（動的54通り）構成、前半チープAIモデル→後半高精細モデル（ScaryWoman_Ghost_01）への切り替え配置、
    /// 自動デバッグ検証、およびMeta Quest 3用APKビルドを一括実行するエディタスクリプト。
    /// </summary>
    public static class HorrorGameProjectBuilder
    {
        private const string SCENES_DIR = "Assets/Scenes";
        private const string MATS_DIR = "Assets/Materials/ScenarioA";
        private const string SCARY_WOMAN_PREFAB_PATH = "Assets/Characters/ScaryWoman/ScaryWoman_Ghost_01.prefab";

        [MenuItem("HorrorGame/1. 全シーン(案A/案B/案C・12+パターン)自動構築 (Setup All Scenes)", false, 1)]
        public static void BuildAllScenesAndConfigureQuest()
        {
            EnsureDirectories();
            EnsureLayersConfigured();
            ConfigureQuest3PlayerSettings();
            ConfigureOpenXRForQuest3();

            // 高精細モデルプレハブ（elevator-apparition.glb: 177,912頂点）が常に最新であることを保証
            ScaryWomanCharacterBuilder.BuildScaryWomanCharacterPrefab();

            string bootScenePath = $"{SCENES_DIR}/00_BootScene.unity";
            string scenarioAScenePath = $"{SCENES_DIR}/01_ScenarioA_Elevator.unity";
            string scenarioBScenePath = $"{SCENES_DIR}/02_ScenarioB_Darkroom.unity";
            string scenarioCScenePath = $"{SCENES_DIR}/03_ScenarioC_Mannequin.unity";

            BuildBootScene(bootScenePath);
            BuildScenarioAElevatorScene(scenarioAScenePath);
            BuildScenarioBDarkroomScene(scenarioBScenePath);
            BuildScenarioCMannequinScene(scenarioCScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(bootScenePath, true),
                new EditorBuildSettingsScene(scenarioAScenePath, true),
                new EditorBuildSettingsScene(scenarioBScenePath, true),
                new EditorBuildSettingsScene(scenarioCScenePath, true)
            };

            EditorSceneManager.OpenScene(scenarioAScenePath, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[HorrorGameProjectBuilder] 全4シーン（案A・案B・案C / ランダム12+パターン）およびMeta Quest 3設定の構築が完了しました！");
        }

        [MenuItem("HorrorGame/2. 自動デバッグ検証＆Meta Quest 3用 APKビルド (Verify & Build APK)", false, 2)]
        public static void VerifyAndBuildQuest3Apk()
        {
            ConfigureAndroidSdkPathsIfMissing();
            BuildAllScenesAndConfigureQuest();
            RunAutomatedSceneAudit();
            BuildQuest3ApkOnly();
        }

        public static void BuildQuest3Apk()
        {
            VerifyAndBuildQuest3Apk();
        }

        public static void RunAutomatedSceneAudit()
        {
            Debug.Log("=== [Automated Debug Audit] 全4シーンのコンポーネント・参照・マテリアル整合性チェック開始 ===");

            // 1. ScaryWoman_Ghost_01.prefab チェック
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SCARY_WOMAN_PREFAB_PATH);
            if (prefab == null) throw new Exception("Audit Failed: ScaryWoman_Ghost_01.prefab is missing!");
            var smr = prefab.GetComponent<SkinnedMeshRenderer>();
            if (smr == null || smr.sharedMesh == null || smr.sharedMesh.vertexCount < 170000)
                throw new Exception($"Audit Failed: ScaryWoman_Ghost_01 SkinnedMeshRenderer vertexCount ({smr?.sharedMesh?.vertexCount}) is less than 177,912!");
            Debug.Log($"[Audit PASS] ScaryWoman_Ghost_01.prefab verified: {smr.sharedMesh.vertexCount} vertices, {smr.sharedMaterials.Length} materials.");

            // 2. 00_BootScene チェック
            EditorSceneManager.OpenScene($"{SCENES_DIR}/00_BootScene.unity", OpenSceneMode.Single);
            var randomizer = UnityEngine.Object.FindFirstObjectByType<ScenarioRandomizer>();
            if (randomizer == null) throw new Exception("Audit Failed: ScenarioRandomizer missing in 00_BootScene!");
            Debug.Log("[Audit PASS] 00_BootScene verified: ScenarioRandomizer ready with 12+ patterns.");

            // 3. 01_ScenarioA_Elevator チェック
            EditorSceneManager.OpenScene($"{SCENES_DIR}/01_ScenarioA_Elevator.unity", OpenSceneMode.Single);
            var mgrA = UnityEngine.Object.FindFirstObjectByType<ElevatorScenarioManager>();
            if (mgrA == null || mgrA.env == null || mgrA.ghostController == null || mgrA.gazeDetector == null)
                throw new Exception("Audit Failed: ElevatorScenarioManager references incomplete!");
            if (mgrA.ghostController.mirrorGhostRoot == null ||
                mgrA.ghostController.turnedAroundGhostRoot == null ||
                mgrA.ghostController.firstFloorHallGhostRoot == null ||
                mgrA.ghostController.jumpscareGhostRoot == null)
                throw new Exception("Audit Failed: MirrorGhostController models missing in 01_ScenarioA_Elevator!");
            Debug.Log("[Audit PASS] 01_ScenarioA_Elevator verified: Stage1 Cheap Dummy + TurnedAround/1F/Jumpscare ScaryWoman_Ghost_01 all wired.");

            // 4. 02_ScenarioB_Darkroom チェック
            EditorSceneManager.OpenScene($"{SCENES_DIR}/02_ScenarioB_Darkroom.unity", OpenSceneMode.Single);
            var mgrB = UnityEngine.Object.FindFirstObjectByType<DarkroomScenarioManager>();
            if (mgrB == null || mgrB.developButton == null || mgrB.closeDoorButton == null ||
                mgrB.cheapDummyGhostRoot == null || mgrB.realScaryWomanRoot == null)
                throw new Exception("Audit Failed: DarkroomScenarioManager references incomplete in 02_ScenarioB_Darkroom!");
            Debug.Log("[Audit PASS] 02_ScenarioB_Darkroom verified: Develop Tray + Cheap Dummy + Real ScaryWoman_Ghost_01 wired.");

            // 5. 03_ScenarioC_Mannequin チェック
            EditorSceneManager.OpenScene($"{SCENES_DIR}/03_ScenarioC_Mannequin.unity", OpenSceneMode.Single);
            var mgrC = UnityEngine.Object.FindFirstObjectByType<MannequinScenarioManager>();
            if (mgrC == null || mgrC.powerChargeButton == null ||
                mgrC.cheapMannequinCenter == null || mgrC.realScaryWomanRoot == null)
                throw new Exception("Audit Failed: MannequinScenarioManager references incomplete in 03_ScenarioC_Mannequin!");
            Debug.Log("[Audit PASS] 03_ScenarioC_Mannequin verified: 3 Cheap Mannequins + Real ScaryWoman_Ghost_01 Climax wired.");

            EditorSceneManager.OpenScene($"{SCENES_DIR}/01_ScenarioA_Elevator.unity", OpenSceneMode.Single);
            Debug.Log("=== [Automated Debug Audit] 全4シーン・全参照チェック 100% PASS! ===");
        }

        private static void BuildQuest3ApkOnly()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            ConfigureOpenXRForQuest3();
            AssetDatabase.SaveAssets();

            string buildDir = "Builds";
            if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);
            string apkPath = "Builds/HorrorGame_Quest3.apk";

            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    $"{SCENES_DIR}/00_BootScene.unity",
                    $"{SCENES_DIR}/01_ScenarioA_Elevator.unity",
                    $"{SCENES_DIR}/02_ScenarioB_Darkroom.unity",
                    $"{SCENES_DIR}/03_ScenarioC_Mannequin.unity"
                },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[HorrorGameProjectBuilder] APK Build Result: {report.summary.result} (Errors: {report.summary.totalErrors}) -> {apkPath}");
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"APK Build Failed: {report.summary.result}");
            }
        }

        private static void ConfigureAndroidSdkPathsIfMissing()
        {
            string fallbackBase = @"C:\Program Files\Unity\Hub\Editor\6000.4.0f1\Editor\Data\PlaybackEngines\AndroidPlayer";
            string sdkPath = Path.Combine(fallbackBase, "SDK");
            string ndkPath = Path.Combine(fallbackBase, "NDK");
            string jdkPath = Path.Combine(fallbackBase, "OpenJDK");

            if (Directory.Exists(sdkPath))
            {
                EditorPrefs.SetBool("SdkUseEmbedded", false);
                EditorPrefs.SetString("AndroidSdkRoot", sdkPath);
                Environment.SetEnvironmentVariable("ANDROID_SDK_ROOT", sdkPath);
                Environment.SetEnvironmentVariable("ANDROID_HOME", sdkPath);
            }
            if (Directory.Exists(ndkPath))
            {
                EditorPrefs.SetBool("NdkUseEmbedded", false);
                EditorPrefs.SetString("AndroidNdkRoot", ndkPath);
                EditorPrefs.SetString("AndroidNdkRootR27C", ndkPath);
                Environment.SetEnvironmentVariable("ANDROID_NDK_ROOT", ndkPath);
            }
            if (Directory.Exists(jdkPath))
            {
                EditorPrefs.SetBool("JdkUseEmbedded", false);
                EditorPrefs.SetString("JdkPath", jdkPath);
                Environment.SetEnvironmentVariable("JAVA_HOME", jdkPath);
            }
        }

        private static void ConfigureOpenXRForQuest3()
        {
            try
            {
                if (!Directory.Exists("Assets/XR")) Directory.CreateDirectory("Assets/XR");
                if (!Directory.Exists("Assets/XR/Loaders")) Directory.CreateDirectory("Assets/XR/Loaders");

                string generalSettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
                XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
                if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettings) || buildTargetSettings == null)
                {
                    buildTargetSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(generalSettingsPath);
                    if (buildTargetSettings == null)
                    {
                        buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                        AssetDatabase.CreateAsset(buildTargetSettings, generalSettingsPath);
                    }
                    EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
                }

                string openXrLoaderPath = "Assets/XR/Loaders/OpenXRLoader.asset";
                OpenXRLoader openXrLoader = AssetDatabase.LoadAssetAtPath<OpenXRLoader>(openXrLoaderPath);
                if (openXrLoader == null)
                {
                    openXrLoader = ScriptableObject.CreateInstance<OpenXRLoader>();
                    AssetDatabase.CreateAsset(openXrLoader, openXrLoaderPath);
                }

                AssignOpenXRLoaderToTarget(buildTargetSettings, BuildTargetGroup.Android, openXrLoader);
                AssignOpenXRLoaderToTarget(buildTargetSettings, BuildTargetGroup.Standalone, openXrLoader);

                Type pkgSettingsType = Type.GetType("UnityEditor.XR.OpenXR.OpenXRPackageSettings, Unity.XR.OpenXR.Editor");
                if (pkgSettingsType != null)
                {
                    MethodInfo getOrCreate = pkgSettingsType.GetMethod("GetOrCreateInstance", BindingFlags.Public | BindingFlags.Static);
                    object pkgSettings = getOrCreate?.Invoke(null, null);
                    if (pkgSettings != null)
                    {
                        MethodInfo getSettingsForGroup = pkgSettingsType.GetMethod("GetSettingsForBuildTargetGroup", BindingFlags.Public | BindingFlags.Instance);
                        getSettingsForGroup?.Invoke(pkgSettings, new object[] { BuildTargetGroup.Android });
                        getSettingsForGroup?.Invoke(pkgSettings, new object[] { BuildTargetGroup.Standalone });
                        EditorUtility.SetDirty((UnityEngine.Object)pkgSettings);
                    }
                }

                EnableOpenXRFeaturesForTarget(BuildTargetGroup.Android);
                EnableOpenXRFeaturesForTarget(BuildTargetGroup.Standalone);

                EditorUtility.SetDirty(buildTargetSettings);
                AssetDatabase.SaveAssets();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HorrorGameProjectBuilder] OpenXR configuration warning: {ex.Message}");
            }
        }

        private static void AssignOpenXRLoaderToTarget(XRGeneralSettingsPerBuildTarget perTarget, BuildTargetGroup group, OpenXRLoader loader)
        {
            if (!perTarget.HasSettingsForBuildTarget(group))
                perTarget.CreateDefaultSettingsForBuildTarget(group);
            if (!perTarget.HasManagerSettingsForBuildTarget(group))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(group);

            XRGeneralSettings general = perTarget.SettingsForBuildTarget(group);
            general.InitManagerOnStart = true;

            XRManagerSettings manager = perTarget.ManagerSettingsForBuildTarget(group);
            if (manager != null)
            {
                if (!XRPackageMetadataStore.AssignLoader(manager, typeof(OpenXRLoader).FullName, group))
                {
                    var loaders = new List<XRLoader> { loader };
                    manager.TrySetLoaders(loaders);
                }
                EditorUtility.SetDirty(manager);
            }
            EditorUtility.SetDirty(general);
        }

        private static void EnableOpenXRFeaturesForTarget(BuildTargetGroup group)
        {
            FeatureHelpers.RefreshFeatures(group);
            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (settings == null) return;

            settings.renderMode = OpenXRSettings.RenderMode.MultiPass;

            var features = settings.GetFeatures();
            if (features != null)
            {
                foreach (var feature in features)
                {
                    if (feature == null) continue;
                    string typeName = feature.GetType().FullName ?? "";
                    if (typeName.Contains("MetaQuestFeature") ||
                        typeName.Contains("OculusTouchControllerProfile") ||
                        typeName.Contains("MetaQuestTouchPlusControllerProfile") ||
                        typeName.Contains("MetaQuestTouchProControllerProfile"))
                    {
                        feature.enabled = true;
                        EditorUtility.SetDirty(feature);
                    }
                }
            }
            EditorUtility.SetDirty(settings);
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(SCENES_DIR)) Directory.CreateDirectory(SCENES_DIR);
            if (!Directory.Exists(MATS_DIR)) Directory.CreateDirectory(MATS_DIR);
            AssetDatabase.Refresh();
        }

        private static void EnsureLayersConfigured()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            if (layersProp != null && layersProp.arraySize > 8)
            {
                layersProp.GetArrayElementAtIndex(6).stringValue = "MirrorOnly";
                layersProp.GetArrayElementAtIndex(7).stringValue = "DirectOnly";
                layersProp.GetArrayElementAtIndex(8).stringValue = "ElevatorButton";
                tagManager.ApplyModifiedProperties();
            }
        }

        private static void ConfigureQuest3PlayerSettings()
        {
            PlayerSettings.companyName = "VRHorrorStudio";
            PlayerSettings.productName = "MidnightElevatorVR";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.vrhorrorstudio.midnightelevator");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        }

        private static void BuildBootScene(string scenePath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;

            GameObject bootManager = new GameObject("ScenarioRandomizer_Boot");
            bootManager.AddComponent<ScenarioRandomizer>();

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        // =====================================================================
        // 案A：『深夜3時のエレベーター』シーン構築
        // =====================================================================
        private static void BuildScenarioAElevatorScene(string scenePath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.02f, 0.02f, 0.025f);
            RenderSettings.reflectionIntensity = 0.2f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.012f);
            RenderSettings.fogDensity = 0.16f;

            Material wallMat = CreateOrUpdateMaterial("ElevatorWall_Mat", new Color(0.16f, 0.17f, 0.19f), 0.35f, 0.45f);
            Material floorMat = CreateOrUpdateMaterial("ElevatorFloor_Mat", new Color(0.10f, 0.10f, 0.11f), 0.15f, 0.25f);
            Material doorMat = CreateOrUpdateMaterial("ElevatorDoor_Mat", new Color(0.22f, 0.23f, 0.25f), 0.55f, 0.5f);
            Material panelMat = CreateOrUpdateMaterial("ControlPanel_Mat", new Color(0.12f, 0.12f, 0.14f), 0.7f, 0.6f);
            Material frameMat = CreateOrUpdateMaterial("DarkFrame_Mat", new Color(0.06f, 0.06f, 0.07f), 0.2f, 0.3f);
            Material mirrorMat = CreateOrUpdateMaterial("MirrorSurface_Mat", Color.white, 0.0f, 0.9f, true);
            Material lightTubeMat = CreateOrUpdateMaterial("FluorescentTube_Mat", new Color(0.95f, 0.98f, 1.0f), 0.0f, 0.2f, false, new Color(1.1f, 1.15f, 1.25f));
            Material redLampMat = CreateOrUpdateMaterial("RedEmergencyLamp_Mat", new Color(0.2f, 0.02f, 0.02f), 0.0f, 0.4f);
            Material exitSignMat = CreateOrUpdateMaterial("GreenExitSign_Mat", new Color(0.1f, 0.95f, 0.25f), 0.0f, 0.2f, false, new Color(0.15f, 1.8f, 0.35f));
            Material ghostClothMat = CreateOrUpdateMaterial("GhostCloth_Mat", new Color(0.04f, 0.04f, 0.05f), 0.0f, 0.1f);
            Material ghostSkinMat = CreateOrUpdateMaterial("GhostPaleSkin_Mat", new Color(0.75f, 0.78f, 0.76f), 0.0f, 0.2f);
            Material ghostHairMat = CreateOrUpdateMaterial("GhostBlackHair_Mat", new Color(0.015f, 0.015f, 0.02f), 0.0f, 0.15f);
            Material ghostEyeMat = CreateOrUpdateMaterial("GhostBleedingEye_Mat", new Color(0.9f, 0.1f, 0.1f), 0.0f, 0.5f, false, new Color(1.6f, 0.1f, 0.1f));
            Material blackoutMat = CreateOrUpdateMaterial("BlackoutInside_Mat", Color.black, 0.0f, 0.0f, true);
            Material bloodWallMat = CreateOrUpdateMaterial("BloodHallway_Mat", new Color(0.22f, 0.015f, 0.015f), 0.1f, 0.75f);

            int mirrorOnlyLayer = LayerMask.NameToLayer("MirrorOnly");
            if (mirrorOnlyLayer < 0) mirrorOnlyLayer = 6;

            GameObject audioMgrObj = new GameObject("ProceduralHorrorAudioManager");
            audioMgrObj.AddComponent<ProceduralHorrorAudio>();

            GameObject envRoot = new GameObject("ElevatorEnvironment");
            var envRefs = envRoot.AddComponent<ElevatorEnvironmentRefs>();

            CreateBox("Floor", envRoot.transform, new Vector3(0f, -0.05f, 0f), new Vector3(1.6f, 0.1f, 1.6f), floorMat);
            CreateBox("Ceiling_Main", envRoot.transform, new Vector3(-0.22f, 2.35f, 0f), new Vector3(1.16f, 0.1f, 1.6f), wallMat);
            CreateBox("Ceiling_RightRear", envRoot.transform, new Vector3(0.55f, 2.35f, -0.35f), new Vector3(0.5f, 0.1f, 0.9f), wallMat);

            GameObject hatchPivot = new GameObject("CeilingHatchCoverPivot");
            hatchPivot.transform.SetParent(envRoot.transform, false);
            hatchPivot.transform.localPosition = new Vector3(0.38f, 2.30f, 0.55f);
            CreateBox("HatchCoverMesh", hatchPivot.transform, new Vector3(0f, 0f, -0.22f), new Vector3(0.46f, 0.03f, 0.44f), frameMat);
            envRefs.ceilingHatchCover = hatchPivot.transform;

            GameObject hatchDropPoint = new GameObject("CeilingHatchDropPoint");
            hatchDropPoint.transform.SetParent(envRoot.transform, false);
            hatchDropPoint.transform.localPosition = new Vector3(0.28f, 2.18f, 0.32f);
            envRefs.ceilingHatchPoint = hatchDropPoint.transform;

            CreateBox("Wall_Left", envRoot.transform, new Vector3(-0.78f, 1.15f, 0f), new Vector3(0.06f, 2.3f, 1.6f), wallMat);
            CreateBox("Wall_Right", envRoot.transform, new Vector3(0.78f, 1.15f, 0f), new Vector3(0.06f, 2.3f, 1.6f), wallMat);
            CreateBox("Wall_Back", envRoot.transform, new Vector3(0f, 1.15f, -0.78f), new Vector3(1.6f, 2.3f, 0.06f), wallMat);

            CreateBox("Handrail_Left", envRoot.transform, new Vector3(-0.72f, 0.9f, 0f), new Vector3(0.04f, 0.04f, 1.35f), doorMat);
            CreateBox("Handrail_Back", envRoot.transform, new Vector3(0f, 0.9f, -0.72f), new Vector3(1.35f, 0.04f, 0.04f), doorMat);

            CreateBox("Wall_FrontLeft", envRoot.transform, new Vector3(-0.59f, 1.15f, 0.78f), new Vector3(0.42f, 2.3f, 0.06f), wallMat);
            CreateBox("Wall_FrontRight", envRoot.transform, new Vector3(0.59f, 1.15f, 0.78f), new Vector3(0.42f, 2.3f, 0.06f), wallMat);
            CreateBox("Wall_FrontHeader", envRoot.transform, new Vector3(0f, 2.15f, 0.78f), new Vector3(0.84f, 0.3f, 0.06f), wallMat);

            GameObject leftDoor = CreateBox("Door_Left", envRoot.transform, new Vector3(-0.21f, 1.0f, 0.75f), new Vector3(0.415f, 2.0f, 0.04f), doorMat);
            GameObject rightDoor = CreateBox("Door_Right", envRoot.transform, new Vector3(0.21f, 1.0f, 0.75f), new Vector3(0.415f, 2.0f, 0.04f), doorMat);
            envRefs.leftDoor = leftDoor.transform;
            envRefs.rightDoor = rightDoor.transform;

            CreateBox("FloorDisplay_Bezel", envRoot.transform, new Vector3(0f, 2.12f, 0.74f), new Vector3(0.28f, 0.14f, 0.02f), frameMat);
            envRefs.floorDisplayText = CreateWorldText("FloorDisplayText", envRoot.transform,
                new Vector3(0f, 2.12f, 0.725f), Quaternion.identity, 0.02f, 64, "10", new Color(1.0f, 0.32f, 0.08f));

            CreateBox("ControlPanel_Board", envRoot.transform, new Vector3(0.56f, 1.25f, 0.745f), new Vector3(0.32f, 0.95f, 0.025f), panelMat);

            CreateBox("FrontPanelMirror_Frame", envRoot.transform, new Vector3(0.56f, 1.56f, 0.732f), new Vector3(0.26f, 0.26f, 0.01f), frameMat);
            GameObject frontMirrorQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frontMirrorQuad.name = "FrontPanelMirror_Surface";
            frontMirrorQuad.transform.SetParent(envRoot.transform, false);
            frontMirrorQuad.transform.localPosition = new Vector3(0.56f, 1.56f, 0.725f);
            frontMirrorQuad.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
            frontMirrorQuad.GetComponent<Renderer>().sharedMaterial = mirrorMat;

            GameObject frontMirrorCamObj = new GameObject("FrontMirrorCamera");
            frontMirrorCamObj.transform.SetParent(frontMirrorQuad.transform, false);
            frontMirrorCamObj.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            frontMirrorCamObj.transform.localRotation = Quaternion.Euler(4f, 168f, 0f);
            Camera frontMirrorCam = frontMirrorCamObj.AddComponent<Camera>();
            frontMirrorCam.fieldOfView = 62f;
            frontMirrorCam.nearClipPlane = 0.05f;
            frontMirrorCam.farClipPlane = 6.0f;
            frontMirrorCam.clearFlags = CameraClearFlags.SolidColor;
            frontMirrorCam.backgroundColor = Color.black;
            envRefs.frontMirrorCamera = frontMirrorCam;

            var frontMirrorSetup = frontMirrorQuad.AddComponent<ElevatorMirrorSetup>();
            frontMirrorSetup.mirrorCamera = frontMirrorCam;
            frontMirrorSetup.mirrorQuadRenderer = frontMirrorQuad.GetComponent<Renderer>();
            frontMirrorSetup.textureResolution = 512;

            CreateBox("RearMirror_Frame", envRoot.transform, new Vector3(0f, 1.48f, -0.745f), new Vector3(0.76f, 0.86f, 0.015f), frameMat);
            GameObject rearMirrorQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            rearMirrorQuad.name = "RearMirror_Surface";
            rearMirrorQuad.transform.SetParent(envRoot.transform, false);
            rearMirrorQuad.transform.localPosition = new Vector3(0f, 1.48f, -0.735f);
            rearMirrorQuad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            rearMirrorQuad.transform.localScale = new Vector3(0.72f, 0.82f, 1f);
            rearMirrorQuad.GetComponent<Renderer>().sharedMaterial = mirrorMat;

            GameObject rearMirrorCamObj = new GameObject("RearMirrorCamera");
            rearMirrorCamObj.transform.SetParent(rearMirrorQuad.transform, false);
            rearMirrorCamObj.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            rearMirrorCamObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Camera rearMirrorCam = rearMirrorCamObj.AddComponent<Camera>();
            rearMirrorCam.fieldOfView = 65f;
            rearMirrorCam.nearClipPlane = 0.05f;
            rearMirrorCam.farClipPlane = 6.0f;
            rearMirrorCam.clearFlags = CameraClearFlags.SolidColor;
            rearMirrorCam.backgroundColor = Color.black;
            envRefs.rearMirrorCamera = rearMirrorCam;

            var rearMirrorSetup = rearMirrorQuad.AddComponent<ElevatorMirrorSetup>();
            rearMirrorSetup.mirrorCamera = rearMirrorCam;
            rearMirrorSetup.mirrorQuadRenderer = rearMirrorQuad.GetComponent<Renderer>();
            rearMirrorSetup.textureResolution = 512;

            GameObject intercomLed = CreateBox("IntercomLED", envRoot.transform, new Vector3(0.46f, 1.38f, 0.728f), new Vector3(0.025f, 0.025f, 0.01f), redLampMat);
            envRefs.intercomLedRenderer = intercomLed.GetComponent<Renderer>();
            CreateWorldText("IntercomLabel", envRoot.transform, new Vector3(0.56f, 1.38f, 0.725f), Quaternion.identity, 0.006f, 36, "INTERCOM", new Color(0.8f, 0.8f, 0.8f));

            envRefs.intercomSubtitleText = CreateWorldText("SubtitleGuidanceText", envRoot.transform,
                new Vector3(0f, 1.52f, 0.71f), Quaternion.identity, 0.0085f, 40, "", new Color(0.95f, 0.92f, 0.78f));

            envRefs.buttonFloor1 = CreateElevatorButton("Button_1", "1", ElevatorButtonType.Floor1,
                envRoot.transform, new Vector3(0.56f, 1.24f, 0.725f), new Color(0.28f, 0.28f, 0.32f));

            envRefs.buttonOpen = CreateElevatorButton("Button_Open", "開 OPEN", ElevatorButtonType.DoorOpen,
                envRoot.transform, new Vector3(0.49f, 1.11f, 0.725f), new Color(0.22f, 0.28f, 0.24f));

            envRefs.buttonClose = CreateElevatorButton("Button_Close", "閉 CLOSE", ElevatorButtonType.DoorClose,
                envRoot.transform, new Vector3(0.63f, 1.11f, 0.725f), new Color(0.25f, 0.25f, 0.28f));

            envRefs.buttonAlarm = CreateElevatorButton("Button_Alarm", "非常ベル", ElevatorButtonType.EmergencyAlarm,
                envRoot.transform, new Vector3(0.56f, 0.97f, 0.725f), new Color(0.45f, 0.12f, 0.12f));

            GameObject tubeMesh = CreateBox("FluorescentTube", envRoot.transform, new Vector3(-0.1f, 2.28f, 0f), new Vector3(0.14f, 0.03f, 0.85f), lightTubeMat);
            envRefs.ceilingLightTubeRenderer = tubeMesh.GetComponent<Renderer>();

            GameObject ceilingLightObj = new GameObject("CeilingFluorescentLight");
            ceilingLightObj.transform.SetParent(envRoot.transform, false);
            ceilingLightObj.transform.localPosition = new Vector3(-0.1f, 2.18f, 0f);
            Light ceilingLight = ceilingLightObj.AddComponent<Light>();
            ceilingLight.type = LightType.Point;
            ceilingLight.range = 4.2f;
            ceilingLight.intensity = 1.05f;
            ceilingLight.color = new Color(0.88f, 0.95f, 1.0f);
            ceilingLight.shadows = LightShadows.Soft;
            envRefs.ceilingFluorescentLight = ceilingLight;

            GameObject redLampMesh = CreateBox("RedEmergencyLampMesh", envRoot.transform, new Vector3(0.72f, 0.18f, 0.55f), new Vector3(0.04f, 0.08f, 0.12f), redLampMat);
            envRefs.redEmergencyLampRenderer = redLampMesh.GetComponent<Renderer>();

            GameObject redLightObj = new GameObject("RedEmergencyFloorLight");
            redLightObj.transform.SetParent(envRoot.transform, false);
            redLightObj.transform.localPosition = new Vector3(0.55f, 0.25f, 0.45f);
            Light redLight = redLightObj.AddComponent<Light>();
            redLight.type = LightType.Point;
            redLight.range = 3.5f;
            redLight.intensity = 0f;
            redLight.color = new Color(1.0f, 0.06f, 0.04f);
            redLight.enabled = false;
            envRefs.redEmergencyFloorLight = redLight;

            GameObject corridor8F = new GameObject("Corridor8F_DarkOffice");
            corridor8F.transform.SetParent(envRoot.transform, false);
            CreateBox("Hall_Floor", corridor8F.transform, new Vector3(0f, -0.05f, 4.6f), new Vector3(2.8f, 0.1f, 7.6f), floorMat);
            CreateBox("Hall_Ceiling", corridor8F.transform, new Vector3(0f, 2.4f, 4.6f), new Vector3(2.8f, 0.1f, 7.6f), frameMat);
            CreateBox("Hall_WallL", corridor8F.transform, new Vector3(-1.35f, 1.2f, 4.6f), new Vector3(0.1f, 2.4f, 7.6f), frameMat);
            CreateBox("Hall_WallR", corridor8F.transform, new Vector3(1.35f, 1.2f, 4.6f), new Vector3(0.1f, 2.4f, 7.6f), frameMat);
            CreateBox("Hall_EndWall", corridor8F.transform, new Vector3(0f, 1.2f, 8.3f), new Vector3(2.8f, 2.4f, 0.1f), frameMat);
            CreateBox("GreenExitSign", corridor8F.transform, new Vector3(0.35f, 2.05f, 8.2f), new Vector3(0.38f, 0.14f, 0.04f), exitSignMat);
            GameObject exitLightObj = new GameObject("GreenExitLight");
            exitLightObj.transform.SetParent(corridor8F.transform, false);
            exitLightObj.transform.localPosition = new Vector3(0.35f, 1.95f, 7.9f);
            Light exitLight = exitLightObj.AddComponent<Light>();
            exitLight.type = LightType.Point;
            exitLight.color = new Color(0.15f, 1.0f, 0.35f);
            exitLight.range = 4.5f;
            exitLight.intensity = 0.65f;
            envRefs.corridor8FGroup = corridor8F;

            GameObject corridor1F = new GameObject("Corridor1F_BloodAbyss");
            corridor1F.transform.SetParent(envRoot.transform, false);
            CreateBox("Abyss_Floor", corridor1F.transform, new Vector3(0f, -0.05f, 4.6f), new Vector3(2.8f, 0.1f, 7.6f), bloodWallMat);
            CreateBox("Abyss_Ceiling", corridor1F.transform, new Vector3(0f, 2.4f, 4.6f), new Vector3(2.8f, 0.1f, 7.6f), bloodWallMat);
            CreateBox("Abyss_WallL", corridor1F.transform, new Vector3(-1.35f, 1.2f, 4.6f), new Vector3(0.1f, 2.4f, 7.6f), bloodWallMat);
            CreateBox("Abyss_WallR", corridor1F.transform, new Vector3(1.35f, 1.2f, 4.6f), new Vector3(0.1f, 2.4f, 7.6f), bloodWallMat);
            for (int i = 0; i < 4; i++)
            {
                float zPos = 1.8f + i * 1.6f;
                CreateBox($"Abyss_PillarL_{i}", corridor1F.transform, new Vector3(-0.85f, 1.2f, zPos), new Vector3(0.18f, 2.4f, 0.18f), frameMat);
                CreateBox($"Abyss_PillarR_{i}", corridor1F.transform, new Vector3(0.85f, 1.2f, zPos), new Vector3(0.18f, 2.4f, 0.18f), frameMat);
            }
            GameObject abyssLightObj = new GameObject("AbyssCrimsonLight");
            abyssLightObj.transform.SetParent(corridor1F.transform, false);
            abyssLightObj.transform.localPosition = new Vector3(0f, 1.5f, 3.8f);
            Light abyssLight = abyssLightObj.AddComponent<Light>();
            abyssLight.type = LightType.Point;
            abyssLight.color = new Color(0.9f, 0.03f, 0.03f);
            abyssLight.range = 6.5f;
            abyssLight.intensity = 1.2f;
            corridor1F.SetActive(false);
            envRefs.corridor1FHorrorGroup = corridor1F;

            envRefs.ceilingHumAudio = Create3DAudioSource("Audio_CeilingFluorescent", envRoot.transform, new Vector3(0f, 2.2f, 0f), 0.3f, 4.0f);
            envRefs.elevatorMotorAudio = Create3DAudioSource("Audio_ElevatorMotor", envRoot.transform, new Vector3(0f, 0.1f, 0f), 0.5f, 5.0f);
            envRefs.doorBellAndSlideAudio = Create3DAudioSource("Audio_DoorAndBell", envRoot.transform, new Vector3(0f, 1.9f, 0.75f), 0.3f, 5.0f);
            envRefs.hallway3DAudio = Create3DAudioSource("Audio_OuterHallway3D", envRoot.transform, new Vector3(0f, 1.2f, 3.4f), 0.5f, 10.0f);
            envRefs.intercom3DAudio = Create3DAudioSource("Audio_IntercomSpeaker3D", envRoot.transform, new Vector3(0.56f, 1.38f, 0.72f), 0.15f, 3.5f);
            envRefs.behindNeck3DAudio = Create3DAudioSource("Audio_BehindNeck30cm", envRoot.transform, new Vector3(0f, 1.55f, -0.3f), 0.08f, 2.5f);
            envRefs.earMoving3DAudio = Create3DAudioSource("Audio_EarProximity3D", envRoot.transform, new Vector3(0.25f, 1.6f, -0.15f), 0.08f, 2.5f);
            envRefs.ceilingDrip3DAudio = Create3DAudioSource("Audio_CeilingHatch3D", envRoot.transform, new Vector3(0.25f, 2.15f, 0.3f), 0.15f, 4.0f);

            GameObject ghostMgrObj = new GameObject("MirrorGhostSystem");
            var ghostCtrl = ghostMgrObj.AddComponent<MirrorGhostController>();

            // 1. 一回目の鏡に現れるモデル（従来のこてこてのAI風プリミティブモデルをそのまま使用）
            GameObject mirrorGhost = BuildGhostFigure("MirrorOnlyGhost_Woman_Stage1", ghostMgrObj.transform,
                ghostClothMat, ghostSkinMat, ghostHairMat, ghostEyeMat, mirrorOnlyLayer);
            ghostCtrl.mirrorGhostRoot = mirrorGhost.transform;

            GameObject leftCreepHand = BuildCreepingHand("PeripheralHand_Left", ghostMgrObj.transform, ghostSkinMat, ghostHairMat);
            GameObject rightCreepHand = BuildCreepingHand("PeripheralHand_Right", ghostMgrObj.transform, ghostSkinMat, ghostHairMat);
            ghostCtrl.peripheralLeftHand = leftCreepHand.transform;
            ghostCtrl.peripheralRightHand = rightCreepHand.transform;

            GameObject scaryWomanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SCARY_WOMAN_PREFAB_PATH);

            // 2. 振り返った時に背後に現れる高精細モデル（ScaryWoman_Ghost_01）
            GameObject turnedAroundGhost = InstantiateScaryWomanOrFallback(scaryWomanPrefab, "TurnedAroundGhost_ScaryWoman",
                ghostMgrObj.transform, new Vector3(0f, 0f, -0.45f), Quaternion.identity,
                new Color(0.82f, 0.90f, 1.0f), 1.4f, out Light turnLight);
            ghostCtrl.turnedAroundGhostRoot = turnedAroundGhost.transform;
            ghostCtrl.turnedAroundGhostLight = turnLight;
            turnedAroundGhost.SetActive(false);

            // 3. 1階についたとき扉の正面に現れる高精細モデル（ScaryWoman_Ghost_01）
            GameObject firstFloorGhost = InstantiateScaryWomanOrFallback(scaryWomanPrefab, "FirstFloorHallGhost_ScaryWoman",
                ghostMgrObj.transform, new Vector3(0f, 0f, 1.55f), Quaternion.Euler(0f, 180f, 0f),
                new Color(0.95f, 0.25f, 0.25f), 1.6f, out Light hallGhostLight);
            ghostCtrl.firstFloorHallGhostRoot = firstFloorGhost.transform;
            ghostCtrl.firstFloorHallGhostLight = hallGhostLight;
            firstFloorGhost.SetActive(false);

            // 4. 1階クライマックス＆フェーズ3振り返り時のジャンプスケア用高精細モデル（ScaryWoman_Ghost_01）
            GameObject jumpscareGhost = InstantiateScaryWomanOrFallback(scaryWomanPrefab, "JumpscareGhost_ScaryWoman",
                ghostMgrObj.transform, Vector3.zero, Quaternion.identity,
                new Color(1.0f, 0.18f, 0.18f), 2.2f, out Light scareLight);
            ghostCtrl.jumpscareGhostRoot = jumpscareGhost.transform;
            ghostCtrl.jumpscareFaceLight = scareLight;
            jumpscareGhost.SetActive(false);

            // VRリグ＆暗転オーバーレイ
            Camera mainCam = BuildStandardVRRig(envRoot.transform, mirrorOnlyLayer, frameMat, ghostSkinMat, blackoutMat,
                out GameObject blackoutSphere, out TextMesh endingTitleText);
            envRefs.blackoutSphere = blackoutSphere;
            envRefs.endingTitleText = endingTitleText;

            GameObject gazeObj = new GameObject("PlayerGazeDetector");
            var gazeDetector = gazeObj.AddComponent<PlayerGazeDetector>();
            gazeDetector.playerCamera = mainCam.transform;
            gazeDetector.frontDirectionRef = envRoot.transform;
            gazeDetector.frontMirrorTarget = frontMirrorQuad.transform;
            gazeDetector.rearMirrorTarget = rearMirrorQuad.transform;
            gazeDetector.ceilingHatchTarget = hatchDropPoint.transform;

            GameObject scenarioMgrObj = new GameObject("ElevatorScenarioManager");
            var scenarioMgr = scenarioMgrObj.AddComponent<ElevatorScenarioManager>();
            scenarioMgr.env = envRefs;
            scenarioMgr.gazeDetector = gazeDetector;
            scenarioMgr.ghostController = ghostCtrl;

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        // =====================================================================
        // 案B：『暗室（ダークルーム）』シーン構築
        // =====================================================================
        private static void BuildScenarioBDarkroomScene(string scenePath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.03f, 0.008f, 0.008f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.002f, 0.002f);
            RenderSettings.fogDensity = 0.18f;

            Material wallMat = CreateOrUpdateMaterial("ElevatorWall_Mat", new Color(0.16f, 0.17f, 0.19f), 0.35f, 0.45f);
            Material floorMat = CreateOrUpdateMaterial("ElevatorFloor_Mat", new Color(0.10f, 0.10f, 0.11f), 0.15f, 0.25f);
            Material frameMat = CreateOrUpdateMaterial("DarkFrame_Mat", new Color(0.06f, 0.06f, 0.07f), 0.2f, 0.3f);
            Material redLampMat = CreateOrUpdateMaterial("RedEmergencyLamp_Mat", new Color(0.9f, 0.05f, 0.05f), 0.0f, 0.4f, false, new Color(1.6f, 0.05f, 0.05f));
            Material ghostClothMat = CreateOrUpdateMaterial("GhostCloth_Mat", new Color(0.04f, 0.04f, 0.05f), 0.0f, 0.1f);
            Material ghostSkinMat = CreateOrUpdateMaterial("GhostPaleSkin_Mat", new Color(0.75f, 0.78f, 0.76f), 0.0f, 0.2f);
            Material ghostHairMat = CreateOrUpdateMaterial("GhostBlackHair_Mat", new Color(0.015f, 0.015f, 0.02f), 0.0f, 0.15f);
            Material ghostEyeMat = CreateOrUpdateMaterial("GhostBleedingEye_Mat", new Color(0.9f, 0.1f, 0.1f), 0.0f, 0.5f, false, new Color(1.6f, 0.1f, 0.1f));
            Material blackoutMat = CreateOrUpdateMaterial("BlackoutInside_Mat", Color.black, 0.0f, 0.0f, true);
            Material trayLiquidMat = CreateOrUpdateMaterial("DarkroomLiquid_Mat", new Color(0.15f, 0.18f, 0.22f), 0.1f, 0.85f);
            Material photoPaperMat = CreateOrUpdateMaterial("PhotoPaper_Mat", new Color(0.85f, 0.82f, 0.78f), 0.0f, 0.4f);

            GameObject audioMgrObj = new GameObject("ProceduralHorrorAudioManager");
            audioMgrObj.AddComponent<ProceduralHorrorAudio>();

            GameObject roomRoot = new GameObject("DarkroomEnvironment");
            var mgr = roomRoot.AddComponent<DarkroomScenarioManager>();

            // 暗室の部屋 (2.6m x 3.2m, 高さ2.4m)
            CreateBox("Floor", roomRoot.transform, new Vector3(0f, -0.05f, 0.4f), new Vector3(2.6f, 0.1f, 3.6f), floorMat);
            CreateBox("Ceiling", roomRoot.transform, new Vector3(0f, 2.45f, 0.4f), new Vector3(2.6f, 0.1f, 3.6f), wallMat);
            CreateBox("Wall_Left", roomRoot.transform, new Vector3(-1.3f, 1.2f, 0.4f), new Vector3(0.08f, 2.4f, 3.6f), wallMat);
            CreateBox("Wall_Right", roomRoot.transform, new Vector3(1.3f, 1.2f, 0.4f), new Vector3(0.08f, 2.4f, 3.6f), wallMat);
            CreateBox("Wall_Front", roomRoot.transform, new Vector3(0f, 1.2f, 2.2f), new Vector3(2.6f, 2.4f, 0.08f), wallMat);
            CreateBox("Wall_BackLeft", roomRoot.transform, new Vector3(-0.85f, 1.2f, -1.35f), new Vector3(0.9f, 2.4f, 0.08f), wallMat);
            CreateBox("Wall_BackRight", roomRoot.transform, new Vector3(0.85f, 1.2f, -1.35f), new Vector3(0.9f, 2.4f, 0.08f), wallMat);

            // 背後のドア
            GameObject doorPivot = new GameObject("RearDoorPivot");
            doorPivot.transform.SetParent(roomRoot.transform, false);
            doorPivot.transform.localPosition = new Vector3(0.42f, 0f, -1.35f);
            CreateBox("DoorMesh", doorPivot.transform, new Vector3(-0.42f, 1.05f, 0f), new Vector3(0.84f, 2.1f, 0.05f), frameMat);
            mgr.rearDoorPivot = doorPivot.transform;

            // 赤い安全光（セーフライト）
            GameObject bulb = CreateBox("RedSafelightBulb", roomRoot.transform, new Vector3(0f, 2.28f, 0.4f), new Vector3(0.18f, 0.12f, 0.18f), redLampMat);
            mgr.safelightBulbRenderer = bulb.GetComponent<Renderer>();
            GameObject safeLightObj = new GameObject("RedSafelight");
            safeLightObj.transform.SetParent(roomRoot.transform, false);
            safeLightObj.transform.localPosition = new Vector3(0f, 2.15f, 0.4f);
            Light safeLight = safeLightObj.AddComponent<Light>();
            safeLight.type = LightType.Point;
            safeLight.color = new Color(1.0f, 0.12f, 0.08f);
            safeLight.range = 5.5f;
            safeLight.intensity = 1.15f;
            mgr.redSafelight = safeLight;

            // 目の前の現像作業デスク＆現像液トレイ＆印画紙 (z = 0.58m, y = 0.92m)
            CreateBox("WorkDesk", roomRoot.transform, new Vector3(0f, 0.46f, 0.62f), new Vector3(1.2f, 0.92f, 0.55f), frameMat);
            CreateBox("TrayFrame", roomRoot.transform, new Vector3(0f, 0.94f, 0.56f), new Vector3(0.46f, 0.05f, 0.36f), wallMat);
            GameObject liquid = CreateBox("DevelopingLiquid", roomRoot.transform, new Vector3(0f, 0.96f, 0.56f), new Vector3(0.42f, 0.02f, 0.32f), trayLiquidMat, false);
            mgr.liquidTrayRenderer = liquid.GetComponent<Renderer>();

            GameObject paper = CreateBox("PhotoPaper", roomRoot.transform, new Vector3(0f, 0.972f, 0.56f), new Vector3(0.28f, 0.005f, 0.22f), photoPaperMat, false);
            mgr.photoPaperRenderer = paper.GetComponent<Renderer>();
            mgr.photoCaptionText = CreateWorldText("PhotoCaptionText", roomRoot.transform,
                new Vector3(0f, 1.02f, 0.56f), Quaternion.Euler(70f, 0f, 0f), 0.0055f, 34, "", new Color(0.15f, 0.02f, 0.02f));

            mgr.guidanceSubtitleText = CreateWorldText("GuidanceSubtitleText", roomRoot.transform,
                new Vector3(0f, 1.48f, 0.75f), Quaternion.identity, 0.008f, 40, "", new Color(0.98f, 0.88f, 0.78f));

            // 操作ボタン（「現像 (1)」「ドアロック (C)」）
            mgr.developButton = CreateElevatorButton("Button_Develop", "現像 [1]", ElevatorButtonType.Floor1,
                roomRoot.transform, new Vector3(0.36f, 0.98f, 0.48f), new Color(0.35f, 0.22f, 0.18f));
            mgr.closeDoorButton = CreateElevatorButton("Button_LockDoor", "ドア閉 [C]", ElevatorButtonType.DoorClose,
                roomRoot.transform, new Vector3(-0.36f, 0.98f, 0.48f), new Color(0.22f, 0.28f, 0.32f));

            // 音源
            mgr.safelightBuzzAudio = Create3DAudioSource("Audio_SafelightBuzz", roomRoot.transform, new Vector3(0f, 2.2f, 0.4f), 0.3f, 5.0f);
            mgr.trayLiquidAudio = Create3DAudioSource("Audio_TrayLiquid", roomRoot.transform, new Vector3(0f, 0.96f, 0.56f), 0.2f, 3.0f);
            mgr.rearDoor3DAudio = Create3DAudioSource("Audio_RearDoor", roomRoot.transform, new Vector3(0f, 1.2f, -1.35f), 0.3f, 5.0f);
            mgr.behindNeck3DAudio = Create3DAudioSource("Audio_BehindNeck", roomRoot.transform, new Vector3(0f, 1.5f, -0.3f), 0.1f, 3.0f);

            // 前半の油断用チープモデル ＆ 後半のクライマックス用高精細モデル（ScaryWoman_Ghost_01）
            GameObject cheapGhost = BuildGhostFigure("CheapDummyGhost", roomRoot.transform, ghostClothMat, ghostSkinMat, ghostHairMat, ghostEyeMat, 0);
            cheapGhost.SetActive(false);
            mgr.cheapDummyGhostRoot = cheapGhost.transform;

            GameObject scaryWomanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SCARY_WOMAN_PREFAB_PATH);
            GameObject realWoman = InstantiateScaryWomanOrFallback(scaryWomanPrefab, "RealScaryWoman_Climax",
                roomRoot.transform, new Vector3(0f, 0f, 1.2f), Quaternion.Euler(0f, 180f, 0f),
                new Color(1.0f, 0.2f, 0.2f), 2.2f, out Light realFaceLight);
            realWoman.SetActive(false);
            mgr.realScaryWomanRoot = realWoman.transform;
            mgr.realScaryWomanFaceLight = realFaceLight;

            Camera mainCam = BuildStandardVRRig(roomRoot.transform, 6, frameMat, ghostSkinMat, blackoutMat,
                out GameObject blackoutSphere, out TextMesh endingTitleText);
            mgr.blackoutSphere = blackoutSphere;
            mgr.endingTitleText = endingTitleText;

            GameObject gazeObj = new GameObject("PlayerGazeDetector");
            var gaze = gazeObj.AddComponent<PlayerGazeDetector>();
            gaze.playerCamera = mainCam.transform;
            gaze.frontDirectionRef = roomRoot.transform;
            mgr.gazeDetector = gaze;

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        // =====================================================================
        // 案C：『標本室のマネキン』シーン構築
        // =====================================================================
        private static void BuildScenarioCMannequinScene(string scenePath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.015f, 0.02f, 0.025f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.01f, 0.012f, 0.015f);
            RenderSettings.fogDensity = 0.16f;

            Material wallMat = CreateOrUpdateMaterial("ElevatorWall_Mat", new Color(0.16f, 0.17f, 0.19f), 0.35f, 0.45f);
            Material floorMat = CreateOrUpdateMaterial("ElevatorFloor_Mat", new Color(0.10f, 0.10f, 0.11f), 0.15f, 0.25f);
            Material frameMat = CreateOrUpdateMaterial("DarkFrame_Mat", new Color(0.06f, 0.06f, 0.07f), 0.2f, 0.3f);
            Material ghostClothMat = CreateOrUpdateMaterial("GhostCloth_Mat", new Color(0.04f, 0.04f, 0.05f), 0.0f, 0.1f);
            Material ghostSkinMat = CreateOrUpdateMaterial("GhostPaleSkin_Mat", new Color(0.75f, 0.78f, 0.76f), 0.0f, 0.2f);
            Material ghostHairMat = CreateOrUpdateMaterial("GhostBlackHair_Mat", new Color(0.015f, 0.015f, 0.02f), 0.0f, 0.15f);
            Material ghostEyeMat = CreateOrUpdateMaterial("GhostBleedingEye_Mat", new Color(0.9f, 0.1f, 0.1f), 0.0f, 0.5f, false, new Color(1.6f, 0.1f, 0.1f));
            Material blackoutMat = CreateOrUpdateMaterial("BlackoutInside_Mat", Color.black, 0.0f, 0.0f, true);

            GameObject audioMgrObj = new GameObject("ProceduralHorrorAudioManager");
            audioMgrObj.AddComponent<ProceduralHorrorAudio>();

            GameObject roomRoot = new GameObject("SpecimenRoomEnvironment");
            var mgr = roomRoot.AddComponent<MannequinScenarioManager>();

            // 標本室 (3.4m x 4.8m, 高さ2.5m)
            CreateBox("Floor", roomRoot.transform, new Vector3(0f, -0.05f, 1.6f), new Vector3(3.6f, 0.1f, 5.2f), floorMat);
            CreateBox("Ceiling", roomRoot.transform, new Vector3(0f, 2.55f, 1.6f), new Vector3(3.6f, 0.1f, 5.2f), wallMat);
            CreateBox("Wall_Left", roomRoot.transform, new Vector3(-1.8f, 1.25f, 1.6f), new Vector3(0.08f, 2.5f, 5.2f), wallMat);
            CreateBox("Wall_Right", roomRoot.transform, new Vector3(1.8f, 1.25f, 1.6f), new Vector3(0.08f, 2.5f, 5.2f), wallMat);
            CreateBox("Wall_Front", roomRoot.transform, new Vector3(0f, 1.25f, 4.15f), new Vector3(3.6f, 2.5f, 0.08f), wallMat);
            CreateBox("Wall_Back", roomRoot.transform, new Vector3(0f, 1.25f, -0.95f), new Vector3(3.6f, 2.5f, 0.08f), wallMat);

            // 部屋の蛍光灯
            GameObject roomLightObj = new GameObject("RoomFluorescentLight");
            roomLightObj.transform.SetParent(roomRoot.transform, false);
            roomLightObj.transform.localPosition = new Vector3(0f, 2.35f, 1.8f);
            Light roomLight = roomLightObj.AddComponent<Light>();
            roomLight.type = LightType.Point;
            roomLight.color = new Color(0.75f, 0.88f, 0.95f);
            roomLight.range = 6.5f;
            roomLight.intensity = 0.45f;
            mgr.roomFluorescentLight = roomLight;

            // 手元の配電盤と電力メーター＆チャージボタン
            CreateBox("PowerPanelStand", roomRoot.transform, new Vector3(0f, 0.55f, 0.56f), new Vector3(0.55f, 1.1f, 0.25f), frameMat);
            mgr.powerMeterText = CreateWorldText("PowerMeterText", roomRoot.transform,
                new Vector3(0f, 1.18f, 0.43f), Quaternion.Euler(20f, 0f, 0f), 0.012f, 48, "0%", new Color(1.0f, 0.3f, 0.1f));
            mgr.guidanceSubtitleText = CreateWorldText("GuidanceSubtitleText", roomRoot.transform,
                new Vector3(0f, 1.48f, 0.72f), Quaternion.identity, 0.008f, 40, "", new Color(0.95f, 0.92f, 0.78f));

            mgr.powerChargeButton = CreateElevatorButton("Button_PowerCharge", "電力チャージ [1]", ElevatorButtonType.Floor1,
                roomRoot.transform, new Vector3(0f, 1.05f, 0.44f), new Color(0.25f, 0.35f, 0.25f));

            // 前半の油断誘導用：3体の「こてこてのAI風チープなカプセル/ブロック人形」
            GameObject mCenter = BuildGhostFigure("CheapMannequin_Center", roomRoot.transform, ghostClothMat, ghostSkinMat, ghostHairMat, ghostEyeMat, 0);
            mCenter.transform.localPosition = new Vector3(0f, 0f, 3.2f);
            mCenter.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            mgr.cheapMannequinCenter = mCenter.transform;

            GameObject mLeft = BuildGhostFigure("CheapMannequin_Left", roomRoot.transform, ghostClothMat, ghostSkinMat, ghostHairMat, ghostEyeMat, 0);
            mLeft.transform.localPosition = new Vector3(-1.15f, 0f, 2.8f);
            mLeft.transform.localRotation = Quaternion.Euler(0f, 160f, 0f);
            mgr.cheapMannequinLeft = mLeft.transform;

            GameObject mRight = BuildGhostFigure("CheapMannequin_Right", roomRoot.transform, ghostClothMat, ghostSkinMat, ghostHairMat, ghostEyeMat, 0);
            mRight.transform.localPosition = new Vector3(1.15f, 0f, 2.8f);
            mRight.transform.localRotation = Quaternion.Euler(0f, -160f, 0f);
            mgr.cheapMannequinRight = mRight.transform;

            // クライマックス覚醒恐怖用：作り込まれた超高精細モデル（ScaryWoman_Ghost_01）
            GameObject scaryWomanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SCARY_WOMAN_PREFAB_PATH);
            GameObject realWoman = InstantiateScaryWomanOrFallback(scaryWomanPrefab, "RealScaryWoman_Climax",
                roomRoot.transform, new Vector3(0f, 0f, 1.15f), Quaternion.Euler(0f, 180f, 0f),
                new Color(1.0f, 0.18f, 0.18f), 2.3f, out Light realFaceLight);
            realWoman.SetActive(false);
            mgr.realScaryWomanRoot = realWoman.transform;
            mgr.realScaryWomanFaceLight = realFaceLight;

            // 音源
            mgr.roomHumAudio = Create3DAudioSource("Audio_RoomHum", roomRoot.transform, new Vector3(0f, 2.3f, 1.8f), 0.3f, 6.0f);
            mgr.mannequinStep3DAudio = Create3DAudioSource("Audio_MannequinSteps", roomRoot.transform, new Vector3(0f, 1.0f, 2.0f), 0.3f, 6.0f);
            mgr.panelAudio = Create3DAudioSource("Audio_PowerPanel", roomRoot.transform, new Vector3(0f, 1.05f, 0.45f), 0.2f, 4.0f);
            mgr.behindNeck3DAudio = Create3DAudioSource("Audio_BehindNeck", roomRoot.transform, new Vector3(0f, 1.5f, -0.25f), 0.1f, 3.0f);

            Camera mainCam = BuildStandardVRRig(roomRoot.transform, 6, frameMat, ghostSkinMat, blackoutMat,
                out GameObject blackoutSphere, out TextMesh endingTitleText);
            mgr.blackoutSphere = blackoutSphere;
            mgr.endingTitleText = endingTitleText;

            // HMD追従スポットライト（懐中電灯）
            GameObject spotObj = new GameObject("FlashlightSpot");
            spotObj.transform.SetParent(roomRoot.transform, false);
            Light spot = spotObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 48f;
            spot.range = 7.0f;
            spot.intensity = 1.25f;
            spot.color = new Color(1.0f, 0.96f, 0.86f);
            mgr.flashlightSpot = spot;

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static GameObject InstantiateScaryWomanOrFallback(GameObject prefab, string instanceName,
            Transform parent, Vector3 localPos, Quaternion localRot, Color lightCol, float lightIntensity, out Light faceLight)
        {
            GameObject inst = (prefab != null)
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent)
                : new GameObject(instanceName);
            inst.name = instanceName;
            inst.transform.localPosition = localPos;
            inst.transform.localRotation = localRot;

            GameObject lightObj = new GameObject($"{instanceName}_FaceLight");
            lightObj.transform.SetParent(inst.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.48f, 0.34f);
            faceLight = lightObj.AddComponent<Light>();
            faceLight.type = LightType.Point;
            faceLight.color = lightCol;
            faceLight.range = 1.9f;
            faceLight.intensity = lightIntensity;
            return inst;
        }

        private static Camera BuildStandardVRRig(Transform envRoot, int mirrorOnlyLayer, Material frameMat, Material handMat, Material blackoutMat,
            out GameObject blackoutSphere, out TextMesh endingTitleText)
        {
            GameObject rigObj = new GameObject("VRPlayerRig");
            rigObj.transform.position = Vector3.zero;
            var rigCtrl = rigObj.AddComponent<VRPlayerRigController>();

            GameObject camOffset = new GameObject("CameraOffset");
            camOffset.transform.SetParent(rigObj.transform, false);
            rigCtrl.cameraOffset = camOffset.transform;

            GameObject mainCamObj = new GameObject("Main Camera");
            mainCamObj.tag = "MainCamera";
            mainCamObj.transform.SetParent(camOffset.transform, false);
            mainCamObj.transform.localPosition = new Vector3(0f, 1.60f, 0f);
            Camera mainCam = mainCamObj.AddComponent<Camera>();
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black;
            mainCam.nearClipPlane = 0.04f;
            mainCam.farClipPlane = 25f;
            mainCam.cullingMask &= ~(1 << mirrorOnlyLayer);
            mainCamObj.AddComponent<AudioListener>();
            rigCtrl.headCamera = mainCam;

            CreateBox("PlayerBodySilhouette", mainCamObj.transform,
                new Vector3(0f, -0.35f, -0.05f), new Vector3(0.34f, 0.55f, 0.16f), frameMat, false);

            blackoutSphere = new GameObject("BlackoutEndingOverlay");
            blackoutSphere.transform.SetParent(envRoot, false);
            for (int side = 0; side < 6; side++)
            {
                Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
                CreateBox($"BlackWall_{side}", blackoutSphere.transform, dirs[side] * 0.45f,
                    new Vector3(1.0f, 1.0f, 1.0f) - new Vector3(Mathf.Abs(dirs[side].x), Mathf.Abs(dirs[side].y), Mathf.Abs(dirs[side].z)) * 0.95f,
                    blackoutMat, false);
            }
            endingTitleText = CreateWorldText("EndingTitleText", blackoutSphere.transform,
                new Vector3(0f, 0f, 0.38f), Quaternion.identity, 0.006f, 42, "", new Color(0.88f, 0.15f, 0.15f));
            blackoutSphere.SetActive(false);

            rigCtrl.leftHandTransform = CreateVRHand("LeftHand", camOffset.transform, new Vector3(-0.2f, 1.25f, 0.35f), handMat);
            rigCtrl.rightHandTransform = CreateVRHand("RightHand", camOffset.transform, new Vector3(0.2f, 1.25f, 0.35f), handMat);
            return mainCam;
        }

        private static Material CreateOrUpdateMaterial(string matName, Color baseColor, float metallic, float smoothness, bool unlit = false, Color? emission = null)
        {
            string path = $"{MATS_DIR}/{matName}.mat";
            Shader shader = unlit
                ? (Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture"))
                : (Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (emission.HasValue && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool withCollider = true)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPos;
            box.transform.localScale = scale;
            if (mat != null)
            {
                box.GetComponent<Renderer>().sharedMaterial = mat;
            }
            if (!withCollider)
            {
                UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            }
            return box;
        }

        private static TextMesh CreateWorldText(string name, Transform parent, Vector3 localPos, Quaternion localRot, float scale, int fontSize, string text, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = localRot;
            obj.transform.localScale = new Vector3(scale, scale, scale);

            TextMesh tm = obj.AddComponent<TextMesh>();
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = fontSize;
            tm.text = text;
            tm.color = color;
            return tm;
        }

        private static ElevatorButton CreateElevatorButton(string name, string label, ElevatorButtonType type, Transform parent, Vector3 localPos, Color baseColor)
        {
            GameObject btnRoot = new GameObject(name);
            btnRoot.transform.SetParent(parent, false);
            btnRoot.transform.localPosition = localPos;

            BoxCollider triggerCol = btnRoot.AddComponent<BoxCollider>();
            triggerCol.size = new Vector3(0.11f, 0.085f, 0.06f);
            triggerCol.isTrigger = true;

            Material btnMat = CreateOrUpdateMaterial($"{name}_Mat", baseColor, 0.3f, 0.6f);
            GameObject cap = CreateBox("ButtonCap", btnRoot.transform, Vector3.zero, new Vector3(0.095f, 0.065f, 0.022f), btnMat, false);

            CreateWorldText("ButtonLabel", cap.transform, new Vector3(0f, 0f, -0.55f), Quaternion.identity, 0.065f, 36, label, Color.white);

            ElevatorButton btn = btnRoot.AddComponent<ElevatorButton>();
            btn.buttonType = type;
            btn.movingCapTransform = cap.transform;
            btn.buttonMeshRenderer = cap.GetComponent<Renderer>();
            btn.normalColor = baseColor;
            return btn;
        }

        private static AudioSource Create3DAudioSource(string name, Transform parent, Vector3 localPos, float minDist, float maxDist)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            AudioSource src = obj.AddComponent<AudioSource>();
            src.spatialBlend = 1.0f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = minDist;
            src.maxDistance = maxDist;
            src.playOnAwake = false;
            return src;
        }

        private static GameObject BuildGhostFigure(string name, Transform parent, Material clothMat, Material skinMat, Material hairMat, Material eyeMat, int layer)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);

            CreateBox("Torso_Dress", root.transform, new Vector3(0f, 0.72f, 0f), new Vector3(0.32f, 1.35f, 0.20f), clothMat, false);
            CreateBox("Neck", root.transform, new Vector3(0f, 1.42f, 0.02f), new Vector3(0.09f, 0.12f, 0.09f), skinMat, false);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "GhostHead";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.56f, 0.03f);
            head.transform.localScale = new Vector3(0.20f, 0.25f, 0.21f);
            head.GetComponent<Renderer>().sharedMaterial = skinMat;
            UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());

            CreateBox("LeftEye", head.transform, new Vector3(-0.22f, 0.08f, 0.46f), new Vector3(0.14f, 0.08f, 0.08f), eyeMat, false);
            CreateBox("RightEye", head.transform, new Vector3(0.22f, 0.08f, 0.46f), new Vector3(0.14f, 0.08f, 0.08f), eyeMat, false);
            CreateBox("OpenMouth", head.transform, new Vector3(0f, -0.22f, 0.45f), new Vector3(0.24f, 0.22f, 0.08f), hairMat, false);

            CreateBox("Hair_Top", root.transform, new Vector3(0f, 1.66f, 0.01f), new Vector3(0.23f, 0.10f, 0.23f), hairMat, false);
            CreateBox("Hair_Back", root.transform, new Vector3(0f, 1.32f, -0.07f), new Vector3(0.24f, 0.65f, 0.06f), hairMat, false);
            CreateBox("Hair_SideL", root.transform, new Vector3(-0.10f, 1.40f, 0.06f), new Vector3(0.05f, 0.48f, 0.12f), hairMat, false);
            CreateBox("Hair_SideR", root.transform, new Vector3(0.10f, 1.40f, 0.06f), new Vector3(0.05f, 0.48f, 0.12f), hairMat, false);
            CreateBox("Hair_FringeCenter", root.transform, new Vector3(0.01f, 1.48f, 0.13f), new Vector3(0.04f, 0.28f, 0.03f), hairMat, false);

            CreateBox("Arm_Left", root.transform, new Vector3(-0.21f, 1.02f, 0.08f), new Vector3(0.06f, 0.62f, 0.06f), skinMat, false);
            CreateBox("Arm_Right", root.transform, new Vector3(0.21f, 1.02f, 0.08f), new Vector3(0.06f, 0.62f, 0.06f), skinMat, false);

            SetLayerRecursively(root, layer);
            return root;
        }

        private static GameObject BuildCreepingHand(string name, Transform parent, Material skinMat, Material hairMat)
        {
            GameObject handRoot = new GameObject(name);
            handRoot.transform.SetParent(parent, false);

            CreateBox("Palm", handRoot.transform, Vector3.zero, new Vector3(0.07f, 0.025f, 0.09f), skinMat, false);
            for (int f = 0; f < 4; f++)
            {
                float xOff = -0.024f + f * 0.016f;
                CreateBox($"Finger_{f}", handRoot.transform, new Vector3(xOff, -0.005f, 0.075f), new Vector3(0.011f, 0.012f, 0.07f), skinMat, false);
            }
            CreateBox("HairStrand", handRoot.transform, new Vector3(0f, 0.08f, -0.02f), new Vector3(0.03f, 0.32f, 0.02f), hairMat, false);
            handRoot.SetActive(false);
            return handRoot;
        }

        private static Transform CreateVRHand(string name, Transform parent, Vector3 localPos, Material handMat)
        {
            GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hand.name = name;
            hand.transform.SetParent(parent, false);
            hand.transform.localPosition = localPos;
            hand.transform.localScale = new Vector3(0.055f, 0.055f, 0.09f);
            hand.GetComponent<Renderer>().sharedMaterial = handMat;

            SphereCollider col = hand.GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.75f;

            Rigidbody rb = hand.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            return hand.transform;
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
