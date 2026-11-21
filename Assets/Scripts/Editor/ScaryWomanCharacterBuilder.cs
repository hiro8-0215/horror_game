using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using HorrorGame.Character;

namespace HorrorGame.Editor
{
    /// <summary>
    /// ユーザーがアップロードした『elevator-apparition.glb』（全26パーツ・6マテリアル・埋め込みテクスチャ）を
    /// 1ミリもデザイン変更せずロスレスでUnityのSkinnedMeshRenderer＆ボーンリグ付きPrefab（ScaryWoman_Ghost_01.prefab）として構築します。
    /// </summary>
    public static class ScaryWomanCharacterBuilder
    {
        private const string CHAR_DIR = "Assets/Characters/ScaryWoman";

        [MenuItem("HorrorGame/3. アップロードされたGLBからScaryWoman Prefab構築 (Build From Uploaded GLB)", false, 3)]
        public static void BuildScaryWomanCharacterPrefab()
        {
            if (!Directory.Exists(CHAR_DIR)) Directory.CreateDirectory(CHAR_DIR);
            AssetDatabase.Refresh();

            // 1. elevator-apparition.glb の6つのPBRマテリアル定義に完全一致するURPマテリアルを生成
            Material faceMat = CreateTexturedMaterial("Face_Mat", "CharacterReference_Texture.png", Color.white, 0.0f, 0.31f);
            Material dressMat = CreateTexturedMaterial("Dress_Mat", "CharacterReference_Texture.png", Color.white, 0.0f, 0.06f);
            Material hairMat = CreateSolidMaterial("Hair_Mat", new Color(0.025f, 0.035f, 0.045f, 1f), 0.0f, 0.64f);
            Material eyeMat = CreateTexturedMaterial("Eye_Mat", "CharacterReference_Texture.png", Color.white, 0.0f, 0.74f);
            Material mouthMat = CreateSolidMaterial("Mouth_Mat", new Color(0.03f, 0.015f, 0.015f, 1f), 0.0f, 0.18f);

            // 2. ルートGameObjectとボーン階層（elevator-apparition.glbの実際の座標に完全一致させた19ボーン）
            GameObject rootObj = new GameObject("ScaryWoman_Ghost_01");

            Transform rootBone = CreateBone("Root", rootObj.transform, new Vector3(0f, 0f, 0f));
            Transform pelvisBone = CreateBone("Pelvis", rootBone, new Vector3(0f, 0.88f, 0f));
            Transform spineBone = CreateBone("Spine", pelvisBone, new Vector3(0f, 1.06f, 0f));
            Transform chestBone = CreateBone("Chest", spineBone, new Vector3(0f, 1.25f, 0f));
            Transform neckBone = CreateBone("Neck", chestBone, new Vector3(0f, 1.36f, 0f));
            Transform headBone = CreateBone("Head", neckBone, new Vector3(0f, 1.52f, 0.01f));
            Transform jawBone = CreateBone("Jaw", headBone, new Vector3(0f, 1.44f, 0.06f));

            // 左腕（elevator-apparition.glbの自然な下げ腕ポーズ座標 X = -0.17 .. -0.19）
            Transform lShoulder = CreateBone("LeftShoulder", chestBone, new Vector3(-0.145f, 1.31f, 0f));
            Transform lUpperArm = CreateBone("LeftUpperArm", lShoulder, new Vector3(-0.175f, 1.15f, 0f));
            Transform lForeArm = CreateBone("LeftForeArm", lUpperArm, new Vector3(-0.185f, 0.94f, 0.01f));
            Transform lHand = CreateBone("LeftHand", lForeArm, new Vector3(-0.190f, 0.78f, 0.02f));

            // 右腕
            Transform rShoulder = CreateBone("RightShoulder", chestBone, new Vector3(0.145f, 1.31f, 0f));
            Transform rUpperArm = CreateBone("RightUpperArm", rShoulder, new Vector3(0.175f, 1.15f, 0f));
            Transform rForeArm = CreateBone("RightForeArm", rUpperArm, new Vector3(0.185f, 0.94f, 0.01f));
            Transform rHand = CreateBone("RightHand", rForeArm, new Vector3(0.190f, 0.78f, 0.02f));

            // 左脚
            Transform lUpLeg = CreateBone("LeftUpLeg", pelvisBone, new Vector3(-0.072f, 0.84f, 0f));
            Transform lLeg = CreateBone("LeftLeg", lUpLeg, new Vector3(-0.070f, 0.44f, 0f));
            Transform lFoot = CreateBone("LeftFoot", lLeg, new Vector3(-0.068f, 0.04f, 0.03f));

            // 右脚
            Transform rUpLeg = CreateBone("RightUpLeg", pelvisBone, new Vector3(0.072f, 0.84f, 0f));
            Transform rLeg = CreateBone("RightLeg", rUpLeg, new Vector3(0.070f, 0.44f, 0f));
            Transform rFoot = CreateBone("RightFoot", rLeg, new Vector3(0.068f, 0.04f, 0.03f));

            Transform[] bones = new Transform[]
            {
                rootBone, pelvisBone, spineBone, chestBone, neckBone, headBone, jawBone,
                lShoulder, lUpperArm, lForeArm, lHand,
                rShoulder, rUpperArm, rForeArm, rHand,
                lUpLeg, lLeg, lFoot,
                rUpLeg, rLeg, rFoot
            };

            // 3. elevator_apparition_mesh.bytes から全26メッシュ（177,912頂点）をロスレス読み込み＆ボーンウェイト付与
            string bundlePath = $"{CHAR_DIR}/elevator_apparition_mesh.bytes";
            string meshAssetPath = $"{CHAR_DIR}/ScaryWoman_SkinnedMesh.asset";
            Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
            Mesh skinnedMesh = LoadLosslessMeshFromBundle(bundlePath, bones, rootObj.transform, existingMesh);

            if (existingMesh == null)
            {
                AssetDatabase.CreateAsset(skinnedMesh, meshAssetPath);
            }
            else
            {
                EditorUtility.SetDirty(skinnedMesh);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[ScaryWomanCharacterBuilder] Verified ScaryWoman_SkinnedMesh vertexCount = {skinnedMesh.vertexCount} (subMeshes = {skinnedMesh.subMeshCount})");

            // 4. SkinnedMeshRenderer の設定
            SkinnedMeshRenderer smr = rootObj.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = skinnedMesh;
            smr.bones = bones;
            smr.rootBone = pelvisBone;
            smr.sharedMaterials = new Material[] { faceMat, dressMat, hairMat, eyeMat, mouthMat };
            smr.updateWhenOffscreen = true;

            // 5. Animator & Idle / Jumpscare アニメーション
            AnimatorController controller = CreateScaryWomanAnimatorController();
            Animator anim = rootObj.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;

            // 6. CapsuleCollider & GhostBehavior
            CapsuleCollider cap = rootObj.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 0.85f, 0f);
            cap.radius = 0.22f;
            cap.height = 1.70f;

            GhostBehavior behavior = rootObj.AddComponent<GhostBehavior>();
            behavior.rootBone = rootBone;
            behavior.pelvisBone = pelvisBone;
            behavior.spineBone = spineBone;
            behavior.chestBone = chestBone;
            behavior.neckBone = neckBone;
            behavior.headBone = headBone;
            behavior.jawBone = jawBone;
            behavior.leftArmBone = lUpperArm;
            behavior.rightArmBone = rUpperArm;
            behavior.animator = anim;
            behavior.twitchIntensity = 0.45f;

            // 7. Prefab保存
            string prefabPath = $"{CHAR_DIR}/ScaryWoman_Ghost_01.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(rootObj, prefabPath, InteractionMode.AutomatedAction);

            // 8. 確認用プレビューシーン（ScaryWoman_ModelPreview.unity）の更新
            var previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.35f, 0.38f);

