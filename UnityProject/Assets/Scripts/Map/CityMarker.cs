using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Map
{
    /// <summary>地図上の 1 都市を表すビルボードピン。常にカメラへ正対し、画面上一定サイズを保つ。
    /// ホバー/選択時に地点名を表示し、選択時はピン色を変える。クリックで選択を通知する。</summary>
    [RequireComponent(typeof(Collider))]
    public class CityMarker : MonoBehaviour
    {
        [SerializeField] private Image pin;        // ピン画像（白ベース、color で tint）
        [SerializeField] private TMP_Text label;   // 地点名（ホバー/選択時のみ表示）
        [SerializeField] private float scalePerUnit = 0.01f; // 距離1あたりのワールドスケール

        public CityData City { get; private set; }
        public event System.Action<CityMarker> Clicked;

        private readonly Color baseColor = new Color(0.45f, 0.65f, 0.85f);      // Color(0.38f, 0.62f, 0.45f);
        private readonly Color selectedColor = new Color(0.95f, 0.3f, 0.3f);

        private UnityEngine.Camera cam;
        private bool selected;
        private bool hovered;

        public void Init(CityData city)
        {
            City = city;
            name = $"Marker_{city.name}";
            if (label != null) label.text = city.name;
            selected = false;
            hovered = false;
            ApplyState();
        }

        public void SetCamera(UnityEngine.Camera c) => cam = c;

        public void SetSelected(bool value)
        {
            selected = value;
            ApplyState();
        }

        public void SetHover(bool value)
        {
            hovered = value;
            ApplyState();
        }

        // ピン色とラベル表示を現在の状態から更新する
        private void ApplyState()
        {
            if (pin != null) pin.color = selected ? selectedColor : baseColor;
            // ラベルは「選択中 または ホバー中」のときだけ表示する
            if (label != null) {
                label.gameObject.SetActive(selected || hovered);
                label.color = selected ? selectedColor : baseColor;
            }
        }

        private void LateUpdate()
        {
            if (cam == null) cam = UnityEngine.Camera.main;
            if (cam == null) return;
            // 正対：カメラ回転をそのまま採用（テキストが鏡像にならない）
            transform.rotation = cam.transform.rotation;
            // 画面上一定サイズ：距離に比例してスケール（collider も同 transform で連動）
            float dist = Vector3.Distance(cam.transform.position, transform.position);
            float s = BillboardScale.ScaleForConstantScreenSize(dist, scalePerUnit);
            transform.localScale = new Vector3(s, s, s);
        }

        // MapManager から Raycast ヒット時に呼ばれる
        public void NotifyClicked() => Clicked?.Invoke(this);
    }
}
