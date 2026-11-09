using UnityEngine;
using UnityEngine.UI;

namespace Rahmanda.PavoSDK
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class PavoLidarVisualizer : MaskableGraphic
    {
        [SerializeField] private PavoLidar lidar;

        public float scale = 1f;
        public float rotation = -90f;
        public float lineThickness = 1f;
        public Color lineColor = Color.green;

        private ScanFrame m_scanFrame;

        protected override void Awake()
        {
            base.Awake();

            if (lidar != null)
                SetLidar(lidar);
        }

        public void SetLidar(PavoLidar newLidar)
        {
            if (lidar != null)
                lidar.onScanFrame.RemoveListener(OnLidarScanFrame);

            lidar = newLidar;

            if (lidar != null)
                lidar.onScanFrame.AddListener(OnLidarScanFrame);
        }

        private void OnLidarScanFrame(ScanFrame scanFrame)
        {
            m_scanFrame = scanFrame;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            VertexDrawerHelper.DrawRect(vh, rectTransform.rect, color);

            if (m_scanFrame.points == null || m_scanFrame.points.Length == 0) return;

            Vector2 center = new Vector2(0, 0);
            foreach (var point in m_scanFrame.points)
            {
                Vector2 pointVector2 = Vector2Helper.ToVector2(center, rotation, point) * scale;
                VertexDrawerHelper.DrawLine(vh, center, pointVector2, lineThickness, lineColor);
            }
        }
    }
}