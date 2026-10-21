using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Caméra à la 3e personne : orbite au clic droit,
    /// zoom à la molette, suit le joueur en douceur et évite de traverser les décors.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 8f;
        public float minDistance = 3f;
        public float maxDistance = 14f;
        public float yaw = 0f;
        public float pitch = 22f;
        public float minPitch = -10f;
        public float maxPitch = 70f;
        public float mouseSensitivity = 3f;
        public float followSharpness = 12f;
        public float pivotHeight = 1.6f;

        Vector3 pivot;
        float currentDistance;

        void Start()
        {
            if (target != null) pivot = target.position + Vector3.up * pivotHeight;
            currentDistance = distance;
        }

        void LateUpdate()
        {
            if (target == null) return;

            bool locked = DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;
            if (!locked)
            {
                if (Input.GetMouseButton(1))
                {
                    yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                    pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                }

                distance -= Input.mouseScrollDelta.y * 1.2f;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            var desiredPivot = target.position + Vector3.up * pivotHeight;
            pivot = Vector3.Lerp(pivot, desiredPivot, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));

            var rot = Quaternion.Euler(pitch, yaw, 0);
            var back = rot * Vector3.back;

            // Collision : on raccourcit la distance si un décor est entre le joueur et la caméra.
            float wanted = distance;
            var hits = Physics.SphereCastAll(pivot, 0.3f, back, distance, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<Health>() != null) continue; // ignore les personnages
                if (hit.distance > 0f && hit.distance < wanted) wanted = hit.distance;
            }
            wanted = Mathf.Max(wanted, 0.8f);
            // Rapproche vite, s'éloigne doucement.
            float sharp = wanted < currentDistance ? 30f : 4f;
            currentDistance = Mathf.Lerp(currentDistance, wanted, 1f - Mathf.Exp(-sharp * Time.deltaTime));

            transform.position = pivot + back * currentDistance;
            transform.rotation = rot;

            if (CameraShake.Instance != null) transform.position += CameraShake.Instance.Offset;
        }
    }
}
