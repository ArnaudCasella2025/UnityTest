using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Déplacements du joueur relatifs à la caméra : marche, course, saut, esquive.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public float walkSpeed = 5f;
        public float runSpeed = 8.5f;
        public float jumpHeight = 1.3f;
        public float gravity = -22f;
        public float turnSpeed = 14f;
        public float dodgeSpeed = 16f;
        public float dodgeDuration = 0.22f;
        public float dodgeCooldown = 0.8f;

        public Health Health { get; private set; }
        public Transform Visual { get; set; }
        public Vector3 SpawnPoint { get; set; }
        public int Gold { get; set; }
        public bool IsDodging { get { return Time.time < dodgeEnd; } }

        /// <summary>Multiplicateur de vitesse appliqué par le combat (ralenti pendant un coup).</summary>
        public float SpeedMultiplier { get; set; }

        CharacterController cc;
        float verticalVelocity;
        Vector3 knockback;
        float dodgeEnd = -1f, nextDodge;
        Vector3 dodgeDir;

        public bool InputLocked
        {
            get
            {
                return Health.IsDead || (DialogueManager.Instance != null && DialogueManager.Instance.BlocksInput);
            }
        }

        void Awake()
        {
            Instance = this;
            cc = GetComponent<CharacterController>();
            Health = GetComponent<Health>();
            SpeedMultiplier = 1f;
            Health.Damaged += OnDamaged;
        }

        void OnDamaged(float amount, Vector3 from)
        {
            var dir = transform.position - from;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f) knockback = dir.normalized * 7f;
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.15f, 0.12f);
        }

        void Update()
        {
            Vector3 move = Vector3.zero;
            bool running = false;

            if (!InputLocked)
            {
                var m = GameInput.Move(); // ZQSD (AZERTY), WASD (QWERTY) ou flèches
                var input = Vector3.ClampMagnitude(new Vector3(m.x, 0, m.y), 1f);

                // Direction relative à la caméra, projetée au sol.
                var cam = Camera.main != null ? Camera.main.transform : transform;
                var fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                var right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
                move = fwd * input.z + right * input.x;

                running = GameInput.Held(KeyCode.LeftShift);

                if (cc.isGrounded && GameInput.Down(KeyCode.Space))
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

                if ((GameInput.Down(KeyCode.LeftControl) || GameInput.Down(KeyCode.C)) && Time.time >= nextDodge)
                {
                    dodgeDir = move.sqrMagnitude > 0.01f ? move.normalized : transform.forward;
                    dodgeEnd = Time.time + dodgeDuration;
                    nextDodge = Time.time + dodgeCooldown;
                    Health.invulnerableUntil = dodgeEnd + 0.05f;
                }
            }

            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 horizontal;
            if (IsDodging)
            {
                horizontal = dodgeDir * dodgeSpeed;
            }
            else
            {
                float speed = (running ? runSpeed : walkSpeed) * SpeedMultiplier;
                horizontal = move * speed;
                if (move.sqrMagnitude > 0.01f)
                {
                    var target = Quaternion.LookRotation(move);
                    transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
                }
            }

            knockback = Vector3.Lerp(knockback, Vector3.zero, 8f * Time.deltaTime);
            cc.Move((horizontal + knockback + Vector3.up * verticalVelocity) * Time.deltaTime);

            AnimateVisual(move.magnitude, running);

            // Sécurité : si on tombe hors du monde, retour au point de départ.
            if (transform.position.y < -20f) Teleport(SpawnPoint);
        }

        float bobTime;
        void AnimateVisual(float moveAmount, bool running)
        {
            if (Visual == null) return;

            // Mort : le personnage bascule en arrière ; il se redresse quand il est relevé.
            var targetRot = Health.IsDead ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity;
            Visual.localRotation = Quaternion.Slerp(Visual.localRotation, targetRot, 8f * Time.deltaTime);

            float targetScale = 1f;
            if (IsDodging)
            {
                // Écrasement pendant l'esquive façon « roulade ».
                Visual.localScale = Vector3.Lerp(Visual.localScale, new Vector3(1.2f, 0.6f, 1.2f), 30f * Time.deltaTime);
                return;
            }
            if (moveAmount > 0.1f && cc.isGrounded)
            {
                bobTime += Time.deltaTime * (running ? 16f : 11f);
                targetScale = 1f + Mathf.Sin(bobTime) * 0.04f;
            }
            var s = new Vector3(1f / Mathf.Sqrt(targetScale), targetScale, 1f / Mathf.Sqrt(targetScale));
            Visual.localScale = Vector3.Lerp(Visual.localScale, s, 20f * Time.deltaTime);
        }

        public void Teleport(Vector3 position)
        {
            cc.enabled = false;
            transform.position = position;
            cc.enabled = true;
            verticalVelocity = 0f;
            knockback = Vector3.zero;
        }

        public void FaceTowards(Vector3 point)
        {
            var d = point - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d);
        }
    }
}
