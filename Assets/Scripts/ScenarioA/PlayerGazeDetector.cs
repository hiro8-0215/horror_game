using UnityEngine;

namespace HorrorGame.ScenarioA
{
    /// <summary>
    /// HMD（プレイヤーの頭・視線）の向きから
    /// 「鏡を見ているか」「後ろを振り返ったか」「天井（点検口）を見上げたか」を毎フレーム判定するクラス。
    /// </summary>
    public class PlayerGazeDetector : MonoBehaviour
    {
        [Header("判定対象Transform")]
        public Transform playerCamera;
        public Transform frontDirectionRef; // エレベーター正面（扉側）の基準
        public Transform frontMirrorTarget; // 操作パネル横の鏡
        public Transform rearMirrorTarget;  // 背面の鏡
        public Transform ceilingHatchTarget; // 天井の点検口

        [Header("角度しきい値（度）")]
        [Tooltip("鏡を見ていると判定する視野角")]
        public float mirrorGazeAngleThreshold = 28f;

        [Tooltip("後ろを振り返ったと判定する正面からの水平角度（110〜125度推奨）")]
        public float lookBackYawThreshold = 115f;

        [Tooltip("天井を見上げたと判定する上向き角度")]
        public float lookUpAngleThreshold = 34f;

        [Header("現在の判定状態（ReadOnly）")]
        public bool isLookingAtFrontMirror;
        public bool isLookingAtRearMirror;
        public bool isLookingAtAnyMirror;
        public bool isLookingBack;
        public bool isLookingUpAtCeiling;
        public float currentYawFromFront;

        private void Update()
        {
            if (playerCamera == null)
            {
                if (Camera.main != null) playerCamera = Camera.main.transform;
                else return;
            }

            Vector3 camForward = playerCamera.forward;
            Vector3 camPos = playerCamera.position;

            // 1. 正面からの水平回転角（Yaw）で「後ろを振り返ったか」を判定
            Vector3 refForward = (frontDirectionRef != null) ? frontDirectionRef.forward : Vector3.forward;
            Vector3 flatCamForward = new Vector3(camForward.x, 0f, camForward.z);
            Vector3 flatRefForward = new Vector3(refForward.x, 0f, refForward.z);

            if (flatCamForward.sqrMagnitude > 0.001f && flatRefForward.sqrMagnitude > 0.001f)
            {
                currentYawFromFront = Vector3.Angle(flatRefForward.normalized, flatCamForward.normalized);
            }
            else
            {
                currentYawFromFront = 0f;
            }

            isLookingBack = currentYawFromFront >= lookBackYawThreshold;

            // 2. 鏡を見ているか判定
            isLookingAtFrontMirror = CheckLookingAtTarget(camPos, camForward, frontMirrorTarget, mirrorGazeAngleThreshold);
            isLookingAtRearMirror = CheckLookingAtTarget(camPos, camForward, rearMirrorTarget, mirrorGazeAngleThreshold);
            isLookingAtAnyMirror = isLookingAtFrontMirror || isLookingAtRearMirror;

            // 3. 天井（点検口）を見上げたか判定
            bool pitchUp = Vector3.Angle(camForward, Vector3.up) <= (90f - lookUpAngleThreshold);
            bool lookingAtHatch = CheckLookingAtTarget(camPos, camForward, ceilingHatchTarget, 35f);
            isLookingUpAtCeiling = pitchUp || lookingAtHatch;
        }

        private bool CheckLookingAtTarget(Vector3 camPos, Vector3 camForward, Transform target, float thresholdDeg)
        {
            if (target == null) return false;
            Vector3 dirToTarget = (target.position - camPos).normalized;
            float angle = Vector3.Angle(camForward, dirToTarget);
            return angle <= thresholdDeg;
        }
    }
}
