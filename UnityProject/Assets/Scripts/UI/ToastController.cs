using System.Collections;
using UnityEngine;
using TMPro;

namespace JapanWeatherDemo.UI
{
    /// <summary>画面上部に一定時間メッセージを表示して自動で消えるトースト。</summary>
    public class ToastController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float showSeconds = 3f;

        private Coroutine routine;

        private void OnEnable()
        {
            if (gameManager != null) gameManager.StatusMessage += Show;
            if (group != null) group.alpha = 0f;
        }

        private void OnDisable()
        {
            if (gameManager != null) gameManager.StatusMessage -= Show;
        }

        public void Show(string message)
        {
            if (label != null) label.text = message;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            if (group != null) group.alpha = 1f;
            yield return new WaitForSeconds(showSeconds);
            float t = 0f;
            while (t < 1f && group != null) { t += Time.deltaTime; group.alpha = 1f - t; yield return null; }
            if (group != null) group.alpha = 0f;
        }
    }
}
