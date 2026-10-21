using UnityEngine;

namespace MiniRPG
{
    /// <summary>Fait aller et venir un objet entre deux points (robot de nettoyage, chariot...).</summary>
    public class Patrol : MonoBehaviour
    {
        public Vector3 pointA;
        public Vector3 pointB;
        public float speed = 1.5f;
        public float pause = 1f;

        bool toB = true;
        float waitUntil;

        void Update()
        {
            if (Time.time < waitUntil) return;
            var target = toB ? pointB : pointA;
            var to = target - transform.position;
            to.y = 0;
            if (to.magnitude < 0.05f)
            {
                toB = !toB;
                waitUntil = Time.time + pause;
                return;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 6f * Time.deltaTime);
            transform.position += to.normalized * Mathf.Min(speed * Time.deltaTime, to.magnitude);
        }
    }
}
