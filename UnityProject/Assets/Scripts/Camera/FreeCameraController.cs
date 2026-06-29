using UnityEngine;
using UnityEngine.InputSystem;

namespace JapanWeatherDemo.CameraControl
{
    /// <summary>
    /// Input System を用いた自由カメラ。右ドラッグでオービット回転、
    /// ホイールでズーム、中ドラッグ/WASD でパン、Q/E で高度。
    /// </summary>
    public class FreeCameraController : MonoBehaviour
    {
        [SerializeField] private float orbitSpeed = 0.2f;
        [SerializeField] private float panSpeed = 0.02f;
        [SerializeField] private float keyPanSpeed = 10f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float altitudeSpeed = 8f;

        private void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            // ポインターが UI（ドロップダウンのリスト等）の上にある間は、マウス由来のカメラ操作を
            // 無視して UI 操作（ホイールでのリストスクロール等）と競合させない。
            // キーボード（WASD/QE）操作は UI 上でも維持する。
            bool pointerOverUI =
                UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (mouse != null && !pointerOverUI)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();

                // 右ドラッグ: オービット回転（注視点を中心に）
                if (mouse.rightButton.isPressed)
                {
                    transform.RotateAround(transform.position, Vector3.up, mouseDelta.x * orbitSpeed);
                    transform.RotateAround(transform.position, transform.right, -mouseDelta.y * orbitSpeed);
                }

                // 中ドラッグ: パン
                if (mouse.middleButton.isPressed)
                {
                    Vector3 pan = (-transform.right * mouseDelta.x - transform.up * mouseDelta.y) * panSpeed;
                    transform.position += pan;
                }

                // ホイール: ズーム（前後移動）
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    transform.position += transform.forward * Mathf.Sign(scroll) * zoomSpeed;
                }
            }

            if (keyboard == null) return;

            // WASD: 水平パン、Q/E: 高度
            Vector3 move = Vector3.zero;
            if (keyboard.wKey.isPressed) move += transform.forward;
            if (keyboard.sKey.isPressed) move -= transform.forward;
            if (keyboard.dKey.isPressed) move += transform.right;
            if (keyboard.aKey.isPressed) move -= transform.right;
            move.y = 0f;
            transform.position += move * keyPanSpeed * Time.deltaTime;

            // Q/E: 高度（altitudeSpeed で上下）
            Vector3 alt = Vector3.zero;
            if (keyboard.eKey.isPressed) alt += Vector3.up;
            if (keyboard.qKey.isPressed) alt -= Vector3.up;
            transform.position += alt * altitudeSpeed * Time.deltaTime;
        }
    }
}