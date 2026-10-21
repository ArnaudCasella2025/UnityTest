using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Personnage non joueur : on peut lui parler (touche E). Son dialogue et le marqueur
    /// au-dessus de sa tête (! ou ?) sont fournis par des fonctions, ce qui permet de les faire
    /// dépendre de l'état de la quête.
    /// </summary>
    public class NPC : MonoBehaviour
    {
        public static readonly List<NPC> All = new List<NPC>();

        public string npcName = "Villageois";
        public float interactRange = 2.8f;
        public float wanderRadius = 0f;
        public float wanderSpeed = 1.2f;

        public Func<Dialogue> dialogueProvider;
        public Func<string> markerProvider;

        Vector3 home;
        Vector3 wanderTarget;
        float waitUntil;

        public string Marker { get { return markerProvider != null ? markerProvider() : null; } }
        bool Talking { get { return DialogueManager.Instance != null && DialogueManager.Instance.IsOpen && DialogueManager.Instance.Speaker == transform; } }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Start()
        {
            home = transform.position;
            wanderTarget = home;
        }

        void Update()
        {
            if (Talking)
            {
                // Se tourne vers le joueur pendant la conversation.
                var p = PlayerController.Instance;
                if (p != null) Face(p.transform.position, 8f);
                return;
            }

            if (wanderRadius <= 0f) return;
            if (Time.time < waitUntil) return;

            var to = wanderTarget - transform.position;
            to.y = 0;
            if (to.magnitude < 0.2f)
            {
                var r = UnityEngine.Random.insideUnitCircle * wanderRadius;
                wanderTarget = home + new Vector3(r.x, 0, r.y);
                waitUntil = Time.time + UnityEngine.Random.Range(2f, 5f);
                return;
            }
            Face(wanderTarget, 5f);
            transform.position += to.normalized * wanderSpeed * Time.deltaTime;
        }

        void Face(Vector3 point, float speed)
        {
            var d = point - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), speed * Time.deltaTime);
        }

        public void Interact()
        {
            if (dialogueProvider == null) return;
            var d = dialogueProvider();
            if (d != null) DialogueManager.Instance.Open(d, transform);
        }

        /// <summary>PNJ le plus proche à portée d'interaction du point donné (ou null).</summary>
        public static NPC Closest(Vector3 position)
        {
            NPC best = null;
            float bestDist = float.MaxValue;
            foreach (var n in All)
            {
                var d = n.transform.position - position;
                d.y = 0;
                float m = d.magnitude;
                if (m <= n.interactRange && m < bestDist)
                {
                    best = n;
                    bestDist = m;
                }
            }
            return best;
        }
    }
}
