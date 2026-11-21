using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using HorrorGame.Core;
using HorrorGame.Audio;

namespace HorrorGame.ScenarioA
{
    public enum ElevatorPhase
    {
        Phase1_IntroAndMidStop,
        Phase2_MirrorGhostAnd4FStop,
        Phase3_DontLookBackEndurance,
        Phase4_1FArrivalAndClimax,
        Ended
    }

    /// <summary>
    /// 案A：『深夜3時のエレベーター』（約5分・1プレイ完結）のランダムパターン対応シナリオ進行マネージャー。
    /// 導入4種 × 中盤3種 × クライマックス3種 ＝ 36通りの組み合わせに対応。
    /// 「1回目はこてこてのAI風チープモデルで油断させ、振り返った時と最後に作り込まれた高精細3Dモデル（ScaryWoman_Ghost_01）が現れる」演出を実行します。
    /// </summary>
    public class ElevatorScenarioManager : MonoBehaviour
    {
        [Header("コンポーネント参照")]
        public ElevatorEnvironmentRefs env;
        public PlayerGazeDetector gazeDetector;
        public MirrorGhostController ghostController;

        [Header("タイムライン速度調整")]
        [Tooltip("1.0 = 標準プレイ尺。テスト時に上げると倍速確認が可能です。")]
        [Range(0.5f, 4.0f)]
        public float timelineSpeedMultiplier = 1.0f;

        [Tooltip("フェーズ3の耐久時間（秒）")]
        public float phase3EnduranceDuration = 38.0f;

        [Header("現在実行中のランダムパターン情報（ReadOnly）")]
        public ElevatorPhase currentPhase = ElevatorPhase.Phase1_IntroAndMidStop;
        public int activePatternId;
        public ElevatorIntroVariant activeIntroVariant;
        public ElevatorMiddleVariant activeMiddleVariant;
        public ElevatorClimaxVariant activeClimaxVariant;

        private bool floor1PressedFlag;
        private bool doorClosePressedFlag;
        private bool alarmPressedFlag;
        private bool doorOpenPressedFlag;

        private void Start()
        {
            if (env == null) env = FindFirstObjectByType<ElevatorEnvironmentRefs>();
            if (gazeDetector == null) gazeDetector = FindFirstObjectByType<PlayerGazeDetector>();
            if (ghostController == null) ghostController = FindFirstObjectByType<MirrorGhostController>();

            ScenarioPatternData pattern = ActiveScenarioConfig.GetOrRollPatternForScene(ScenarioType.ScenarioA_Elevator);
            activePatternId = pattern.patternId;
            activeIntroVariant = pattern.elevatorIntro;
            activeMiddleVariant = pattern.elevatorMiddle;
            activeClimaxVariant = pattern.elevatorClimax;

            Debug.Log($"[ElevatorScenarioManager] Running Pattern #{activePatternId}: Intro={activeIntroVariant}, Mid={activeMiddleVariant}, Climax={activeClimaxVariant}");

            BindButtons();
            StartCoroutine(RunFullScenarioTimeline());
        }

        private void BindButtons()
        {
            if (env == null) return;
            if (env.buttonFloor1 != null)
                env.buttonFloor1.OnButtonPressed += _ => floor1PressedFlag = true;
            if (env.buttonClose != null)
                env.buttonClose.OnButtonPressed += _ => doorClosePressedFlag = true;
            if (env.buttonAlarm != null)
                env.buttonAlarm.OnButtonPressed += _ => alarmPressedFlag = true;
            if (env.buttonOpen != null)
                env.buttonOpen.OnButtonPressed += _ => doorOpenPressedFlag = true;
        }

        private WaitForSeconds ScaledWait(float seconds)
        {
            float s = Mathf.Max(0.05f, seconds / Mathf.Max(0.1f, timelineSpeedMultiplier));
            return new WaitForSeconds(s);
        }

