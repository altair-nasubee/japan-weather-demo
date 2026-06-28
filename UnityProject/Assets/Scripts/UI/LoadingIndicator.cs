using UnityEngine;

namespace JapanWeatherDemo.UI
{
    /// <summary>取得中に回転表示するローディングインジケーター。</summary>
    public class LoadingIndicator : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private RectTransform spinner;
        [SerializeField] private float degPerSecond = 180f;

        private bool loading;

        private void OnEnable()
        {
            if (gameManager != null) gameManager.LoadingChanged += OnLoading;
            SetVisible(false);
        }

        private void OnDisable()
        {
            if (gameManager != null) gameManager.LoadingChanged -= OnLoading;
        }

        private void OnLoading(bool isLoading) { loading = isLoading; SetVisible(isLoading); }

        private void SetVisible(bool v) { if (spinner != null) spinner.gameObject.SetActive(v); }

        private void Update()
        {
            if (loading && spinner != null) spinner.Rotate(0f, 0f, -degPerSecond * Time.deltaTime);
        }
    }
}
