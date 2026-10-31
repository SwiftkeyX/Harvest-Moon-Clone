using UnityEngine;
using UnityEngine.InputSystem;

namespace Farm.View
{
    /// <summary>
    /// Walks around a test map, relative to the MapCamera's facing. WASD or arrows move, Shift runs, Space jumps, F toggles flying
    /// (Space up, Ctrl down), R goes back to the start. Falling into the sea also goes back to the start.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class MapWalker : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float runSpeed = 16f;
        [SerializeField] private float flySpeed = 40f;
        [SerializeField] private float jumpHeight = 2.5f;
        [SerializeField] private float gravity = 25f;
        [SerializeField] private float resetBelowY = -120f;

        private CharacterController body;
        private Vector3 start;
        private float verticalSpeed;
        private bool flying;
        private MapCamera view;

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            start = transform.position;
        }

        private void Update()
        {
            var keys = Keyboard.current;
            if (keys == null) return;
            float dt = UnityEngine.Time.deltaTime;

            if (keys.rKey.wasPressedThisFrame || transform.position.y < resetBelowY) ResetToStart();
            if (keys.fKey.wasPressedThisFrame) { flying = !flying; verticalSpeed = 0f; }

            var input = new Vector3(Axis(keys.dKey, keys.rightArrowKey) - Axis(keys.aKey, keys.leftArrowKey), 0f,
                Axis(keys.wKey, keys.upArrowKey) - Axis(keys.sKey, keys.downArrowKey));
            if (input.sqrMagnitude > 1f) input.Normalize();
            if (view == null && Camera.main != null) view = Camera.main.GetComponent<MapCamera>();
            if (view != null) input = view.Heading * input; // W walks away from the camera, however it's turned

            float speed = flying ? flySpeed : keys.shiftKey.isPressed ? runSpeed : walkSpeed;
            Vector3 move = input * speed;

            if (flying)
            {
                move.y = (keys.spaceKey.isPressed ? 1f : 0f) - (keys.ctrlKey.isPressed ? 1f : 0f);
                move.y *= flySpeed;
            }
            else
            {
                if (body.isGrounded)
                {
                    verticalSpeed = -2f; // keeps the body pressed onto slopes and steps
                    if (keys.spaceKey.wasPressedThisFrame) verticalSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
                }
                verticalSpeed -= gravity * dt;
                move.y = verticalSpeed;
            }

            body.Move(move * dt);
            if (input.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(input);
        }

        private void ResetToStart()
        {
            body.enabled = false; // a CharacterController ignores position changes while enabled
            transform.position = start;
            body.enabled = true;
            verticalSpeed = 0f;
        }

        private static float Axis(UnityEngine.InputSystem.Controls.KeyControl a, UnityEngine.InputSystem.Controls.KeyControl b) =>
            a.isPressed || b.isPressed ? 1f : 0f;
    }
}