        private IEnumerator RunFullScenarioTimeline()
        {
            var audio = ProceduralHorrorAudio.Instance;

            // =========================================================================
            // 【フェーズ1：導入とランダム途中階停止（4バリエーション）】
            // =========================================================================
            currentPhase = ElevatorPhase.Phase1_IntroAndMidStop;
            env.SetBlackout(false);
            env.SetDoorOpenAmount(0f);
            env.SetFloorText("10");
            env.SetFluorescentLightIntensity(1.05f);
            env.SetRedEmergencyLight(false);
            env.SetIntercomLed(false);
            if (env.corridor8FGroup != null) env.corridor8FGroup.SetActive(true);
            if (env.corridor1FHorrorGroup != null) env.corridor1FHorrorGroup.SetActive(false);

            if (env.ceilingHumAudio != null && audio != null)
            {
                env.ceilingHumAudio.clip = audio.fluorescentHumClip;
                env.ceilingHumAudio.loop = true;
                env.ceilingHumAudio.volume = 0.45f;
                env.ceilingHumAudio.Play();
            }

            env.SetSubtitleText($"深夜3:00 - 残業帰り (Pattern #{activePatternId:00})\n[ 1 ] ボタンを押して1階へ降りる");
            env.buttonFloor1.SetBlinkingGuide(true);

            floor1PressedFlag = false;
            float introWait = 0f;
            while (!floor1PressedFlag)
            {
                introWait += Time.deltaTime;
                float subtleFlicker = 1.0f + Mathf.Sin(introWait * 23f) * 0.04f;
                env.SetFluorescentLightIntensity(subtleFlicker);
                yield return null;
            }

            env.buttonFloor1.SetBlinkingGuide(false);
            env.SetSubtitleText("");

            StartElevatorMotor(true, 0.55f);
            yield return ScaledWait(5.5f);

            // 導入バリエーション（4パターン）の分岐実行
            yield return StartCoroutine(RunPhase1IntroVariantRoutine());

            // =========================================================================
            // 【フェーズ2：鏡の怪異（1回目＝チープなAIモデルで油断 → 振り返り＝高精細モデル！）】
            // =========================================================================
            currentPhase = ElevatorPhase.Phase2_MirrorGhostAnd4FStop;
            StartElevatorMotor(true, 0.6f);
            yield return ScaledWait(4.5f);

            env.SetFloorText("6");
            yield return StartCoroutine(FlickerFluorescentRoutine(1.2f));

            // 1回目の鏡：あえて「こてこてのAI風チープなプリミティブモデル」を鏡にだけ映してプレイヤーを油断させる！
            ghostController.ShowMirrorGhostStage(1);
            env.SetSubtitleText("…鏡の中に、何かチープな人影が映っている…？");

            float mirrorObserveTimer = 0f;
            float maxMirrorWait = 9.0f / Mathf.Max(0.1f, timelineSpeedMultiplier);
            while (mirrorObserveTimer < maxMirrorWait)
            {
                mirrorObserveTimer += Time.deltaTime;
                if (gazeDetector != null && gazeDetector.isLookingAtAnyMirror && mirrorObserveTimer > 2.2f)
                {
                    break;
                }
                yield return null;
            }

            env.SetSubtitleText("後ろに誰かいる…？（後ろを振り返って確認する）");

            float lookBackWait = 0f;
            float maxLookBackWait = 11.0f / Mathf.Max(0.1f, timelineSpeedMultiplier);
            while (lookBackWait < maxLookBackWait)
            {
                lookBackWait += Time.deltaTime;
                if (gazeDetector != null && gazeDetector.isLookingBack)
                {
                    break;
                }
                yield return null;
            }

            // 「チープなモデルだろう」と思って振り返った瞬間、作り込まれた高精細モデル（ScaryWoman_Ghost_01）が背後に現れる！
            ghostController.SetMirrorGhostVisible(false);
            ghostController.SetTurnedAroundGhostVisible(true, false);
            if (env.behindNeck3DAudio != null && Camera.main != null)
            {
                Transform cam = Camera.main.transform;
                env.behindNeck3DAudio.transform.position = cam.position - cam.forward * 0.25f + cam.right * 0.2f;
                PlayOneShotSafe(env.behindNeck3DAudio, audio != null ? audio.ghostWhisperClip : null, 0.95f);
            }
            env.SetSubtitleText("『…見えた？』");

            yield return new WaitForSeconds(0.65f);
            yield return StartCoroutine(FlickerFluorescentRoutine(0.35f));
            ghostController.SetTurnedAroundGhostVisible(false, false);
            env.SetSubtitleText("…今、生々しい女がすぐそこにいた…！？（正面へ戻る）");

            yield return ScaledWait(2.5f);

            // 再び正面の鏡を見ると、今度は高精細モデル（ScaryWoman_Ghost_01）が真後ろに映っている！
            env.SetFloorText("5");
            yield return StartCoroutine(FlickerFluorescentRoutine(0.7f));
            ghostController.ShowMirrorGhostStage(2);
            env.SetSubtitleText("鏡の中の女が、すぐ真後ろに迫っている…！");

            float stage2Timer = 0f;
            float stage2Duration = 6.5f / Mathf.Max(0.1f, timelineSpeedMultiplier);
            while (stage2Timer < stage2Duration)
            {
                stage2Timer += Time.deltaTime;
                if (gazeDetector != null)
                {
                    ghostController.SetMirrorGhostVisible(!gazeDetector.isLookingBack);
                }
                yield return null;
            }

            // 「4階」で緊急停止！
            env.SetFloorText("4");
            yield return ScaledWait(1.0f);

            ghostController.SetMirrorGhostVisible(false);
            StartElevatorMotor(false);
            if (env.ceilingHumAudio != null) env.ceilingHumAudio.Stop();

            PlayOneShotSafe(env.doorBellAndSlideAudio, audio != null ? audio.emergencyStopCrashClip : null, 1.0f);
            if (VRPlayerRigController.Instance != null)
            {
                VRPlayerRigController.Instance.TriggerHaptics(1.0f, 0.45f);
            }

            env.SetFloorText("E", new Color(1.0f, 0.05f, 0.05f));
            env.SetFluorescentLightIntensity(0f);
            env.SetRedEmergencyLight(true, 1.25f);

            yield return ScaledWait(2.2f);
            env.SetSubtitleText("緊急停止した…\n点滅している [ 非常ベル ] ボタンを押す");
            env.buttonAlarm.SetBlinkingGuide(true);

            // =========================================================================
            // 【フェーズ3：インターホン通信＆『振り返ってはいけない』耐久（3バリエーション）】
            // =========================================================================
            currentPhase = ElevatorPhase.Phase3_DontLookBackEndurance;
            alarmPressedFlag = false;
            while (!alarmPressedFlag)
            {
                yield return null;
            }

            env.buttonAlarm.SetBlinkingGuide(false);
            env.SetIntercomLed(true);
            PlayOneShotSafe(env.intercom3DAudio, audio != null ? audio.intercomMessageClip : null, 0.95f);

            // 中盤バリエーションに応じた管理人通信と指定ボタン
            ElevatorButton targetHoldButton = env.buttonFloor1;
            string buttonLabel = "[ 1 ]";

            if (activeMiddleVariant == ElevatorMiddleVariant.Mid0_HoldButton1_Standard)
            {
                env.SetSubtitleText("【インターホン】\n『…こちら管理室！今、4階で止まってますね！？』");
                yield return ScaledWait(3.0f);
                env.SetSubtitleText("【インターホン】\n『いいですか、絶対に後ろを振り返らないでください！！\n正面の [ 1 ] ボタンを長押しして再起動させてください！』");
                yield return ScaledWait(3.6f);
                targetHoldButton = env.buttonFloor1;
                buttonLabel = "[ 1 ]";
            }
            else if (activeMiddleVariant == ElevatorMiddleVariant.Mid1_HoldDoorClose_MirrorWarn)
            {
                env.SetSubtitleText("【インターホン】\n『…こちら管理室！4階の磁気異常です！』");
                yield return ScaledWait(3.0f);
                env.SetSubtitleText("【インターホン】\n『鏡を見ちゃダメです！後ろも絶対に振り返らないで！\nそのまま [ 閉 ] ボタンを押し続けてください！！』");
                yield return ScaledWait(3.6f);
                targetHoldButton = env.buttonClose;
                buttonLabel = "[ 閉 ]";
            }
            else // Mid2_HijackedIntercomVoice
            {
                env.SetSubtitleText("【インターホン】\n『…こちら管理室！絶対に後ろを見ずに [ 1 ] ボタンを長押し…ザザッ…』");
                yield return ScaledWait(3.2f);
                PlayOneShotSafe(env.intercom3DAudio, audio != null ? audio.ghostWhisperClip : null, 1.0f);
                env.SetSubtitleText("【インターホン（女の声）】\n『…うそだよ…ねえ、うしろを見て…こっち向いてよ…』\n（※絶対に振り返らず [ 1 ] ボタンを長押しし続けろ！）");
                yield return ScaledWait(3.6f);
                targetHoldButton = env.buttonFloor1;
                buttonLabel = "[ 1 ]";
            }

            env.SetIntercomLed(false);
            targetHoldButton.SetBlinkingGuide(true);

            if (env.behindNeck3DAudio != null && audio != null)
            {
                env.behindNeck3DAudio.clip = audio.ghostBreathingLoopClip;
                env.behindNeck3DAudio.loop = true;
                env.behindNeck3DAudio.volume = 0.9f;
                env.behindNeck3DAudio.Play();
            }
            if (env.earMoving3DAudio != null && audio != null)
            {
                env.earMoving3DAudio.clip = audio.ghostDragLoopClip;
                env.earMoving3DAudio.loop = true;
                env.earMoving3DAudio.volume = 0.75f;
                env.earMoving3DAudio.Play();
            }

            float targetEndurance = phase3EnduranceDuration / Mathf.Max(0.1f, timelineSpeedMultiplier);
            float currentEndurance = 0f;
            bool whisperedMidway = false;
            bool lookedBackViolation = false;
            float gracePeriod = 1.5f;

            while (currentEndurance < targetEndurance)
            {
                gracePeriod -= Time.deltaTime;

                if (Camera.main != null)
                {
                    Transform cam = Camera.main.transform;
                    if (env.behindNeck3DAudio != null)
                    {
                        env.behindNeck3DAudio.transform.position = cam.position - cam.forward * 0.28f - cam.up * 0.05f;
                    }
                    if (env.earMoving3DAudio != null)
                    {
                        float earPan = Mathf.Sin(Time.time * 0.85f) * 0.28f;
                        env.earMoving3DAudio.transform.position = cam.position + cam.right * earPan - cam.forward * 0.15f;
                    }
                }

                // もし後ろを振り返ってしまったら → 高精細モデル（ScaryWoman_Ghost_01）が目の前に現れて即死ジャンプスケア！
                if (gracePeriod <= 0f && gazeDetector != null && gazeDetector.isLookingBack)
                {
                    lookedBackViolation = true;
                    break;
                }

                bool holdingTarget = targetHoldButton.IsCurrentlyHeld || env.buttonFloor1.IsCurrentlyHeld || env.buttonClose.IsCurrentlyHeld;
                float progressRate = holdingTarget ? 1.45f : 0.70f;
                currentEndurance += Time.deltaTime * progressRate;

                float norm = Mathf.Clamp01(currentEndurance / targetEndurance);
                ghostController.SetPeripheralHandsProgress(norm, true);

                if (!whisperedMidway && norm >= 0.55f)
                {
                    whisperedMidway = true;
                    if (env.earMoving3DAudio != null && audio != null)
                    {
                        env.earMoving3DAudio.PlayOneShot(audio.ghostWhisperClip, 1.0f);
                    }
                }

                int percent = Mathf.Clamp(Mathf.RoundToInt(norm * 100f), 0, 100);
                string holdStatus = holdingTarget ? "【長押し中】" : $"【{buttonLabel}ボタンを長押ししてください】";
                env.SetSubtitleText($"絶対に後ろを振り返るな\n{holdStatus} 再起動まで: {percent}%");

                if (holdingTarget && VRPlayerRigController.Instance != null && Time.frameCount % 25 == 0)
                {
                    VRPlayerRigController.Instance.TriggerHaptics(0.15f + norm * 0.25f, 0.05f);
                }

                yield return null;
            }

            if (env.behindNeck3DAudio != null) env.behindNeck3DAudio.Stop();
            if (env.earMoving3DAudio != null) env.earMoving3DAudio.Stop();
            targetHoldButton.SetBlinkingGuide(false);
            env.buttonFloor1.SetBlinkingGuide(false);
            env.buttonClose.SetBlinkingGuide(false);
            ghostController.SetPeripheralHandsProgress(0f, false);

            if (lookedBackViolation)
            {
                env.SetSubtitleText("");
                PlayOneShotSafe(env.behindNeck3DAudio, audio != null ? audio.jumpscareScreamClip : null, 1.0f);
                if (VRPlayerRigController.Instance != null)
                {
                    VRPlayerRigController.Instance.TriggerHaptics(1.0f, 0.7f);
                }
                yield return StartCoroutine(ghostController.PlayLookBackViolationJumpscareRoutine(0.78f));
                yield return StartCoroutine(PlayBlackoutEndingRoutine("BAD END\n振り返ってはならなかった"));
                yield break;
            }

            // 耐久成功 → 電力復旧して1階へ下降
            PlayOneShotSafe(env.doorBellAndSlideAudio, audio != null ? audio.doorSlideClip : null, 0.8f);
            TriggerSubtleRumble(0.7f, 0.2f);
            env.SetRedEmergencyLight(false);
            env.SetFluorescentLightIntensity(1.05f);
            if (env.ceilingHumAudio != null) env.ceilingHumAudio.Play();

            env.SetSubtitleText("電力が復旧した…！");
            StartElevatorMotor(true, 0.65f);

            env.SetFloorText("3");
            yield return ScaledWait(2.8f);
            env.SetFloorText("2");
            yield return ScaledWait(2.8f);

            // =========================================================================
            // 【フェーズ4：1階到着＆クライマックス（3バリエーション・高精細モデル出現！）】
            // =========================================================================
            currentPhase = ElevatorPhase.Phase4_1FArrivalAndClimax;
            env.SetFloorText("1");
            StartElevatorMotor(false);
            PlayOneShotSafe(env.doorBellAndSlideAudio, audio != null ? audio.elevatorDingClip : null, 0.9f);

            if (env.corridor8FGroup != null) env.corridor8FGroup.SetActive(false);
            if (env.corridor1FHorrorGroup != null) env.corridor1FHorrorGroup.SetActive(true);

            yield return ScaledWait(1.5f);
            env.SetSubtitleText("1階に到着した。\n[ 開 ] ボタンを押して外へ出る");
            env.buttonOpen.SetBlinkingGuide(true);

            doorOpenPressedFlag = false;
            while (!doorOpenPressedFlag)
            {
                yield return null;
            }

            env.buttonOpen.SetBlinkingGuide(false);
            env.SetSubtitleText("");

            if (env.hallway3DAudio != null && audio != null)
            {
                env.hallway3DAudio.clip = audio.dissonantStringsLoopClip;
                env.hallway3DAudio.loop = true;
                env.hallway3DAudio.volume = 0.65f;
                env.hallway3DAudio.Play();
            }

            // クライマックス3バリエーションの分岐実行
            yield return StartCoroutine(RunPhase4ClimaxVariantRoutine());

            yield return StartCoroutine(PlayBlackoutEndingRoutine("深夜3時のエレベーター\n- 完 -"));
        }

