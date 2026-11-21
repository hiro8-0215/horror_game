using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using HorrorGame.Core;
using HorrorGame.Audio;
using HorrorGame.ScenarioA;
using HorrorGame.Character;

namespace HorrorGame.ScenarioB
{
    /// <summary>
    /// 案B：『暗室（ダークルーム）』のメインシナリオ進行マネージャー。
    /// ・赤い安全光が灯る暗室で、目の前の現像トレイを使って3枚の写真を現像する能動的VRホラー。
    /// ・1〜2枚目は「こてこてのAI風チープなブロック幽霊」でプレイヤーを油断させ、
    ///   3枚目の現像完了と同時に作り込まれた超高精細3Dモデル（ScaryWoman_Ghost_01 / elevator-apparition.glb）が現れて襲いかかります。
    /// </summary>
    public class DarkroomScenarioManager : MonoBehaviour
    {
        [Header("空間・ギミック参照")]
        public Light redSafelight;
        public Renderer safelightBulbRenderer;
        public Transform rearDoorPivot;
        public Renderer liquidTrayRenderer;
        public Renderer photoPaperRenderer;
        public TextMesh photoCaptionText;
        public TextMesh guidanceSubtitleText;

        [Header("操作ボタン")]
        public ElevatorButton developButton;
        public ElevatorButton closeDoorButton;

        [Header("モデル参照（前半のチープなAIモデル vs 後半の高精細ScaryWoman_Ghost_01）")]
        public Transform cheapDummyGhostRoot;
        public Transform realScaryWomanRoot;
        public Light realScaryWomanFaceLight;

        [Header("3D音源")]
        public AudioSource safelightBuzzAudio;
        public AudioSource trayLiquidAudio;
        public AudioSource rearDoor3DAudio;
        public AudioSource behindNeck3DAudio;

        [Header("暗転・エンディング")]
        public GameObject blackoutSphere;
        public TextMesh endingTitleText;

        [Header("進行設定")]
        public PlayerGazeDetector gazeDetector;
        public float timelineSpeedMultiplier = 1.0f;

        private bool developPressed;
        private bool closeDoorPressed;
        private ScenarioPatternData activePattern;

        private void Start()
        {
            activePattern = ActiveScenarioConfig.GetOrRollPatternForScene(ScenarioType.ScenarioB_Darkroom);

            if (developButton != null)
                developButton.OnButtonPressed += _ => developPressed = true;
            if (closeDoorButton != null)
                closeDoorButton.OnButtonPressed += _ => closeDoorPressed = true;

            if (cheapDummyGhostRoot != null) cheapDummyGhostRoot.gameObject.SetActive(false);
            if (realScaryWomanRoot != null) realScaryWomanRoot.gameObject.SetActive(false);
            if (blackoutSphere != null) blackoutSphere.SetActive(false);

            StartCoroutine(RunDarkroomTimeline());
        }

        private WaitForSeconds ScaledWait(float seconds)
        {
            return new WaitForSeconds(Mathf.Max(0.05f, seconds / Mathf.Max(0.1f, timelineSpeedMultiplier)));
        }

