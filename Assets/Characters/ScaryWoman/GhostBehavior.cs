using UnityEngine;

namespace HorrorGame.Character
{
    /// <summary>
    /// ScaryWoman_Ghost_01 用のホラー挙動・ボーン制御スクリプト。
    /// ・目の焦点が合っていない眼球（LeftEye / RightEye）の不規則な微細ドリフト
    /// ・崩れた顎（Jawボーン）の不気味な震え・開閉
    /// ・Idle時の首の傾きと呼吸、Jumpscare時の急接近ポーズを制御します。
    /// </summary>
    public class GhostBehavior : MonoBehaviour
    {
        [Header("ボーン参照")]
        public Transform rootBone;
        public Transform pelvisBone;
        public Transform spineBone;
        public Transform chestBone;
        public Transform neckBone;
        public Transform headBone;
        public Transform jawBone;
        public Transform leftArmBone;
        public Transform rightArmBone;

        [Header("アニメーション・挙動設定")]
        public Animator animator;
        [Range(0f, 1f)]
        public float twitchIntensity = 0.5f;
        public bool enableIdleMicroTwitch = true;
        public bool isJumpscareMode = false;

        private Quaternion initialHeadRot;
        private Quaternion initialJawRot;
        private Quaternion initialChestRot;
        private bool initialized = false;
        private float nextTwitchTime = 0f;
        private Vector3 currentHeadTwitch = Vector3.zero;

        private void Start()
        {
            InitializeBones();
        }

        public void InitializeBones()
        {
            if (initialized) return;
            if (headBone != null) initialHeadRot = headBone.localRotation;
            if (jawBone != null) initialJawRot = jawBone.localRotation;
            if (chestBone != null) initialChestRot = chestBone.localRotation;
            initialized = true;
        }

        private void LateUpdate()
        {
            if (!enableIdleMicroTwitch) return;
            InitializeBones();

            float t = Time.time;

            // 呼吸による胸郭の微細な揺れ
            if (chestBone != null)
            {
                float breath = Mathf.Sin(t * 2.1f) * 2.2f * twitchIntensity;
                chestBone.localRotation = initialChestRot * Quaternion.Euler(breath, 0f, 0f);
            }

            // 不規則な首の痙攣（ホラー特有のカクつき）
            if (headBone != null)
            {
                if (t >= nextTwitchTime)
                {
                    nextTwitchTime = t + Random.Range(0.35f, 1.6f);
                    if (Random.value < 0.45f)
                    {
                        currentHeadTwitch = new Vector3(
                            Random.Range(-7f, 6f),
                            Random.Range(-10f, 10f),
                            Random.Range(-14f, 14f)
                        ) * twitchIntensity;
                    }
                    else
                    {
                        currentHeadTwitch = Vector3.zero;
                    }
                }

                currentHeadTwitch = Vector3.Lerp(currentHeadTwitch, Vector3.zero, Time.deltaTime * 8f);
                float slowSway = Mathf.Sin(t * 1.3f) * 4.5f * twitchIntensity;
                headBone.localRotation = initialHeadRot * Quaternion.Euler(
                    currentHeadTwitch.x,
                    currentHeadTwitch.y,
                    slowSway + currentHeadTwitch.z
                );
            }

            // 崩れた顎（Jaw）の不気味な震えと脱臼した傾き
            if (jawBone != null)
            {
                float jawTremor = (Mathf.Sin(t * 19f) * 1.8f + Mathf.Cos(t * 31f) * 1.2f) * twitchIntensity;
                float jawOpen = isJumpscareMode ? 24f : (4f + Mathf.Max(0f, Mathf.Sin(t * 0.9f)) * 7f);
                jawBone.localRotation = initialJawRot * Quaternion.Euler(jawOpen + jawTremor, 3.5f, -8.5f);
            }
        }

        public void TriggerJumpscare()
        {
            isJumpscareMode = true;
            if (animator != null)
            {
                animator.Play("Jumpscare", 0, 0f);
            }
        }
    }
}