        private IEnumerator RunPhase1IntroVariantRoutine()
        {
            var audio = ProceduralHorrorAudio.Instance;

            if (activeIntroVariant == ElevatorIntroVariant.Var0_8F_DarkCorridorRush)
            {
                // バリエーション0：8階停止・暗い廊下の足音＋閉まる直前の猛ダッシュ
                env.SetFloorText("9");
                TriggerSubtleRumble();
                yield return ScaledWait(4.5f);

                env.SetFloorText("8");
                StartElevatorMotor(false);
                PlayOneShotSafe(env.doorBellAndSlideAudio, audio != null ? audio.elevatorDingClip : null, 0.8f);
                env.SetSubtitleText("…8階？ 押していないのに止まった…");
                yield return ScaledWait(2.0f);

                yield return StartCoroutine(AnimateDoorsRoutine(0f, 1f, 2.3f));
                yield return ScaledWait(1.0f);
                PlayOneShotSafe(env.hallway3DAudio, audio != null ? audio.hallwayFootstepsSlowClip : null, 0.95f);
                env.SetSubtitleText("外には誰もいない…\n[ 閉 ] ボタンで扉を閉める");
                env.buttonClose.SetBlinkingGuide(true);

                yield return StartCoroutine(WaitForCloseButtonOrTimeout(8.5f));

                PlayOneShotSafe(env.hallway3DAudio, audio != null ? audio.hallwayFootstepsRushClip : null, 1.0f);
                yield return StartCoroutine(AnimateDoorsRoutine(1f, 0f, 1.8f));
                TriggerSubtleRumble(0.6f, 0.15f);
            }
            else if (activeIntroVariant == ElevatorIntroVariant.Var1_7F_CheapDummyStanding)
            {
                // バリエーション1：7階停止・廊下に「こてこてのAI風チープなブロック人影」が棒立ちしていてプレイヤーを油断させる！
                env.SetFloorText("9");
                yield return ScaledWait(3.5f);
                env.SetFloorText("8");
                yield return ScaledWait(3.5f);

                env.SetFloorText("7");
                StartElevatorMotor(false);
                PlayOneShotSafe(env.doorBellAndSlideAudio, audio != null ? audio.elevatorDingClip : null, 0.8f);
                env.SetSubtitleText("…7階で勝手に止まった…");
                yield return ScaledWait(1.8f);

                // 廊下の奥（z=2.9m）にチープなブロック人形を配置して扉を開く
                ghostController.ShowCheapDummyInHallway(true, new Vector3(0f, 0f, 2.9f));
                yield return StartCoroutine(AnimateDoorsRoutine(0f, 0.75f, 2.2f));
                env.SetSubtitleText("廊下に何かチープな人形が棒立ちしている…\n[ 閉 ] ボタンで扉を閉める");
                env.buttonClose.SetBlinkingGuide(true);

                yield return StartCoroutine(WaitForCloseButtonOrTimeout(7.5f));

                yield return StartCoroutine(AnimateDoorsRoutine(0.75f, 0f, 1.8f));
                ghostController.ShowCheapDummyInHallway(false, Vector3.zero);
            }
            else if (activeIntroVariant == ElevatorIntroVariant.Var2_9F_RedHallwayWhisper)
            {
                // バリエーション2：9階停止・赤い非常灯が灯る廊下から囁き声が聞こえて勝手に閉まる
                env.SetFloorText("9");
                StartElevatorMotor(false);
                PlayOneShotSafe(env.doorBellAndSlideAudio, audio != null ? audio.elevatorDingClip : null, 0.85f);
                env.SetRedEmergencyLight(true, 0.7f);
                env.SetSubtitleText("…9階？ 非常灯が点いている…");
                yield return ScaledWait(1.8f);

                yield return StartCoroutine(AnimateDoorsRoutine(0f, 0.85f, 2.2f));
                PlayOneShotSafe(env.hallway3DAudio, audio != null ? audio.ghostWhisperClip : null, 0.95f);
                env.SetSubtitleText("『…乗せて…』\n[ 閉 ] ボタンで扉を閉める");
                env.buttonClose.SetBlinkingGuide(true);

                yield return StartCoroutine(WaitForCloseButtonOrTimeout(6.5f));

                PlayOneShotSafe(env.hallway3DAudio, audio != null ? audio.hallwayFootstepsRushClip : null, 0.9f);
                yield return StartCoroutine(AnimateDoorsRoutine(0.85f, 0f, 1.6f));
                env.SetRedEmergencyLight(false);
                env.SetFloorText("8");
                yield return ScaledWait(2.5f);
            }
            else // Var3_6F_CrazyBellAndCrack
            {
                // バリエーション3：7階で到着ベルが狂ったように連続で鳴り響き、扉の18cmの隙間から白い手が覗く
                env.SetFloorText("9");
                yield return ScaledWait(3.5f);
                env.SetFloorText("8");
                yield return ScaledWait(3.5f);

                env.SetFloorText("7");
                StartElevatorMotor(false);
                for (int b = 0; b < 3; b++)
                {
                    PlayOneShotSafe(env.doorBellAndSlideAudio, audio != null ? audio.elevatorDingClip : null, 0.9f);
                    yield return new WaitForSeconds(0.45f);
                }

                env.SetSubtitleText("ベルが鳴り止まない…！？ 扉が少しだけ開いた…");
                yield return StartCoroutine(AnimateDoorsRoutine(0f, 0.22f, 1.2f));
                PlayOneShotSafe(env.hallway3DAudio, audio != null ? audio.hallwayFootstepsSlowClip : null, 1.0f);
                env.SetSubtitleText("隙間の向こうに誰かいる！\n[ 閉 ] ボタンで扉を閉める");
                env.buttonClose.SetBlinkingGuide(true);

                yield return StartCoroutine(WaitForCloseButtonOrTimeout(6.5f));

                yield return StartCoroutine(AnimateDoorsRoutine(0.22f, 0f, 1.1f));
                TriggerSubtleRumble(0.7f, 0.15f);
            }
        }

