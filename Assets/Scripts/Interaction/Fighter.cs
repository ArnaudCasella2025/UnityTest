using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>Fabrique un chasseur spatial à partir de primitives (avant du vaisseau = +Z local).</summary>
    public static class FighterFactory
    {
        public class Parts
        {
            public GameObject root;
            public Transform canopyPivot;
            public List<Transform> gear = new List<Transform>();
            public List<Transform> engineGlows = new List<Transform>();
            public List<Transform> muzzles = new List<Transform>();
        }

        public static Parts Build(string name, Transform parent, Vector3 position, float yaw, Color stripe)
        {
            var p = new Parts();
            p.root = new GameObject(name);
            p.root.transform.SetParent(parent, false);
            p.root.transform.position = position;
            p.root.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var t = p.root.transform;

            var hull = new Color(0.86f, 0.88f, 0.9f);
            var dark = new Color(0.25f, 0.27f, 0.3f);
            const float h = 1.7f; // hauteur du fuselage au sol

            // Fuselage, nez et bandes de couleur.
            Prim.Make(PrimitiveType.Capsule, "Fuselage", t, new Vector3(0, h, 0), new Vector3(1.3f, 2.8f, 1.3f), hull, false, new Vector3(90, 0, 0));
            Prim.Make(PrimitiveType.Cube, "Stripe", t, new Vector3(0, h + 0.62f, -0.6f), new Vector3(0.35f, 0.06f, 3f), stripe, false);
            Prim.Make(PrimitiveType.Sphere, "NoseCone", t, new Vector3(0, h, 2.75f), new Vector3(0.55f, 0.55f, 0.6f), stripe, false);

            // Verrière : pivot à l'arrière, elle bascule vers le haut quand elle est ouverte.
            p.canopyPivot = new GameObject("CanopyPivot").transform;
            p.canopyPivot.SetParent(t, false);
            p.canopyPivot.localPosition = new Vector3(0, h + 0.45f, 0.1f);
            p.canopyPivot.localRotation = Quaternion.Euler(-40f, 0, 0);
            Prim.Make(PrimitiveType.Sphere, "Canopy", p.canopyPivot, new Vector3(0, 0.05f, 0.85f), new Vector3(0.85f, 0.55f, 1.7f), new Color(0.15f, 0.45f, 0.65f), false);
            Prim.Make(PrimitiveType.Cube, "Seat", t, new Vector3(0, h + 0.35f, 0.7f), new Vector3(0.5f, 0.5f, 0.4f), dark, false);

            // Ailes en flèche, saumons colorés, canons.
            for (int side = -1; side <= 1; side += 2)
            {
                Prim.Make(PrimitiveType.Cube, "Wing", t, new Vector3(side * 2.0f, h - 0.1f, -0.5f), new Vector3(3.2f, 0.12f, 1.8f), hull, false, new Vector3(0, side * -18f, 0));
                Prim.Make(PrimitiveType.Cube, "WingTip", t, new Vector3(side * 3.45f, h - 0.1f, -0.95f), new Vector3(0.5f, 0.14f, 1.3f), stripe, false, new Vector3(0, side * -18f, 0));
                Prim.Make(PrimitiveType.Cylinder, "Cannon", t, new Vector3(side * 3.35f, h - 0.1f, 0.2f), new Vector3(0.14f, 0.8f, 0.14f), dark, false, new Vector3(90, 0, 0));
                var muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(t, false);
                muzzle.localPosition = new Vector3(side * 3.35f, h - 0.1f, 1.1f);
                p.muzzles.Add(muzzle);

                // Réacteurs et leur lueur.
                Prim.Make(PrimitiveType.Cylinder, "Engine", t, new Vector3(side * 0.75f, h - 0.05f, -2.4f), new Vector3(0.65f, 0.55f, 0.65f), dark, false, new Vector3(90, 0, 0));
                var glow = Prim.Make(PrimitiveType.Sphere, "EngineGlow", t, new Vector3(side * 0.75f, h - 0.05f, -2.95f), new Vector3(0.5f, 0.5f, 0.1f), new Color(1f, 0.55f, 0.15f), false, null, true);
                p.engineGlows.Add(glow.transform);

                // Train d'atterrissage arrière.
                var leg = Prim.Make(PrimitiveType.Cylinder, "Gear", t, new Vector3(side * 1.3f, 0.55f, -1f), new Vector3(0.16f, 0.55f, 0.16f), dark, false);
                p.gear.Add(leg.transform);
            }
            var front = Prim.Make(PrimitiveType.Cylinder, "Gear", t, new Vector3(0, 0.55f, 1.6f), new Vector3(0.16f, 0.55f, 0.16f), dark, false);
            p.gear.Add(front.transform);

            // Dérive.
            Prim.Make(PrimitiveType.Cube, "TailFin", t, new Vector3(0, h + 0.85f, -2.0f), new Vector3(0.12f, 1.2f, 1.3f), hull, false, new Vector3(-25, 0, 0));
            Prim.Make(PrimitiveType.Cube, "TailStripe", t, new Vector3(0, h + 1.25f, -2.25f), new Vector3(0.14f, 0.3f, 0.8f), stripe, false, new Vector3(-25, 0, 0));

            return p;
        }
    }

    /// <summary>
    /// Chasseur garé dans le hangar. Touche Action à côté : le joueur monte à bord, la verrière se ferme,
    /// le chasseur décolle, sort du hangar et la scène de l'espace est chargée.
    /// </summary>
    public class Fighter : Interactable
    {
        public Transform canopyPivot;
        public Transform[] gear;
        public Transform[] engineGlows;
        public float launchSpeed = 55f;

        bool launching;

        public override string Prompt
        {
            get { return Allowed ? "Monter dans le chasseur" : "Chasseur (ordre de mission requis)"; }
        }

        public override bool CanInteract { get { return !launching; } }

        bool Allowed { get { return QuestManager.Instance == null || QuestManager.Instance.State == QuestState.GoToHangar; } }

        public override void Interact()
        {
            if (!Allowed)
            {
                HUD.Instance.ShowMessage("Il vous faut l'ordre de mission du commandant.", 2.5f);
                return;
            }
            StartCoroutine(Launch());
        }

        IEnumerator Launch()
        {
            launching = true;
            if (QuestManager.Instance != null) QuestManager.Instance.OnLaunch();

            // Le joueur disparaît dans le cockpit ; la caméra suit désormais le chasseur.
            var player = PlayerController.Instance;
            if (player != null) player.Hide();
            var cam = Camera.main != null ? Camera.main.GetComponent<ThirdPersonCamera>() : null;
            if (cam != null)
            {
                cam.target = transform;
                cam.distance = 13f;
                cam.pitch = 16f;
            }

            yield return new WaitForSeconds(0.3f);

            // Fermeture de la verrière.
            var open = canopyPivot != null ? canopyPivot.localRotation : Quaternion.identity;
            yield return Animate(0.6f, k =>
            {
                if (canopyPivot != null) canopyPivot.localRotation = Quaternion.Slerp(open, Quaternion.identity, k);
                FollowYaw(cam, 4f);
            });

            // Allumage des réacteurs.
            var glowScales = new Vector3[engineGlows != null ? engineGlows.Length : 0];
            for (int i = 0; i < glowScales.Length; i++) glowScales[i] = engineGlows[i].localScale;
            HUD.Instance.ShowMessage("Allumage des réacteurs...", 1.5f);
            yield return Animate(1f, k =>
            {
                for (int i = 0; i < glowScales.Length; i++)
                    engineGlows[i].localScale = Vector3.Scale(glowScales[i], new Vector3(1f + 0.4f * k, 1f + 0.4f * k, 1f + 14f * k));
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.1f, 0.03f + 0.04f * k);
                FollowYaw(cam, 4f);
            });

            // Décollage vertical, rentrée du train.
            var start = transform.position;
            var gearScales = new Vector3[gear != null ? gear.Length : 0];
            for (int i = 0; i < gearScales.Length; i++) gearScales[i] = gear[i].localScale;
            yield return Animate(1.6f, k =>
            {
                float e = Mathf.SmoothStep(0, 1, k);
                transform.position = start + Vector3.up * 2.6f * e;
                transform.rotation = Quaternion.Euler(-4f * Mathf.Sin(k * Mathf.PI), transform.eulerAngles.y, 0);
                for (int i = 0; i < gearScales.Length; i++)
                    gear[i].localScale = new Vector3(gearScales[i].x, gearScales[i].y * (1f - e), gearScales[i].z);
                FollowYaw(cam, 4f);
            });

            // Accélération vers la sortie du hangar (droit devant), puis fondu au noir.
            HUD.Instance.ShowMessage("Décollage !", 1.5f);
            float speed = 0f;
            float t = 0f;
            const float flightTime = 3.2f;
            bool fading = false;
            while (t < flightTime)
            {
                t += Time.deltaTime;
                speed = Mathf.MoveTowards(speed, launchSpeed, launchSpeed * 0.6f * Time.deltaTime);
                transform.position += transform.forward * speed * Time.deltaTime;
                FollowYaw(cam, 6f);
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.1f, 0.05f);
                if (!fading && t > flightTime - 1f)
                {
                    fading = true;
                    HUD.FadeTo(1f, 1f);
                }
                yield return null;
            }
            HUD.FadeAlpha = 1f;
            GameBoot.LoadSpace();
        }

        void FollowYaw(ThirdPersonCamera cam, float sharpness)
        {
            if (cam == null) return;
            cam.yaw = Mathf.LerpAngle(cam.yaw, transform.eulerAngles.y, 1f - Mathf.Exp(-sharpness * Time.deltaTime));
        }

        static IEnumerator Animate(float duration, System.Action<float> step)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                step(Mathf.Clamp01(t / duration));
                yield return null;
            }
        }
    }
}
