using UnityEngine;

namespace HorrorGame.Audio
{
    /// <summary>
    /// 外部音声ファイルが無くても即座に3Dホラー音響を体験できるよう、
    /// シナリオA『深夜3時のエレベーター』の全16種類の効果音・環境音をプロシージャル波形合成で生成するクラス。
    /// インスペクターから任意のAudioClipに差し替えることも可能です。
    /// </summary>
    public class ProceduralHorrorAudio : MonoBehaviour
    {
        public static ProceduralHorrorAudio Instance { get; private set; }

        [Header("環境音・エレベーター稼働音 (未設定時は自動合成)")]
        public AudioClip fluorescentHumClip;
        public AudioClip elevatorMoveLoopClip;
        public AudioClip elevatorDingClip;
        public AudioClip doorSlideClip;
        public AudioClip buttonClickClip;

        [Header("怪異・ホラー演出音 (未設定時は自動合成)")]
        public AudioClip hallwayFootstepsSlowClip;
        public AudioClip hallwayFootstepsRushClip;
        public AudioClip lightFlickerBuzzClip;
        public AudioClip ghostWhisperClip;
        public AudioClip emergencyStopCrashClip;
        public AudioClip intercomMessageClip;
        public AudioClip ghostBreathingLoopClip;
        public AudioClip ghostDragLoopClip;
        public AudioClip bloodDripClip;
        public AudioClip dissonantStringsLoopClip;
        public AudioClip jumpscareScreamClip;
        public AudioClip tinnitusRingClip;

        private const int SAMPLE_RATE = 44100;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureAllClipsGenerated();
        }

        public void EnsureAllClipsGenerated()
        {
            if (fluorescentHumClip == null) fluorescentHumClip = CreateFluorescentHum(4.0f);
            if (elevatorMoveLoopClip == null) elevatorMoveLoopClip = CreateElevatorMoveLoop(5.0f);
            if (elevatorDingClip == null) elevatorDingClip = CreateDistortedDing(2.2f);
            if (doorSlideClip == null) doorSlideClip = CreateDoorSlide(2.5f);
            if (buttonClickClip == null) buttonClickClip = CreateButtonClick(0.12f);

            if (hallwayFootstepsSlowClip == null) hallwayFootstepsSlowClip = CreateFootsteps(3.2f, false);
            if (hallwayFootstepsRushClip == null) hallwayFootstepsRushClip = CreateFootsteps(1.8f, true);
            if (lightFlickerBuzzClip == null) lightFlickerBuzzClip = CreateLightFlickerBuzz(1.2f);
            if (ghostWhisperClip == null) ghostWhisperClip = CreateEerieWhisper(1.8f);
            if (emergencyStopCrashClip == null) emergencyStopCrashClip = CreateEmergencyCrash(2.6f);
            if (intercomMessageClip == null) intercomMessageClip = CreateIntercomTransmission(11.0f);
            if (ghostBreathingLoopClip == null) ghostBreathingLoopClip = CreateWetBreathingLoop(3.6f);
            if (ghostDragLoopClip == null) ghostDragLoopClip = CreateDraggingSoundLoop(4.0f);
            if (bloodDripClip == null) bloodDripClip = CreateBloodDrip(0.7f);
            if (dissonantStringsLoopClip == null) dissonantStringsLoopClip = CreateDissonantHorrorChord(6.0f);
            if (jumpscareScreamClip == null) jumpscareScreamClip = CreateJumpscareScream(2.2f);
            if (tinnitusRingClip == null) tinnitusRingClip = CreateTinnitusRing(4.5f);
        }

