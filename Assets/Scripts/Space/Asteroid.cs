using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>Astéroïde : tourne lentement, explose en débris quand on le détruit au laser.</summary>
    [RequireComponent(typeof(Health))]
    public class Asteroid : MonoBehaviour
    {
        public static readonly List<Asteroid> All = new List<Asteroid>();
        public static int Destroyed;

        public Vector3 spin;
        public Color color = Color.gray;
        public float Radius { get { return transform.localScale.x * 0.5f; } }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Awake() { GetComponent<Health>().Died += OnDied; }

        void Update() { transform.Rotate(spin * Time.deltaTime, Space.Self); }

        void OnDied(Health h)
        {
            Destroyed++;
            for (int i = 0; i < 7; i++)
            {
                var piece = Prim.Make(PrimitiveType.Cube, "Debris", null, transform.position + Random.insideUnitSphere * Radius * 0.5f,
                    Vector3.one * Radius * Random.Range(0.15f, 0.35f), color, false, Random.rotation.eulerAngles);
                var d = piece.AddComponent<Debris>();
                d.velocity = Random.onUnitSphere * Random.Range(6f, 16f);
            }
            Spark.Spawn(transform.position, new Color(1f, 0.6f, 0.2f));
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.15f, 0.12f);
            Destroy(gameObject);
        }
    }

    /// <summary>Débris qui s'éloigne en tournant, rétrécit puis disparaît.</summary>
    public class Debris : MonoBehaviour
    {
        public Vector3 velocity;
        Vector3 spin;
        Vector3 startScale;
        float born;
        const float Life = 2.5f;

        void Start()
        {
            spin = Random.insideUnitSphere * 300f;
            startScale = transform.localScale;
            born = Time.time;
        }

        void Update()
        {
            float k = (Time.time - born) / Life;
            if (k >= 1f) { Destroy(gameObject); return; }
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(spin * Time.deltaTime);
            transform.localScale = startScale * (1f - k);
        }
    }
}
