using System.Collections;
using UnityEngine;
using HorrorGame.Character;

namespace HorrorGame.ScenarioA
{
    /// <summary>
    /// 案A『深夜3時のエレベーター』の幽霊制御クラス。
    /// ・前半（1回目の鏡や遠景デコイ）：あえて「こてこてのAIホラー風のチープなプリミティブモデル（mirrorGhostRoot）」を出してプレイヤーを油断させる
    /// ・後半（振り返った時・1階到着時・クライマックス）：作り込まれた高精細3Dモデル（ScaryWoman_Ghost_01 / elevator-apparition.glb）が現れ、強烈なギャップ恐怖を与える
    /// </summary>
    public class MirrorGhostController : MonoBehaviour
    {
        [Header("1回目の鏡＆遠景油断誘導用（こてこてのAI風チープモデル）")]
        public Transform mirrorGhostRoot;

        [Header("作り込まれた高精細3Dモデル（ScaryWoman_Ghost_01 / elevator-apparition.glb）")]
        [Tooltip("振り返った時＆2回目の鏡で背後に現れるScaryWoman_Ghost_01")]
        public Transform turnedAroundGhostRoot;
        public Light turnedAroundGhostLight;

        [Tooltip("1階についたとき扉の向こうに立っているScaryWoman_Ghost_01")]
        public Transform firstFloorHallGhostRoot;
        public Light firstFloorHallGhostLight;

        [Tooltip("クライマックス落下・正面突進・振り返りジャンプスケア用のScaryWoman_Ghost_01")]
        public Transform jumpscareGhostRoot;
        public Light jumpscareFaceLight;

        [Header("フェーズ3で視界の左右端に迫る白い手と黒髪")]
        public Transform peripheralLeftHand;
        public Transform peripheralRightHand;

        [Header("エレベーター内ローカル出現座標")]
        public Vector3 stage1BackCornerPos = new Vector3(0.42f, 0.0f, -0.52f);
        public Vector3 stage2CloseBehindPos = new Vector3(0.16f, 0.0f, -0.26f);
        public Vector3 turnAroundBehindPos = new Vector3(0.0f, 0.0f, -0.45f);
        public Vector3 firstFloorHallPos = new Vector3(0.0f, 0.0f, 1.55f);

        private int currentMirrorStage = 0;
        private bool isJumpscareActive = false;

        private void Start()
        {
            SetMirrorGhostVisible(false);
            SetTurnedAroundGhostVisible(false, false);
            SetFirstFloorHallGhostVisible(false);
            SetPeripheralHandsProgress(0f, false);
            if (jumpscareGhostRoot != null) jumpscareGhostRoot.gameObject.SetActive(false);
        }

        /// <summary>
        /// 鏡の中の幽霊を表示（Stage 1 = 1回目の鏡はチープなAI風モデルで油断させ、Stage 2 = 振り返った後は高精細モデル）
        /// </summary>
        public void ShowMirrorGhostStage(int stage)
        {
            currentMirrorStage = stage;
            if (stage <= 1)
            {
                if (turnedAroundGhostRoot != null) turnedAroundGhostRoot.gameObject.SetActive(false);
                if (mirrorGhostRoot != null)
                {
                    SetLayerRecursively(mirrorGhostRoot.gameObject, GetMirrorOnlyLayer());
                    mirrorGhostRoot.gameObject.SetActive(true);
                    mirrorGhostRoot.localPosition = stage1BackCornerPos;
                    mirrorGhostRoot.localRotation = Quaternion.Euler(0f, -15f, 3f);
                }
            }
            else
            {
                if (mirrorGhostRoot != null) mirrorGhostRoot.gameObject.SetActive(false);
                SetTurnedAroundGhostVisible(true, true);
            }
        }

        /// <summary>
        /// 廊下（7階や1階奥）に「こてこてのAI風チープモデル」を直接見える状態で配置してプレイヤーを油断させる
        /// </summary>
        public void ShowCheapDummyInHallway(bool visible, Vector3 localPos)
        {
            if (mirrorGhostRoot == null) return;
            if (visible)
            {
                SetLayerRecursively(mirrorGhostRoot.gameObject, 0); // Defaultレイヤーにして肉眼で見えるようにする
                mirrorGhostRoot.localPosition = localPos;
                mirrorGhostRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
                mirrorGhostRoot.gameObject.SetActive(true);
            }
            else
            {
                mirrorGhostRoot.gameObject.SetActive(false);
                SetLayerRecursively(mirrorGhostRoot.gameObject, GetMirrorOnlyLayer());
            }
        }

