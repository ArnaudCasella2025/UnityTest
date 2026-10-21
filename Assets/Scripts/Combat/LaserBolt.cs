using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Tir de pistolaser : une capsule lumineuse qui avance très vite et teste les collisions par rayon
    /// à chaque image (pas de Rigidbody). Inflige des dégâts aux objets qui ont un composant Health.
    /// </summary>
    public class LaserBolt : MonoBehaviour
    {
        public float speed = 45f;
        public float damage = 10f;
        public float lifetime = 2f;
        public Transform owner;
        public Color color = Color.red;

        float dieAt;

        static readonly string[] NpcComplaints =
        {
            "Hé ! Range ce pistolaser !",
            "Pas dans les coursives !",
            "Tu vas finir au rapport !",
            "Attention, ça brûle !",
        };

        public static LaserBolt Fire(Vector3 position, Vector3 direction, Color color, float damage, Transform owner, float speed = 45f, float length = 0.9f)
        {
            direction.Normalize();
            var go = Prim.Make(PrimitiveType.Capsule, "Laser", null, position, new Vector3(0.09f, length * 0.5f, 0.09f), color, false, null, true);
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            var bolt = go.AddComponent<LaserBolt>();
            bolt.speed = speed;
            bolt.damage = damage;
            bolt.owner = owner;
            bolt.color = color;
            return bolt;
        }

        void Start() { dieAt = Time.time + lifetime; }

        void Update()
        {
            if (Time.time > dieAt) { Destroy(gameObject); return; }

            var dir = transform.up;
            float step = speed * Time.deltaTime;
            var hits = Physics.RaycastAll(transform.position, dir, step, ~0, QueryTriggerInteraction.Ignore);
            RaycastHit? nearest = null;
            foreach (var h in hits)
            {
                if (owner != null && h.collider.transform.IsChildOf(owner)) continue;
                if (nearest == null || h.distance < nearest.Value.distance) nearest = h;
            }

            if (nearest != null)
            {
                var hit = nearest.Value;
                var health = hit.collider.GetComponentInParent<Health>();
                if (health != null && !health.IsDead) health.Damage(damage, transform.position);

                var npc = hit.collider.GetComponentInParent<NPC>();
                if (npc != null) npc.Say(NpcComplaints[Random.Range(0, NpcComplaints.Length)]);

                Spark.Spawn(hit.point, color);
                Destroy(gameObject);
                return;
            }

            transform.position += dir * step;
        }
    }

    /// <summary>Petit éclat lumineux à l'impact d'un laser.</summary>
    public class Spark : MonoBehaviour
    {
        float born;
        const float Duration = 0.18f;

        public static void Spawn(Vector3 position, Color color)
        {
            var go = Prim.Make(PrimitiveType.Sphere, "Spark", null, position, Vector3.one * 0.2f, color, false, null, true);
            go.AddComponent<Spark>();
        }

        void Start() { born = Time.time; }

        void Update()
        {
            float k = (Time.time - born) / Duration;
            if (k >= 1f) { Destroy(gameObject); return; }
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.7f, k) * (1f - k * 0.5f);
        }
    }
}
