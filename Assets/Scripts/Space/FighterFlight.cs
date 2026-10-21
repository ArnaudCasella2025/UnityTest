using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Pilotage arcade du chasseur dans l'espace : il avance toujours, on oriente le nez,
    /// Maj pour le turbo, clic gauche (ou J) pour tirer avec les canons des ailes.
    /// </summary>
    public class FighterFlight : MonoBehaviour
    {
        public float cruiseSpeed = 28f;
        public float boostSpeed = 60f;
        public float pitchSpeed = 70f;
        public float yawSpeed = 60f;
        public float fireInterval = 0.12f;
        public Transform[] muzzles;
        public Transform model;
        public Color laserColor = new Color(0.3f, 1f, 0.45f);

        /// <summary>Tant que faux, le chasseur vole tout droit (sortie du hangar, fin de mission).</summary>
        public bool controlsEnabled;

        public float Speed { get; private set; }

        float bank;
        float nextShot;
        int muzzleIndex;

        void Start() { Speed = cruiseSpeed; }

        void Update()
        {
            float target = cruiseSpeed;
            if (controlsEnabled)
            {
                var m = GameInput.Move();
                // Z/W : monter, S : descendre, Q/A et D : virer.
                transform.Rotate(-m.y * pitchSpeed * Time.deltaTime, 0f, 0f, Space.Self);
                transform.Rotate(0f, m.x * yawSpeed * Time.deltaTime, 0f, Space.World);
                bank = Mathf.Lerp(bank, -m.x * 35f, 4f * Time.deltaTime);

                if (GameInput.Held(KeyCode.LeftShift)) target = boostSpeed;
                if ((GameInput.MouseHeld(0) || GameInput.Held(KeyCode.J)) && Time.time >= nextShot) Fire();
            }
            else bank = Mathf.Lerp(bank, 0f, 3f * Time.deltaTime);

            // Le roulis visuel ne touche que le modèle, pas la direction de vol.
            if (model != null) model.localRotation = Quaternion.Euler(0, 0, bank);

            Speed = Mathf.MoveTowards(Speed, target, 40f * Time.deltaTime);
            transform.position += transform.forward * Speed * Time.deltaTime;

            CheckAsteroids();
        }

        void Fire()
        {
            nextShot = Time.time + fireInterval;
            if (muzzles == null || muzzles.Length == 0) return;
            var m = muzzles[muzzleIndex++ % muzzles.Length];
            LaserBolt.Fire(m.position, transform.forward, laserColor, 12f, transform, 160f + Speed, 2.2f);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.05f, 0.04f);
        }

        void CheckAsteroids()
        {
            foreach (var a in Asteroid.All)
            {
                var d = transform.position - a.transform.position;
                float minDist = a.Radius + 2.2f;
                if (d.sqrMagnitude < minDist * minDist)
                {
                    // Rebond : on repousse le chasseur hors de l'astéroïde.
                    transform.position = a.transform.position + d.normalized * minDist;
                    Speed *= 0.4f;
                    if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.35f, 0.35f);
                    if (SpaceGame.Instance != null) SpaceGame.Instance.Radio("Ordinateur de bord", "Collision ! Coque endommagée... légèrement.");
                    break;
                }
            }
        }
    }
}
