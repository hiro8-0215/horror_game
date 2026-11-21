using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using HorrorGame.Core;
using HorrorGame.Audio;
using HorrorGame.ScenarioA;
using HorrorGame.Character;

namespace HorrorGame.ScenarioC
{
    /// <summary>
    /// 案C：『標本室のマネキン』のメインシナリオ進行マネージャー。
    /// ・前半は3体の「こてこてのAI風チープなカプセル/ブロック人形」が視線を外すたびに少しずつ近づく（だるまさんが転んだ）演出で油断させ、
    /// ・電力復旧100%のクライマックスで照明が明滅した瞬間、チープな人形が消え去り、
    ///   作り込まれた超高精細3Dモデル（ScaryWoman_Ghost_01 / elevator-apparition.glb）へと豹変して至近距離から襲いかかります。
    /// </summary>
    public class MannequinScenarioManager : MonoBehaviour
    {
        [Header("空間・照明・表示パネル")]
        public Light roomFluorescentLight;
        public Light flashlightSpot;
        public TextMesh powerMeterText;
        public TextMesh guidanceSubtitleText;

        [Header("配電盤ボタン")]
        public ElevatorButton powerChargeButton;

        [Header("前半の油断誘導用：3体のチープなAIダミーマネキン")]
        public Transform cheapMannequinCenter;
        public Transform cheapMannequinLeft;
        public Transform cheapMannequinRight;

        [Header("クライマックス覚醒恐怖用：高精細3Dモデル（ScaryWoman_Ghost_01）")]
        public Transform realScaryWomanRoot;
        public Light realScaryWomanFaceLight;

        [Header("3D音源")]
        public AudioSource roomHumAudio;
        public AudioSource mannequinStep3DAudio;
        public AudioSource panelAudio;
        public AudioSource behindNeck3DAudio;

        [Header("暗転・エンディング")]
        public GameObject blackoutSphere;
        public TextMesh endingTitleText;

        [Header("進行設定")]
        public float totalChargeSeconds = 22.0f;
        public float timelineSpeedMultiplier = 1.0f;

        private ScenarioPatternData activePattern;
        private Vector3 centerStartPos;
        private Vector3 leftStartPos;
        private Vector3 rightStartPos;

        private void Start()
        {
            activePattern = ActiveScenarioConfig.GetOrRollPatternForScene(ScenarioType.ScenarioC_Mannequin);

            if (cheapMannequinCenter != null) centerStartPos = cheapMannequinCenter.localPosition;
            if (cheapMannequinLeft != null) leftStartPos = cheapMannequinLeft.localPosition;
            if (cheapMannequinRight != null) rightStartPos = cheapMannequinRight.localPosition;

            if (realScaryWomanRoot != null) realScaryWomanRoot.gameObject.SetActive(false);
            if (blackoutSphere != null) blackoutSphere.SetActive(false);

            StartCoroutine(RunMannequinScenarioTimeline());
        }

        private void Update()
        {
            // HMDの向きに懐中電灯スポットライトを追従させる
            if (flashlightSpot != null && Camera.main != null)
            {
                flashlightSpot.transform.position = Camera.main.transform.position;
                flashlightSpot.transform.rotation = Camera.main.transform.rotation;
            }
        }

