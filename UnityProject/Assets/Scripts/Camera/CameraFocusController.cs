using System.Collections;
using UnityEngine;
using JapanWeatherDemo.Map;

namespace JapanWeatherDemo.CameraControl
{
    /// <summary>選択都市の上空・斜め見下ろし構図へカメラをスムーズに移動する。</summary>
    public class CameraFocusController : MonoBehaviour
    {
        [SerializeField] private MapManager mapManager;
        [SerializeField] private FreeCameraController freeCamera;
        [SerializeField] private float height = 14f;
        [SerializeField] private float backDistance = 10f;
        [SerializeField] private float lookAtYOffset = 0f;
        [SerializeField] private float duration = 0.7f;

        private Coroutine moving;

        private void OnEnable()
        {
            if (mapManager != null) mapManager.CityFocused += OnCityFocused;
        }

        private void OnDisable()
        {
            if (mapManager != null) mapManager.CityFocused -= OnCityFocused;
        }

        private void OnCityFocused(Vector3 worldPos)
        {
            Vector3 target = worldPos + Vector3.up * lookAtYOffset;
            var (pos, rot) = CameraFraming.ComputeFocusPose(target, height, backDistance);
            if (moving != null) StopCoroutine(moving);
            moving = StartCoroutine(MoveTo(pos, rot));
        }

        private IEnumerator MoveTo(Vector3 targetPos, Quaternion targetRot)
        {
            // 移動中は自由カメラ入力を無効化して競合を防ぐ
            if (freeCamera != null) freeCamera.enabled = false;
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, duration);
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                transform.position = Vector3.Lerp(startPos, targetPos, e);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, e);
                yield return null;
            }
            transform.position = targetPos;
            transform.rotation = targetRot;
            if (freeCamera != null) freeCamera.enabled = true;
            moving = null;
        }
    }
}
