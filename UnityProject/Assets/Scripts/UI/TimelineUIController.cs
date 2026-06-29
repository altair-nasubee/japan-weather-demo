using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.UI
{
    /// <summary>下部タイムライン。再生・スライダーで連続位置を制御し DateTime を表示する。</summary>
    public class TimelineUIController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Slider slider;
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text dateTimeLabel;
        [SerializeField] private Image playIcon;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private float secondsPerSnapshot = 1.0f; // 再生速度

        private bool isPlaying;
        private float pos;
        private bool suppressSliderCallback;

        private WeatherTimelineSO Timeline => gameManager != null ? gameManager.Timeline : null;

        private void OnEnable()
        {
            if (Timeline != null) Timeline.OnSnapshotChanged += OnSnapshot;
            if (slider != null) slider.onValueChanged.AddListener(OnSliderChanged);
            if (playButton != null) playButton.onClick.AddListener(TogglePlay);
            UpdatePlayIcon();
        }

        private void OnDisable()
        {
            if (Timeline != null) Timeline.OnSnapshotChanged -= OnSnapshot;
            if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged);
            if (playButton != null) playButton.onClick.RemoveListener(TogglePlay);
        }

        // 新しい都市データが入ったらスライダーの範囲を更新する
        public void ConfigureForCurrentData()
        {
            if (Timeline == null || Timeline.Count == 0 || slider == null) return;
            suppressSliderCallback = true;
            slider.minValue = 0;
            slider.maxValue = Timeline.Count - 1;
            slider.value = 0;
            suppressSliderCallback = false;
            pos = 0f;
        }

        private void OnSliderChanged(float v)
        {
            if (suppressSliderCallback) return;
            pos = v;
            Timeline.SetContinuousIndex(pos);
        }

        private void TogglePlay()
        {
            isPlaying = !isPlaying;
            UpdatePlayIcon();
        }

        // 再生中は一時停止アイコン、停止中は再生アイコンを表示
        private void UpdatePlayIcon()
        {
            if (playIcon != null) playIcon.sprite = isPlaying ? pauseSprite : playSprite;
        }

        private void Update()
        {
            if (!isPlaying || Timeline == null || Timeline.Count < 2) return;
            pos += Time.deltaTime / Mathf.Max(0.01f, secondsPerSnapshot);
            if (pos >= Timeline.Count - 1) { pos = Timeline.Count - 1; isPlaying = false; UpdatePlayIcon(); }

            suppressSliderCallback = true;
            if (slider != null) slider.value = pos;
            suppressSliderCallback = false;
            Timeline.SetContinuousIndex(pos);
        }

        private void OnSnapshot(WeatherSnapshot s)
        {
            if (dateTimeLabel != null) dateTimeLabel.text = s.dateTime.ToString("yyyy/MM/dd HH:mm");
        }
    }
}
