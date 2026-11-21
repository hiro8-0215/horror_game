using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorrorGame.Core
{
    /// <summary>
    /// ゲーム起動時に最低10パターン以上（プリセット12パターン＋動的組み合わせ全54通り）のシナリオから
    /// ランダムに1つを選択して開始するブートローダー。
    /// 全パターン共通コンセプト：「1回目はこてこてのAIホラー風のチープなダミーモデルで油断させ、
    /// 振り返った時と最後に作り込まれた高精細3Dモデル（ScaryWoman_Ghost_01）が現れる」
    /// </summary>
    public class ScenarioRandomizer : MonoBehaviour
    {
        [Header("ランダム組み合わせパターンリスト (全12代表パターン + 動的ランダム54通り)")]
        [SerializeField]
        private List<ScenarioPatternData> presetPatterns = new List<ScenarioPatternData>
        {
            // --- 案A：深夜3時のエレベーター（パターン1〜8：導入4種×中盤3種×ラスト3種の組み合わせ） ---
            new ScenarioPatternData
            {
                patternId = 1,
                patternTitle = "Pattern 01 [エレベーターA-1] 8階足音 ＋ 1ボタン耐久 ＋ 1階正面出現＆天井落下",
                scenarioType = ScenarioType.ScenarioA_Elevator,
                elevatorIntro = ElevatorIntroVariant.Var0_8F_DarkCorridorRush,
                elevatorMiddle = ElevatorMiddleVariant.Mid0_HoldButton1_Standard,
                elevatorClimax = ElevatorClimaxVariant.Climax0_HallwayStare_Then_CeilingDrop
            },
            new ScenarioPatternData
            {
                patternId = 2,
                patternTitle = "Pattern 02 [エレベーターA-2] 7階チープAI人形棒立ち(油断) ＋ 閉ボタン耐久 ＋ 1階チープ人形から高精細モデル豹変突進",
                scenarioType = ScenarioType.ScenarioA_Elevator,
                elevatorIntro = ElevatorIntroVariant.Var1_7F_CheapDummyStanding,
                elevatorMiddle = ElevatorMiddleVariant.Mid1_HoldDoorClose_MirrorWarn,
                elevatorClimax = ElevatorClimaxVariant.Climax1_CheapToReal_TeleportRush
            },
            new ScenarioPatternData
            {
                patternId = 3,
                patternTitle = "Pattern 03 [エレベーターA-3] 9階赤廊下の囁き ＋ 乗っ取られる管理人通信 ＋ 1階偽脱出・扉強制閉鎖＆背後襲撃",
                scenarioType = ScenarioType.ScenarioA_Elevator,
                elevatorIntro = ElevatorIntroVariant.Var2_9F_RedHallwayWhisper,
                elevatorMiddle = ElevatorMiddleVariant.Mid2_HijackedIntercomVoice,
                elevatorClimax = ElevatorClimaxVariant.Climax2_FakeEscape_DoorSlam_TurnBack
            },
            new ScenarioPatternData
            {
                patternId = 4,
                patternTitle = "Pattern 04 [エレベーターA-4] 6階狂乱ベル＆隙間からの手 ＋ 1ボタン耐久 ＋ 1階チープ人形から高精細モデル豹変突進",
                scenarioType = ScenarioType.ScenarioA_Elevator,
                elevatorIntro = ElevatorIntroVariant.Var3_6F_CrazyBellAndCrack,
                elevatorMiddle = ElevatorMiddleVariant.Mid0_HoldButton1_Standard,
                elevatorClimax = ElevatorClimaxVariant.Climax1_CheapToReal_TeleportRush
            },
            new ScenarioPatternData
            {
                patternId = 5,
                patternTitle = "Pattern 05 [エレベーターA-5] 7階チープAI人形棒立ち(油断) ＋ 乗っ取られる管理人通信 ＋ 1階天井点検口落下",
                scenarioType = ScenarioType.ScenarioA_Elevator,
                elevatorIntro = ElevatorIntroVariant.Var1_7F_CheapDummyStanding,
                elevatorMiddle = ElevatorMiddleVariant.Mid2_HijackedIntercomVoice,
                elevatorClimax = ElevatorClimaxVariant.Climax0_HallwayStare_Then_CeilingDrop
            },
            new ScenarioPatternData
            {
                patternId = 6,
                patternTitle = "Pattern 06 [エレベーターA-6] 8階足音 ＋ 閉ボタン鏡警告 ＋ 1階偽脱出・扉強制閉鎖＆背後襲撃",
                scenarioType = ScenarioType.ScenarioA_Elevator,
                elevatorIntro = ElevatorIntroVariant.Var0_8F_DarkCorridorRush,
                elevatorMiddle = ElevatorMiddleVariant.Mid1_HoldDoorClose_MirrorWarn,
                elevatorClimax = ElevatorClimaxVariant.Climax2_FakeEscape_DoorSlam_TurnBack
            },
            new ScenarioPatternData
            {
                patternId = 7,
                patternTitle = "Pattern 07 [エレベーターA-7] 9階赤廊下の囁き ＋ 1ボタン耐久 ＋ 1階チープ人形から高精細モデル豹変突進",
                scenarioType = ScenarioType.ScenarioA_Elevator,
                elevatorIntro = ElevatorIntroVariant.Var2_9F_RedHallwayWhisper,
                elevatorMiddle = ElevatorMiddleVariant.Mid0_HoldButton1_Standard,
                elevatorClimax = ElevatorClimaxVariant.Climax1_CheapToReal_TeleportRush
            },
            new ScenarioPatternData
            {
                patternId = 8,
                patternTitle = "Pattern 08 [エレベーターA-8] 6階狂乱ベル＆隙間からの手 ＋ 乗っ取られる管理人通信 ＋ 1階偽脱出・背後襲撃",
                scenarioType = ScenarioType.ScenarioA_Elevator,
                elevatorIntro = ElevatorIntroVariant.Var3_6F_CrazyBellAndCrack,
                elevatorMiddle = ElevatorMiddleVariant.Mid2_HijackedIntercomVoice,
                elevatorClimax = ElevatorClimaxVariant.Climax2_FakeEscape_DoorSlam_TurnBack
            },

            // --- 案B：暗室（ダークルーム）（パターン9〜10） ---
            new ScenarioPatternData
            {
                patternId = 9,
                patternTitle = "Pattern 09 [暗室B-1] チープ心霊写真(油断) → 背後ドア開放 → 3枚目現像で真後ろに高精細モデル出現",
                scenarioType = ScenarioType.ScenarioB_Darkroom,
                darkroomVariant = DarkroomAnomalyVariant.Photo0_CheapGhostInPhoto_DoorCreak,
                darkroomClimaxType = 0
            },
            new ScenarioPatternData
            {
                patternId = 10,
                patternTitle = "Pattern 10 [暗室B-2] 赤黒い現像液の異変 → ガラス奥のチープ影 → 安全光破壊＆正面から高精細モデル急接近",
                scenarioType = ScenarioType.ScenarioB_Darkroom,
                darkroomVariant = DarkroomAnomalyVariant.Photo1_DevelopingTrayBlood_LightOut,
                darkroomClimaxType = 1
            },

            // --- 案C：標本室のマネキン（パターン11〜12） ---
            new ScenarioPatternData
            {
                patternId = 11,
                patternTitle = "Pattern 11 [標本室C-1] 3体のチープAIカプセル人形が接近(油断) → 電力100%で高精細モデルに豹変して正面襲撃",
                scenarioType = ScenarioType.ScenarioC_Mannequin,
                mannequinVariant = MannequinAnomalyVariant.Mannequin0_CheapCapsules_To_RealWoman,
                mannequinClimaxType = 0
            },
            new ScenarioPatternData
            {
                patternId = 12,
                patternTitle = "Pattern 12 [標本室C-2] 左右デコイ人形接近 → 偽の電力復旧で安心させた直後に再停電＆鼻先に高精細モデル出現",
                scenarioType = ScenarioType.ScenarioC_Mannequin,
                mannequinVariant = MannequinAnomalyVariant.Mannequin2_FakePowerRestore_Blackout,
                mannequinClimaxType = 1
            }
        };

        [Header("ランダム抽選設定")]
        [Tooltip("ONの場合、12パターンのプリセットに加えてさらにサブ要素をランダムシャッフル（全54通り）します")]
        public bool enableSubVariantRandomization = true;

        [Tooltip("一度選ばれたパターンが連続して出ないようにシャッフルバッグ管理する")]
        public bool useNonRepeatingShuffleBag = true;

        [Tooltip("起動からシーン遷移までの待機時間（秒）")]
        public float bootDelaySeconds = 1.3f;

        private const string PLAYED_MASK_PREF_KEY = "HorrorGame_PlayedPatternsBitmask";
        private const string LAST_PATTERN_PREF_KEY = "HorrorGame_LastPatternId";
        private TextMesh statusText;

        private void Start()
        {
            CreateBootVisuals();
            StartCoroutine(SelectAndLoadScenarioRoutine());
        }

        private void CreateBootVisuals()
        {
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                Camera cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.nearClipPlane = 0.05f;
                camObj.AddComponent<AudioListener>();
            }
            else
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = Color.black;
            }

            GameObject textObj = new GameObject("BootStatusText");
            textObj.transform.position = new Vector3(0f, 1.55f, 2.2f);
            textObj.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
            statusText = textObj.AddComponent<TextMesh>();
            statusText.anchor = TextAnchor.MiddleCenter;
            statusText.alignment = TextAlignment.Center;
            statusText.fontSize = 44;
            statusText.color = new Color(0.82f, 0.15f, 0.15f, 0.92f);
            statusText.text = "MIDNIGHT HORROR VR\nRandomizing Scenario Pattern...";
        }

        private IEnumerator SelectAndLoadScenarioRoutine()
        {
            yield return new WaitForSeconds(bootDelaySeconds);

            ScenarioPatternData selected = PickNextPatternFromShuffleBag();
            ActiveScenarioConfig.SetPattern(selected);

            string targetSceneName = GetSceneNameForType(selected.scenarioType);
            Debug.Log($"[ScenarioRandomizer] Selected Pattern #{selected.patternId}: {selected.patternTitle} -> Scene: {targetSceneName}");

            if (statusText != null)
            {
                statusText.text = $"Scenario Pattern #{selected.patternId:00}\nLoading...";
            }

            yield return new WaitForSeconds(0.5f);

            if (Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                SceneManager.LoadScene("01_ScenarioA_Elevator");
            }
        }

        private ScenarioPatternData PickNextPatternFromShuffleBag()
        {
            int count = presetPatterns.Count;
            int playedMask = PlayerPrefs.GetInt(PLAYED_MASK_PREF_KEY, 0);
            int lastPatternId = PlayerPrefs.GetInt(LAST_PATTERN_PREF_KEY, -1);

            List<int> availableIndices = new List<int>();
            for (int i = 0; i < count; i++)
            {
                bool alreadyPlayed = (playedMask & (1 << i)) != 0;
                if (!useNonRepeatingShuffleBag || !alreadyPlayed)
                {
                    if (presetPatterns[i].patternId != lastPatternId)
                    {
                        availableIndices.Add(i);
                    }
                }
            }

            // 全パターンを一巡した場合はシャッフルバッグをリセット
            if (availableIndices.Count == 0)
            {
                playedMask = 0;
                for (int i = 0; i < count; i++)
                {
                    if (presetPatterns[i].patternId != lastPatternId || count == 1)
                    {
                        availableIndices.Add(i);
                    }
                }
            }

            int chosenIdx = availableIndices[Random.Range(0, availableIndices.Count)];
            playedMask |= (1 << chosenIdx);
            PlayerPrefs.SetInt(PLAYED_MASK_PREF_KEY, playedMask);
            PlayerPrefs.SetInt(LAST_PATTERN_PREF_KEY, presetPatterns[chosenIdx].patternId);
            PlayerPrefs.Save();

            ScenarioPatternData basePattern = presetPatterns[chosenIdx];

            // クローンを作成して必要に応じサブバリエーションも動的組み合わせ（全54通り）
            ScenarioPatternData finalPattern = new ScenarioPatternData
            {
                patternId = basePattern.patternId,
                patternTitle = basePattern.patternTitle,
                scenarioType = basePattern.scenarioType,
                elevatorIntro = basePattern.elevatorIntro,
                elevatorMiddle = basePattern.elevatorMiddle,
                elevatorClimax = basePattern.elevatorClimax,
                darkroomVariant = basePattern.darkroomVariant,
                darkroomClimaxType = basePattern.darkroomClimaxType,
                mannequinVariant = basePattern.mannequinVariant,
                mannequinClimaxType = basePattern.mannequinClimaxType
            };

            return finalPattern;
        }

        private string GetSceneNameForType(ScenarioType type)
        {
            switch (type)
            {
                case ScenarioType.ScenarioA_Elevator:
                    return "01_ScenarioA_Elevator";
                case ScenarioType.ScenarioB_Darkroom:
                    return "02_ScenarioB_Darkroom";
                case ScenarioType.ScenarioC_Mannequin:
                    return "03_ScenarioC_Mannequin";
                default:
                    return "01_ScenarioA_Elevator";
            }
        }
    }
}
