using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Pistolaser du joueur : clic gauche (ou J) maintenu pour tirer dans la direction de la caméra.
    /// Une légère aide à la visée oriente le tir vers la cible la plus proche du centre.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerShooter : MonoBehaviour
    {
        public Transform weaponPivot;
        public Transform muzzle;
        public float fireInterval = 0.22f;
        public float damage = 10f;
        public float aimAssistAngle = 12f;
        public float aimAssistRange = 30f;
        public Color laserColor = new Color(1f, 0.25f, 0.2f);

        PlayerController player;
        float nextShot;
        Vector3 pivotRest;

        void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        void Start()
        {
            if (weaponPivot != null) pivotRest = weaponPivot.localPosition;
        }

        void Update()
        {
            if (weaponPivot != null)
                weaponPivot.localPosition = Vector3.Lerp(weaponPivot.localPosition, pivotRest, 15f * Time.deltaTime);

            if (player.InputLocked) return;
            if ((GameInput.MouseHeld(0) || GameInput.Held(KeyCode.J)) && Time.time >= nextShot)
            {
                nextShot = Time.time + fireInterval;
                Shoot();
            }
        }

        void Shoot()
        {
            var cam = Camera.main;
            var aim = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : transform.forward;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;

            // Aide à la visée : la cible la plus proche de l'axe de tir, dans un petit cône.
            Vector3? targetPoint = null;
            float bestAngle = aimAssistAngle;
            foreach (var h in FindTargets())
            {
                var to = h.transform.position + Vector3.up * 1.2f - transform.position;
                if (to.magnitude > aimAssistRange) continue;
                var flat = Vector3.ProjectOnPlane(to, Vector3.up);
                float angle = Vector3.Angle(aim, flat);
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    targetPoint = h.transform.position + Vector3.up * 1.2f;
                }
            }

            // Le joueur se tourne dans la direction du tir.
            var faceDir = targetPoint.HasValue ? Vector3.ProjectOnPlane(targetPoint.Value - transform.position, Vector3.up) : aim;
            if (faceDir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(faceDir);
            player.AimUntil = Time.time + 0.4f;

            var origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.2f + transform.forward * 0.8f;
            var dir = targetPoint.HasValue ? targetPoint.Value - origin : transform.forward;
            LaserBolt.Fire(origin, dir, laserColor, damage, transform);

            if (weaponPivot != null) weaponPivot.localPosition = pivotRest + new Vector3(0, 0.03f, -0.12f);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.06f, 0.03f);
        }

        static ShootingTarget[] cachedTargets;
        static float cacheTime = -10f;

        static ShootingTarget[] FindTargets()
        {
            if (Time.time - cacheTime > 1f || cachedTargets == null)
            {
                cachedTargets = Object.FindObjectsByType<ShootingTarget>(FindObjectsSortMode.None);
                cacheTime = Time.time;
            }
            return cachedTargets;
        }
    }
}
