using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Scène « Espace » : le chasseur sort du hangar de l'Aurore, traverse un champ d'astéroïdes
    /// et rejoint son escadron au point de rendez-vous. Tout est généré en code.
    /// </summary>
    public class SpaceGame : MonoBehaviour
    {
        public static SpaceGame Instance { get; private set; }

        static readonly Vector3 Rendezvous = new Vector3(-330f, 40f, 160f);

        FighterFlight fighter;
        Camera cam;
        Vector3 camVelocity;
        bool completed;
        float startTime;

        string radioSpeaker, radioText;
        float radioUntil;

        void Awake()
        {
            Instance = this;
            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 8);
            Asteroid.Destroyed = 0;

            SetupLighting();
            SetupCamera();
            BuildMothership();
            BuildStarsAndPlanets();
            BuildAsteroids();
            BuildSquadron();
            BuildFighter();

            startTime = Time.time;
            HUD.FadeAlpha = 1f;
            HUD.FadeTo(0f, 1.2f);
        }

        void SetupLighting()
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) l.enabled = false;
            var sun = new GameObject("Soleil").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.3f;
            sun.transform.rotation = Quaternion.Euler(25f, -60f, 0f);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.2f, 0.26f);
        }

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            var tpc = cam.GetComponent<ThirdPersonCamera>();
            if (tpc != null) tpc.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.005f, 0.02f);
            cam.farClipPlane = 2500f;
            cam.fieldOfView = 62f;
            if (cam.GetComponent<CameraShake>() == null) cam.gameObject.AddComponent<CameraShake>();
        }

        void BuildMothership()
        {
            // L'Aurore vue de l'extérieur : coque, passerelle, réacteurs et hangar ouvert à bâbord (ouest).
            var ship = new GameObject("Aurore").transform;
            var hull = Prim.Hex("#9AA3AD");
            var dark = Prim.Hex("#4A525C");
            Prim.Make(PrimitiveType.Cube, "Hull", ship, new Vector3(0, 0, 0), new Vector3(30f, 12f, 110f), hull, false);
            Prim.Make(PrimitiveType.Cube, "Spine", ship, new Vector3(0, 7.5f, -5f), new Vector3(12f, 4f, 80f), dark, false);
            Prim.Make(PrimitiveType.Cube, "Bridge", ship, new Vector3(0, 11f, 42f), new Vector3(16f, 5f, 14f), hull, false);
            Prim.Make(PrimitiveType.Cube, "BridgeWindow", ship, new Vector3(0, 11.5f, 49.05f), new Vector3(14f, 1.5f, 0.2f), Prim.Hex("#35D0FF"), false, null, true);
            Prim.Make(PrimitiveType.Sphere, "Prow", ship, new Vector3(0, 0, 55f), new Vector3(30f, 12f, 30f), hull, false);
            for (int i = -1; i <= 1; i++)
            {
                Prim.Make(PrimitiveType.Cylinder, "Engine", ship, new Vector3(i * 9f, 0, -58f), new Vector3(7f, 5f, 7f), dark, false, new Vector3(90, 0, 0));
                Prim.Make(PrimitiveType.Sphere, "EngineGlow", ship, new Vector3(i * 9f, 0, -63f), new Vector3(6f, 6f, 2f), Prim.Hex("#4FB8FF"), false, null, true);
            }
            // Ouverture du hangar et son champ de force.
            Prim.Make(PrimitiveType.Cube, "HangarField", ship, new Vector3(-15.1f, 0, 0), new Vector3(0.2f, 8f, 26f), Prim.Hex("#2A7FFF"), false, null, true);
            Prim.Make(PrimitiveType.Cube, "HangarFrame", ship, new Vector3(-15.2f, 4.3f, 0), new Vector3(0.3f, 0.6f, 27f), Prim.Hex("#35D0FF"), false, null, true);
            // Feux de position.
            for (int z = -50; z <= 50; z += 20)
            {
                Prim.Make(PrimitiveType.Sphere, "NavLight", ship, new Vector3(-15.2f, 6f, z), Vector3.one * 0.8f, Prim.Hex("#FF4040"), false, null, true);
                Prim.Make(PrimitiveType.Sphere, "NavLight", ship, new Vector3(15.2f, 6f, z), Vector3.one * 0.8f, Prim.Hex("#5BD66B"), false, null, true);
            }
        }

        void BuildStarsAndPlanets()
        {
            var sky = new GameObject("Ciel").transform;
            for (int i = 0; i < 700; i++)
            {
                var p = Random.onUnitSphere * Random.Range(1200f, 1800f);
                Prim.Make(PrimitiveType.Sphere, "Star", sky, p, Vector3.one * Random.Range(2f, 6f),
                    Random.value < 0.15f ? Prim.Hex("#9FC4FF") : Color.white, false, null, true);
            }
            Prim.Make(PrimitiveType.Sphere, "Kepler-9b", sky, new Vector3(-900f, -250f, 900f), Vector3.one * 700f, Prim.Hex("#C77D4A"), false);
            Prim.Make(PrimitiveType.Cylinder, "Anneaux", sky, new Vector3(-900f, -250f, 900f), new Vector3(1250f, 2f, 1250f), Prim.Hex("#8C6A4E"), false, new Vector3(15, 0, 20));
            Prim.Make(PrimitiveType.Sphere, "Soleil", sky, new Vector3(1200f, 600f, -1300f), Vector3.one * 160f, Prim.Hex("#FFF2C0"), false, null, true);
        }

        void BuildAsteroids()
        {
            var field = new GameObject("Champ d'astéroïdes").transform;
            var start = new Vector3(-70f, 0f, 10f);
            for (int i = 0; i < 90; i++)
            {
                // Répartis autour de la route vers le point de rendez-vous.
                float t = Random.value;
                var p = Vector3.Lerp(start, Rendezvous, t) + Random.insideUnitSphere * 75f;
                if (Vector3.Distance(p, Rendezvous) < 30f || Vector3.Distance(p, Vector3.zero) < 60f) continue;
                float size = Random.Range(3f, 14f);
                var color = Color.Lerp(Prim.Hex("#6B5E52"), Prim.Hex("#8A8178"), Random.value);
                var a = Prim.Make(PrimitiveType.Sphere, "Asteroïde", field, p, Vector3.one * size, color, false, Random.rotation.eulerAngles);
                // Petites bosses pour casser la forme parfaite de la sphère.
                for (int b = 0; b < 3; b++)
                    Prim.Make(PrimitiveType.Sphere, "Bump", a.transform, Random.onUnitSphere * 0.35f, Vector3.one * Random.Range(0.4f, 0.6f), color, false);
                a.AddComponent<SphereCollider>();
                a.AddComponent<Health>().Init("Astéroïde", size * 4f);
                var ast = a.AddComponent<Asteroid>();
                ast.spin = Random.insideUnitSphere * 25f;
                ast.color = color;
            }
        }

        void BuildSquadron()
        {
            // Balise du point de rendez-vous : un anneau de lumières.
            var beacon = new GameObject("Point de rendez-vous").transform;
            beacon.position = Rendezvous;
            beacon.rotation = Quaternion.LookRotation(Rendezvous.normalized);
            for (int i = 0; i < 20; i++)
            {
                float a = i / 20f * Mathf.PI * 2f;
                Prim.Make(PrimitiveType.Sphere, "BeaconLight", beacon, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 14f, Vector3.one * 1.2f, Prim.Hex("#F2C230"), false, null, true);
            }

            // L'escadron tourne autour de la balise.
            var orbit = new GameObject("Escadron").transform;
            orbit.position = Rendezvous;
            var stripes = new[] { Prim.Hex("#3E8EDE"), Prim.Hex("#E3B341"), Prim.Hex("#D8433A") };
            for (int i = 0; i < 3; i++)
            {
                float a = i / 3f * Mathf.PI * 2f;
                var pos = Rendezvous + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 32f + Vector3.up * (i * 3f);
                float yaw = 180f - a * Mathf.Rad2Deg; // tangente au cercle, dans le sens de rotation
                var f = FighterFactory.Build("Ailier " + (i + 1), orbit, pos, yaw, stripes[i]);
                foreach (var g in f.gear) g.gameObject.SetActive(false);
                f.canopyPivot.localRotation = Quaternion.identity;
                foreach (var e in f.engineGlows) e.localScale = new Vector3(0.7f, 0.7f, 1.4f);
            }
            orbit.gameObject.AddComponent<Spinner>().speed = 25f;
        }

        void BuildFighter()
        {
            // Le chasseur du joueur vient de franchir le champ de force du hangar.
            var parts = FighterFactory.Build("Chasseur", null, new Vector3(-22f, -1.7f, 0f), -90f, Prim.Hex("#D8433A"));
            foreach (var g in parts.gear) g.gameObject.SetActive(false);
            parts.canopyPivot.localRotation = Quaternion.identity;
            foreach (var e in parts.engineGlows) e.localScale = new Vector3(0.7f, 0.7f, 1.4f);

            // Racine (trajectoire) > pivot de roulis centré sur le fuselage > modèle.
            var root = new GameObject("Chasseur du joueur").transform;
            root.position = parts.root.transform.position + Vector3.up * 1.7f;
            root.rotation = parts.root.transform.rotation;
            var bankPivot = new GameObject("Roulis").transform;
            bankPivot.SetParent(root, false);
            parts.root.transform.SetParent(bankPivot, true);

            fighter = root.gameObject.AddComponent<FighterFlight>();
            fighter.model = bankPivot;
            fighter.muzzles = parts.muzzles.ToArray();

            cam.transform.position = root.position - root.forward * 14f + Vector3.up * 4f;
            cam.transform.LookAt(root.position);
        }

        public void Radio(string speaker, string text, float duration = 4f)
        {
            radioSpeaker = speaker;
            radioText = text;
            radioUntil = Time.time + duration;
        }

        void Update()
        {
            HUD.TickFade();
            float t = Time.time - startTime;

            if (!fighter.controlsEnabled && !completed && t > 2.5f)
            {
                fighter.controlsEnabled = true;
                Radio("Nyx", "Te voilà enfin ! On t'attend au point de rendez-vous, de l'autre côté du champ d'astéroïdes. Suis la balise jaune.", 6f);
            }

            if (!completed && Vector3.Distance(fighter.transform.position, Rendezvous) < 26f)
            {
                completed = true;
                fighter.controlsEnabled = false;
                Radio("Nyx", "Bien joué ! Escadron au complet. Cap sur le signal de détresse...", 8f);
            }

            if (completed && (GameInput.Down(KeyCode.Return) || GameInput.Down(KeyCode.KeypadEnter)))
            {
                HUD.FadeTo(1f, 0.5f);
                GameBoot.LoadShip();
            }
        }

        void LateUpdate()
        {
            if (fighter == null) return;
            // Caméra de poursuite souple, derrière et un peu au-dessus du chasseur.
            var f = fighter.transform;
            float back = 12f + fighter.Speed * 0.08f;
            var desired = f.position - f.forward * back + f.up * 3.5f;
            cam.transform.position = Vector3.SmoothDamp(cam.transform.position, desired, ref camVelocity, 0.12f);
            var look = Quaternion.LookRotation(f.position + f.forward * 12f - cam.transform.position, f.up);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, look, 8f * Time.deltaTime);
            if (CameraShake.Instance != null) cam.transform.position += CameraShake.Instance.Offset;
        }

        void OnGUI()
        {
            float s = HUD.UIScale;

            // Objectif.
            var r = new Rect(Screen.width - 460 * s, 20 * s, 440 * s, 86 * s);
            HUD.DrawRect(r, new Color(0.02f, 0.05f, 0.1f, 0.6f));
            HUD.DrawRect(new Rect(r.x, r.y, 4 * s, r.height), new Color(1f, 0.85f, 0.3f));
            HUD.ShadowLabel(new Rect(r.x + 16 * s, r.y + 8 * s, r.width, 28 * s), "Ordre de mission", HUD.Style(19, FontStyle.Bold, new Color(1f, 0.85f, 0.4f)));
            HUD.ShadowLabel(new Rect(r.x + 16 * s, r.y + 44 * s, r.width - 20 * s, 30 * s),
                completed ? "• Escadron rejoint : mission accomplie !" : "• Rejoindre l'escadron au point de rendez-vous",
                HUD.Style(17, FontStyle.Normal, completed ? new Color(0.5f, 1f, 0.5f) : Color.white));

            // Tableau de bord.
            var panel = new Rect(20 * s, Screen.height - 96 * s, 330 * s, 76 * s);
            HUD.DrawRect(panel, new Color(0.02f, 0.05f, 0.1f, 0.6f));
            HUD.DrawRect(new Rect(panel.x, panel.y, 4 * s, panel.height), new Color(0.3f, 0.85f, 1f));
            HUD.ShadowLabel(new Rect(panel.x + 16 * s, panel.y + 6 * s, panel.width, 24 * s), "Chasseur CX-4", HUD.Style(17, FontStyle.Bold, Color.white));
            HUD.ShadowLabel(new Rect(panel.x + 16 * s, panel.y + 28 * s, panel.width, 22 * s),
                "Vitesse : " + Mathf.RoundToInt(fighter.Speed * 10f) + " m/s", HUD.Style(15, FontStyle.Normal, new Color(0.7f, 0.9f, 1f)));
            HUD.ShadowLabel(new Rect(panel.x + 16 * s, panel.y + 48 * s, panel.width, 22 * s),
                "Astéroïdes détruits : " + Asteroid.Destroyed, HUD.Style(15, FontStyle.Normal, new Color(0.7f, 0.9f, 1f)));

            // Commandes.
            var help = new Rect(20 * s, 20 * s, 380 * s, 150 * s);
            HUD.DrawRect(help, new Color(0.02f, 0.05f, 0.1f, 0.6f));
            GUI.Label(new Rect(help.x + 14 * s, help.y + 10 * s, help.width - 28 * s, help.height - 20 * s),
                "<b>PILOTAGE</b>\nZ/W : monter    S : descendre\nQ/A et D : virer\nMaj gauche : turbo\nClic gauche ou J : canons laser",
                HUD.Style(15, FontStyle.Normal, Color.white));

            // Repère du point de rendez-vous et viseur.
            if (!completed)
            {
                HUD.DrawWaypoint(cam, Rendezvous, fighter.transform.position, new Color(1f, 0.85f, 0.2f));
                Vector2 c;
                if (HUD.WorldToGUI(cam, fighter.transform.position + fighter.transform.forward * 60f, out c))
                {
                    var col = new Color(0.4f, 1f, 0.5f, 0.8f);
                    HUD.DrawRect(new Rect(c.x - 12 * s, c.y - 1, 8 * s, 2), col);
                    HUD.DrawRect(new Rect(c.x + 4 * s, c.y - 1, 8 * s, 2), col);
                    HUD.DrawRect(new Rect(c.x - 1, c.y - 12 * s, 2, 8 * s), col);
                    HUD.DrawRect(new Rect(c.x - 1, c.y + 4 * s, 2, 8 * s), col);
                }
            }

            // Messages radio.
            if (Time.time < radioUntil && radioText != null)
            {
                var rr = new Rect(Screen.width / 2f - 360 * s, Screen.height - 170 * s, 720 * s, 110 * s);
                HUD.DrawRect(rr, new Color(0.02f, 0.05f, 0.1f, 0.8f));
                HUD.DrawRect(new Rect(rr.x, rr.y, rr.width, 3 * s), new Color(0.3f, 0.85f, 1f));
                HUD.ShadowLabel(new Rect(rr.x + 18 * s, rr.y + 10 * s, rr.width, 26 * s), "[Radio] " + radioSpeaker, HUD.Style(18, FontStyle.Bold, new Color(0.5f, 0.9f, 1f)));
                var st = HUD.Style(18, FontStyle.Normal, Color.white);
                st.wordWrap = true;
                GUI.Label(new Rect(rr.x + 18 * s, rr.y + 40 * s, rr.width - 36 * s, rr.height - 46 * s), radioText, st);
            }

            if (completed)
            {
                HUD.ShadowLabel(new Rect(0, Screen.height * 0.3f, Screen.width, 60 * s), "MISSION ACCOMPLIE",
                    HUD.Style(46, FontStyle.Bold, new Color(1f, 0.9f, 0.5f), TextAnchor.MiddleCenter));
                HUD.ShadowLabel(new Rect(0, Screen.height * 0.3f + 64 * s, Screen.width, 36 * s), "Fin de la démo — [Entrée] retourner à bord de l'Aurore",
                    HUD.Style(20, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter));
            }

            HUD.DrawFade();
        }
    }
}
