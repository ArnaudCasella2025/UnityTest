using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Construit tout le niveau en code, uniquement avec des primitives :
    /// le village de Clairval, la forêt, le chemin, le camp des bandits, le joueur et les PNJ.
    /// </summary>
    public static class WorldBuilder
    {
        // Disposition générale (nord = +Z).
        static readonly Vector3 VillageCenter = Vector3.zero;
        static readonly Vector3 CampCenter = new Vector3(48f, 0f, 72f);
        const float VillageRadius = 20f;
        const float CampRadius = 14f;
        const float WorldHalfSize = 100f;

        // Palette.
        static readonly Color Grass = Prim.Hex("#5E9E48");
        static readonly Color GrassDark = Prim.Hex("#4C8A3C");
        static readonly Color Dirt = Prim.Hex("#9C7A50");
        static readonly Color Stone = Prim.Hex("#8E8E8E");
        static readonly Color StoneDark = Prim.Hex("#6B6B6B");
        static readonly Color Wood = Prim.Hex("#7A5230");
        static readonly Color WoodDark = Prim.Hex("#5A3A20");
        static readonly Color Leaves = Prim.Hex("#3F7D32");
        static readonly Color LeavesLight = Prim.Hex("#5C9B3A");
        static readonly Color Pine = Prim.Hex("#2F5F35");
        static readonly Color Water = Prim.Hex("#3A6EA5");

        // Points d'apparition par défaut (position, orientation Y). Dans une scène où le décor a été généré
        // (menu Mini RPG), ce sont des objets SpawnMarker que l'on peut déplacer librement.
        public const string PlayerId = "Joueur";
        public const string CampId = "Camp des bandits";
        static readonly Dictionary<string, Vector4> DefaultSpawns = new Dictionary<string, Vector4>
        {
            { PlayerId, new Vector4(0f, 0f, -9f, 0f) },
            { "Aldric", new Vector4(3.2f, 0f, -2.5f, 200f) },
            { "Garde Hugo", new Vector4(5f, 0f, 16.5f, 180f) },
            { "Berthe", new Vector4(-5.6f, 0f, 3.3f, 30f) },
            { "Lina", new Vector4(-2f, 0f, -7f, 0f) },
            { "Paul", new Vector4(5f, 0f, -9f, 0f) },
        };

        static Transform world;
        static Transform gameplay;
        static System.Random rng;
        static readonly Dictionary<string, Transform> markers = new Dictionary<string, Transform>();

        static readonly List<Vector3> pathPoints = new List<Vector3>();

        /// <summary>
        /// Lancement du jeu : construit le décor s'il n'est pas déjà dans la scène, puis les personnages et la quête.
        /// </summary>
        public static void Build()
        {
            if (Object.FindAnyObjectByType<LevelRoot>() == null)
            {
                SetupLighting(true);
                BuildEnvironment();
            }

            rng = new System.Random(5678);
            markers.Clear();
            foreach (var m in Object.FindObjectsByType<SpawnMarker>(FindObjectsSortMode.None))
                markers[m.id] = m.transform;
            gameplay = new GameObject("Gameplay").transform;

            var player = BuildPlayer();
            SetupCamera(player.transform);
            BuildNPCs();
            SetupQuest();
        }

        /// <summary>
        /// Construit uniquement le décor (lumière, sol, village, forêt, camp, points d'apparition).
        /// Appelé au lancement, ou dans l'éditeur par le menu « Mini RPG > Générer le décor dans la scène ».
        /// </summary>
        public static GameObject BuildEnvironment()
        {
            rng = new System.Random(1234);
            world = new GameObject("Environnement").transform;
            world.gameObject.AddComponent<LevelRoot>();

            SetupLighting(false);
            BuildGround();
            BuildPath();
            BuildVillage();
            BuildCamp();
            BuildForest();
            BuildBorders();
            return world.gameObject;
        }

        static void Marker(string id, Transform parent, Vector3 worldPos, float rotY)
        {
            var go = new GameObject("Spawn - " + id);
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0, rotY, 0);
            go.AddComponent<SpawnMarker>().id = id;
        }

        static void DefaultMarker(string id, Transform parent)
        {
            var d = DefaultSpawns[id];
            Marker(id, parent, new Vector3(d.x, d.y, d.z), d.w);
        }

        /// <summary>Position et orientation d'un point d'apparition : celles du marqueur s'il existe, sinon les valeurs par défaut.</summary>
        static void Spawn(string id, out Vector3 pos, out Quaternion rot)
        {
            Transform t;
            if (markers.TryGetValue(id, out t) && t != null)
            {
                pos = t.position;
                rot = Quaternion.Euler(0, t.eulerAngles.y, 0);
                return;
            }
            Vector4 d;
            if (id == CampId) d = new Vector4(CampCenter.x, CampCenter.y, CampCenter.z, 0f);
            else d = DefaultSpawns[id];
            pos = new Vector3(d.x, d.y, d.z);
            rot = Quaternion.Euler(0, d.w, 0);
        }

        static Vector3 SpawnPos(string id)
        {
            Vector3 p; Quaternion r;
            Spawn(id, out p, out r);
            return p;
        }

        static float Rand(float min, float max) { return min + (float)rng.NextDouble() * (max - min); }

        // ------------------------------------------------------------------ ambiance

        static void SetupLighting(bool disableSceneLights)
        {
            // Au lancement sans décor généré : désactive les lumières déjà présentes (ex. scène par défaut).
            if (disableSceneLights)
                foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) l.enabled = false;
            if (world == null) return;

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(world);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.7f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.55f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.28f, 0.22f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.8f, 0.88f);
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 170f;
        }

        static void SetupCamera(Transform target)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            cam.farClipPlane = 260f;
            cam.fieldOfView = 55f;

            // Pas d'opérateur ?? avec les objets Unity (leur « null » est spécial).
            var tpc = cam.GetComponent<ThirdPersonCamera>();
            if (tpc == null) tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
            tpc.target = target;
            tpc.yaw = target.eulerAngles.y;
            if (cam.GetComponent<CameraShake>() == null) cam.gameObject.AddComponent<CameraShake>();
            cam.transform.position = target.position + target.rotation * new Vector3(0, 4, -8);
        }

        // ------------------------------------------------------------------ terrain

        static void BuildGround()
        {
            Prim.Make(PrimitiveType.Cube, "Ground", world, new Vector3(0, -0.5f, 0), new Vector3(420f, 1f, 420f), Grass);

            // Taches d'herbe plus sombre pour casser l'uniformité.
            for (int i = 0; i < 60; i++)
            {
                var p = new Vector3(Rand(-95, 95), 0.01f, Rand(-95, 95));
                float s = Rand(4f, 12f);
                Prim.Make(PrimitiveType.Cylinder, "GrassPatch", world, p, new Vector3(s, 0.01f, s * Rand(0.6f, 1.2f)), GrassDark, false);
            }

            // Collines lointaines (hors de la zone jouable, simple décor).
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f + Rand(-0.1f, 0.1f);
                float r = Rand(125f, 150f);
                var p = new Vector3(Mathf.Cos(a) * r, -4f, Mathf.Sin(a) * r);
                var s = new Vector3(Rand(40, 70), Rand(18, 35), Rand(40, 70));
                Prim.Make(PrimitiveType.Sphere, "Hill", world, p, s, i % 2 == 0 ? GrassDark : Pine, false);
            }
        }

        static void BuildPath()
        {
            // Chemin de terre du village jusqu'au camp, légèrement sinueux.
            var start = new Vector3(0, 0, 10);
            var end = CampCenter - (CampCenter - start).normalized * (CampRadius - 2f);
            int segments = 22;
            var prev = start;
            pathPoints.Clear();
            pathPoints.Add(start);
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                var p = Vector3.Lerp(start, end, t);
                p.x += Mathf.Sin(t * Mathf.PI * 2.2f) * 6f * (1f - t);
                pathPoints.Add(p);

                var mid = (prev + p) / 2f;
                var dir = p - prev;
                var seg = Prim.Make(PrimitiveType.Cube, "Path", world, new Vector3(mid.x, 0.015f, mid.z),
                    new Vector3(3.2f, 0.03f, dir.magnitude + 1.2f), Dirt, false);
                seg.transform.rotation = Quaternion.LookRotation(dir);
                prev = p;
            }
        }

        static float DistanceToPath(Vector3 p)
        {
            float best = float.MaxValue;
            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                var a = pathPoints[i];
                var b = pathPoints[i + 1];
                var ab = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
                var proj = a + ab * t;
                proj.y = p.y;
                best = Mathf.Min(best, Vector3.Distance(p, proj));
            }
            return best;
        }

        // ------------------------------------------------------------------ village

        static void BuildVillage()
        {
            var village = new GameObject("Village").transform;
            village.SetParent(world);

            // Place centrale en terre battue.
            Prim.Make(PrimitiveType.Cylinder, "Plaza", village, new Vector3(0, 0.01f, 0), new Vector3(18f, 0.01f, 18f), Dirt, false);

            BuildWell(village, VillageCenter);

            var spawns = new GameObject("Points d'apparition").transform;
            spawns.SetParent(village);
            foreach (var id in DefaultSpawns.Keys) DefaultMarker(id, spawns);

            House(village, new Vector3(-11, 0, 6), 90f, Prim.Hex("#E8D9B5"), Prim.Hex("#A33B2B"));
            House(village, new Vector3(-12, 0, -6), 90f, Prim.Hex("#D9C7A0"), Prim.Hex("#7A4B2A"));
            House(village, new Vector3(11, 0, 7), -90f, Prim.Hex("#EDE3CC"), Prim.Hex("#4F6D8A"));
            House(village, new Vector3(12, 0, -5), -90f, Prim.Hex("#E2CFA8"), Prim.Hex("#A33B2B"));
            House(village, new Vector3(-3, 0, -16), 0f, Prim.Hex("#D6C9AE"), Prim.Hex("#5F7F3A"), 1.3f);
            House(village, new Vector3(8, 0, -15), 0f, Prim.Hex("#E8D9B5"), Prim.Hex("#7A4B2A"));

            BuildMarketStall(village, new Vector3(-5, 0, 5), 30f);

            // Tonneaux et caisses.
            Barrel(village, new Vector3(-7.5f, 0, 2.5f));
            Barrel(village, new Vector3(-8.2f, 0, 3.4f));
            Crate(village, new Vector3(7.5f, 0, 3f), 15f);
            Crate(village, new Vector3(8.3f, 0, 2.2f), -10f);
            Crate(village, new Vector3(7.9f, 1f, 2.6f), 40f);

            // Palissade basse autour du village, ouverte au nord (vers le chemin) et au sud.
            for (int i = 0; i < 64; i++)
            {
                float a = i / 64f * Mathf.PI * 2f;
                var p = new Vector3(Mathf.Cos(a) * VillageRadius, 0, Mathf.Sin(a) * VillageRadius);
                if (Mathf.Abs(p.x) < 4f) continue; // entrées nord et sud
                Prim.Make(PrimitiveType.Cylinder, "FencePost", village, p + Vector3.up * 0.6f, new Vector3(0.25f, 0.6f, 0.25f), Wood);
                float a2 = (i + 1) / 64f * Mathf.PI * 2f;
                var p2 = new Vector3(Mathf.Cos(a2) * VillageRadius, 0, Mathf.Sin(a2) * VillageRadius);
                if (Mathf.Abs(p2.x) < 4f) continue;
                var mid = (p + p2) / 2f;
                var rail = Prim.Make(PrimitiveType.Cube, "FenceRail", village, mid + Vector3.up * 0.8f, new Vector3(0.12f, 0.15f, (p2 - p).magnitude), WoodDark);
                rail.transform.rotation = Quaternion.LookRotation(p2 - p);
            }

            // Lampadaires.
            Lamp(village, new Vector3(3.5f, 0, 18f));
            Lamp(village, new Vector3(-3.5f, 0, 18f));
            Lamp(village, new Vector3(5f, 0, -4f));

            // Quelques arbres et buissons dans le village.
            RoundTree(village, new Vector3(-15, 0, 13), 1.1f);
            RoundTree(village, new Vector3(15, 0, -12), 1f);
            RoundTree(village, new Vector3(-14, 0, -13), 0.9f);
            Bush(village, new Vector3(-6, 0, -10));
            Bush(village, new Vector3(5, 0, 11));
            Bush(village, new Vector3(14, 0, 1));
        }

        static void BuildWell(Transform parent, Vector3 pos)
        {
            var well = new GameObject("Well").transform;
            well.SetParent(parent);
            well.position = pos;
            Prim.Make(PrimitiveType.Cylinder, "Ring", well, new Vector3(0, 0.45f, 0), new Vector3(2.4f, 0.45f, 2.4f), Stone);
            Prim.Make(PrimitiveType.Cylinder, "Water", well, new Vector3(0, 0.92f, 0), new Vector3(1.9f, 0.02f, 1.9f), Water, false);
            Prim.Make(PrimitiveType.Cube, "PostL", well, new Vector3(-1.05f, 1.5f, 0), new Vector3(0.18f, 2.2f, 0.18f), Wood);
            Prim.Make(PrimitiveType.Cube, "PostR", well, new Vector3(1.05f, 1.5f, 0), new Vector3(0.18f, 2.2f, 0.18f), Wood);
            Prim.Make(PrimitiveType.Cube, "RoofL", well, new Vector3(-0.55f, 2.75f, 0), new Vector3(1.4f, 0.1f, 1.6f), Prim.Hex("#A33B2B"), false, new Vector3(0, 0, 30));
            Prim.Make(PrimitiveType.Cube, "RoofR", well, new Vector3(0.55f, 2.75f, 0), new Vector3(1.4f, 0.1f, 1.6f), Prim.Hex("#A33B2B"), false, new Vector3(0, 0, -30));
            Prim.Make(PrimitiveType.Cylinder, "Axle", well, new Vector3(0, 2.2f, 0), new Vector3(0.12f, 1.05f, 0.12f), WoodDark, false, new Vector3(0, 0, 90));
            Prim.Make(PrimitiveType.Cube, "Bucket", well, new Vector3(0, 1.5f, 0), new Vector3(0.3f, 0.35f, 0.3f), WoodDark, false);
        }

        static void House(Transform parent, Vector3 pos, float rotY, Color wall, Color roof, float size = 1f)
        {
            var h = new GameObject("House").transform;
            h.SetParent(parent);
            h.position = pos;
            h.rotation = Quaternion.Euler(0, rotY, 0);
            h.localScale = Vector3.one * size;

            Prim.Make(PrimitiveType.Cube, "Walls", h, new Vector3(0, 1.5f, 0), new Vector3(5f, 3f, 4.5f), wall);
            // Toit : un cube tourné de 45° forme un prisme triangulaire.
            Prim.Make(PrimitiveType.Cube, "Roof", h, new Vector3(0, 3.0f, 0), new Vector3(3.8f, 3.8f, 5.4f), roof, true, new Vector3(0, 90, 45));
            // Poutres d'angle.
            foreach (var c in new[] { new Vector3(-2.5f, 1.5f, -2.25f), new Vector3(2.5f, 1.5f, -2.25f), new Vector3(-2.5f, 1.5f, 2.25f), new Vector3(2.5f, 1.5f, 2.25f) })
                Prim.Make(PrimitiveType.Cube, "Beam", h, c, new Vector3(0.25f, 3.02f, 0.25f), WoodDark, false);
            // Porte et fenêtres sur la façade (+Z local).
            Prim.Make(PrimitiveType.Cube, "Door", h, new Vector3(0, 1.05f, 2.27f), new Vector3(1.1f, 2.1f, 0.08f), WoodDark, false);
            Prim.Make(PrimitiveType.Sphere, "Knob", h, new Vector3(0.35f, 1.05f, 2.33f), Vector3.one * 0.1f, Prim.Hex("#D8B040"), false);
            var glass = Prim.Hex("#9FC9E8");
            Prim.Make(PrimitiveType.Cube, "WindowL", h, new Vector3(-1.6f, 1.8f, 2.27f), new Vector3(0.8f, 0.8f, 0.06f), glass, false);
            Prim.Make(PrimitiveType.Cube, "WindowR", h, new Vector3(1.6f, 1.8f, 2.27f), new Vector3(0.8f, 0.8f, 0.06f), glass, false);
            Prim.Make(PrimitiveType.Cube, "WindowSide", h, new Vector3(2.52f, 1.8f, 0), new Vector3(0.06f, 0.8f, 1f), glass, false);
            // Cheminée.
            Prim.Make(PrimitiveType.Cube, "Chimney", h, new Vector3(1.4f, 4.2f, -0.8f), new Vector3(0.6f, 1.6f, 0.6f), StoneDark, false);
        }

        static void BuildMarketStall(Transform parent, Vector3 pos, float rotY)
        {
            var s = new GameObject("MarketStall").transform;
            s.SetParent(parent);
            s.position = pos;
            s.rotation = Quaternion.Euler(0, rotY, 0);
            Prim.Make(PrimitiveType.Cube, "Counter", s, new Vector3(0, 0.5f, 0), new Vector3(3f, 1f, 1f), Wood);
            foreach (var x in new[] { -1.4f, 1.4f })
            {
                Prim.Make(PrimitiveType.Cube, "Post", s, new Vector3(x, 1.25f, -0.4f), new Vector3(0.12f, 2.5f, 0.12f), WoodDark, false);
                Prim.Make(PrimitiveType.Cube, "Post", s, new Vector3(x, 1.25f, 0.4f), new Vector3(0.12f, 2.5f, 0.12f), WoodDark, false);
            }
            Prim.Make(PrimitiveType.Cube, "Awning", s, new Vector3(0, 2.55f, 0.1f), new Vector3(3.3f, 0.08f, 1.6f), Prim.Hex("#C8453A"), false, new Vector3(-12, 0, 0));
            // Fruits sur le comptoir.
            var fruit = new[] { Prim.Hex("#D33A2C"), Prim.Hex("#F0A030"), Prim.Hex("#9BC53D") };
            for (int i = 0; i < 9; i++)
                Prim.Make(PrimitiveType.Sphere, "Fruit", s, new Vector3(-1.1f + i * 0.28f, 1.12f, Rand(-0.25f, 0.25f)), Vector3.one * 0.24f, fruit[i % 3], false);
        }

        static void Barrel(Transform parent, Vector3 pos)
        {
            Prim.Make(PrimitiveType.Cylinder, "Barrel", parent, pos + Vector3.up * 0.55f, new Vector3(0.8f, 0.55f, 0.8f), Wood);
            Prim.Make(PrimitiveType.Cylinder, "Hoop", parent, pos + Vector3.up * 0.8f, new Vector3(0.84f, 0.04f, 0.84f), StoneDark, false);
            Prim.Make(PrimitiveType.Cylinder, "Hoop", parent, pos + Vector3.up * 0.3f, new Vector3(0.84f, 0.04f, 0.84f), StoneDark, false);
        }

        static void Crate(Transform parent, Vector3 pos, float rotY)
        {
            Prim.Make(PrimitiveType.Cube, "Crate", parent, pos + Vector3.up * 0.5f, Vector3.one, Prim.Hex("#A57A48"), true, new Vector3(0, rotY, 0));
        }

        static void Lamp(Transform parent, Vector3 pos)
        {
            Prim.Make(PrimitiveType.Cylinder, "LampPost", parent, pos + Vector3.up * 1.5f, new Vector3(0.15f, 1.5f, 0.15f), WoodDark);
            Prim.Make(PrimitiveType.Sphere, "LampGlow", parent, pos + Vector3.up * 3.1f, Vector3.one * 0.45f, Prim.Hex("#FFE8A0"), false);
        }

        // ------------------------------------------------------------------ végétation

        static void RoundTree(Transform parent, Vector3 pos, float s)
        {
            var t = new GameObject("Tree").transform;
            t.SetParent(parent);
            t.position = pos;
            t.localScale = Vector3.one * s;
            Prim.Make(PrimitiveType.Cylinder, "Trunk", t, new Vector3(0, 1.5f, 0), new Vector3(0.5f, 1.5f, 0.5f), Wood);
            Prim.Make(PrimitiveType.Sphere, "Leaves", t, new Vector3(0, 3.8f, 0), new Vector3(3.2f, 2.8f, 3.2f), Leaves, false);
            Prim.Make(PrimitiveType.Sphere, "Leaves2", t, new Vector3(0.6f, 4.8f, 0.3f), new Vector3(2f, 1.8f, 2f), LeavesLight, false);
        }

        static void PineTree(Transform parent, Vector3 pos, float s)
        {
            var t = new GameObject("Pine").transform;
            t.SetParent(parent);
            t.position = pos;
            t.localScale = Vector3.one * s;
            Prim.Make(PrimitiveType.Cylinder, "Trunk", t, new Vector3(0, 1f, 0), new Vector3(0.4f, 1f, 0.4f), WoodDark);
            // Étages de feuillage : cubes tournés, de plus en plus petits.
            for (int i = 0; i < 3; i++)
            {
                float size = 2.8f - i * 0.8f;
                Prim.Make(PrimitiveType.Cube, "Needles", t, new Vector3(0, 2.2f + i * 1.2f, 0), new Vector3(size, 1.2f, size), Pine, false, new Vector3(0, 45f * i, 0));
            }
        }

        static void Bush(Transform parent, Vector3 pos)
        {
            Prim.Make(PrimitiveType.Sphere, "Bush", parent, pos + Vector3.up * 0.4f, new Vector3(1.6f, 1f, 1.4f), LeavesLight, false);
            Prim.Make(PrimitiveType.Sphere, "Bush", parent, pos + new Vector3(0.6f, 0.3f, 0.3f), new Vector3(1f, 0.8f, 1f), Leaves, false);
        }

        static void Rock(Transform parent, Vector3 pos, float s)
        {
            var r = Prim.Make(PrimitiveType.Sphere, "Rock", parent, pos + Vector3.up * 0.2f * s,
                new Vector3(1.6f, 1f, 1.3f) * s, Rand(0f, 1f) > 0.5f ? Stone : StoneDark, true, new Vector3(Rand(-10, 10), Rand(0, 360), Rand(-10, 10)));
            r.name = "Rock";
        }

        static void BuildForest()
        {
            var forest = new GameObject("Forest").transform;
            forest.SetParent(world);

            int placed = 0, attempts = 0;
            while (placed < 260 && attempts < 5000)
            {
                attempts++;
                var p = new Vector3(Rand(-WorldHalfSize + 2, WorldHalfSize - 2), 0, Rand(-WorldHalfSize + 2, WorldHalfSize - 2));
                if (Vector3.Distance(p, VillageCenter) < VillageRadius + 4f) continue;
                if (Vector3.Distance(p, CampCenter) < CampRadius + 5f) continue;
                if (DistanceToPath(p) < 5f) continue;
                // Une clairière dégagée entre le village et le camp, pour le confort de jeu.
                if (Vector3.Distance(p, new Vector3(20, 0, 40)) < 9f) continue;

                float s = Rand(0.8f, 1.4f);
                if (rng.NextDouble() < 0.5) PineTree(forest, p, s); else RoundTree(forest, p, s);
                placed++;
            }

            for (int i = 0; i < 70; i++)
            {
                var p = new Vector3(Rand(-95, 95), 0, Rand(-95, 95));
                if (Vector3.Distance(p, VillageCenter) < VillageRadius) continue;
                if (DistanceToPath(p) < 3f) continue;
                if (rng.NextDouble() < 0.5) Rock(forest, p, Rand(0.6f, 1.8f)); else Bush(forest, p);
            }
        }

        static void BuildBorders()
        {
            // Murs invisibles en bordure de la zone jouable.
            for (int i = 0; i < 4; i++)
            {
                var w = new GameObject("InvisibleWall");
                w.transform.SetParent(world);
                var bc = w.AddComponent<BoxCollider>();
                bool alongX = i < 2;
                float sign = i % 2 == 0 ? 1f : -1f;
                w.transform.position = alongX ? new Vector3(0, 5, sign * WorldHalfSize) : new Vector3(sign * WorldHalfSize, 5, 0);
                bc.size = alongX ? new Vector3(WorldHalfSize * 2, 10, 1) : new Vector3(1, 10, WorldHalfSize * 2);
            }
        }

        // ------------------------------------------------------------------ camp des bandits

        static void BuildCamp()
        {
            var camp = new GameObject("BanditCamp").transform;
            camp.SetParent(world);
            camp.position = CampCenter;
            Marker(CampId, camp, CampCenter, 0f);

            Prim.Make(PrimitiveType.Cylinder, "CampGround", camp, new Vector3(0, 0.012f, 0), new Vector3(CampRadius * 2f, 0.01f, CampRadius * 2f), Prim.Hex("#8A7350"), false);

            // Palissade de rondins pointus, avec une ouverture tournée vers le village.
            var toVillage = (VillageCenter - CampCenter);
            toVillage.y = 0;
            float gateAngle = Mathf.Atan2(toVillage.z, toVillage.x);
            int logs = 70;
            for (int i = 0; i < logs; i++)
            {
                float a = i / (float)logs * Mathf.PI * 2f;
                float delta = Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, gateAngle * Mathf.Rad2Deg));
                if (delta < 16f) continue;
                var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * CampRadius;
                float h = Rand(1.4f, 1.9f);
                Prim.Make(PrimitiveType.Cylinder, "Log", camp, p + Vector3.up * h, new Vector3(0.7f, h, 0.7f), Wood);
                Prim.Make(PrimitiveType.Cube, "Spike", camp, p + Vector3.up * (h * 2f + 0.1f), new Vector3(0.45f, 0.45f, 0.45f), WoodDark, false, new Vector3(45, 0, 45));
            }

            // Tentes (prismes : cubes tournés de 45°).
            Tent(camp, new Vector3(-6, 0, 5), 20f, Prim.Hex("#7D6B4A"));
            Tent(camp, new Vector3(5, 0, 7), -30f, Prim.Hex("#6E5A3A"));
            Tent(camp, new Vector3(7, 0, -4), 80f, Prim.Hex("#7D6B4A"));
            Tent(camp, new Vector3(-7, 0, -5), 120f, Prim.Hex("#5E4C30"), 1.4f); // tente du chef

            BuildCampfire(camp, Vector3.zero);

            // Tour de guet.
            var tower = new GameObject("Watchtower").transform;
            tower.SetParent(camp);
            tower.localPosition = new Vector3(0, 0, 10f);
            foreach (var c in new[] { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(-1, 0, 1), new Vector3(1, 0, 1) })
                Prim.Make(PrimitiveType.Cylinder, "Leg", tower, c + Vector3.up * 2.5f, new Vector3(0.3f, 2.5f, 0.3f), WoodDark);
            Prim.Make(PrimitiveType.Cube, "Platform", tower, new Vector3(0, 5.1f, 0), new Vector3(2.8f, 0.2f, 2.8f), Wood);
            Prim.Make(PrimitiveType.Cube, "Rail", tower, new Vector3(0, 5.6f, 1.35f), new Vector3(2.8f, 0.8f, 0.1f), Wood);
            Prim.Make(PrimitiveType.Cube, "Rail", tower, new Vector3(0, 5.6f, -1.35f), new Vector3(2.8f, 0.8f, 0.1f), Wood);
            Prim.Make(PrimitiveType.Cube, "Rail", tower, new Vector3(1.35f, 5.6f, 0), new Vector3(0.1f, 0.8f, 2.8f), Wood);
            Prim.Make(PrimitiveType.Cube, "Rail", tower, new Vector3(-1.35f, 5.6f, 0), new Vector3(0.1f, 0.8f, 2.8f), Wood);

            // Bannière rouge des bandits.
            Prim.Make(PrimitiveType.Cylinder, "BannerPole", camp, new Vector3(-2.5f, 2.5f, -9f), new Vector3(0.15f, 2.5f, 0.15f), WoodDark);
            Prim.Make(PrimitiveType.Cube, "Banner", camp, new Vector3(-1.75f, 4.2f, -9f), new Vector3(1.4f, 1f, 0.05f), Prim.Hex("#9B1C1C"), false);
            Prim.Make(PrimitiveType.Sphere, "Skull", camp, new Vector3(-1.75f, 4.25f, -9.05f), new Vector3(0.4f, 0.4f, 0.1f), Prim.Hex("#EEE8D8"), false);

            // Butin et caisses.
            Crate(camp, new Vector3(3, 0, -8), 10f);
            Crate(camp, new Vector3(4, 0, -9), 35f);
            Crate(camp, new Vector3(3.5f, 1, -8.5f), 60f);
            Barrel(camp, new Vector3(-9, 0, 1));
            Barrel(camp, new Vector3(-9.5f, 0, 2.2f));
            for (int i = 0; i < 6; i++)
                Prim.Make(PrimitiveType.Sphere, "Gold", camp, new Vector3(5.2f + Rand(-0.4f, 0.4f), 0.15f, -6.5f + Rand(-0.4f, 0.4f)), Vector3.one * 0.3f, Prim.Hex("#E8C440"), false);

            // Râtelier d'armes.
            Prim.Make(PrimitiveType.Cube, "Rack", camp, new Vector3(-3f, 0.9f, 9f), new Vector3(2.5f, 0.12f, 0.12f), WoodDark, false);
            for (int i = 0; i < 4; i++)
                Prim.Make(PrimitiveType.Cube, "Spear", camp, new Vector3(-4f + i * 0.65f, 1.1f, 9.05f), new Vector3(0.06f, 2.2f, 0.06f), Stone, false, new Vector3(0, 0, 8));
        }

        static void Tent(Transform parent, Vector3 pos, float rotY, Color color, float size = 1f)
        {
            var t = new GameObject("Tent").transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0, rotY, 0);
            t.localScale = Vector3.one * size;
            Prim.Make(PrimitiveType.Cube, "Canvas", t, new Vector3(0, 0, 0), new Vector3(2.6f, 2.6f, 3.4f), color, true, new Vector3(0, 0, 45));
            Prim.Make(PrimitiveType.Cube, "Opening", t, new Vector3(0, 0.6f, 1.71f), new Vector3(0.9f, 1.2f, 0.02f), Prim.Hex("#2A2218"), false);
        }

        static void BuildCampfire(Transform parent, Vector3 pos)
        {
            var fire = new GameObject("Campfire").transform;
            fire.SetParent(parent, false);
            fire.localPosition = pos;

            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Prim.Make(PrimitiveType.Sphere, "FireStone", fire, new Vector3(Mathf.Cos(a), 0.15f, Mathf.Sin(a)) * 1.1f, Vector3.one * 0.45f, StoneDark);
            }
            for (int i = 0; i < 3; i++)
                Prim.Make(PrimitiveType.Cylinder, "FireLog", fire, new Vector3(0, 0.2f, 0), new Vector3(0.2f, 0.6f, 0.2f), WoodDark, false, new Vector3(90, i * 60f, 0));

            var flames = new List<Transform>();
            flames.Add(Prim.Make(PrimitiveType.Sphere, "Flame", fire, new Vector3(0, 0.55f, 0), new Vector3(0.7f, 1f, 0.7f), Prim.Hex("#FF7A1A"), false).transform);
            flames.Add(Prim.Make(PrimitiveType.Sphere, "Flame", fire, new Vector3(0.15f, 0.75f, 0.1f), new Vector3(0.4f, 0.8f, 0.4f), Prim.Hex("#FFC93A"), false).transform);
            flames.Add(Prim.Make(PrimitiveType.Sphere, "Flame", fire, new Vector3(-0.15f, 0.65f, -0.1f), new Vector3(0.35f, 0.6f, 0.35f), Prim.Hex("#FFE27A"), false).transform);

            var lightGo = new GameObject("FireLight");
            lightGo.transform.SetParent(fire, false);
            lightGo.transform.localPosition = new Vector3(0, 1.5f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.6f, 0.25f);
            light.range = 12f;

            var flicker = fire.gameObject.AddComponent<Flicker>();
            flicker.lightSource = light;
            flicker.flames = flames.ToArray();
            flicker.baseIntensity = 2.5f;

            // Sièges autour du feu.
            for (int i = 0; i < 3; i++)
            {
                float a = (i / 3f + 0.1f) * Mathf.PI * 2f;
                var p = new Vector3(Mathf.Cos(a), 0.25f, Mathf.Sin(a)) * 3f;
                p.y = 0.25f;
                Prim.Make(PrimitiveType.Cylinder, "Seat", fire, p, new Vector3(0.5f, 0.6f, 0.5f), Wood, true,
                    new Vector3(90, -a * Mathf.Rad2Deg, 0));
            }
        }

        // ------------------------------------------------------------------ personnages

        static GameObject BuildPlayer()
        {
            Vector3 spawn; Quaternion spawnRot;
            Spawn(PlayerId, out spawn, out spawnRot);
            var parts = CharacterFactory.Build("Player", spawn, Prim.Hex("#3C6FD1"), 1f, true);
            parts.root.transform.SetParent(gameplay, true);
            parts.root.transform.rotation = spawnRot;
            // Petite cape rouge pour distinguer le héros.
            Prim.Make(PrimitiveType.Cube, "Cape", parts.visual, new Vector3(0, 1.05f, -0.48f), new Vector3(0.8f, 1.3f, 0.06f), Prim.Hex("#B8322C"), false, new Vector3(-6, 0, 0));
            // Bouclier sur le bras gauche.
            Prim.Make(PrimitiveType.Cylinder, "Shield", parts.visual, new Vector3(-0.58f, 1.05f, 0.1f), new Vector3(0.7f, 0.04f, 0.7f), Prim.Hex("#8A6A2A"), false, new Vector3(0, 0, 90));
            Prim.Make(PrimitiveType.Sphere, "ShieldBoss", parts.visual, new Vector3(-0.62f, 1.05f, 0.1f), Vector3.one * 0.2f, Prim.Hex("#C9C9C9"), false);

            var go = parts.root;
            CharacterFactory.AddController(go);
            go.AddComponent<Health>().Init("Héros", 100f);
            var pc = go.AddComponent<PlayerController>();
            pc.Visual = parts.visual;
            pc.SpawnPoint = spawn;
            var combat = go.AddComponent<PlayerCombat>();
            combat.weaponPivot = parts.weaponPivot;
            combat.potions = 2;
            parts.weaponPivot.localRotation = Quaternion.Euler(20f, 0, 0);
            go.AddComponent<PlayerInteractor>();
            return go;
        }

        static NPC MakeNPC(string name, Color color, float size = 1f, float wander = 0f)
        {
            Vector3 pos; Quaternion rot;
            Spawn(name, out pos, out rot);
            var parts = CharacterFactory.Build(name, pos, color, size);
            parts.root.transform.SetParent(gameplay, true);
            parts.root.transform.rotation = rot;
            var col = parts.root.AddComponent<CapsuleCollider>();
            col.height = 2f * size;
            col.radius = 0.45f * size;
            col.center = new Vector3(0, size, 0);
            var npc = parts.root.AddComponent<NPC>();
            npc.npcName = name;
            npc.wanderRadius = wander;
            return npc;
        }

        static void BuildNPCs()
        {
            var q = QuestManager.Instance;

            // --- Aldric, le chef du village : le donneur de quête.
            var aldric = MakeNPC("Aldric", Prim.Hex("#6B4C9A"));
            var av = aldric.transform.Find("Visual");
            Prim.Make(PrimitiveType.Sphere, "Beard", av, new Vector3(0, 1.3f, 0.38f), new Vector3(0.5f, 0.55f, 0.3f), Prim.Hex("#EDEDED"), false);
            Prim.Make(PrimitiveType.Cylinder, "HatBrim", av, new Vector3(0, 1.98f, 0), new Vector3(1.1f, 0.03f, 1.1f), Prim.Hex("#3B2A55"), false);
            Prim.Make(PrimitiveType.Cylinder, "HatTop", av, new Vector3(0, 2.2f, 0), new Vector3(0.6f, 0.22f, 0.6f), Prim.Hex("#3B2A55"), false);
            Prim.Make(PrimitiveType.Cylinder, "Staff", av, new Vector3(0.6f, 1.1f, 0.2f), new Vector3(0.08f, 1.1f, 0.08f), Wood, false);
            q.giver = aldric.transform;
            q.giverName = "Aldric";

            aldric.markerProvider = () =>
            {
                if (q.State == QuestState.NotStarted) return "!";
                if (q.State == QuestState.ReturnToGiver) return "?";
                return null;
            };
            aldric.dialogueProvider = () => AldricDialogue(q);

            // --- Hugo, le garde à l'entrée nord.
            var hugo = MakeNPC("Garde Hugo", Prim.Hex("#7F8C8D"));
            var hv = hugo.transform.Find("Visual");
            Prim.Make(PrimitiveType.Sphere, "Helmet", hv, new Vector3(0, 1.82f, 0), new Vector3(1.02f, 0.7f, 1.02f), Prim.Hex("#B0B6BA"), false);
            Prim.Make(PrimitiveType.Cylinder, "Spear", hv, new Vector3(0.6f, 1.4f, 0.2f), new Vector3(0.07f, 1.4f, 0.07f), WoodDark, false);
            Prim.Make(PrimitiveType.Cube, "SpearTip", hv, new Vector3(0.6f, 2.85f, 0.2f), new Vector3(0.15f, 0.3f, 0.05f), Stone, false);
            hugo.dialogueProvider = () =>
            {
                switch (q.State)
                {
                    case QuestState.NotStarted:
                        return new Dialogue()
                            .Say("Garde Hugo", "Halte, voyageur. Le chemin vers le nord-est n'est plus sûr.")
                            .Say("Garde Hugo", "Des bandits ont installé un camp sur la colline. Va plutôt voir Aldric, notre chef, près du puits.");
                    case QuestState.GoToCamp:
                    case QuestState.DefeatBandits:
                        return new Dialogue()
                            .Say("Garde Hugo", "Alors c'est toi qui vas les chasser ? Écoute un conseil de vieux soldat.")
                            .Say("Garde Hugo", "Quand un bandit lève son arme, il va frapper : esquive avec Ctrl gauche, puis contre-attaque pendant qu'il récupère.")
                            .Say("Garde Hugo", "Et enchaîne trois coups d'épée : le troisième frappe bien plus fort.");
                    case QuestState.ReturnToGiver:
                        return new Dialogue().Say("Garde Hugo", "Tu es revenu entier ! Va vite annoncer la nouvelle à Aldric.");
                    default:
                        return new Dialogue().Say("Garde Hugo", "Grâce à toi, je vais enfin pouvoir dormir pendant mes gardes. Euh... oublie ce que je viens de dire.");
                }
            };

            // --- Berthe, la marchande.
            var berthe = MakeNPC("Berthe", Prim.Hex("#C0632B"));
            berthe.interactRange = 3.5f;
            Prim.Make(PrimitiveType.Cube, "Apron", berthe.transform.Find("Visual"), new Vector3(0, 0.85f, 0.45f), new Vector3(0.6f, 0.8f, 0.05f), Prim.Hex("#F2EBDD"), false);
            bool gavePotion = false;
            berthe.dialogueProvider = () =>
            {
                var d = new Dialogue().Say("Berthe", "Des pommes, des poires, des oranges ! Enfin... quand les bandits ne volent pas tout.");
                if (q.State != QuestState.NotStarted && q.State != QuestState.Completed && !gavePotion)
                {
                    d.Say("Berthe", "Tu pars affronter ces brigands ? Tiens, prends cette potion. Bois-la avec H quand ça tourne mal.")
                     .OnEnd(() =>
                     {
                         gavePotion = true;
                         PlayerController.Instance.GetComponent<PlayerCombat>().potions++;
                         HUD.Instance.ShowMessage("Vous recevez une potion", 2f);
                     });
                }
                else if (q.State == QuestState.Completed)
                {
                    d.Say("Berthe", "Les marchands reviennent déjà ! Merci, héros.");
                }
                return d;
            };

            // --- Lina, une enfant qui se promène.
            var lina = MakeNPC("Lina", Prim.Hex("#E07AA8"), 0.7f, 4f);
            lina.dialogueProvider = () =>
            {
                if (q.State == QuestState.Completed)
                    return new Dialogue().Say("Lina", "C'est toi qui as battu le gros Brutus ? Quand je serai grande, je serai une héroïne aussi !");
                return new Dialogue()
                    .Say("Lina", "T'as vu ? Je suis une capsule rose !")
                    .Say("Lina", "Maman dit qu'il faut pas aller dans la forêt à cause des méchants. Toi t'as une épée, t'as pas peur hein ?");
            };

            // --- Paul, un fermier.
            var paul = MakeNPC("Paul", Prim.Hex("#4E8F5A"), 1f, 2.5f);
            Prim.Make(PrimitiveType.Cylinder, "StrawHat", paul.transform.Find("Visual"), new Vector3(0, 2.0f, 0), new Vector3(1.2f, 0.05f, 1.2f), Prim.Hex("#E3C565"), false);
            paul.dialogueProvider = () =>
            {
                if (q.State == QuestState.Completed)
                    return new Dialogue().Say("Paul", "Mes récoltes sont enfin en sécurité. Je te garderai les plus belles carottes !");
                return new Dialogue()
                    .Say("Paul", "Ces bandits m'ont pris trois sacs de grain la semaine dernière.")
                    .Say("Paul", "Leur chef, Brutus, est une vraie montagne. Ne te bats pas contre lui sans potion.");
            };
        }

        static Dialogue AldricDialogue(QuestManager q)
        {
            switch (q.State)
            {
                case QuestState.NotStarted:
                    return new Dialogue()
                        .Say("Aldric", "Ah, un voyageur ! Bienvenue à Clairval.")
                        .Say("Aldric", "Je suis Aldric, le chef de ce village. Hélas, tu arrives en des temps difficiles.")
                        .Say("Aldric", "Une bande de brigands s'est installée sur la colline, au nord-est. Ils pillent nos récoltes et attaquent nos marchands.")
                        .Say("Aldric", "Toi qui portes une épée... accepterais-tu d'aller au camp et de nous débarrasser d'eux ?")
                        .Choice("J'accepte, je m'en occupe.", () =>
                        {
                            q.Accept();
                            DialogueManager.Instance.Open(new Dialogue()
                                .Say("Aldric", "Merci, voyageur ! Suis le chemin de terre qui part de l'entrée nord du village.")
                                .Say("Aldric", "Leur chef s'appelle Brutus. Il est fort mais lent : sois plus rapide que lui.")
                                .Say("Aldric", "Reviens me voir quand le camp sera libéré. Le village saura te récompenser."), q.giver);
                        })
                        .Choice("Pas maintenant.", () =>
                        {
                            DialogueManager.Instance.Open(new Dialogue()
                                .Say("Aldric", "Je comprends... Reviens me voir si tu changes d'avis."), q.giver);
                        });

                case QuestState.GoToCamp:
                case QuestState.DefeatBandits:
                    return new Dialogue()
                        .Say("Aldric", "Le camp des bandits est au nord-est. Suis le chemin de terre depuis l'entrée nord.")
                        .Say("Aldric", "Si tu es blessé, n'hésite pas à boire une potion.");

                case QuestState.ReturnToGiver:
                    return new Dialogue()
                        .Say("Aldric", "Te voilà de retour ! Alors, ces bandits ?")
                        .Say("Héros", "Le camp est libéré. Brutus et sa bande ne vous causeront plus de soucis.")
                        .Say("Aldric", "Merveilleux ! Tout Clairval te doit une fière chandelle.")
                        .Say("Aldric", "Voici ta récompense : 150 pièces d'or. Et sache que tu seras toujours le bienvenu ici.")
                        .OnEnd(q.TurnIn);

                default:
                    return new Dialogue().Say("Aldric", "Le village est en paix grâce à toi, héros. Merci encore !");
            }
        }

        static List<Health> SpawnBandits()
        {
            var result = new List<Health>();
            var camp = SpawnPos(CampId);
            result.Add(MakeBandit("Bandit", camp + new Vector3(-4, 0, 2), false));
            result.Add(MakeBandit("Bandit", camp + new Vector3(4, 0, 3), false));
            result.Add(MakeBandit("Bandit", camp + new Vector3(2, 0, -5), false));
            result.Add(MakeBandit("Brutus, chef des bandits", camp + new Vector3(-3, 0, -1), true));
            return result;
        }

        static Health MakeBandit(string name, Vector3 pos, bool boss)
        {
            float size = boss ? 1.35f : 1f;
            var parts = CharacterFactory.Build(name, pos, boss ? Prim.Hex("#7A1F1F") : Prim.Hex("#B03A2E"), size, true,
                boss ? Prim.Hex("#555555") : (Color?)null);
            parts.root.transform.SetParent(gameplay, true);
            parts.root.transform.rotation = Quaternion.Euler(0, Rand(0, 360), 0);

            if (boss)
            {
                // Casque à cornes.
                Prim.Make(PrimitiveType.Sphere, "Helmet", parts.visual, new Vector3(0, 1.82f, 0), new Vector3(1.04f, 0.7f, 1.04f), Prim.Hex("#4A4A4A"), false);
                Prim.Make(PrimitiveType.Cylinder, "HornL", parts.visual, new Vector3(-0.55f, 2.1f, 0), new Vector3(0.12f, 0.3f, 0.12f), Prim.Hex("#EEE8D8"), false, new Vector3(0, 0, 35));
                Prim.Make(PrimitiveType.Cylinder, "HornR", parts.visual, new Vector3(0.55f, 2.1f, 0), new Vector3(0.12f, 0.3f, 0.12f), Prim.Hex("#EEE8D8"), false, new Vector3(0, 0, -35));
                // Grosse hache : une lame plus large au bout de l'arme.
                Prim.Make(PrimitiveType.Cube, "AxeHead", parts.weaponPivot, new Vector3(0.25f, 0, 1.2f), new Vector3(0.5f, 0.06f, 0.5f), Prim.Hex("#777777"), false);
            }
            else
            {
                // Bandana noir.
                Prim.Make(PrimitiveType.Cylinder, "Bandana", parts.visual, new Vector3(0, 1.72f, 0), new Vector3(1.02f, 0.08f, 1.02f), Prim.Hex("#1E1E1E"), false);
                Prim.Make(PrimitiveType.Cube, "Mask", parts.visual, new Vector3(0, 1.35f, 0.44f), new Vector3(0.6f, 0.25f, 0.06f), Prim.Hex("#1E1E1E"), false);
            }
            parts.weaponPivot.localRotation = Quaternion.Euler(20f, 0, 0);

            CharacterFactory.AddController(parts.root, size);
            var h = parts.root.AddComponent<Health>();
            h.Init(boss ? "Brutus" : name, boss ? 140f : 55f);
            var ai = parts.root.AddComponent<EnemyAI>();
            ai.weaponPivot = parts.weaponPivot;
            ai.visual = parts.visual;
            if (boss)
            {
                ai.damage = 18f;
                ai.windup = 0.75f;
                ai.recovery = 0.9f;
                ai.chaseSpeed = 3.8f;
                ai.attackRange = 2.4f;
                ai.patrolRadius = 2f;
            }
            return h;
        }

        static void SetupQuest()
        {
            var q = QuestManager.Instance;
            q.campCenter = SpawnPos(CampId);
            q.campRadius = CampRadius - 1f;
            q.spawnBandits = SpawnBandits;

            // Colonne lumineuse qui signale le camp tant qu'on n'y est pas allé.
            var beacon = new GameObject("QuestBeacon");
            beacon.transform.SetParent(gameplay);
            Prim.Make(PrimitiveType.Cylinder, "Beam", beacon.transform, new Vector3(0, 20f, 0), new Vector3(0.5f, 20f, 0.5f), Prim.Hex("#FFD84A"), false);
            var ring = Prim.Make(PrimitiveType.Cube, "Diamond", beacon.transform, new Vector3(0, 9f, 0), Vector3.one * 1.4f, Prim.Hex("#FFD84A"), false, new Vector3(45, 0, 45));
            ring.AddComponent<Spinner>();
            beacon.SetActive(false);
            q.beacon = beacon;
        }
    }
}
