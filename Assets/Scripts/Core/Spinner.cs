using UnityEngine;

namespace MiniRPG
{
    /// <summary>Fait tourner lentement un objet (repère d'objectif).</summary>
    public class Spinner : MonoBehaviour
    {
        public float speed = 60f;
        void Update() { transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.World); }
    }
}