        public void SetMirrorGhostVisible(bool visible)
        {
            if (currentMirrorStage <= 1)
            {
                if (mirrorGhostRoot != null)
                {
                    SetLayerRecursively(mirrorGhostRoot.gameObject, GetMirrorOnlyLayer());
                    mirrorGhostRoot.gameObject.SetActive(visible);
                }
                if (!visible && turnedAroundGhostRoot != null) turnedAroundGhostRoot.gameObject.SetActive(false);
            }
            else
            {
                if (mirrorGhostRoot != null) mirrorGhostRoot.gameObject.SetActive(false);
                SetTurnedAroundGhostVisible(visible, true);
            }
        }

        /// <summary>
        /// プレイヤーが後ろを振り返った時に高精細モデル（ScaryWoman_Ghost_01）を背後に直接出現させる制御
        /// </summary>
        public void SetTurnedAroundGhostVisible(bool visible, bool mirrorOnlyMode)
        {
            if (turnedAroundGhostRoot == null) return;

            int targetLayer = mirrorOnlyMode ? GetMirrorOnlyLayer() : 0;
            SetLayerRecursively(turnedAroundGhostRoot.gameObject, targetLayer);

            if (visible)
            {
                turnedAroundGhostRoot.localPosition = mirrorOnlyMode ? stage2CloseBehindPos : turnAroundBehindPos;
                if (Camera.main != null)
                {
                    Vector3 lookPos = Camera.main.transform.position;
                    lookPos.y = turnedAroundGhostRoot.position.y;
                    Vector3 dir = (lookPos - turnedAroundGhostRoot.position).normalized;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        turnedAroundGhostRoot.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    }
                }
                else
                {
                    turnedAroundGhostRoot.localRotation = Quaternion.identity;
                }
            }

            turnedAroundGhostRoot.gameObject.SetActive(visible);
            if (turnedAroundGhostLight != null)
            {
                turnedAroundGhostLight.enabled = visible && !mirrorOnlyMode;
            }
        }

        /// <summary>
        /// 1階についたとき、開いた扉の正面に高精細モデル（ScaryWoman_Ghost_01）を出現させる
        /// </summary>
        public void SetFirstFloorHallGhostVisible(bool visible, float customZ = 1.55f)
        {
            if (firstFloorHallGhostRoot == null) return;
            firstFloorHallGhostRoot.localPosition = new Vector3(0f, 0f, customZ);
            firstFloorHallGhostRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
            firstFloorHallGhostRoot.gameObject.SetActive(visible);
            if (firstFloorHallGhostLight != null)
            {
                firstFloorHallGhostLight.enabled = visible;
            }
        }

        private void Update()
        {
            if (mirrorGhostRoot != null && mirrorGhostRoot.gameObject.activeSelf && mirrorGhostRoot.gameObject.layer == GetMirrorOnlyLayer())
            {
                float sway = Mathf.Sin(Time.time * 1.7f) * 2.5f;
                mirrorGhostRoot.localRotation = Quaternion.Euler(0f, -10f + sway, 4f + Mathf.Cos(Time.time * 3.1f) * 1.5f);
            }
        }

        public void SetPeripheralHandsProgress(float normalizedProgress, bool active)
        {
            if (peripheralLeftHand != null) peripheralLeftHand.gameObject.SetActive(active);
            if (peripheralRightHand != null) peripheralRightHand.gameObject.SetActive(active);

            if (!active || Camera.main == null) return;

            Transform cam = Camera.main.transform;
            float forwardOffset = Mathf.Lerp(-0.12f, 0.24f, normalizedProgress);
            float sideOffset = Mathf.Lerp(0.32f, 0.21f, normalizedProgress);
            float tremor = Mathf.Sin(Time.time * 14f) * 0.004f;

            if (peripheralLeftHand != null)
            {
                peripheralLeftHand.position = cam.position
                                            + cam.forward * forwardOffset
                                            - cam.right * (sideOffset + tremor)
                                            - cam.up * 0.06f;
                peripheralLeftHand.rotation = cam.rotation * Quaternion.Euler(10f, 35f, -15f);
            }

            if (peripheralRightHand != null)
            {
                peripheralRightHand.position = cam.position
                                             + cam.forward * (forwardOffset - 0.03f)
                                             + cam.right * (sideOffset - tremor)
                                             + cam.up * 0.04f;
                peripheralRightHand.rotation = cam.rotation * Quaternion.Euler(-8f, -35f, 18f);
            }
        }