        private IEnumerator WaitForCloseButtonOrTimeout(float maxSeconds)
        {
            doorClosePressedFlag = false;
            float t = 0f;
            float scaledMax = maxSeconds / Mathf.Max(0.1f, timelineSpeedMultiplier);
            while (!doorClosePressedFlag && t < scaledMax)
            {
                t += Time.deltaTime;
                yield return null;
            }
            env.buttonClose.SetBlinkingGuide(false);
            env.SetSubtitleText("");
        }

        private IEnumerator RunPhase4ClimaxVariantRoutine()
        {
            var audio = ProceduralHorrorAudio.Instance;

            if (activeClimaxVariant == ElevatorClimaxVariant.Climax0_HallwayStare_Then_CeilingDrop)
            {
                // クライマックス0：1階扉が開くと高精細モデル（ScaryWoman_Ghost_01）が正面に立ち、消えた後に天井から落下！
                ghostController.SetFirstFloorHallGhostVisible(true, 1.55f);

                yield return StartCoroutine(AnimateDoorsRoutine(0f, 1f, 2.8f));
                env.SetSubtitleText("…扉の前に、あの女が立っている…！？");
                yield return ScaledWait(2.2f);

                yield return StartCoroutine(FlickerFluorescentRoutine(0.45f));
                ghostController.SetFirstFloorHallGhostVisible(false);

                if (env.ceilingHatchCover != null)
                {
                    env.ceilingHatchCover.localRotation = Quaternion.Euler(32f, 0f, 0f);
                }

                env.SetSubtitleText("…上から何か垂れてきている…？（天井を見上げる）");

                float dripWait = 0f;
                float maxDripWait = 4.2f;
                float nextDripTime = 0.1f;

                while (dripWait < maxDripWait)
                {
                    dripWait += Time.deltaTime;
                    if (dripWait >= nextDripTime)
                    {
                        nextDripTime += 1.0f;
                        env.SpawnCeilingBloodDroplet();
                        PlayOneShotSafe(env.ceilingDrip3DAudio, audio != null ? audio.bloodDripClip : null, 1.0f);
                    }

                    if (dripWait > 0.6f && gazeDetector != null && gazeDetector.isLookingUpAtCeiling)
                    {
                        break;
                    }
                    yield return null;
                }

                if (env.ceilingHatchCover != null)
                {
                    env.ceilingHatchCover.localRotation = Quaternion.Euler(85f, 0f, 0f);
                }
                env.SetSubtitleText("");

                if (env.hallway3DAudio != null) env.hallway3DAudio.Stop();
                PlayOneShotSafe(env.ceilingDrip3DAudio, audio != null ? audio.jumpscareScreamClip : null, 1.0f);
                if (VRPlayerRigController.Instance != null)
                {
                    VRPlayerRigController.Instance.TriggerHaptics(1.0f, 0.8f);
                }

                Vector3 hatchWorldPos = (env.ceilingHatchPoint != null) ? env.ceilingHatchPoint.position : new Vector3(0f, 2.25f, 0.25f);
                yield return StartCoroutine(ghostController.PlayCeilingDropJumpscareRoutine(hatchWorldPos, 0.75f));
            }
            else if (activeClimaxVariant == ElevatorClimaxVariant.Climax1_CheapToReal_TeleportRush)
            {
                // クライマックス1：最初は廊下の奥（z=3.4m）に「こてこてのAI風チープモデル」が立っていて油断させ、
                // 照明が明滅した瞬間に目の前1.15mで「高精細モデル（ScaryWoman_Ghost_01）」に豹変して顔面へ突進！
                ghostController.ShowCheapDummyInHallway(true, new Vector3(0f, 0f, 3.3f));

                yield return StartCoroutine(AnimateDoorsRoutine(0f, 1f, 2.6f));
                env.SetSubtitleText("…廊下の奥に、またあのチープな人形が立っているだけ…？");
                yield return ScaledWait(2.2f);

                // 照明がバチッと明滅して、チープな人形から超高精細モデル（ScaryWoman_Ghost_01）へ瞬間移動＆豹変！
                yield return StartCoroutine(FlickerFluorescentRoutine(0.45f));
                ghostController.ShowCheapDummyInHallway(false, Vector3.zero);
                ghostController.SetFirstFloorHallGhostVisible(true, 1.15f);
                PlayOneShotSafe(env.hallway3DAudio, audio != null ? audio.lightFlickerBuzzClip : null, 1.0f);
                env.SetSubtitleText("！？");
                yield return new WaitForSeconds(0.55f);

                // そこから一気にプレイヤーの顔面（HMD）へ猛スピード突進！
                env.SetSubtitleText("");
                if (env.hallway3DAudio != null) env.hallway3DAudio.Stop();
                PlayOneShotSafe(env.doorBellAndSlideAudio, audio != null ? audio.jumpscareScreamClip : null, 1.0f);
                if (VRPlayerRigController.Instance != null)
                {
                    VRPlayerRigController.Instance.TriggerHaptics(1.0f, 0.85f);
                }

                yield return StartCoroutine(ghostController.PlayHallwayRushJumpscareRoutine(new Vector3(0f, 0f, 1.15f), 0.75f));
            }
            else // Climax2_FakeEscape_DoorSlam_TurnBack
            {
                // クライマックス2：1階で扉が開くが誰もいない（偽の脱出成功）→ 直後に扉が強制閉鎖＆血文字で『うしろ』→ 振り返ると高精細モデル！
                yield return StartCoroutine(AnimateDoorsRoutine(0f, 1f, 2.6f));
                env.SetSubtitleText("…外には誰もいない。助かった…？");
                yield return ScaledWait(2.2f);

                // 突然「ガンッ！」と扉が高速で強制閉鎖！
                PlayOneShotSafe(env.doorBellAndSlideAudio, audio != null ? audio.emergencyStopCrashClip : null, 1.0f);
                yield return StartCoroutine(AnimateDoorsRoutine(1f, 0f, 0.45f));
                env.SetFluorescentLightIntensity(0.08f);
                env.SetRedEmergencyLight(true, 1.5f);
                env.SetFloorText("後", new Color(1.0f, 0.02f, 0.02f));
                env.SetSubtitleText("扉が勝手に閉まった！？\n『…うしろ…』（後ろを振り返る）");

                if (env.behindNeck3DAudio != null && Camera.main != null)
                {
                    Transform cam = Camera.main.transform;
                    env.behindNeck3DAudio.transform.position = cam.position - cam.forward * 0.22f;
                    PlayOneShotSafe(env.behindNeck3DAudio, audio != null ? audio.ghostWhisperClip : null, 1.0f);
                }

                float turnWait = 0f;
                while (turnWait < 3.0f)
                {
                    turnWait += Time.deltaTime;
                    if (turnWait > 0.4f && gazeDetector != null && gazeDetector.isLookingBack)
                    {
                        break;
                    }
                    yield return null;
                }

                env.SetSubtitleText("");
                if (env.hallway3DAudio != null) env.hallway3DAudio.Stop();
                PlayOneShotSafe(env.behindNeck3DAudio, audio != null ? audio.jumpscareScreamClip : null, 1.0f);
                if (VRPlayerRigController.Instance != null)
                {
                    VRPlayerRigController.Instance.TriggerHaptics(1.0f, 0.85f);
                }

                yield return StartCoroutine(ghostController.PlayLookBackViolationJumpscareRoutine(0.78f));
            }
        }

