using UnityEngine;

namespace MiniRPG
{
    /// <summary>Petit tremblement de caméra pour donner du poids aux coups.</summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }
        public Vector3 Offset { get; private set; }

        float timeLeft, duration, amplitude;

        void Awake() { Instance = this; }

        public void Shake(float time, float amp)
        {
            if (amp < amplitude && timeLeft > 0f) return;
            duration = timeLeft = time;
            amplitude = amp;
        }

        void Update()
        {
            if (timeLeft <= 0f) { Offset = Vector3.zero; return; }
            timeLeft -= Time.deltaTime;
            float k = Mathf.Clamp01(timeLeft / duration);
            Offset = Random.insideUnitSphere * amplitude * k;
        }
    }
}