        /// <summary>
        /// クライマックス①：天井の点検口から高精細モデル（ScaryWoman_Ghost_01）がHMD（顔面）に向かって落下してくる
        /// </summary>
        public IEnumerator PlayCeilingDropJumpscareRoutine(Vector3 ceilingHatchPos, float duration = 0.78f)
        {
            if (jumpscareGhostRoot == null || isJumpscareActive) yield break;
            isJumpscareActive = true;
            HideAllNonJumpscareGhosts();

            jumpscareGhostRoot.gameObject.SetActive(true);
            if (jumpscareFaceLight != null) jumpscareFaceLight.enabled = true;

            GhostBehavior gb = jumpscareGhostRoot.GetComponent<GhostBehavior>();
            if (gb != null) gb.TriggerJumpscare();

            Transform cam = Camera.main != null ? Camera.main.transform : transform;
            float elapsed = 0f;
            float dropTime = 0.18f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dropTime);
                float curve = 1f - Mathf.Pow(1f - t, 3f);

                Vector3 targetRootPos = cam.position + cam.forward * 0.33f - Vector3.up * 1.48f;
                Vector3 startRootPos = ceilingHatchPos - Vector3.up * 1.15f;
                Vector3 jitter = Random.insideUnitSphere * 0.015f;

                jumpscareGhostRoot.position = Vector3.Lerp(startRootPos, targetRootPos, curve) + jitter;

                Vector3 lookDir = cam.position - (jumpscareGhostRoot.position + Vector3.up * 1.48f);
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    jumpscareGhostRoot.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                }

                yield return null;
            }
        }

        /// <summary>
        /// クライマックス②：1階廊下で「チープなAIモデル」から「高精細モデル（ScaryWoman_Ghost_01）」に豹変した後、
        /// 目の前からHMD（顔面30cm）へ猛スピードで突進してくるジャンプスケア！
        /// </summary>
        public IEnumerator PlayHallwayRushJumpscareRoutine(Vector3 startHallwayPos, float duration = 0.78f)
        {
            if (jumpscareGhostRoot == null || isJumpscareActive) yield break;
            isJumpscareActive = true;
            HideAllNonJumpscareGhosts();

            jumpscareGhostRoot.gameObject.SetActive(true);
            if (jumpscareFaceLight != null) jumpscareFaceLight.enabled = true;

            GhostBehavior gb = jumpscareGhostRoot.GetComponent<GhostBehavior>();
            if (gb != null) gb.TriggerJumpscare();

            Transform cam = Camera.main != null ? Camera.main.transform : transform;
            float elapsed = 0f;
            float rushTime = 0.20f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rushTime);
                float curve = t * t * (3f - 2f * t);

                Vector3 targetRootPos = cam.position + cam.forward * 0.31f - Vector3.up * 1.48f;
                Vector3 jitter = Random.insideUnitSphere * 0.016f;

                jumpscareGhostRoot.position = Vector3.Lerp(startHallwayPos, targetRootPos, curve) + jitter;

                Vector3 lookDir = cam.position - (jumpscareGhostRoot.position + Vector3.up * 1.48f);
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    jumpscareGhostRoot.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                }

                yield return null;
            }
        }

        /// <summary>
        /// クライマックス③＆振り返り違反：高精細モデル（ScaryWoman_Ghost_01）がすぐ目の前に張り付くジャンプスケア
        /// </summary>
        public IEnumerator PlayLookBackViolationJumpscareRoutine(float duration = 0.78f)
        {
            if (jumpscareGhostRoot == null || isJumpscareActive) yield break;
            isJumpscareActive = true;
            HideAllNonJumpscareGhosts();

            jumpscareGhostRoot.gameObject.SetActive(true);
            if (jumpscareFaceLight != null) jumpscareFaceLight.enabled = true;

            GhostBehavior gb = jumpscareGhostRoot.GetComponent<GhostBehavior>();
            if (gb != null) gb.TriggerJumpscare();

            Transform cam = Camera.main != null ? Camera.main.transform : transform;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Vector3 targetRootPos = cam.position + cam.forward * 0.31f - Vector3.up * 1.48f;
                Vector3 jitter = Random.insideUnitSphere * 0.018f;

                jumpscareGhostRoot.position = targetRootPos + jitter;
                Vector3 lookDir = cam.position - (jumpscareGhostRoot.position + Vector3.up * 1.48f);
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    jumpscareGhostRoot.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                }

                yield return null;
            }
        }

        private void HideAllNonJumpscareGhosts()
        {
            SetMirrorGhostVisible(false);
            SetTurnedAroundGhostVisible(false, false);
            SetFirstFloorHallGhostVisible(false);
            SetPeripheralHandsProgress(0f, false);
        }

        private static int GetMirrorOnlyLayer()
        {
            int layer = LayerMask.NameToLayer("MirrorOnly");
            return layer >= 0 ? layer : 6;
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
