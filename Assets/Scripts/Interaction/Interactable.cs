using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Tout ce avec quoi le joueur peut interagir avec la touche Action (E) :
    /// PNJ, portes, chasseurs...
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public float interactRange = 2.8f;

        /// <summary>Texte affiché à l'écran quand on est à portée (sans la touche).</summary>
        public abstract string Prompt { get; }

        /// <summary>Point utilisé pour mesurer la distance (par défaut la position de l'objet).</summary>
        public virtual Vector3 InteractionPoint { get { return transform.position; } }

        public virtual bool CanInteract { get { return true; } }

        public abstract void Interact();

        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        /// <summary>Élément interactif le plus proche, à portée du point donné (ou null).</summary>
        public static Interactable Closest(Vector3 position)
        {
            Interactable best = null;
            float bestDist = float.MaxValue;
            foreach (var it in All)
            {
                if (!it.CanInteract) continue;
                var d = it.InteractionPoint - position;
                d.y = 0;
                float m = d.magnitude;
                if (m <= it.interactRange && m < bestDist)
                {
                    best = it;
                    bestDist = m;
                }
            }
            return best;
        }
    }
}
