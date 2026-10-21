using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Déplacements du joueur relatifs à la caméra : marche, course, saut, esquive.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
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

        public Transform Visual { get; set; }
        public Vector3 SpawnPoint { get; set; }
        public bool IsDodging { get { return Time.time < dodgeEnd; } }

        /// <summary>Vrai quand le joueur est monté dans un chasseur : il ne bouge plus et n'est plus visible.</summary>
        public bool Hidden { get; private set; }

        /// <summary>Jusqu'à cet instant, le joueur garde l'orientation imposée par le tir (il peut se déplacer en crabe).</summary>
        public float AimUntil { get; set; }

        /// <summary>Multiplicateur de vitesse (ralenti pendant un tir).</summary>
        public float SpeedMultiplier { get; set; }

        CharacterController cc;
        float verticalVelocity;
        float dodgeEnd = -1f, nextDodge;
        Vector3 dodgeDir;

        public bool InputLocked
        {
            get
            {
                return Hidden || (DialogueManager.Instance != null && DialogueManager.Instance.BlocksInput);
            }
        }

        void Awake()
        {
            Instance = this;
            cc = GetComponent<CharacterController>();
            SpeedMultiplier = 1f;
        }

        /// <summary>Fait disparaître le joueur (il monte dans le chasseur).</summary>
        public void Hide()
        {
            Hidden = true;
            cc.enabled = false;
            if (Visual != null) Visual.gameObject.SetActive(false);
        }

        void Update()
        {
            if (Hidden) return;

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
                if (move.sqrMagnitude > 0.01f && Time.time >= AimUntil)
                {
                    var target = Quaternion.LookRotation(move);
                    transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
                }
            }

            cc.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);

            AnimateVisual(move.magnitude, running);

            // Sécurité : si on tombe hors du monde, retour au point de départ.
            if (transform.position.y < -20f) Teleport(SpawnPoint);
        }

        float bobTime;
        void AnimateVisual(float moveAmount, bool running)
        {
            if (Visual == null) return;

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
        }

        public void FaceTowards(Vector3 point)
        {
            var d = point - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d);
        }
    }
}