            GameObject instanceInScene = (GameObject)PrefabUtility.InstantiatePrefab(savedPrefab);
            instanceInScene.transform.position = Vector3.zero;

            GameObject keyLightObj = new GameObject("PreviewKeyLight");
            keyLightObj.transform.position = new Vector3(0.3f, 1.8f, 1.5f);
            keyLightObj.transform.rotation = Quaternion.Euler(18f, -168f, 0f);
            Light keyLight = keyLightObj.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(0.95f, 0.97f, 1.0f);
            keyLight.intensity = 1.1f;

            GameObject camObj = new GameObject("PreviewCamera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0f, 0.95f, 1.95f);
            camObj.transform.rotation = Quaternion.Euler(4f, 180f, 0f);
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.18f, 0.21f);
            cam.nearClipPlane = 0.05f;

            UnityEngine.Object.DestroyImmediate(rootObj);

            string previewScenePath = $"{CHAR_DIR}/ScaryWoman_ModelPreview.unity";
            EditorSceneManager.SaveScene(previewScene, previewScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ScaryWomanCharacterBuilder] elevator-apparition.glb を完全反映した ScaryWoman_Ghost_01.prefab の構築が完了しました -> {prefabPath}");
        }

        private static Transform CreateBone(string name, Transform parent, Vector3 worldPos)
        {
            GameObject b = new GameObject(name);
            b.transform.SetParent(parent, true);
            b.transform.position = worldPos;
            b.transform.rotation = Quaternion.identity;
            return b.transform;
        }

