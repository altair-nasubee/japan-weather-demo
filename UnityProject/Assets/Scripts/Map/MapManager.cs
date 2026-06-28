using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Map
{
    /// <summary>都市マスタを読み、光柱マーカーを配置・選択管理する。</summary>
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
                marker.Clicked += OnMarkerClicked;
                markers.Add(marker);
            }
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            // UI（タイムライン・情報パネル等）の上では地図クリックを無視する
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 screen = mouse.position.ReadValue();
            Ray ray = raycastCamera.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                var marker = hit.collider.GetComponentInParent<CityMarker>();
                if (marker != null) marker.NotifyClicked();
            }
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
        }
    }
}