        private IEnumerator PlayBlackoutEndingRoutine(string titleText)
        {
            currentPhase = ElevatorPhase.Ended;
            var audio = ProceduralHorrorAudio.Instance;

            if (env.ceilingHumAudio != null) env.ceilingHumAudio.Stop();
            if (env.elevatorMotorAudio != null) env.elevatorMotorAudio.Stop();
            if (env.hallway3DAudio != null) env.hallway3DAudio.Stop();

            env.SetBlackout(true, "");
            PlayOneShotSafe(env.behindNeck3DAudio, audio != null ? audio.tinnitusRingClip : null, 0.85f);

            yield return new WaitForSeconds(2.0f);
            env.SetBlackout(true, $"{titleText}\n(Pattern #{activePatternId:00})");

            yield return new WaitForSeconds(4.5f);
            if (Application.CanStreamedLevelBeLoaded("00_BootScene"))
            {
                SceneManager.LoadScene("00_BootScene");
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        private IEnumerator AnimateDoorsRoutine(float fromOpen, float toOpen, float duration)
        {
            var audio = ProceduralHorrorAudio.Instance;
            PlayOneShotSafe(env.doorBellAndSlideAudio, audio != null ? audio.doorSlideClip : null, 0.75f);

            float scaledDuration = Mathf.Max(0.2f, duration / Mathf.Max(0.1f, timelineSpeedMultiplier));
            float elapsed = 0f;
            while (elapsed < scaledDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / scaledDuration);
                env.SetDoorOpenAmount(Mathf.Lerp(fromOpen, toOpen, t));
                yield return null;
            }
            env.SetDoorOpenAmount(toOpen);
        }

        private IEnumerator FlickerFluorescentRoutine(float duration)
        {
            var audio = ProceduralHorrorAudio.Instance;
            PlayOneShotSafe(env.ceilingHumAudio, audio != null ? audio.lightFlickerBuzzClip : null, 0.9f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bool on = Random.value > 0.45f;
                env.SetFluorescentLightIntensity(on ? Random.Range(0.2f, 1.2f) : 0.02f);
                yield return new WaitForSeconds(0.05f);
            }
            env.SetFluorescentLightIntensity(0.85f);
        }

        private void StartElevatorMotor(bool active, float volume = 0.55f)
        {
            if (env == null || env.elevatorMotorAudio == null) return;
            var audio = ProceduralHorrorAudio.Instance;
            if (active)
            {
                if (audio != null && env.elevatorMotorAudio.clip == null)
                {
                    env.elevatorMotorAudio.clip = audio.elevatorMoveLoopClip;
                }
                env.elevatorMotorAudio.loop = true;
                env.elevatorMotorAudio.volume = volume;
                if (!env.elevatorMotorAudio.isPlaying) env.elevatorMotorAudio.Play();
            }
            else
            {
                env.elevatorMotorAudio.Stop();
            }
        }

        private void TriggerSubtleRumble(float amplitude = 0.25f, float duration = 0.12f)
        {
            if (VRPlayerRigController.Instance != null)
            {
                VRPlayerRigController.Instance.TriggerHaptics(amplitude, duration);
            }
        }

        private void PlayOneShotSafe(AudioSource source, AudioClip clip, float volume = 1.0f)
        {
            if (source != null && clip != null)
            {
                source.PlayOneShot(clip, volume);
            }
        }
    }
}
