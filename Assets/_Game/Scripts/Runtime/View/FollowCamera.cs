using UnityEngine;

namespace Farm.View
{
    /// <summary>Follows the target smoothly, keeping the offset the camera has in the scene.</summary>
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothing = 6f;

        private Vector3 offset;

        private void Awake()
        {
            offset = transform.position - target.position;
        }

        private void LateUpdate()
        {
            Vector3 desired = target.position + offset;
            float t = 1f - Mathf.Exp(-smoothing * UnityEngine.Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
        }
    }
}
