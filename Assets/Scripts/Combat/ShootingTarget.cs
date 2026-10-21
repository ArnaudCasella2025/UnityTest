using System.Collections;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Cible du stand de tir : elle bascule quand on la touche assez, puis se relève.
    /// Certaines se déplacent de gauche à droite.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class ShootingTarget : MonoBehaviour
    {
        public static int Hits;

        public float moveAmplitude = 0f;
        public float moveSpeed = 1f;
        public float respawnDelay = 2.5f;

        Health health;
        Vector3 origin;
        Quaternion uprightRotation;
        float phase;
        bool down;

        void Awake()
        {
            health = GetComponent<Health>();
            health.Died += OnDied;
        }

        void Start()
        {
            origin = transform.localPosition;
            uprightRotation = transform.localRotation;
            phase = Random.value * 10f;
        }

        void Update()
        {
            if (moveAmplitude > 0f && !down)
                transform.localPosition = origin + uprightRotation * Vector3.right * Mathf.Sin(Time.time * moveSpeed + phase) * moveAmplitude;
        }

        void OnDied(Health h)
        {
            Hits++;
            if (HUD.Instance != null) HUD.Instance.ShowMessage("Cible abattue ! (" + Hits + ")", 1.2f);
            StartCoroutine(FallAndRise());
        }

        IEnumerator FallAndRise()
        {
            down = true;
            var up = uprightRotation;
            var flat = uprightRotation * Quaternion.Euler(-85f, 0, 0);
            for (float t = 0; t < 0.25f; t += Time.deltaTime)
            {
                transform.localRotation = Quaternion.Slerp(up, flat, t / 0.25f);
                yield return null;
            }
            transform.localRotation = flat;
            yield return new WaitForSeconds(respawnDelay);
            for (float t = 0; t < 0.4f; t += Time.deltaTime)
            {
                transform.localRotation = Quaternion.Slerp(flat, up, t / 0.4f);
                yield return null;
            }
            transform.localRotation = up;
            health.Revive();
            down = false;
        }
    }
}
