using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Map
{
    /// <summary>都市マスタを読み、ビルボードピンマーカーを配置・選択管理する。</summary>
    public class MapManager : MonoBehaviour
    {
        [SerializeField] private MapBoundsSO bounds;
        [SerializeField] private CityMarker markerPrefab;
        [SerializeField] private Transform markerParent;
        [SerializeField] private float markerY = 0.1f;
        [SerializeField] private UnityEngine.Camera raycastCamera;

        private readonly List<CityMarker> markers = new();
        private CityMarker selected;

        public IReadOnlyList<CityMarker> Markers => markers;
        public event System.Action<CityData> CitySelected;
        /// <summary>選択マーカーのワールド位置を通知する（カメラフォーカス用）。</summary>
        public event System.Action<Vector3> CityFocused;


        private void Awake()
        {
            if (raycastCamera == null) raycastCamera = UnityEngine.Camera.main;
            BuildMarkers();
        }

        private void BuildMarkers()
        {
            var cities = CityCatalog.LoadFromStreamingAssets();
            foreach (var city in cities)
            {
                Vector2 xz = GeoProjection.LatLonToXZ(city.lat, city.lon, bounds);
                var marker = Instantiate(markerPrefab, markerParent);
                marker.transform.position = new Vector3(xz.x, markerY, xz.y);
                marker.Init(city);
                marker.SetCamera(raycastCamera);
                marker.Clicked += OnMarkerClicked;
                markers.Add(marker);
            }
        }

        private CityMarker hovered;

        private void Update()
        {
            // UI（タイムライン・情報パネル等）の上では地図の hover/クリックを無視する
            bool overUI = UnityEngine.EventSystems.EventSystem.current != null &&
                          UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            var mouse = Mouse.current;
            if (mouse == null || overUI)
            {
                SetHover(null);
                return;
            }

            Vector2 screen = mouse.position.ReadValue();
            Ray ray = raycastCamera.ScreenPointToRay(screen);
            CityMarker hit = null;
            if (Physics.Raycast(ray, out RaycastHit h, 1000f))
                hit = h.collider.GetComponentInParent<CityMarker>();

            SetHover(hit);

            if (mouse.leftButton.wasPressedThisFrame && hit != null)
                hit.NotifyClicked();
        }

        // ホバー対象の切替を管理する
        private void SetHover(CityMarker marker)
        {
            if (hovered == marker) return;
            if (hovered != null) hovered.SetHover(false);
            hovered = marker;
            if (hovered != null) hovered.SetHover(true);
        }

        private void OnMarkerClicked(CityMarker marker) => Select(marker);

        public void SelectByName(string cityName)
        {
            var m = markers.Find(x => x.City.name == cityName);
            if (m != null) Select(m);
        }

        private void Select(CityMarker marker)
        {
            if (selected != null) selected.SetSelected(false);
            selected = marker;
            selected.SetSelected(true);
            CitySelected?.Invoke(marker.City);
            CityFocused?.Invoke(marker.transform.position);

        }
    }
}