        private IEnumerator RunMannequinScenarioTimeline()
        {
            var audio = ProceduralHorrorAudio.Instance;

            if (roomHumAudio != null && audio != null)
            {
                roomHumAudio.clip = audio.fluorescentHumClip;
                roomHumAudio.loop = true;
                roomHumAudio.volume = 0.35f;
                roomHumAudio.Play();
            }

            SetPowerText("0%", new Color(1.0f, 0.3f, 0.1f));
            SetSubtitle($"夜の標本室 - 停電復旧作業 (Pattern #{activePattern.patternId:00})\n奥に3体のチープなマネキンが並んでいる。\n手元の [ 電力チャージ (1) ] ボタンを長押しして100%まで復旧せよ");
            powerChargeButton.SetBlinkingGuide(true);

            float targetCharge = Mathf.Max(5.0f, totalChargeSeconds / Mathf.Max(0.1f, timelineSpeedMultiplier));
            float currentCharge = 0f;
            float nextStepCheckTime = 2.2f;
            int stepStage = 0;

            while (currentCharge < targetCharge)
            {
                bool holding = powerChargeButton != null && powerChargeButton.IsCurrentlyHeld;
                float rate = holding ? 1.35f : 0.45f;
                currentCharge += Time.deltaTime * rate;
                float norm = Mathf.Clamp01(currentCharge / targetCharge);

                int pct = Mathf.Clamp(Mathf.RoundToInt(norm * 100f), 0, 99);
                SetPowerText($"{pct}%", Color.Lerp(new Color(1f, 0.25f, 0.1f), new Color(0.2f, 1f, 0.3f), norm));

                string holdMsg = holding ? "【チャージ中】" : "【[1]ボタンを長押ししてください】";
                SetSubtitle($"視線を外すとマネキンが近づいてくる！\n{holdMsg} 電力復旧まで: {pct}%");

                if (holding && VRPlayerRigController.Instance != null && Time.frameCount % 25 == 0)
                {
                    VRPlayerRigController.Instance.TriggerHaptics(0.18f + norm * 0.2f, 0.05f);
                }

                // 一定間隔で照明が一瞬消え、プレイヤーが見ていない（または暗転した）隙にチープなマネキンが1歩ずつ近づく
                if (currentCharge >= nextStepCheckTime && stepStage < 6)
                {
                    nextStepCheckTime += targetCharge / 6.5f;
                    stepStage++;
                    yield return StartCoroutine(AdvanceCheapMannequinsStepRoutine(norm));
                }

                yield return null;
            }

            powerChargeButton.SetBlinkingGuide(false);
            SetPowerText("100%", new Color(0.2f, 1.0f, 0.35f));

            // =========================================================================
            // 【クライマックス：チープなAIダミー人形から、超高精細3Dモデル（ScaryWoman_Ghost_01）へ豹変！】
            // =========================================================================
            if (activePattern.mannequinVariant == MannequinAnomalyVariant.Mannequin2_FakePowerRestore_Blackout)
            {
                // バリエーション2：一度部屋が明るくなり、チープなマネキンが元の位置に戻って「助かったｗ」と安心させてから再停電＆高精細モデル急襲！
                if (roomFluorescentLight != null) roomFluorescentLight.intensity = 1.35f;
                ResetCheapMannequinsToStart();
                PlayOneShotSafe(panelAudio, audio != null ? audio.elevatorDingClip : null, 0.9f);
                SetSubtitle("電力100%復旧！\nマネキンも元の位置に戻った。なんだ、ただのチープな人形だったか…");
                yield return new WaitForSeconds(2.6f);

                // 突然「バンッ！」とブレーカーが落ち、真っ暗になる！
                PlayOneShotSafe(panelAudio, audio != null ? audio.emergencyStopCrashClip : null, 1.0f);
                if (roomFluorescentLight != null) roomFluorescentLight.intensity = 0.02f;
                if (flashlightSpot != null) flashlightSpot.intensity = 0.25f;
                HideAllCheapMannequins();
                SetPowerText("ERR", new Color(1f, 0.05f, 0.05f));
                SetSubtitle("！？ ブレーカーが落ちた…！");
                yield return new WaitForSeconds(1.1f);
            }
            else
            {
                // バリエーション0/1：電力100%になった瞬間に照明がバチバチッと明滅し、目の前のチープな人形が消えて高精細モデル（ScaryWoman_Ghost_01）に豹変！
                PlayOneShotSafe(panelAudio, audio != null ? audio.lightFlickerBuzzClip : null, 1.0f);
                for (int i = 0; i < 6; i++)
                {
                    if (roomFluorescentLight != null) roomFluorescentLight.intensity = (i % 2 == 0) ? 0.02f : 1.1f;
                    yield return new WaitForSeconds(0.06f);
                }
                HideAllCheapMannequins();

                // 目の前1.15mに超高精細モデル（ScaryWoman_Ghost_01）が突如立ち現れる！
                if (realScaryWomanRoot != null)
                {
                    realScaryWomanRoot.localPosition = new Vector3(0f, 0f, 1.15f);
                    realScaryWomanRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    realScaryWomanRoot.gameObject.SetActive(true);
                    if (realScaryWomanFaceLight != null) realScaryWomanFaceLight.enabled = true;
                }
                SetSubtitle("！？ マネキンが…生々しい女に変わった…！？");
                yield return new WaitForSeconds(0.65f);
            }

            // 超高精細モデル（ScaryWoman_Ghost_01）が顔面（HMD）へ猛突進ジャンプスケア！
            SetSubtitle("");
            PlayOneShotSafe(behindNeck3DAudio, audio != null ? audio.jumpscareScreamClip : null, 1.0f);
            if (VRPlayerRigController.Instance != null)
            {
                VRPlayerRigController.Instance.TriggerHaptics(1.0f, 0.85f);
            }

            yield return StartCoroutine(PlayRealScaryWomanRushJumpscareRoutine(0.78f));

            // 暗転終了
            if (roomHumAudio != null) roomHumAudio.Stop();
            ShowBlackoutEnding($"標本室のマネキン\n- 完 -\n(Pattern #{activePattern.patternId:00})");
            PlayOneShotSafe(behindNeck3DAudio, audio != null ? audio.tinnitusRingClip : null, 0.85f);

            yield return new WaitForSeconds(5.5f);
            if (Application.CanStreamedLevelBeLoaded("00_BootScene"))
            {
                SceneManager.LoadScene("00_BootScene");
            }
        }