        private IEnumerator RunDarkroomTimeline()
        {
            var audio = ProceduralHorrorAudio.Instance;

            if (safelightBuzzAudio != null && audio != null)
            {
                safelightBuzzAudio.clip = audio.fluorescentHumClip;
                safelightBuzzAudio.loop = true;
                safelightBuzzAudio.volume = 0.35f;
                safelightBuzzAudio.Play();
            }

            SetPhotoPaperVisual(new Color(0.85f, 0.82f, 0.78f), "【未現像の印画紙 1/3】");

            // =========================================================================
            // 【1枚目の現像：チープなAI風心霊写真で油断させる】
            // =========================================================================
            SetSubtitle($"暗室 - 深夜の現像作業 (Pattern #{activePattern.patternId:00})\n[ 現像 (1) ] ボタンを押して1枚目の写真を現像液に浸す");
            developButton.SetBlinkingGuide(true);

            developPressed = false;
            while (!developPressed) yield return null;
            developButton.SetBlinkingGuide(false);

            SetSubtitle("現像液に浸しています…（写真が浮かび上がるのを待つ）");
            PlayOneShotSafe(trayLiquidAudio, audio != null ? audio.bloodDripClip : null, 0.9f);
            yield return StartCoroutine(AnimatePhotoDevelopingRoutine(1, 4.0f));

            // 1枚目完了：テーブルの奥に「こてこてのAI風チープモデル」が一瞬だけ見えて油断させる
            if (cheapDummyGhostRoot != null)
            {
                cheapDummyGhostRoot.localPosition = new Vector3(0f, 0f, 2.2f);
                cheapDummyGhostRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
                cheapDummyGhostRoot.gameObject.SetActive(true);
            }
            SetPhotoPaperVisual(new Color(0.35f, 0.12f, 0.12f), "【写真1：廃墟の廊下に立つ\nチープなブロック人影】");
            SetSubtitle("1枚目の現像完了。\n…なんだかチープな合成写真みたいだ。\n続けて [ 現像 (1) ] ボタンで2枚目を現像する");

            developButton.SetBlinkingGuide(true);
            developPressed = false;
            while (!developPressed) yield return null;
            developButton.SetBlinkingGuide(false);

            if (cheapDummyGhostRoot != null) cheapDummyGhostRoot.gameObject.SetActive(false);

            // =========================================================================
            // 【2枚目の現像：今いる暗室の背後のドアが開いている写真 → 実際に背後が開く】
            // =========================================================================
            SetSubtitle("2枚目を現像液に浸しています…");
            PlayOneShotSafe(trayLiquidAudio, audio != null ? audio.bloodDripClip : null, 0.9f);
            yield return StartCoroutine(AnimatePhotoDevelopingRoutine(2, 4.5f));

            SetPhotoPaperVisual(new Color(0.28f, 0.08f, 0.08f), "【写真2：今いるこの暗室の\n背後のドアが開いている写真】");
            PlayOneShotSafe(rearDoor3DAudio, audio != null ? audio.doorSlideClip : null, 0.85f);

            // 背後のドアが実際にギイッと少し開き、そこにチープなブロック人形が立っている（まだ油断させる）
            if (rearDoorPivot != null)
            {
                rearDoorPivot.localRotation = Quaternion.Euler(0f, -38f, 0f);
            }
            if (cheapDummyGhostRoot != null)
            {
                cheapDummyGhostRoot.localPosition = new Vector3(-0.25f, 0f, -1.55f);
                cheapDummyGhostRoot.localRotation = Quaternion.identity;
                cheapDummyGhostRoot.gameObject.SetActive(true);
            }

            SetSubtitle("後ろでドアが開く音がした…！（後ろを振り返って確認する）");
            float lookWait = 0f;
            while (lookWait < 7.0f)
            {
                lookWait += Time.deltaTime;
                if (gazeDetector != null && gazeDetector.isLookingBack && lookWait > 1.2f)
                {
                    break;
                }
                yield return null;
            }

            SetSubtitle("ドアの隙間にあのチープな人形が立っている…\n正面の [ ドアロック (C) ] ボタンを押してドアを閉める");
            closeDoorButton.SetBlinkingGuide(true);
            closeDoorPressed = false;
            float closeTimeout = 0f;
            while (!closeDoorPressed && closeTimeout < 8.0f)
            {
                closeTimeout += Time.deltaTime;
                yield return null;
            }
            closeDoorButton.SetBlinkingGuide(false);

            // ドアがバタンと閉まり、チープ人形が消える
            PlayOneShotSafe(rearDoor3DAudio, audio != null ? audio.doorSlideClip : null, 0.95f);
            if (rearDoorPivot != null) rearDoorPivot.localRotation = Quaternion.identity;
            if (cheapDummyGhostRoot != null) cheapDummyGhostRoot.gameObject.SetActive(false);

            yield return ScaledWait(1.8f);

            // =========================================================================
            // 【3枚目の現像（クライマックス）：作り込まれた高精細モデル ScaryWoman_Ghost_01 出現！】
            // =========================================================================
            SetSubtitle("最後の1枚（3/3）。\n[ 現像 (1) ] ボタンを押して現像を完了させる");
            developButton.SetBlinkingGuide(true);
            developPressed = false;
            while (!developPressed) yield return null;
            developButton.SetBlinkingGuide(false);

            // 現像液が赤黒く染まり、安全光が激しく明滅し始める
            if (liquidTrayRenderer != null)
            {
                liquidTrayRenderer.material.color = new Color(0.38f, 0.01f, 0.01f);
            }
            PlayOneShotSafe(trayLiquidAudio, audio != null ? audio.lightFlickerBuzzClip : null, 0.9f);
            SetSubtitle("現像液が血のように赤黒く濁っていく…！");

            yield return StartCoroutine(AnimatePhotoDevelopingRoutine(3, 4.5f));

            SetPhotoPaperVisual(new Color(0.55f, 0.02f, 0.02f), "【写真3：あなたの真後ろ30cmに\n血まみれの女が立っている】");

            // 耳元で湿った呼吸音と囁き声
            if (behindNeck3DAudio != null && Camera.main != null)
            {
                Transform cam = Camera.main.transform;
                behindNeck3DAudio.transform.position = cam.position - cam.forward * 0.25f;
                PlayOneShotSafe(behindNeck3DAudio, audio != null ? audio.ghostWhisperClip : null, 1.0f);
            }

            if (activePattern.darkroomClimaxType == 0)
            {
                // クライマックスパターン0：後ろを振り返った瞬間に超高精細モデル（ScaryWoman_Ghost_01）が至近距離に現れる！
                SetSubtitle("『…見つけた…』（後ろを振り返る）");
                float turnWait = 0f;
                while (turnWait < 3.5f)
                {
                    turnWait += Time.deltaTime;
                    if (turnWait > 0.4f && gazeDetector != null && gazeDetector.isLookingBack)
                    {
                        break;
                    }
                    yield return null;
                }
            }
            else
            {
                // クライマックスパターン1/2：正面デスクの向こうにまずチープ人形が一瞬見え、明滅した瞬間に高精細モデルへ豹変して飛びかかる！
                if (cheapDummyGhostRoot != null)
                {
                    cheapDummyGhostRoot.localPosition = new Vector3(0f, 0f, 1.8f);
                    cheapDummyGhostRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    cheapDummyGhostRoot.gameObject.SetActive(true);
                }
                SetSubtitle("正面にまたあのチープな影が…？");
                yield return new WaitForSeconds(1.1f);
                if (cheapDummyGhostRoot != null) cheapDummyGhostRoot.gameObject.SetActive(false);
            }

            // 超高精細3Dモデル（ScaryWoman_Ghost_01）がプレイヤーの顔面（HMD）へ急襲！
            SetSubtitle("");
            PlayOneShotSafe(behindNeck3DAudio, audio != null ? audio.jumpscareScreamClip : null, 1.0f);
            if (VRPlayerRigController.Instance != null)
            {
                VRPlayerRigController.Instance.TriggerHaptics(1.0f, 0.85f);
            }

            yield return StartCoroutine(PlayRealScaryWomanJumpscareRoutine(0.78f));

            // ブラックアウト＆余韻
            if (safelightBuzzAudio != null) safelightBuzzAudio.Stop();
            ShowBlackoutEnding($"暗室（ダークルーム）\n- 完 -\n(Pattern #{activePattern.patternId:00})");
            PlayOneShotSafe(behindNeck3DAudio, audio != null ? audio.tinnitusRingClip : null, 0.85f);

            yield return new WaitForSeconds(5.5f);
            if (Application.CanStreamedLevelBeLoaded("00_BootScene"))
            {
                SceneManager.LoadScene("00_BootScene");
            }
        }

