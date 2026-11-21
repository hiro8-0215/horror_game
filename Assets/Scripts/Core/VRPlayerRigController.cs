using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace HorrorGame.Core
{
    /// <summary>
    /// Meta Quest 3 (OpenXR) のHMD・左右コントローラ追跡・ハプティクス振動、
    /// およびUnity Editor上でのマウス視点操作・ボタンクリック操作を統合管理するリグコントローラ。
    /// </summary>
    public class VRPlayerRigController : MonoBehaviour
    {
        public static VRPlayerRigController Instance { get; private set; }

        [Header("リグ参照")]
        public Transform cameraOffset;
        public Camera headCamera;
        public Transform leftHandTransform;
        public Transform rightHandTransform;

        [Header("エディタ・PCデバッグ操作設定")]
        [Tooltip("VRヘッドセット未接続時にマウスで視点を動かせるようにする")]
        public bool enableDesktopFallback = true;
        public float mouseSensitivity = 0.22f;

        private float desktopYaw = 0f;
        private float desktopPitch = 0f;
        private bool isVrActive = false;

        private void Awake()
        {
            Instance = this;
            if (headCamera == null) headCamera = Camera.main;
        }

        private void Update()
        {
            UpdateXRTracking();

            if (!isVrActive && enableDesktopFallback)
            {
                UpdateDesktopLook();
            }
        }

        private void UpdateXRTracking()
        {
            XRInputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            bool tracked = false;
            if (headDevice.isValid && headDevice.TryGetFeatureValue(XRCommonUsages.isTracked, out tracked) && tracked)
            {
                isVrActive = true;
                if (headDevice.TryGetFeatureValue(XRCommonUsages.centerEyePosition, out Vector3 headPos))
                {
                    headCamera.transform.localPosition = headPos;
                }
                if (headDevice.TryGetFeatureValue(XRCommonUsages.centerEyeRotation, out Quaternion headRot))
                {
                    headCamera.transform.localRotation = headRot;
                }
            }
            else
            {
                isVrActive = false;
            }

            UpdateHandNode(XRNode.LeftHand, leftHandTransform);
            UpdateHandNode(XRNode.RightHand, rightHandTransform);
        }

        private void UpdateHandNode(XRNode node, Transform handTarget)
        {
            if (handTarget == null) return;

            XRInputDevice handDevice = InputDevices.GetDeviceAtXRNode(node);
            if (handDevice.isValid)
            {
                if (handDevice.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3 pos))
                {
                    handTarget.localPosition = pos;
                }
                if (handDevice.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion rot))
                {
                    handTarget.localRotation = rot;
                }
            }
            else if (!isVrActive && headCamera != null)
            {
                // デスクトップ時は視線の少し下・左右に手を配置
                float side = (node == XRNode.LeftHand) ? -0.22f : 0.22f;
                Vector3 targetWorld = headCamera.transform.position
                                    + headCamera.transform.forward * 0.45f
                                    + headCamera.transform.right * side
                                    - headCamera.transform.up * 0.22f;
                handTarget.position = Vector3.Lerp(handTarget.position, targetWorld, Time.deltaTime * 15f);
                handTarget.rotation = headCamera.transform.rotation;
            }
        }

        private void UpdateDesktopLook()
        {
            if (headCamera == null) return;

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            // 右ドラッグで視点回転
            bool rotateActive = (mouse != null && mouse.rightButton.isPressed);
            if (rotateActive)
            {
                Vector2 delta = mouse.delta.ReadValue();
                desktopYaw += delta.x * mouseSensitivity;
                desktopPitch -= delta.y * mouseSensitivity;
                desktopPitch = Mathf.Clamp(desktopPitch, -85f, 85f);
            }

            // キーボードの矢印キー / WASDでも視点回転可能
            if (keyboard != null)
            {
                float keyTurnSpeed = 110f * Time.deltaTime;
                if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) desktopYaw -= keyTurnSpeed;
                if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) desktopYaw += keyTurnSpeed;
                if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) desktopPitch = Mathf.Clamp(desktopPitch - keyTurnSpeed, -85f, 85f);
                if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) desktopPitch = Mathf.Clamp(desktopPitch + keyTurnSpeed, -85f, 85f);
            }

            headCamera.transform.localRotation = Quaternion.Euler(desktopPitch, desktopYaw, 0f);
        }

        /// <summary>
        /// Meta Quest 3の左右コントローラでトリガーやグリップが押されているかを取得
        /// </summary>
        public bool IsVRSelectPressed(out Transform activeHand)
        {
            activeHand = rightHandTransform;

            if (CheckHandTrigger(XRNode.RightHand))
            {
                activeHand = rightHandTransform;
                return true;
            }
            if (CheckHandTrigger(XRNode.LeftHand))
            {
                activeHand = leftHandTransform;
                return true;
            }
            return false;
        }

        private bool CheckHandTrigger(XRNode node)
        {
            XRInputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid) return false;

            if (device.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool trigger) && trigger)
                return true;
            if (device.TryGetFeatureValue(XRCommonUsages.gripButton, out bool grip) && grip)
                return true;
            if (device.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool primary) && primary)
                return true;
            return false;
        }

        /// <summary>
        /// Meta Quest 3の左右コントローラにハプティクス振動を送る
        /// </summary>
        public void TriggerHaptics(float amplitude = 0.5f, float duration = 0.1f, XRNode? specificHand = null)
        {
            if (specificHand.HasValue)
            {
                SendHapticToNode(specificHand.Value, amplitude, duration);
            }
            else
            {
                SendHapticToNode(XRNode.LeftHand, amplitude, duration);
                SendHapticToNode(XRNode.RightHand, amplitude, duration);
            }
        }

        private void SendHapticToNode(XRNode node, float amplitude, float duration)
        {
            XRInputDevice device = InputDevices.GetDeviceAtXRNode(node);
            HapticCapabilities caps;
            if (device.isValid && device.TryGetHapticCapabilities(out caps) && caps.supportsImpulse)
            {
                device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), duration);
            }
        }
    }
}
