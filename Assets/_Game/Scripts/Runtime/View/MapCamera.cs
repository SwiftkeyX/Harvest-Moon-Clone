using UnityEngine;
using UnityEngine.InputSystem;

namespace Farm.View
{
    /// <summary>
    /// Follows the target from above, like the game's camera. The mouse wheel zooms from close
    /// behind the player out to the whole map. Q/E or dragging with the right mouse button turns
    /// the view; dragging up and down tilts it.
    /// </summary>
    public class MapCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float pitch = 50f;
        [SerializeField] private float yaw;
        [SerializeField] private float distance = 18f;
        [SerializeField] private float minDistance = 6f;
        [SerializeField] private float maxDistance = 600f;
        [SerializeField] private float minPitch = 10f;
        [SerializeField] private float maxPitch = 85f;
        [SerializeField] private float keyTurnSpeed = 90f;   // degrees per second
        [SerializeField] private float mouseTurnSpeed = 0.25f; // degrees per pixel
        [SerializeField] private float smoothing = 8f;

        /// <summary>The way the camera faces, flattened onto the ground. Movement uses it so W is always "away from the camera".</summary>
        public Quaternion Heading => Quaternion.Euler(0f, yaw, 0f);

        private void Start() => transform.SetPositionAndRotation(Desired(), Quaternion.Euler(pitch, yaw, 0f));

        private void LateUpdate()
        {
            float dt = UnityEngine.Time.deltaTime;
            var keys = Keyboard.current;
            if (keys != null)
                yaw += ((keys.eKey.isPressed ? 1f : 0f) - (keys.qKey.isPressed ? 1f : 0f)) * keyTurnSpeed * dt;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    distance = Mathf.Clamp(distance * (scroll > 0f ? 0.85f : 1.18f), minDistance, maxDistance);

                if (mouse.rightButton.isPressed)
                {
                    Vector2 drag = mouse.delta.ReadValue();
                    yaw += drag.x * mouseTurnSpeed;
                    pitch = Mathf.Clamp(pitch - drag.y * mouseTurnSpeed, minPitch, maxPitch);
                }
            }

            // Turning is applied at once so the view never swings through the ground; only the follow is smoothed.
            float t = 1f - Mathf.Exp(-smoothing * dt);
            Vector3 position = Vector3.Lerp(transform.position, Desired(), t);
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = target.position + Vector3.up * 0.8f;
            position = focus - rotation * Vector3.forward * Vector3.Distance(position, focus);
            transform.SetPositionAndRotation(position, rotation);
        }

        private Vector3 Desired() =>
            target.position + Vector3.up * 0.8f - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * distance;
    }
}
