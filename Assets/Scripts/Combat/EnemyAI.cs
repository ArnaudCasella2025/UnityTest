using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// IA d'ennemi simple : patrouille autour de son camp, poursuit le joueur quand il approche,
    /// prépare son coup (télégraphié : arme levée + gonflement) puis frappe, revient s'il est trop loin.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class EnemyAI : MonoBehaviour
    {
        public static readonly List<EnemyAI> All = new List<EnemyAI>();

        public float patrolSpeed = 2f;
        public float chaseSpeed = 4.6f;
        public float aggroRange = 10f;
        public float attackRange = 1.9f;
        public float leashRange = 26f;
        public float patrolRadius = 5f;
        public float damage = 10f;
        public float windup = 0.5f;
        public float recovery = 0.7f;
        public float alertRadius = 14f;

        public Transform weaponPivot;
        public Transform visual;

        enum State { Patrol, Chase, Attack, Return, Dead }
        State state = State.Patrol;

        CharacterController cc;
        Health health;
        Vector3 home;
        Quaternion homeRotation;
        Vector3 patrolTarget;
        float waitUntil;
        float verticalVelocity;
        Vector3 knockback;
        float stunUntil;
        Vector3 baseVisualScale = Vector3.one;

        public bool IsDead { get { return state == State.Dead; } }

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void Start()
        {
            home = transform.position;
            homeRotation = transform.rotation;
            patrolTarget = home;
            if (visual != null) baseVisualScale = visual.localScale;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        Transform Player { get { return PlayerController.Instance != null ? PlayerController.Instance.transform : null; } }
        bool PlayerAlive { get { return PlayerController.Instance != null && !PlayerController.Instance.Health.IsDead; } }

        void Update()
        {
            if (state == State.Dead) return;

            Vector3 desired = Vector3.zero;
            float distToPlayer = Player != null ? Flat(Player.position - transform.position).magnitude : 999f;
            float distFromHome = Flat(transform.position - home).magnitude;

            switch (state)
            {
                case State.Patrol:
                    if (PlayerAlive && distToPlayer < aggroRange) { Alert(); break; }
                    if (Time.time >= waitUntil)
                    {
                        var to = Flat(patrolTarget - transform.position);
                        if (to.magnitude < 0.4f)
                        {
                            var r = Random.insideUnitCircle * patrolRadius;
                            patrolTarget = home + new Vector3(r.x, 0, r.y);
                            waitUntil = Time.time + Random.Range(1f, 3f);
                        }
                        else desired = to.normalized * patrolSpeed;
                    }
                    break;

                case State.Chase:
                    if (!PlayerAlive || distFromHome > leashRange) { state = State.Return; break; }
                    if (distToPlayer <= attackRange) { StartCoroutine(AttackRoutine()); break; }
                    desired = Flat(Player.position - transform.position).normalized * chaseSpeed;
                    // Évite que tous les ennemis s'empilent au même endroit.
                    desired += Separation() * 2f;
                    break;

                case State.Attack:
                    // Géré par la coroutine ; on reste face au joueur pendant la préparation.
                    break;

                case State.Return:
                    if (PlayerAlive && distToPlayer < aggroRange * 0.6f && distFromHome < leashRange * 0.8f) { state = State.Chase; break; }
                    var back = Flat(home - transform.position);
                    if (back.magnitude < 0.5f)
                    {
                        state = State.Patrol;
                        health.Revive(); // récupère ses PV en rentrant au camp
                    }
                    else desired = back.normalized * chaseSpeed;
                    break;
            }

            if (Time.time < stunUntil) desired = Vector3.zero;

            if (desired.sqrMagnitude > 0.01f && state != State.Attack)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Flat(desired)), 10f * Time.deltaTime);
            else if (state == State.Attack && Player != null)
                FaceSmooth(Player.position, 6f);

            if (cc.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
            verticalVelocity += -22f * Time.deltaTime;
            knockback = Vector3.Lerp(knockback, Vector3.zero, 7f * Time.deltaTime);
            cc.Move((desired + knockback + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        /// <summary>Passe en poursuite et prévient les camarades proches.</summary>
        public void Alert()
        {
            if (state == State.Dead) return;
            if (state == State.Patrol || state == State.Return) state = State.Chase;
            foreach (var e in All)
            {
                if (e == this || e.state != State.Patrol) continue;
                if (Flat(e.transform.position - transform.position).magnitude < alertRadius) e.state = State.Chase;
            }
        }

        IEnumerator AttackRoutine()
        {
            state = State.Attack;

            // Télégraphe : l'arme se lève et le corps gonfle légèrement.
            float t = 0f;
            var raised = Quaternion.Euler(-100f, 0, 0);
            var rest = Quaternion.Euler(20f, 0, 0);
            while (t < windup)
            {
                if (state == State.Dead) yield break;
                t += Time.deltaTime;
                float k = t / windup;
                if (weaponPivot != null) weaponPivot.localRotation = Quaternion.Slerp(rest, raised, k);
                if (visual != null) visual.localScale = baseVisualScale * (1f + 0.12f * k);
                yield return null;
            }

            // Frappe.
            t = 0f;
            bool hit = false;
            var down = Quaternion.Euler(80f, 0, 0);
            while (t < 0.12f)
            {
                if (state == State.Dead) yield break;
                t += Time.deltaTime;
                if (weaponPivot != null) weaponPivot.localRotation = Quaternion.Slerp(raised, down, t / 0.12f);
                if (!hit && t > 0.06f)
                {
                    hit = true;
                    TryHitPlayer();
                }
                yield return null;
            }
            if (visual != null) visual.localScale = baseVisualScale;

            // Récupération : fenêtre où le joueur peut contre-attaquer.
            t = 0f;
            while (t < recovery)
            {
                if (state == State.Dead) yield break;
                t += Time.deltaTime;
                if (weaponPivot != null) weaponPivot.localRotation = Quaternion.Slerp(down, rest, t / recovery);
                yield return null;
            }

            if (state == State.Attack) state = State.Chase;
        }

        void TryHitPlayer()
        {
            if (!PlayerAlive) return;
            var to = Flat(Player.position - transform.position);
            if (to.magnitude > attackRange + 0.5f) return;
            if (Vector3.Angle(transform.forward, to) > 60f) return;
            PlayerController.Instance.Health.Damage(damage * Random.Range(0.85f, 1.15f), transform.position);
        }

        void OnDamaged(float amount, Vector3 from)
        {
            var dir = Flat(transform.position - from);
            if (dir.sqrMagnitude > 0.001f) knockback = dir.normalized * 6f;
            stunUntil = Time.time + 0.15f;
            Alert();
        }

        void OnDied(Health h)
        {
            state = State.Dead;
            StopAllCoroutines();
            if (visual != null) visual.localScale = baseVisualScale;
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            cc.enabled = false;
            var start = transform.rotation;
            var end = start * Quaternion.Euler(-90f, 0, 0); // tombe en arrière
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(start, end, Mathf.SmoothStep(0, 1, t / 0.5f));
                yield return null;
            }
            yield return new WaitForSeconds(2f);
            t = 0f;
            var p = transform.position;
            while (t < 1.5f)
            {
                t += Time.deltaTime;
                transform.position = p + Vector3.down * t;
                yield return null;
            }
            Destroy(gameObject);
        }

        /// <summary>Remet l'ennemi à son point de départ avec tous ses PV (après la mort du joueur).</summary>
        public void ResetToHome()
        {
            if (state == State.Dead) return;
            StopAllCoroutines();
            cc.enabled = false;
            transform.position = home;
            transform.rotation = homeRotation;
            cc.enabled = true;
            if (visual != null) visual.localScale = baseVisualScale;
            if (weaponPivot != null) weaponPivot.localRotation = Quaternion.Euler(20f, 0, 0);
            health.Revive();
            state = State.Patrol;
            patrolTarget = home;
        }

        Vector3 Separation()
        {
            Vector3 push = Vector3.zero;
            foreach (var e in All)
            {
                if (e == this || e.state == State.Dead) continue;
                var d = Flat(transform.position - e.transform.position);
                float m = d.magnitude;
                if (m > 0.01f && m < 1.6f) push += d / m * (1.6f - m);
            }
            return push;
        }

        void FaceSmooth(Vector3 point, float speed)
        {
            var d = Flat(point - transform.position);
            if (d.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), speed * Time.deltaTime);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }
}
