using System.Collections.Generic;
using UnityEngine;
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

        private void Awake()
        {
            // MapManager と同じソース・同じ並び（Cities.json 順 ≒ 北→南）で選択肢を生成
            cityNames.Clear();
            foreach (var c in CityCatalog.LoadFromStreamingAssets())
                cityNames.Add(c.name);

            dropdown.ClearOptions();
            dropdown.AddOptions(cityNames);
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
        }

        // ドロップダウン操作 → 既存の選択フローを駆動
        private void OnDropdownChanged(int index)
        {
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
    }
}
