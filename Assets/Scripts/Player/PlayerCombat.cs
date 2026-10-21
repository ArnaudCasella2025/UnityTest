using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Attaque à l'épée (clic gauche / J), combo de 3 coups, potions (H / 1).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        public Transform weaponPivot;
        public float range = 2.2f;
        public float arcDegrees = 110f;
        public float[] comboDamage = { 18f, 22f, 34f };
        public float attackDuration = 0.32f;
        public float comboWindow = 0.45f;
        public int potions = 3;
        public float potionHeal = 45f;

        PlayerController player;
        bool attacking;
        int comboIndex;
        float lastAttackEnd;

        void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        void Update()
        {
            if (player.InputLocked) return;

            if ((GameInput.MouseDown(0) || GameInput.Down(KeyCode.J)) && !attacking && !player.IsDodging)
            {
                if (Time.time - lastAttackEnd > comboWindow) comboIndex = 0;
                StartCoroutine(Attack(comboIndex));
                comboIndex = (comboIndex + 1) % comboDamage.Length;
            }

            if (GameInput.Down(KeyCode.H) || GameInput.Down(KeyCode.Alpha1))
                DrinkPotion();
        }

        void DrinkPotion()
        {
            if (potions <= 0)
            {
                HUD.Instance.ShowMessage("Plus de potion !", 1.5f);
                return;
            }
            if (player.Health.Current >= player.Health.max)
            {
                HUD.Instance.ShowMessage("Vous êtes déjà en pleine forme.", 1.5f);
                return;
            }
            potions--;
            player.Health.Heal(potionHeal);
        }

        IEnumerator Attack(int index)
        {
            attacking = true;
            player.SpeedMultiplier = 0.35f;

            // Le 3e coup est un coup vertical plus lourd.
            bool heavy = index == comboDamage.Length - 1;
            Quaternion from = heavy ? Quaternion.Euler(-80f, 0, 0) : Quaternion.Euler(0, index == 0 ? -80f : 80f, 0);
            Quaternion to = heavy ? Quaternion.Euler(70f, 0, 0) : Quaternion.Euler(0, index == 0 ? 80f : -80f, 0);
            Quaternion rest = Quaternion.Euler(20f, 0, 0);

            bool hitDone = false;
            float t = 0f;
            float duration = heavy ? attackDuration * 1.3f : attackDuration;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                if (weaponPivot != null)
                    weaponPivot.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0, 1, k));
                if (!hitDone && k >= 0.45f)
                {
                    hitDone = true;
                    DoHit(comboDamage[index], heavy);
                }
                yield return null;
            }

            if (weaponPivot != null) weaponPivot.localRotation = rest;
            player.SpeedMultiplier = 1f;
            attacking = false;
            lastAttackEnd = Time.time;
        }

        void DoHit(float baseDamage, bool heavy)
        {
            var center = transform.position + Vector3.up * 1f + transform.forward * (range * 0.5f);
            var hits = Physics.OverlapSphere(center, range * 0.75f, ~0, QueryTriggerInteraction.Ignore);
            var already = new HashSet<Health>();
            bool any = false;

            foreach (var col in hits)
            {
                var h = col.GetComponentInParent<Health>();
                if (h == null || h == player.Health || h.IsDead || already.Contains(h)) continue;
                // Seuls les ennemis sont frappables (pas les villageois).
                if (h.GetComponent<EnemyAI>() == null) continue;

                var to = h.transform.position - transform.position;
                to.y = 0;
                if (to.magnitude > range + 0.6f) continue;
                if (Vector3.Angle(transform.forward, to) > arcDegrees * 0.5f) continue;

                already.Add(h);
                bool crit = Random.value < 0.15f;
                float dmg = baseDamage * Random.Range(0.9f, 1.1f) * (crit ? 1.8f : 1f);
                h.Damage(dmg, transform.position);
                any = true;
            }

            if (any && CameraShake.Instance != null)
                CameraShake.Instance.Shake(heavy ? 0.2f : 0.1f, heavy ? 0.15f : 0.07f);
        }
    }
}
