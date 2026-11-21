using UnityEngine;

namespace HorrorGame.ScenarioA
{
    /// <summary>
    /// エレベーター内の鏡（正面パネル横の鏡＋背面の鏡）のRenderTextureを自動生成・管理し、
    /// メインカメラには映らず鏡の中にだけ幽霊が映るレイヤーマスク制御（MirrorOnly = Layer 6）を設定するクラス。
    /// </summary>
    public class ElevatorMirrorSetup : MonoBehaviour
    {
        public Camera mirrorCamera;
        public Renderer mirrorQuadRenderer;
        public int textureResolution = 512;

        private RenderTexture mirrorRT;

        private void Awake()
        {
            SetupLayerMasksAndRenderTexture();
        }

        public void SetupLayerMasksAndRenderTexture()
        {
            int mirrorOnlyLayer = LayerMask.NameToLayer("MirrorOnly");
            if (mirrorOnlyLayer < 0) mirrorOnlyLayer = 6;

            // メインカメラ（プレイヤーの肉眼）からは MirrorOnly レイヤーを除外
            if (Camera.main != null)
            {
                Camera.main.cullingMask &= ~(1 << mirrorOnlyLayer);
            }

            if (mirrorCamera == null || mirrorQuadRenderer == null) return;

            // 鏡用カメラには MirrorOnly レイヤーを含める
            mirrorCamera.cullingMask |= (1 << mirrorOnlyLayer);

            mirrorRT = new RenderTexture(textureResolution, textureResolution, 16, RenderTextureFormat.ARGB32);
            mirrorRT.name = "ElevatorMirror_RT_" + gameObject.name;
            mirrorRT.Create();

            mirrorCamera.targetTexture = mirrorRT;

            Material mat = mirrorQuadRenderer.material;
            mat.mainTexture = mirrorRT;
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", mirrorRT);
            }
            // 左右反転して鏡像にする
            mat.mainTextureScale = new Vector2(-1f, 1f);
            mat.mainTextureOffset = new Vector2(1f, 0f);
            if (mat.HasProperty("_BaseMap_ST"))
            {
                mat.SetVector("_BaseMap_ST", new Vector4(-1f, 1f, 1f, 0f));
            }
        }

        private void OnDestroy()
        {
            if (mirrorRT != null)
            {
                if (mirrorCamera != null) mirrorCamera.targetTexture = null;
                mirrorRT.Release();
                Destroy(mirrorRT);
            }
        }
    }
}
