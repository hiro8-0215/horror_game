using System;
using UnityEngine;
using UnityEngine.InputSystem;
using HorrorGame.Core;
using HorrorGame.Audio;

namespace HorrorGame.ScenarioA
{
    public enum ElevatorButtonType
    {
        Floor1,
        DoorOpen,
        DoorClose,
        EmergencyAlarm
    }

    /// <summary>
    /// VRの指・コントローラ接触（Poke）、VRトリガー遠隔入力、
    /// およびエディタ上のマウスクリック／キーボード入力すべてに対応したエレベーターボタン。
    /// 単押しと「長押し（フェーズ3耐久）」の両方を判定します。
    /// </summary>
    public class ElevatorButton : MonoBehaviour
    {
        public ElevatorButtonType buttonType;
        public Renderer buttonMeshRenderer;
        public Transform movingCapTransform;

        [Header("発光カラー設定")]
        public Color normalColor = new Color(0.22f, 0.22f, 0.25f);
        public Color highlightBlinkColor = new Color(1.0f, 0.65f, 0.1f);
        public Color activePressedColor = new Color(1.0f, 0.2f, 0.1f);

        public event Action<ElevatorButton> OnButtonPressed;

        public bool IsCurrentlyHeld { get; private set; }
        public bool IsBlinkingGuide { get; private set; }

        private Vector3 initialLocalPos;
        private Material runtimeMat;
        private bool isTouchedByVRHand;
        private bool isMouseHeld;
        private bool wasHeldLastFrame;
        private AudioSource buttonAudioSource;

        private void Awake()
        {
            if (movingCapTransform == null) movingCapTransform = transform;
            initialLocalPos = movingCapTransform.localPosition;

            if (buttonMeshRenderer == null) buttonMeshRenderer = GetComponentInChildren<Renderer>();
            if (buttonMeshRenderer != null)
            {
                runtimeMat = buttonMeshRenderer.material;
            }

            buttonAudioSource = gameObject.AddComponent<AudioSource>();
            buttonAudioSource.spatialBlend = 1.0f;
            buttonAudioSource.minDistance = 0.2f;
            buttonAudioSource.maxDistance = 3.0f;
            buttonAudioSource.playOnAwake = false;
        }

        public void SetBlinkingGuide(bool blink)
        {
            IsBlinkingGuide = blink;
        }

        private void Update()
        {
            bool keyboardHeld = CheckKeyboardShortcut();
            bool vrTriggerAimHeld = CheckVRControllerAimAndTrigger();
            bool desktopMouseRayHeld = CheckDesktopMouseClick();

            IsCurrentlyHeld = isTouchedByVRHand || isMouseHeld || keyboardHeld || vrTriggerAimHeld || desktopMouseRayHeld;

            // 押された瞬間のイベント発火
            if (IsCurrentlyHeld && !wasHeldLastFrame)
            {
                PlayClickFeedback();
                OnButtonPressed?.Invoke(this);
            }

            wasHeldLastFrame = IsCurrentlyHeld;

            UpdateVisualState();
        }

        private bool CheckKeyboardShortcut()
        {
            var kb = Keyboard.current;
            if (kb == null) return false;

            switch (buttonType)
            {
                case ElevatorButtonType.Floor1:
                    return kb.digit1Key.isPressed || kb.numpad1Key.isPressed;
                case ElevatorButtonType.DoorOpen:
                    return kb.oKey.isPressed;
                case ElevatorButtonType.DoorClose:
                    return kb.cKey.isPressed;
                case ElevatorButtonType.EmergencyAlarm:
                    return kb.eKey.isPressed || kb.spaceKey.isPressed;
                default:
                    return false;
            }
        }

        private bool CheckVRControllerAimAndTrigger()
        {
            if (VRPlayerRigController.Instance == null) return false;

            if (VRPlayerRigController.Instance.IsVRSelectPressed(out Transform hand))
            {
                if (hand == null) return false;
                float dist = Vector3.Distance(hand.position, transform.position);
                if (dist < 0.18f) return true;

                Vector3 toButton = (transform.position - hand.position).normalized;
                float angle = Vector3.Angle(hand.forward, toButton);
                if (dist < 1.6f && angle < 28f)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckDesktopMouseClick()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.isPressed)
            {
                isMouseHeld = false;
                return false;
            }

            Camera cam = Camera.main;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 3.0f))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    isMouseHeld = true;
                    return true;
                }
            }
            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.name.Contains("Hand"))
            {
                isTouchedByVRHand = true;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.name.Contains("Hand"))
            {
                isTouchedByVRHand = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.name.Contains("Hand"))
            {
                isTouchedByVRHand = false;
            }
        }

        private void PlayClickFeedback()
        {
            if (ProceduralHorrorAudio.Instance != null && ProceduralHorrorAudio.Instance.buttonClickClip != null)
            {
                buttonAudioSource.PlayOneShot(ProceduralHorrorAudio.Instance.buttonClickClip, 0.85f);
            }
            if (VRPlayerRigController.Instance != null)
            {
                VRPlayerRigController.Instance.TriggerHaptics(0.45f, 0.06f);
            }
        }

        private void UpdateVisualState()
        {
            // ボタンの押し込みアニメーション
            Vector3 targetOffset = IsCurrentlyHeld ? new Vector3(0f, 0f, 0.008f) : Vector3.zero;
            movingCapTransform.localPosition = Vector3.Lerp(
                movingCapTransform.localPosition,
                initialLocalPos + targetOffset,
                Time.deltaTime * 20f
            );

            if (runtimeMat == null) return;

            Color targetColor = normalColor;
            float emissionIntensity = 0f;

            if (IsCurrentlyHeld)
            {
                targetColor = activePressedColor;
                emissionIntensity = 2.0f;
            }
            else if (IsBlinkingGuide)
            {
                float pulse = (Mathf.Sin(Time.time * 6.5f) + 1f) * 0.5f;
                targetColor = Color.Lerp(normalColor, highlightBlinkColor, pulse);
                emissionIntensity = pulse * 1.6f;
            }

            runtimeMat.color = targetColor;
            if (runtimeMat.HasProperty("_BaseColor"))
            {
                runtimeMat.SetColor("_BaseColor", targetColor);
            }
            if (runtimeMat.HasProperty("_EmissionColor"))
            {
                runtimeMat.EnableKeyword("_EMISSION");
                runtimeMat.SetColor("_EmissionColor", targetColor * emissionIntensity);
            }
        }
    }
}