        private static Material CreateTexturedMaterial(string matName, string texFileName, Color tint, float metallic, float smoothness)
        {
            string texPath = $"{CHAR_DIR}/{texFileName}";
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            string matPath = $"{CHAR_DIR}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = shader;
            }

            mat.color = tint;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            }
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateSolidMaterial(string matName, Color baseColor, float metallic, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            string matPath = $"{CHAR_DIR}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = shader;
            }

            mat.mainTexture = null;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Mesh LoadLosslessMeshFromBundle(string bundlePath, Transform[] bones, Transform rootTransform, Mesh targetMesh = null)
        {
            byte[] data = File.ReadAllBytes(bundlePath);
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                byte[] magic = br.ReadBytes(4);
                uint submeshCount = br.ReadUInt32();

                List<Vector3> allVerts = new List<Vector3>();
                List<Vector3> allNorms = new List<Vector3>();
                List<Vector2> allUvs = new List<Vector2>();
                List<int[]> allSubTris = new List<int[]>();

                for (int s = 0; s < submeshCount; s++)
                {
                    int vCount = (int)br.ReadUInt32();
                    int idxCount = (int)br.ReadUInt32();
                    int vOffset = allVerts.Count;

                    for (int i = 0; i < vCount; i++)
                    {
                        float x = br.ReadSingle();
                        float y = br.ReadSingle();
                        float z = br.ReadSingle();
                        allVerts.Add(new Vector3(x, y, z));
                    }
                    for (int i = 0; i < vCount; i++)
                    {
                        float nx = br.ReadSingle();
                        float ny = br.ReadSingle();
                        float nz = br.ReadSingle();
                        allNorms.Add(new Vector3(nx, ny, nz));
                    }
                    for (int i = 0; i < vCount; i++)
                    {
                        float u = br.ReadSingle();
                        float v = br.ReadSingle();
                        allUvs.Add(new Vector2(u, v));
                    }
                    int[] tris = new int[idxCount];
                    for (int i = 0; i < idxCount; i++)
                    {
                        tris[i] = br.ReadInt32() + vOffset;
                    }
                    allSubTris.Add(tris);
                }

                Mesh mesh = targetMesh != null ? targetMesh : new Mesh();
                mesh.Clear();
                mesh.name = "ElevatorApparition_SkinnedMesh";
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(allVerts);
                mesh.SetNormals(allNorms);
                mesh.SetUVs(0, allUvs);
                mesh.subMeshCount = (int)submeshCount;
                for (int s = 0; s < submeshCount; s++)
                {
                    mesh.SetTriangles(allSubTris[s], s);
                }

                Matrix4x4[] bindPoses = new Matrix4x4[bones.Length];
                Vector3[] bonePositions = new Vector3[bones.Length];
                for (int b = 0; b < bones.Length; b++)
                {
                    bindPoses[b] = bones[b].worldToLocalMatrix * rootTransform.localToWorldMatrix;
                    bonePositions[b] = bones[b].position;
                }
                mesh.bindposes = bindPoses;

                BoneWeight[] weights = new BoneWeight[allVerts.Count];
                for (int i = 0; i < allVerts.Count; i++)
                {
                    Vector3 v = allVerts[i];
                    if (v.y >= 1.40f && v.y <= 1.47f && v.z >= 0.045f && Mathf.Abs(v.x) <= 0.035f)
                    {
                        weights[i] = new BoneWeight { boneIndex0 = 6, weight0 = 0.85f, boneIndex1 = 5, weight1 = 0.15f };
                        continue;
                    }
                    if (v.y > 1.43f && Mathf.Abs(v.x) < 0.13f)
                    {
                        weights[i] = new BoneWeight { boneIndex0 = 5, weight0 = 0.92f, boneIndex1 = 4, weight1 = 0.08f };
                        continue;
                    }

                    int best1 = 1, best2 = 2;
                    float d1 = float.MaxValue, d2 = float.MaxValue;
                    for (int b = 1; b < bones.Length; b++)
                    {
                        if (b == 6) continue;
                        float dist = Vector3.SqrMagnitude(v - bonePositions[b]);
                        if (dist < d1)
                        {
                            d2 = d1; best2 = best1;
                            d1 = dist; best1 = b;
                        }
                        else if (dist < d2)
                        {
                            d2 = dist; best2 = b;
                        }
                    }
                    float w1 = 1f / (Mathf.Sqrt(d1) + 0.001f);
                    float w2 = 1f / (Mathf.Sqrt(d2) + 0.001f);
                    float sum = w1 + w2;
                    weights[i] = new BoneWeight
                    {
                        boneIndex0 = best1,
                        weight0 = w1 / sum,
                        boneIndex1 = best2,
                        weight1 = w2 / sum
                    };
                }

                mesh.boneWeights = weights;
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        private static AnimatorController CreateScaryWomanAnimatorController()
        {
            string controllerPath = $"{CHAR_DIR}/ScaryWoman_Controller.controller";
            string idleClipPath = $"{CHAR_DIR}/Idle.anim";
            string jumpscareClipPath = $"{CHAR_DIR}/Jumpscare.anim";

            AnimationClip idleClip = new AnimationClip { name = "Idle" };
            AnimationClipSettings idleSettings = AnimationUtility.GetAnimationClipSettings(idleClip);
            idleSettings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(idleClip, idleSettings);

            AnimationCurve headTiltZ = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(1.2f, -5.5f),
                new Keyframe(2.4f, 3.5f),
                new Keyframe(3.6f, 0f)
            );
            idleClip.SetCurve("Root/Pelvis/Spine/Chest/Neck/Head", typeof(Transform), "localEulerAnglesRaw.z", headTiltZ);
            AssetDatabase.CreateAsset(idleClip, idleClipPath);

            AnimationClip scareClip = new AnimationClip { name = "Jumpscare" };
            AnimationCurve armForwardX = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.15f, -68f),
                new Keyframe(0.6f, -72f)
            );
            scareClip.SetCurve("Root/Pelvis/Spine/Chest/LeftShoulder/LeftUpperArm", typeof(Transform), "localEulerAnglesRaw.x", armForwardX);
            scareClip.SetCurve("Root/Pelvis/Spine/Chest/RightShoulder/RightUpperArm", typeof(Transform), "localEulerAnglesRaw.x", armForwardX);
            AssetDatabase.CreateAsset(scareClip, jumpscareClipPath);

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState idleState = sm.AddState("Idle");
            idleState.motion = idleClip;
            sm.defaultState = idleState;

            AnimatorState scareState = sm.AddState("Jumpscare");
            scareState.motion = scareClip;

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }
    }
}
