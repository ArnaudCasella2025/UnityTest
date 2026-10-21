using System;
using System.Collections;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>Points de vie, dégâts, soins, flash blanc à l'impact.</summary>
    public class Health : MonoBehaviour
    {
        public string displayName = "?";
        public float max = 100f;
        public float Current { get; private set; }
        public bool IsDead { get { return Current <= 0f; } }
        public float Ratio { get { return Mathf.Clamp01(Current / max); } }

        /// <summary>Tant que &gt; Time.time, les dégâts sont ignorés (esquive).</summary>
        public float invulnerableUntil;

        /// <summary>Dernière fois qu'il a pris des coups (pour l'affichage des barres de vie).</summary>
        public float LastHitTime { get; private set; }

        public event Action<float, Vector3> Damaged;   // montant, position de l'attaquant
        public event Action<Health> Died;

        Renderer[] renderers;
        Material[] originalMats;
        Coroutine flashRoutine;

        void Awake()
        {
            Current = max;
            LastHitTime = -999f;
        }

        public void Init(string name, float maxHp)
        {
            displayName = name;
            max = maxHp;
            Current = maxHp;
        }

        public void Damage(float amount, Vector3 fromPosition)
        {
            if (IsDead || Time.time < invulnerableUntil) return;

            Current = Mathf.Max(0f, Current - amount);
            LastHitTime = Time.time;

            if (HUD.Instance != null)
                HUD.Instance.SpawnPopup(transform.position + Vector3.up * 2.3f, Mathf.RoundToInt(amount).ToString(),
                    GetComponent<PlayerController>() != null ? new Color(1f, 0.35f, 0.3f) : Color.white);

            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(Flash());

            if (Damaged != null) Damaged(amount, fromPosition);
            if (IsDead && Died != null) Died(this);
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(max, Current + amount);
            if (HUD.Instance != null)
                HUD.Instance.SpawnPopup(transform.position + Vector3.up * 2.3f, "+" + Mathf.RoundToInt(amount), new Color(0.4f, 1f, 0.4f));
        }

        public void Revive()
        {
            Current = max;
        }

        IEnumerator Flash()
        {
            if (renderers == null)
            {
                renderers = GetComponentsInChildren<Renderer>();
                originalMats = new Material[renderers.Length];
                for (int i = 0; i < renderers.Length; i++) originalMats[i] = renderers[i].sharedMaterial;
            }

            var white = Prim.Mat(Color.white);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].sharedMaterial = white;

            yield return new WaitForSeconds(0.08f);

            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].sharedMaterial = originalMats[i];
            flashRoutine = null;
        }
    }
}
