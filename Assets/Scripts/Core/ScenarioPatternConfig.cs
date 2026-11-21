using System;
using UnityEngine;

namespace HorrorGame.Core
{
    /// <summary>
    /// シナリオ全体のランダム組み合わせパターン定義（最低10パターン以上、全54通りの動的組み合わせ対応）。
    /// 「前半はこてこてのAIホラーゲーム風のチープなダミーモデルで油断させ、
    ///  振り返った時・最後に作り込まれた高精細3Dモデル（ScaryWoman_Ghost_01）が現れる」ギャップ恐怖を全パターンで演出します。
    /// </summary>
    public enum ScenarioType
    {
        ScenarioA_Elevator = 0,
        ScenarioB_Darkroom = 1,
        ScenarioC_Mannequin = 2
    }

    // 案A：エレベーターの導入（途中停止階）バリエーション（4種）
    public enum ElevatorIntroVariant
    {
        Var0_8F_DarkCorridorRush = 0,      // 8階停止：暗い廊下の足音＋閉まる直前の猛ダッシュ足音
        Var1_7F_CheapDummyStanding = 1,    // 7階停止：廊下に「こてこてのチープなAIダミー人形」が棒立ちしていて油断させる
        Var2_9F_RedHallwayWhisper = 2,     // 9階停止：赤い非常灯の廊下から囁き声が聞こえて勝手に閉まる
        Var3_6F_CrazyBellAndCrack = 3      // 6階停止：到着ベルが狂ったように連打され、隙間から白い手が覗く
    }

    // 案A：中盤（鏡のチープモデル→振り返り高精細モデル＆4階緊急停止ルール）バリエーション（3種）
    public enum ElevatorMiddleVariant
    {
        Mid0_HoldButton1_Standard = 0,     // 管理人「後ろを振り返らずに[1]ボタンを長押ししろ」
        Mid1_HoldDoorClose_MirrorWarn = 1, // 管理人「鏡を見るな、後ろも向かずに[閉]ボタンを押し続けろ」
        Mid2_HijackedIntercomVoice = 2     // 管理人の通信が途中で怪異の声（「やっぱり後ろを向いて…」）に乗っ取られる
    }

    // 案A：1階到着＆クライマックス（高精細モデル ScaryWoman_Ghost_01 出現）バリエーション（3種）
    public enum ElevatorClimaxVariant
    {
        Climax0_HallwayStare_Then_CeilingDrop = 0, // 1階正面に高精細モデルが立つ→消えて天井から血→見上げると頭上から落下
        Climax1_CheapToReal_TeleportRush = 1,      // 1階奥にチープなAIモデルが立つ→「最後までこれかｗ」と思った瞬間に照明明滅→目の前に高精細モデルへ豹変して突進！
        Climax2_FakeEscape_DoorSlam_TurnBack = 2   // 1階で扉が開くが誰もいない→直後に扉が強制閉鎖＆正面に『うしろ』→振り返ると至近距離に高精細モデル！
    }

    // 案B：暗室のランダムバリエーション（3×3 = 9通り）
    public enum DarkroomAnomalyVariant
    {
        Photo0_CheapGhostInPhoto_DoorCreak = 0,    // 1枚目にチープなAI幽霊写真（油断）→2枚目で背後ドア開放→3枚目で真後ろに高精細モデル
        Photo1_DevelopingTrayBlood_LightOut = 1,   // 現像液が赤黒く変色→1枚目はチープな合成風→3枚目で現像トレイ正面に実体の高精細モデル出現
        Photo2_MirrorReflection_FakeClear = 2      // 暗室奥のガラスにチープな影（油断）→現像完了直後に安全光が割れ、高精細モデルが至近距離に突進
    }

    // 案C：標本室のマネキンのランダムバリエーション（3×3 = 9通り）
    public enum MannequinAnomalyVariant
    {
        Mannequin0_CheapCapsules_To_RealWoman = 0, // 3体のチープなカプセル人形がだるまさんが転んだで接近（油断）→電力100%で高精細モデルに豹変して襲撃
        Mannequin1_DecoyLeftRight_CenterReal = 1,  // 左右のチープ人形に気を取られていると、最後に天井・正面から高精細モデルが落下襲撃
        Mannequin2_FakePowerRestore_Blackout = 2   // 電力復旧で一度チープ人形が倒れて安心させる→2秒後に再停電し、鼻先25cmに高精細モデルが出現
    }

    [Serializable]
    public class ScenarioPatternData
    {
        public int patternId;
        public string patternTitle;
        public ScenarioType scenarioType;

        // 案A用パラメータ
        public ElevatorIntroVariant elevatorIntro;
        public ElevatorMiddleVariant elevatorMiddle;
        public ElevatorClimaxVariant elevatorClimax;

        // 案B用パラメータ
        public DarkroomAnomalyVariant darkroomVariant;
        public int darkroomClimaxType; // 0..2

        // 案C用パラメータ
        public MannequinAnomalyVariant mannequinVariant;
        public int mannequinClimaxType; // 0..2
    }

    /// <summary>
    /// シーン間で選択されたランダムシナリオパターンを保持する静的コンフィグ
    /// </summary>
    public static class ActiveScenarioConfig
    {
        public static ScenarioPatternData CurrentPattern { get; private set; }
        public static int TotalPatternsGeneratedCount { get; private set; }

        public static void SetPattern(ScenarioPatternData pattern)
        {
            CurrentPattern = pattern;
            TotalPatternsGeneratedCount++;
        }

        /// <summary>
        /// シーン単体で直接再生された場合でも必ず有効なランダムパターンを生成して返す
        /// </summary>
        public static ScenarioPatternData GetOrRollPatternForScene(ScenarioType defaultType)
        {
            if (CurrentPattern != null && CurrentPattern.scenarioType == defaultType)
            {
                return CurrentPattern;
            }

            ScenarioPatternData rolled = new ScenarioPatternData
            {
                patternId = UnityEngine.Random.Range(100, 999),
                scenarioType = defaultType,
                elevatorIntro = (ElevatorIntroVariant)UnityEngine.Random.Range(0, 4),
                elevatorMiddle = (ElevatorMiddleVariant)UnityEngine.Random.Range(0, 3),
                elevatorClimax = (ElevatorClimaxVariant)UnityEngine.Random.Range(0, 3),
                darkroomVariant = (DarkroomAnomalyVariant)UnityEngine.Random.Range(0, 3),
                darkroomClimaxType = UnityEngine.Random.Range(0, 3),
                mannequinVariant = (MannequinAnomalyVariant)UnityEngine.Random.Range(0, 3),
                mannequinClimaxType = UnityEngine.Random.Range(0, 3)
            };
            rolled.patternTitle = $"Dynamic_{defaultType}_{rolled.elevatorIntro}_{rolled.elevatorMiddle}_{rolled.elevatorClimax}";
            CurrentPattern = rolled;
            return rolled;
        }
    }
}
