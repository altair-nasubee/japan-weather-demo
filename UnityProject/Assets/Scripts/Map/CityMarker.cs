using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Map
{
    /// <summary>地図上の 1 都市を表す光柱マーカー。クリックで選択を通知する。</summary>
    [RequireComponent(typeof(Collider))]
    public class CityMarker : MonoBehaviour
    {
        [SerializeField] private Light beamLight;          // 光柱の核となるライト（任意）
        [SerializeField] private Renderer beamRenderer;    // 光柱メッシュ（円柱など）

        public CityData City { get; private set; }
        public event System.Action<CityMarker> Clicked;

        private Color baseColor = new Color(0.4f, 0.7f, 1f);
        private Color selectedColor = new Color(1f, 0.85f, 0.3f);

        public void Init(CityData city)
        {
            City = city;
            name = $"Marker_{city.name}";
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            Color c = selected ? selectedColor : baseColor;
            // HDRP の _EmissiveColor は nits 相当。地図は太陽光(10万Lux)で約1.6万nits相当に照らされるため、
            // 光柱を目立たせるにはそれを上回る強度が要る。
            float emission = selected ? 20000f : 5000f;
            // 全都市は Emissive メッシュで表現する
            if (beamRenderer != null)
                beamRenderer.material.SetColor("_EmissiveColor", c * emission);
            // リアルタイム Light は選択都市のみ有効化する（HDRP で 150〜200 個の常時ライトは負荷が高いため）
            if (beamLight != null)
            {
                beamLight.enabled = selected;
                beamLight.color = c;
                beamLight.intensity = 8f;
            }
        }

        // MapManager から Raycast ヒット時に呼ばれる
        public void NotifyClicked() => Clicked?.Invoke(this);
    }
}