        private AudioClip CreateFluorescentHum(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(101);

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float hum60 = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.25f;
                float hum120 = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.45f;
                float hum240 = Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.15f;
                float crackle = ((float)rng.NextDouble() * 2f - 1f) * 0.04f;
                data[i] = (hum60 + hum120 + hum240 + crackle) * 0.25f;
            }
            return BuildClip("Proc_FluorescentHum", data);
        }

        private AudioClip CreateElevatorMoveLoop(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(202);
            float lowPass = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float rumble = Mathf.Sin(2f * Mathf.PI * 38f * t) * 0.4f +
                               Mathf.Sin(2f * Mathf.PI * 52f * t + Mathf.Sin(t * 2.5f)) * 0.3f;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                lowPass = lowPass * 0.96f + noise * 0.04f;
                float creak = Mathf.Sin(2f * Mathf.PI * (180f + Mathf.Sin(t * 1.6f) * 25f) * t) *
                              Mathf.Max(0f, Mathf.Sin(t * 2f * Mathf.PI * 0.4f)) * 0.08f;
                data[i] = Mathf.Clamp((rumble + lowPass * 0.9f + creak) * 0.45f, -1f, 1f);
            }
            return BuildClip("Proc_ElevatorMoveLoop", data);
        }

        private AudioClip CreateDistortedDing(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float env = Mathf.Exp(-t * 2.2f);
                // Slightly detuned, unsettling elevator bell ("ピン…ポーン…")
                float freq = t < 0.55f ? 784f : 615f; // Detuned G5 -> D#5
                float localT = t < 0.55f ? t : (t - 0.55f);
                float localEnv = Mathf.Exp(-localT * 2.8f);
                float bend = Mathf.Sin(t * 9f) * 6f;
                float wave = Mathf.Sin(2f * Mathf.PI * (freq + bend) * localT) * 0.65f +
                             Mathf.Sin(2f * Mathf.PI * (freq * 2.02f) * localT) * 0.25f;
                data[i] = wave * localEnv * env * 0.7f;
            }
            return BuildClip("Proc_DistortedDing", data);
        }

        private AudioClip CreateDoorSlide(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(303);
            float lp = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float norm = t / duration;
                float env = Mathf.Sin(norm * Mathf.PI);
                float raw = (float)rng.NextDouble() * 2f - 1f;
                lp = lp * 0.92f + raw * 0.08f;
                float metal = Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.25f;
                float clunk = (norm > 0.9f) ? Mathf.Exp(-(norm - 0.9f) * 45f) * Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.8f : 0f;
                data[i] = Mathf.Clamp((lp * 1.4f + metal) * env * 0.45f + clunk, -1f, 1f);
            }
            return BuildClip("Proc_DoorSlide", data);
        }

        private AudioClip CreateButtonClick(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float env = Mathf.Exp(-t * 60f);
                data[i] = (Mathf.Sin(2f * Mathf.PI * 950f * t) + Mathf.Sin(2f * Mathf.PI * 420f * t)) * 0.4f * env;
            }
            return BuildClip("Proc_ButtonClick", data);
        }

        private AudioClip CreateFootsteps(float duration, bool rushing)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(rushing ? 405 : 404);

            float[] stepTimes = rushing
                ? new float[] { 0.05f, 0.28f, 0.48f, 0.66f, 0.82f, 0.96f, 1.09f, 1.21f, 1.32f, 1.43f }
                : new float[] { 0.4f, 1.55f };

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float sampleVal = 0f;
                for (int s = 0; s < stepTimes.Length; s++)
                {
                    float dt = t - stepTimes[s];
                    if (dt >= 0f && dt < 0.25f)
                    {
                        float distanceGain = rushing ? Mathf.Lerp(0.25f, 1.0f, s / (float)stepTimes.Length) : 0.45f;
                        float env = Mathf.Exp(-dt * 28f);
                        float thud = Mathf.Sin(2f * Mathf.PI * (140f - dt * 260f) * dt) * 0.7f;
                        float heel = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-dt * 45f) * 0.6f;
                        sampleVal += (thud + heel) * env * distanceGain;
                    }
                }
                data[i] = Mathf.Clamp(sampleVal, -1f, 1f);
            }
            return BuildClip(rushing ? "Proc_FootstepsRush" : "Proc_FootstepsSlow", data);
        }

        private AudioClip CreateLightFlickerBuzz(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(505);
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float gate = (Mathf.Sin(t * 38f) > -0.2f) ? 1f : 0.05f;
                float buzz = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 120f * t)) * 0.35f;
                float spark = ((float)rng.NextDouble() * 2f - 1f) * 0.4f;
                data[i] = (buzz + spark) * gate * 0.5f;
            }
            return BuildClip("Proc_LightFlickerBuzz", data);
        }

        private AudioClip CreateEerieWhisper(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(606);
            float bp1 = 0f;
            float bp2 = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                // Two-syllable breath envelope ("...み・え・た？")
                float syllable1 = Mathf.Exp(-Mathf.Pow((t - 0.45f) / 0.22f, 2f));
                float syllable2 = Mathf.Exp(-Mathf.Pow((t - 1.05f) / 0.32f, 2f));
                float env = Mathf.Clamp01(syllable1 + syllable2);

                float white = (float)rng.NextDouble() * 2f - 1f;
                bp1 = bp1 * 0.85f + white * 0.15f;
                bp2 = bp2 * 0.7f + (white - bp1) * 0.3f;
                float formant = Mathf.Sin(2f * Mathf.PI * (520f + t * 90f) * t) * 0.25f;
                data[i] = Mathf.Clamp((bp2 * 1.4f + formant * bp2) * env * 0.75f, -1f, 1f);
            }
            return BuildClip("Proc_GhostWhisper", data);
        }

        private AudioClip CreateEmergencyCrash(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(707);
            float lp = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                lp = lp * 0.88f + noise * 0.12f;

                // Metallic screech (0.0 -> 0.7s) followed by heavy slam (0.7s)
                float screechEnv = t < 0.75f ? 1f : Mathf.Exp(-(t - 0.75f) * 8f);
                float screech = Mathf.Sin(2f * Mathf.PI * (440f - t * 180f) * t + Mathf.Sin(t * 80f) * 3f) * 0.4f * screechEnv;

                float slam = 0f;
                if (t >= 0.7f)
                {
                    float dt = t - 0.7f;
                    slam = (Mathf.Sin(2f * Mathf.PI * 55f * dt) * 0.9f + lp * 1.2f) * Mathf.Exp(-dt * 3.5f);
                }
                data[i] = Mathf.Clamp(screech + slam + lp * 0.3f * screechEnv, -1f, 1f);
            }
            return BuildClip("Proc_EmergencyCrash", data);
        }

        private AudioClip CreateIntercomTransmission(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(808);
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                // Radio static base
                float staticNoise = ((float)rng.NextDouble() * 2f - 1f) * 0.14f;

                // Urgent radio beep at start and end
                float beep = 0f;
                if (t < 0.35f || (t > duration - 0.45f))
                {
                    beep = Mathf.Sin(2f * Mathf.PI * 1050f * t) * 0.35f;
                }

                // Simulated urgent distorted speech cadence over intercom
                float speechGate = Mathf.Max(0f, Mathf.Sin(t * 14f) * Mathf.Sin(t * 3.7f));
                if (t < 0.5f || t > duration - 0.6f) speechGate = 0f;

                float pitch = 195f + Mathf.Sin(t * 7.5f) * 35f + Mathf.Cos(t * 19f) * 20f;
                float voiceWave = Mathf.Sin(2f * Mathf.PI * pitch * t) * 0.4f +
                                  Mathf.Sign(Mathf.Sin(2f * Mathf.PI * pitch * 2f * t)) * 0.2f;
                // Band-limit distortion
                float radioVoice = Mathf.Clamp(voiceWave * speechGate, -0.45f, 0.45f);

                data[i] = Mathf.Clamp(staticNoise + beep + radioVoice, -1f, 1f);
            }
            return BuildClip("Proc_IntercomTransmission", data);
        }

        private AudioClip CreateWetBreathingLoop(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(909);
            float filter = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float cycle = (t % 1.8f) / 1.8f; // One breath every 1.8s
                // Inhale (0.05..0.4), Exhale (0.48..0.9)
                float inhale = Mathf.Exp(-Mathf.Pow((cycle - 0.22f) / 0.11f, 2f)) * 0.65f;
                float exhale = Mathf.Exp(-Mathf.Pow((cycle - 0.68f) / 0.16f, 2f)) * 1.0f;
                float env = inhale + exhale;

                float white = (float)rng.NextDouble() * 2f - 1f;
                filter = filter * 0.91f + white * 0.09f;
                float throat = Mathf.Sin(2f * Mathf.PI * (290f - cycle * 60f) * t) * 0.18f;
                data[i] = Mathf.Clamp((filter * 2.2f + throat * filter) * env * 0.85f, -1f, 1f);
            }
            return BuildClip("Proc_GhostBreathingLoop", data);
        }

        private AudioClip CreateDraggingSoundLoop(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(1010);
            float lp = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float scrapeGate = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 1.0f)), 1.5f);
                float white = (float)rng.NextDouble() * 2f - 1f;
                lp = lp * 0.94f + white * 0.06f;
                data[i] = Mathf.Clamp(lp * scrapeGate * 1.3f, -1f, 1f);
            }
            return BuildClip("Proc_GhostDragLoop", data);
        }

        private AudioClip CreateBloodDrip(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                if (t < 0.18f)
                {
                    // Classic rising pitch droplet plop
                    float env = Mathf.Exp(-t * 28f);
                    float freq = 420f + (t / 0.18f) * 980f;
                    data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.85f;
                }
            }
            return BuildClip("Proc_BloodDrip", data);
        }

        private AudioClip CreateDissonantHorrorChord(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            float[] freqs = { 55.0f, 58.27f, 116.54f, 233.08f, 246.94f, 311.13f };
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float sum = 0f;
                for (int f = 0; f < freqs.Length; f++)
                {
                    float vibrato = Mathf.Sin(t * (2.1f + f * 0.4f)) * 1.5f;
                    sum += Mathf.Sin(2f * Mathf.PI * (freqs[f] + vibrato) * t) / freqs.Length;
                }
                data[i] = Mathf.Clamp(sum * 0.85f, -1f, 1f);
            }
            return BuildClip("Proc_DissonantStrings", data);
        }

        private AudioClip CreateJumpscareScream(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(1111);
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float env = t < 0.04f ? (t / 0.04f) : Mathf.Exp(-(t - 0.04f) * 1.1f);

                // Sub-bass impact + piercing dissonant shriek + noise
                float sub = Mathf.Sin(2f * Mathf.PI * (65f * Mathf.Exp(-t * 1.2f)) * t) * 0.9f;
                float fm = Mathf.Sin(2f * Mathf.PI * 38f * t) * 90f;
                float shriek1 = Mathf.Sin(2f * Mathf.PI * (1180f + fm) * t) * 0.6f;
                float shriek2 = Mathf.Sin(2f * Mathf.PI * (1540f - fm * 0.8f) * t) * 0.55f;
                float shriek3 = Mathf.Sin(2f * Mathf.PI * (2120f + fm * 1.3f) * t) * 0.4f;
                float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.55f;

                float combined = (sub + shriek1 + shriek2 + shriek3 + noise) * env;
                // Hard clip distortion for terrifying punch
                data[i] = Mathf.Clamp(combined * 1.25f, -0.98f, 0.98f);
            }
            return BuildClip("Proc_JumpscareScream", data);
        }

        private AudioClip CreateTinnitusRing(float duration)
        {
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float env = Mathf.Clamp01(1f - (t / duration));
                env = env * env;
                float ring = Mathf.Sin(2f * Mathf.PI * 3850f * t) * 0.35f +
                             Mathf.Sin(2f * Mathf.PI * 62f * t) * 0.25f;
                data[i] = ring * env;
            }
            return BuildClip("Proc_TinnitusRing", data);
        }

        private AudioClip BuildClip(string clipName, float[] samples)
        {
            AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