        private IEnumerator AdvanceCheapMannequinsStepRoutine(float progress)
        {
            var audio = ProceduralHorrorAudio.Instance;
            if (roomFluorescentLight != null) roomFluorescentLight.intensity = 0.02f;
            if (flashlightSpot != null) flashlightSpot.intensity = 0.15f;

            PlayOneShotSafe(mannequinStep3DAudio, audio != null ? audio.hallwayFootstepsSlowClip : null, 0.95f);
            yield return new WaitForSeconds(0.18f);

            // プレイヤーに向かってじわじわ距離を詰める（ただし見た目はあえてチープなカプセル人形なので油断する）
            Vector3 playerTarget = new Vector3(0f, 0f, 0.78f);
            if (cheapMannequinCenter != null)
            {
                cheapMannequinCenter.localPosition = Vector3.Lerp(centerStartPos, playerTarget, progress * 0.88f);
            }
            if (cheapMannequinLeft != null)
            {
                cheapMannequinLeft.localPosition = Vector3.Lerp(leftStartPos, new Vector3(-0.62f, 0f, 0.65f), progress * 0.92f);
                cheapMannequinLeft.localRotation = Quaternion.Euler(0f, 135f, 0f);
            }
            if (cheapMannequinRight != null)
            {
                cheapMannequinRight.localPosition = Vector3.Lerp(rightStartPos, new Vector3(0.62f, 0f, 0.65f), progress * 0.92f);
                cheapMannequinRight.localRotation = Quaternion.Euler(0f, -135f, 0f);
            }

            if (roomFluorescentLight != null) roomFluorescentLight.intensity = 0.35f;
            if (flashlightSpot != null) flashlightSpot.intensity = 1.2f;
        }

        private void ResetCheapMannequinsToStart()
        {
            if (cheapMannequinCenter != null) cheapMannequinCenter.localPosition = centerStartPos;
            if (cheapMannequinLeft != null) cheapMannequinLeft.localPosition = leftStartPos;
            if (cheapMannequinRight != null) cheapMannequinRight.localPosition = rightStartPos;
        }

        private void HideAllCheapMannequins()
        {
            if (cheapMannequinCenter != null) cheapMannequinCenter.gameObject.SetActive(false);
            if (cheapMannequinLeft != null) cheapMannequinLeft.gameObject.SetActive(false);
            if (cheapMannequinRight != null) cheapMannequinRight.gameObject.SetActive(false);
        }

        private IEnumerator PlayRealScaryWomanRushJumpscareRoutine(float duration)
        {
            if (realScaryWomanRoot == null) yield break;
            realScaryWomanRoot.gameObject.SetActive(true);
            if (realScaryWomanFaceLight != null) realScaryWomanFaceLight.enabled = true;

            GhostBehavior gb = realScaryWomanRoot.GetComponent<GhostBehavior>();
            if (gb != null) gb.TriggerJumpscare();

            Transform cam = Camera.main != null ? Camera.main.transform : transform;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.16f);
                float dist = Mathf.Lerp(1.05f, 0.31f, t);

                Vector3 targetRootPos = cam.position + cam.forward * dist - Vector3.up * 1.48f;
                Vector3 jitter = Random.insideUnitSphere * 0.018f;
                realScaryWomanRoot.position = targetRootPos + jitter;

                Vector3 lookDir = cam.position - (realScaryWomanRoot.position + Vector3.up * 1.48f);
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    realScaryWomanRoot.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                }
                yield return null;
            }
        }

        private void SetPowerText(string text, Color col)
        {
            if (powerMeterText != null)
            {
                powerMeterText.text = text;
                powerMeterText.color = col;
            }
        }

        private void SetSubtitle(string text)
        {
            if (guidanceSubtitleText != null)
            {
                guidanceSubtitleText.text = text;
            }
        }

        private void ShowBlackoutEnding(string title)
        {
            if (blackoutSphere != null && Camera.main != null)
            {
                blackoutSphere.transform.position = Camera.main.transform.position;
                blackoutSphere.transform.rotation = Camera.main.transform.rotation;
                blackoutSphere.SetActive(true);
            }
            if (endingTitleText != null)
            {
                endingTitleText.text = title;
            }
        }

        private void PlayOneShotSafe(AudioSource src, AudioClip clip, float vol = 1f)
        {
            if (src != null && clip != null) src.PlayOneShot(clip, vol);
        }
    }
}
