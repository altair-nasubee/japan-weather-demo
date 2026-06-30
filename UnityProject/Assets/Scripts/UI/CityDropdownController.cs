using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using JapanWeatherDemo.Data;
using JapanWeatherDemo.Map;

namespace JapanWeatherDemo.UI
{
    /// <summary>右上情報パネルの都市選択ドロップダウン。マーカー選択と双方向連動する。</summary>
    public class CityDropdownController : MonoBehaviour
    {
        [SerializeField] private MapManager mapManager;
        [SerializeField] private TMP_Dropdown dropdown;

        private readonly List<string> cityNames = new();
        private CityDropdownOpenHandler openHandler;
        private Coroutine scrollCoroutine;

        private void Awake()
        {
            // MapManager と同じソース・同じ並び（Cities.json 順 ≒ 北→南）で選択肢を生成
            cityNames.Clear();
            foreach (var c in CityCatalog.LoadFromStreamingAssets())
                cityNames.Add(c.name);

            dropdown.ClearOptions();
            dropdown.AddOptions(cityNames);

            openHandler = dropdown.gameObject.GetComponent<CityDropdownOpenHandler>();
            if (openHandler == null)
                openHandler = dropdown.gameObject.AddComponent<CityDropdownOpenHandler>();
            openHandler.Setup(this);
        }

        private void OnEnable()
        {
            dropdown.onValueChanged.AddListener(OnDropdownChanged);
            if (mapManager != null) mapManager.CitySelected += OnCitySelected;
        }

        private void OnDisable()
        {
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            if (mapManager != null) mapManager.CitySelected -= OnCitySelected;
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
                scrollCoroutine = null;
            }
        }

        internal void NotifyDropdownClicked()
        {
            if (scrollCoroutine != null)
                StopCoroutine(scrollCoroutine);
            scrollCoroutine = StartCoroutine(ScrollToSelectedAfterShow());
        }

        // ドロップダウン操作 → 既存の選択フローを駆動
        private void OnDropdownChanged(int index)
        {
            if (mapManager == null) return;
            if (index < 0 || index >= cityNames.Count) return;
            mapManager.SelectByName(cityNames[index]);
        }

        // マーカー選択など外部からの選択 → ドロップダウン現在値を同期（無限ループ防止）
        private void OnCitySelected(CityData city)
        {
            int index = cityNames.IndexOf(city.name);
            if (index < 0) return;
            dropdown.SetValueWithoutNotify(index);
            dropdown.RefreshShownValue();
        }

        // TMP_Dropdown は展開時に選択項目へ自動スクロールしないため、開いた直後に合わせる
        private IEnumerator ScrollToSelectedAfterShow()
        {
            yield return null;

            if (!dropdown.IsExpanded)
            {
                scrollCoroutine = null;
                yield break;
            }

            var listRoot = dropdown.transform.Find("Dropdown List");
            if (listRoot == null)
            {
                scrollCoroutine = null;
                yield break;
            }

            int index = Mathf.Clamp(dropdown.value, 0, dropdown.options.Count - 1);
            int count = dropdown.options.Count;
            if (count > 1)
            {
                var scrollRect = listRoot.GetComponent<ScrollRect>();
                if (scrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    float normalized = 1f - (float)index / (count - 1);
                    scrollRect.normalizedPosition = new Vector2(0f, normalized);
                }
            }

            foreach (var toggle in listRoot.GetComponentsInChildren<Toggle>(false))
            {
                if (!toggle.isOn) continue;
                toggle.Select();
                break;
            }

            scrollCoroutine = null;
        }

        sealed class CityDropdownOpenHandler : MonoBehaviour, IPointerClickHandler
        {
            CityDropdownController controller;

            public void Setup(CityDropdownController controller) => this.controller = controller;

            public void OnPointerClick(PointerEventData eventData) => controller?.NotifyDropdownClicked();
        }
    }
}
