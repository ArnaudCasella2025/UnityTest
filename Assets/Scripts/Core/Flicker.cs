using UnityEngine;

namespace MiniRPG
{
    /// <summary>Fait vaciller une lumière et pulser les flammes d'un feu de camp.</summary>
    public class Flicker : MonoBehaviour
    {
        public Light lightSource;
        public Transform[] flames;
        public float baseIntensity = 2.5f;

        Vector3[] flameScales;
        float seed;

        void Start()
        {
            seed = Random.value * 100f;
            flameScales = new Vector3[flames != null ? flames.Length : 0];
            for (int i = 0; i < flameScales.Length; i++) flameScales[i] = flames[i].localScale;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * 6f);
            if (lightSource != null) lightSource.intensity = baseIntensity * (0.75f + 0.5f * n);
            for (int i = 0; i < flameScales.Length; i++)
            {
                float k = 0.8f + 0.4f * Mathf.PerlinNoise(seed + i * 7.3f, Time.time * 8f);
                flames[i].localScale = new Vector3(flameScales[i].x, flameScales[i].y * k, flameScales[i].z);
            }
        }
    }
}
