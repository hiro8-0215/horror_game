using System.Collections;
using UnityEngine;

namespace HorrorGame.ScenarioA
{
    /// <summary>
    /// 案A『深夜3時のエレベーター』の空間内の全オブジェクト・ライト・ドア・表示パネル・3D音源への参照と演出制御。
    /// </summary>
    public class ElevatorEnvironmentRefs : MonoBehaviour
    {
        [Header("エレベーター扉・点検口")]
        public Transform leftDoor;
        public Transform rightDoor;
        public Transform ceilingHatchCover;
        public Transform ceilingHatchPoint;

        [Header("外側の廊下セット (8階 / 1階)")]
        public GameObject corridor8FGroup;
        public GameObject corridor1FHorrorGroup;

        [Header("表示パネル (階数表示・インターホン字幕・操作ガイド)")]
        public TextMesh floorDisplayText;
        public TextMesh intercomSubtitleText;

        [Header("操作ボタン")]
        public ElevatorButton buttonFloor1;
        public ElevatorButton buttonOpen;
        public ElevatorButton buttonClose;
        public ElevatorButton buttonAlarm;
        public Renderer intercomLedRenderer;

        [Header("照明")]
        public Light ceilingFluorescentLight;
        public Renderer ceilingLightTubeRenderer;
        public Light redEmergencyFloorLight;
        public Renderer redEmergencyLampRenderer;

        [Header("鏡カメラ (RenderTexture)")]
        public Camera frontMirrorCamera;
        public Camera rearMirrorCamera;

        [Header("3D空間オーディオ音源")]
        public AudioSource ceilingHumAudio;
        public AudioSource elevatorMotorAudio;
        public AudioSource doorBellAndSlideAudio;
        public AudioSource hallway3DAudio;
        public AudioSource intercom3DAudio;
        public AudioSource behindNeck3DAudio;
        public AudioSource earMoving3DAudio;
        public AudioSource ceilingDrip3DAudio;

        [Header("暗転・エンディングオーバーレイ")]
        public GameObject blackoutSphere;
        public TextMesh endingTitleText;

        private Vector3 leftDoorClosedPos;
        private Vector3 rightDoorClosedPos;
        private bool doorPositionsInitialized = false;

        private void Awake()
        {
            InitializeDoorPositions();
        }

        public void InitializeDoorPositions()
        {
            if (doorPositionsInitialized) return;
            if (leftDoor != null) leftDoorClosedPos = leftDoor.localPosition;
            if (rightDoor != null) rightDoorClosedPos = rightDoor.localPosition;
            doorPositionsInitialized = true;
        }

        /// <summary>
        /// エレベーターの左右の扉の開閉度を設定 (0.0 = 完全閉, 1.0 = 完全開)
        /// </summary>
        public void SetDoorOpenAmount(float openNormalized)
        {
            InitializeDoorPositions();
            float slideDistance = 0.44f * Mathf.Clamp01(openNormalized);
            if (leftDoor != null)
            {
                leftDoor.localPosition = leftDoorClosedPos + new Vector3(-slideDistance, 0f, 0f);
            }
            if (rightDoor != null)
            {
                rightDoor.localPosition = rightDoorClosedPos + new Vector3(slideDistance, 0f, 0f);
            }
        }

        public void SetFloorText(string text, Color? colorOverride = null)
        {
            if (floorDisplayText == null) return;
            floorDisplayText.text = text;
            floorDisplayText.color = colorOverride ?? new Color(1.0f, 0.32f, 0.08f);
        }

        public void SetSubtitleText(string text)
        {
            if (intercomSubtitleText == null) return;
            intercomSubtitleText.text = text;
        }

        /// <summary>
        /// 蛍光灯の明るさを設定 (0.0〜1.5)
        /// </summary>
        public void SetFluorescentLightIntensity(float intensity)
        {
            if (ceilingFluorescentLight != null)
            {
                ceilingFluorescentLight.intensity = intensity;
            }
            if (ceilingLightTubeRenderer != null)
            {
                Material mat = ceilingLightTubeRenderer.material;
                Color c = Color.Lerp(new Color(0.08f, 0.08f, 0.1f), new Color(0.92f, 0.96f, 1.0f), Mathf.Clamp01(intensity));
                mat.color = c;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.85f, 0.92f, 1.0f) * (intensity * 1.4f));
                }
            }
        }

        /// <summary>
        /// 足元の赤い非常灯のON/OFF
        /// </summary>
        public void SetRedEmergencyLight(bool active, float intensity = 1.1f)
        {
            if (redEmergencyFloorLight != null)
            {
                redEmergencyFloorLight.enabled = active;
                redEmergencyFloorLight.intensity = active ? intensity : 0f;
            }
            if (redEmergencyLampRenderer != null)
            {
                Material mat = redEmergencyLampRenderer.material;
                Color c = active ? new Color(1.0f, 0.05f, 0.05f) : new Color(0.15f, 0.02f, 0.02f);
                mat.color = c;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", active ? new Color(1.8f, 0.05f, 0.05f) : Color.black);
                }
            }
        }

        public void SetIntercomLed(bool active)
        {
            if (intercomLedRenderer == null) return;
            Material mat = intercomLedRenderer.material;
            Color c = active ? new Color(1.0f, 0.1f, 0.1f) : new Color(0.2f, 0.05f, 0.05f);
            mat.color = c;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", active ? new Color(2.0f, 0.1f, 0.1f) : Color.black);
            }
        }

        /// <summary>
        /// 天井の点検口から黒い血の水滴をプレイヤーの目の前に落下させる演出
        /// </summary>
        public void SpawnCeilingBloodDroplet()
        {
            Vector3 startPos = (ceilingHatchPoint != null) ? ceilingHatchPoint.position : new Vector3(0f, 2.25f, 0.25f);
            if (Camera.main != null)
            {
                Vector3 camPos = Camera.main.transform.position + Camera.main.transform.forward * 0.35f;
                startPos = new Vector3(camPos.x + Random.Range(-0.06f, 0.06f), 2.22f, camPos.z + Random.Range(-0.06f, 0.06f));
            }

            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drop.name = "BloodDroplet";
            drop.transform.position = startPos;
            drop.transform.localScale = new Vector3(0.025f, 0.045f, 0.025f);
            Collider col = drop.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer rend = drop.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.25f, 0.01f, 0.01f);
                if (rend.material.HasProperty("_BaseColor"))
                {
                    rend.material.SetColor("_BaseColor", new Color(0.25f, 0.01f, 0.01f));
                }
            }

            Rigidbody rb = drop.AddComponent<Rigidbody>();
            rb.useGravity = true;
            Destroy(drop, 1.5f);
        }

        public void SetBlackout(bool blackout, string endingMessage = "")
        {
            if (blackoutSphere != null && Camera.main != null)
            {
                blackoutSphere.transform.position = Camera.main.transform.position;
                blackoutSphere.transform.rotation = Camera.main.transform.rotation;
                blackoutSphere.SetActive(blackout);
            }
            if (endingTitleText != null)
            {
                endingTitleText.text = endingMessage;
            }
        }
    }
}
