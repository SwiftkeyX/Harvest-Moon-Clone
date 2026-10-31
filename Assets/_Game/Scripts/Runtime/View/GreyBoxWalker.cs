using UnityEngine;
using UnityEngine.InputSystem;

namespace Farm.View
{
    /// <summary>
    /// Walks a capsule around a grey-box level with the game's Move action, at the game's speed,
    /// so layouts can be tested at the real scale and camera angle. There is no jump, like the game:
    /// higher ground has to be reached by stairs or ramps.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class GreyBoxWalker : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private float speed = 4f; // same as PlayerController
        [SerializeField] private float gravity = 20f;

        private CharacterController body;
        private InputAction move;
        private float fallSpeed;

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            move = actions.FindActionMap("Player", true).FindAction("Move", true);
        }

        private void OnEnable() => move.Enable();
        private void OnDisable() => move.Disable();

        private void Update()
        {
            float dt = UnityEngine.Time.deltaTime;
            Vector2 input = move.ReadValue<Vector2>();
            var direction = new Vector3(input.x, 0f, input.y);
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            fallSpeed = body.isGrounded ? 1f : fallSpeed + gravity * dt;
            body.Move((direction * speed + Vector3.down * fallSpeed) * dt);

            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