        private IEnumerator AnimatePhotoDevelopingRoutine(int photoIndex, float duration)
        {
            float scaled = Mathf.Max(0.5f, duration / Mathf.Max(0.1f, timelineSpeedMultiplier));
            float elapsed = 0f;
            while (elapsed < scaled)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / scaled);
                int pct = Mathf.RoundToInt(t * 100f);
                SetPhotoPaperVisual(
                    Color.Lerp(new Color(0.85f, 0.82f, 0.78f), new Color(0.42f, 0.15f, 0.15f), t),
                    $"【現像中 ({photoIndex}/3) : {pct}%】"
                );
                if (redSafelight != null && photoIndex == 3)
                {
                    redSafelight.intensity = (Random.value > 0.3f) ? Random.Range(0.3f, 1.4f) : 0.05f;
                }
                yield return null;
            }
            if (redSafelight != null) redSafelight.intensity = 1.1f;
        }

        private IEnumerator PlayRealScaryWomanJumpscareRoutine(float duration)
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
                float dist = Mathf.Lerp(0.85f, 0.31f, t);

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

        private void SetPhotoPaperVisual(Color color, string caption)
        {
            if (photoPaperRenderer != null)
            {
                photoPaperRenderer.material.color = color;
                if (photoPaperRenderer.material.HasProperty("_BaseColor"))
                    photoPaperRenderer.material.SetColor("_BaseColor", color);
            }
            if (photoCaptionText != null)
            {
                photoCaptionText.text = caption;
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
