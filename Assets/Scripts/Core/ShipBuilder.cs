using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Construit le vaisseau « Aurore » uniquement avec des primitives : coursive centrale, quartiers,
    /// laboratoire, serre botanique, salle des machines, station de tir, passerelle et hangar à chasseurs.
    /// Puis le joueur, l'équipage (PNJ) et la quête.
    ///
    /// Plan (vu de dessus, nord = +Z) :
    ///                    [ Passerelle ]
    ///                          |
    ///   [ Hangar ]==== coursive transversale
    ///                          |
    ///   [ Machines ]  coursive  [ Station de tir ]
    ///   [ Labo     ]  centrale  [ Botanique      ]
    ///                          |
    ///                    [ Quartiers ]
    /// </summary>
    public static class ShipBuilder
    {
        public const string LevelName = "Vaisseau";
        public const string PlayerId = "Pilote";

        const float WallH = 4f;
        const float WallT = 0.4f;
        const float DoorW = 2.4f;
        const float DoorH = 3f;

        // Points d'apparition par défaut (x, y, z, orientation Y). Dans une scène où le décor a été généré
        // (menu Vaisseau RPG), ce sont des objets SpawnMarker que l'on peut déplacer librement.
        static readonly Dictionary<string, Vector4> DefaultSpawns = new Dictionary<string, Vector4>
        {
            { PlayerId,            new Vector4(0f, 0f, -33.5f, 0f) },
            { "Commandant Vega",   new Vector4(-2.5f, 0f, 46.5f, 180f) },
            { "Lt. Okoro",         new Vector4(5f, 0f, 51.3f, 0f) },
            { "Enseigne Sato",     new Vector4(-5f, 0f, 51.3f, 0f) },
            { "Sergent Kova",      new Vector4(5f, 0f, 11.5f, 90f) },
            { "Canonnier Mills",   new Vector4(5f, 0f, -2.2f, 180f) },
            { "Dr Maris",          new Vector4(-11.8f, 0f, -16f, 90f) },
            { "Théo",              new Vector4(-12f, 0f, -21f, 0f) },
            { "Ama",               new Vector4(6.8f, 0f, -16f, 90f) },
            { "Pico",              new Vector4(13.5f, 0f, -16f, 0f) },
            { "Rhea",              new Vector4(-5.5f, 0f, 6.5f, -90f) },
            { "Bram",              new Vector4(-12.5f, 0f, -1.5f, 0f) },
            { "Chef Doyle",        new Vector4(-17.5f, 0f, 25f, -90f) },
            { "Nyx",               new Vector4(-24f, 0f, 16.5f, 90f) },
            { "Enseigne Lou",      new Vector4(-1.2f, 0f, -6f, 90f) },
        };

        // Palette.
        static readonly Color FloorColor = Prim.Hex("#3A3F47");
        static readonly Color FloorAlt = Prim.Hex("#2F343B");
        static readonly Color WallColor = Prim.Hex("#8C949E");
        static readonly Color Trim = Prim.Hex("#2E3238");
        static readonly Color Metal = Prim.Hex("#6E7681");
        static readonly Color White = Prim.Hex("#E4E8EC");
        static readonly Color Cyan = Prim.Hex("#35D0FF");
        static readonly Color Orange = Prim.Hex("#FF9A3A");
        static readonly Color Purple = Prim.Hex("#B45CFF");
        static readonly Color Green = Prim.Hex("#5BD66B");
        static readonly Color Yellow = Prim.Hex("#F2C230");
        static readonly Color RedGlow = Prim.Hex("#FF4040");

        // Couleurs de département (épaulettes et ceinture).
        static readonly Color Command = Prim.Hex("#E3B341");
        static readonly Color Security = Prim.Hex("#D8433A");
        static readonly Color Science = Prim.Hex("#3E8EDE");
        static readonly Color Engineering = Prim.Hex("#F08A24");
        static readonly Color Botany = Prim.Hex("#4CB050");

        static Transform world;
        static Transform gameplay;
        static System.Random rng;
        static readonly Dictionary<string, Transform> markers = new Dictionary<string, Transform>();

        static float Rand(float min, float max) { return min + (float)rng.NextDouble() * (max - min); }

        // ================================================================== points d'entrée

        /// <summary>Lancement du jeu : décor (s'il n'est pas déjà dans la scène), joueur, équipage et quête.</summary>
        public static void Build()
        {
            if (!LevelRoot.Exists(LevelName))
            {
                foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) l.enabled = false;
                BuildEnvironment();
            }

            rng = new System.Random(5678);
            markers.Clear();
            foreach (var m in Object.FindObjectsByType<SpawnMarker>(FindObjectsSortMode.None))
                markers[m.id] = m.transform;
            gameplay = new GameObject("Gameplay").transform;

            var player = BuildPlayer();
            SetupCamera(player.transform);
            BuildCrew();
        }

        /// <summary>
        /// Construit uniquement le décor (lumières, salles, portes, mobilier, chasseurs, points d'apparition).
        /// Appelé au lancement, ou dans l'éditeur par le menu « Vaisseau RPG > Générer le décor dans la scène ».
        /// </summary>
        public static GameObject BuildEnvironment()
        {
            rng = new System.Random(1234);
            world = new GameObject("Vaisseau").transform;
            world.gameObject.AddComponent<LevelRoot>().level = LevelName;

            SetupLighting();
            BuildStructure();
            BuildCorridor();
            BuildQuarters();
            BuildLab();
            BuildBotany();
            BuildEngineRoom();
            BuildGunnery();
            BuildBridge();
            BuildHangar();
            BuildSpace();

            var spawns = new GameObject("Points d'apparition").transform;
            spawns.SetParent(world);
            foreach (var kv in DefaultSpawns)
            {
                var go = new GameObject("Spawn - " + kv.Key);
                go.transform.SetParent(spawns, true);
                go.transform.position = new Vector3(kv.Value.x, kv.Value.y, kv.Value.z);
                go.transform.rotation = Quaternion.Euler(0, kv.Value.w, 0);
                go.AddComponent<SpawnMarker>().id = kv.Key;
            }
            return world.gameObject;
        }

        static void Spawn(string id, out Vector3 pos, out Quaternion rot)
        {
            Transform t;
            if (markers.TryGetValue(id, out t) && t != null)
            {
                pos = t.position;
                rot = Quaternion.Euler(0, t.eulerAngles.y, 0);
                return;
            }
            var d = DefaultSpawns[id];
            pos = new Vector3(d.x, d.y, d.z);
            rot = Quaternion.Euler(0, d.w, 0);
        }

        // ================================================================== ambiance

        static void SetupLighting()
        {
            var sun = new GameObject("Éclairage général").AddComponent<Light>();
            sun.transform.SetParent(world);
            sun.type = LightType.Directional;
            sun.color = new Color(0.85f, 0.9f, 1f);
            sun.intensity = 0.55f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(72f, 30f, 0f);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.33f, 0.4f);
            RenderSettings.fog = false;
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
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.03f);
            cam.farClipPlane = 700f;
            cam.fieldOfView = 55f;

            // Pas d'opérateur ?? avec les objets Unity (leur « null » est spécial).
            var tpc = cam.GetComponent<ThirdPersonCamera>();
            if (tpc == null) tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
            tpc.target = target;
            tpc.yaw = target.eulerAngles.y;
            tpc.pitch = 42f;
            tpc.distance = 9f;
            tpc.maxDistance = 15f;
            if (cam.GetComponent<CameraShake>() == null) cam.gameObject.AddComponent<CameraShake>();
            cam.transform.position = target.position + target.rotation * new Vector3(0, 7, -7);
        }

        // ================================================================== briques de construction

        struct Gap
        {
            public float at, width, top;
        }

        /// <summary>Ouverture dans un mur : position absolue (x ou z selon le mur), largeur, hauteur du linteau.</summary>
        static Gap G(float at, float width = DoorW, float top = DoorH)
        {
            return new Gap { at = at, width = width, top = top };
        }

        static Transform Group(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent != null ? parent : world);
            return t;
        }

        static void Floor(Transform parent, float x0, float z0, float x1, float z1, Color color)
        {
            Prim.Make(PrimitiveType.Cube, "Floor", parent, new Vector3((x0 + x1) / 2f, -0.1f, (z0 + z1) / 2f), new Vector3(x1 - x0, 0.2f, z1 - z0), color);
        }

        /// <summary>Mur le long de l'axe X (z constant), de x0 à x1.</summary>
        static void WallX(Transform parent, float z, float x0, float x1, float height, params Gap[] gaps)
        {
            var g = new Gap[gaps.Length];
            for (int i = 0; i < gaps.Length; i++) { g[i] = gaps[i]; g[i].at -= x0; }
            Wall(parent, new Vector3(x0, 0, z), new Vector3(x1, 0, z), height, g);
        }

        /// <summary>Mur le long de l'axe Z (x constant), de z0 à z1.</summary>
        static void WallZ(Transform parent, float x, float z0, float z1, float height, params Gap[] gaps)
        {
            var g = new Gap[gaps.Length];
            for (int i = 0; i < gaps.Length; i++) { g[i] = gaps[i]; g[i].at -= z0; }
            Wall(parent, new Vector3(x, 0, z0), new Vector3(x, 0, z1), height, g);
        }

        static void Wall(Transform parent, Vector3 a, Vector3 b, float height, Gap[] gaps)
        {
            var dir = b - a;
            float len = dir.magnitude;
            dir /= len;
            var rot = Quaternion.LookRotation(dir);
            System.Array.Sort(gaps, (p, q) => p.at.CompareTo(q.at));

            float cursor = 0f;
            foreach (var gap in gaps)
            {
                float g0 = gap.at - gap.width / 2f, g1 = gap.at + gap.width / 2f;
                WallSegment(parent, a, dir, rot, cursor, g0, 0f, height);
                if (gap.top < height) WallSegment(parent, a, dir, rot, g0, g1, gap.top, height);
                cursor = g1;
            }
            WallSegment(parent, a, dir, rot, cursor, len, 0f, height);
        }

        static void WallSegment(Transform parent, Vector3 a, Vector3 dir, Quaternion rot, float s0, float s1, float y0, float y1)
        {
            float len = s1 - s0;
            if (len < 0.01f || y1 - y0 < 0.01f) return;
            var mid = a + dir * ((s0 + s1) / 2f);
            var seg = Prim.Make(PrimitiveType.Cube, "Wall", parent, mid + Vector3.up * ((y0 + y1) / 2f), new Vector3(WallT, y1 - y0, len), WallColor);
            seg.transform.rotation = rot;

            if (y0 <= 0f)
            {
                // Plinthe sombre et bandeau lumineux.
                var bb = Prim.Make(PrimitiveType.Cube, "Baseboard", parent, mid + Vector3.up * 0.15f, new Vector3(WallT + 0.06f, 0.3f, len), Trim, false);
                bb.transform.rotation = rot;
                if (y1 < 3f) return;
                var strip = Prim.Make(PrimitiveType.Cube, "LightStrip", parent, mid + Vector3.up * 2.75f, new Vector3(WallT + 0.05f, 0.07f, len), Cyan * 0.8f, false, null, true);
                strip.transform.rotation = rot;
            }
        }

        /// <summary>Porte coulissante. <paramref name="alongX"/> : la porte est dans un mur orienté selon X.</summary>
        static SlidingDoor Door(Transform parent, Vector3 center, bool alongX, string room, bool locked = false, bool mission = false, string lockedMessage = "")
        {
            var root = new GameObject("Porte - " + room).transform;
            root.SetParent(parent);
            root.position = center;
            root.rotation = Quaternion.Euler(0, alongX ? 0f : 90f, 0f);

            var lights = new List<Renderer>();
            for (int side = -1; side <= 1; side += 2)
            {
                Prim.Make(PrimitiveType.Cube, "Frame", root, new Vector3(side * (DoorW / 2f + 0.12f), WallH / 2f, 0), new Vector3(0.28f, WallH, WallT + 0.2f), Trim);
                for (int face = -1; face <= 1; face += 2)
                    lights.Add(Prim.Make(PrimitiveType.Cube, "Status", root, new Vector3(side * (DoorW / 2f + 0.12f), 1.6f, face * (WallT / 2f + 0.11f)),
                        new Vector3(0.12f, 0.35f, 0.03f), Green, false, null, true).GetComponent<Renderer>());
            }
            Prim.Make(PrimitiveType.Cube, "Header", root, new Vector3(0, DoorH + 0.1f, 0), new Vector3(DoorW + 0.5f, 0.2f, WallT + 0.2f), Trim, false);

            var left = new GameObject("Left").transform;
            left.SetParent(root, false);
            left.localPosition = new Vector3(-DoorW / 4f, 0, 0);
            var right = new GameObject("Right").transform;
            right.SetParent(root, false);
            right.localPosition = new Vector3(DoorW / 4f, 0, 0);
            foreach (var panel in new[] { left, right })
            {
                float inner = panel == left ? 1f : -1f;
                Prim.Make(PrimitiveType.Cube, "Panel", panel, new Vector3(0, DoorH / 2f, 0), new Vector3(DoorW / 2f, DoorH, 0.16f), Prim.Hex("#B8C0CA"));
                Prim.Make(PrimitiveType.Cube, "Edge", panel, new Vector3(inner * (DoorW / 4f - 0.05f), DoorH / 2f, 0), new Vector3(0.06f, DoorH * 0.85f, 0.18f), Cyan, false, null, true);
                Prim.Make(PrimitiveType.Cube, "Band", panel, new Vector3(0, DoorH * 0.55f, 0), new Vector3(DoorW / 2f - 0.1f, 0.12f, 0.18f), Trim, false);
            }

            var door = root.gameObject.AddComponent<SlidingDoor>();
            door.roomName = room;
            door.left = left;
            door.right = right;
            door.width = DoorW;
            door.statusLights = lights.ToArray();
            door.locked = locked;
            door.unlockedByMission = mission;
            if (lockedMessage != "") door.lockedMessage = lockedMessage;
            door.interactRange = 2.6f;
            return door;
        }

        static Light PointLight(Transform parent, Vector3 pos, Color color, float range, float intensity)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            return l;
        }

        /// <summary>Console : un bureau et un écran lumineux incliné.</summary>
        static void Console(Transform parent, Vector3 pos, float rotY, float width, Color screen)
        {
            var c = Group("Console", parent);
            c.position = pos;
            c.rotation = Quaternion.Euler(0, rotY, 0);
            Prim.Make(PrimitiveType.Cube, "Desk", c, new Vector3(0, 0.5f, 0), new Vector3(width, 1f, 0.9f), Metal);
            Prim.Make(PrimitiveType.Cube, "Top", c, new Vector3(0, 1.02f, 0.05f), new Vector3(width, 0.05f, 0.95f), Trim, false);
            Prim.Make(PrimitiveType.Cube, "Screen", c, new Vector3(0, 1.35f, -0.3f), new Vector3(width * 0.85f, 0.6f, 0.05f), screen, false, new Vector3(-15, 0, 0), true);
            for (int i = 0; i < 4; i++)
                Prim.Make(PrimitiveType.Cube, "Button", c, new Vector3(-width * 0.3f + i * width * 0.2f, 1.07f, 0.25f), new Vector3(0.12f, 0.03f, 0.12f),
                    i % 2 == 0 ? Yellow : RedGlow, false, null, true);
        }

        static void Crate(Transform parent, Vector3 pos, float size, float rotY)
        {
            Prim.Make(PrimitiveType.Cube, "Crate", parent, pos + Vector3.up * size / 2f, Vector3.one * size, Prim.Hex("#5F6B5A"), true, new Vector3(0, rotY, 0));
            Prim.Make(PrimitiveType.Cube, "CrateBand", parent, pos + Vector3.up * size / 2f, new Vector3(size + 0.02f, size * 0.15f, size + 0.02f), Yellow, false, new Vector3(0, rotY, 0));
        }

        // ================================================================== structure

        static void BuildStructure()
        {
            var s = Group("Structure");

            // Sols.
            Floor(s, -2.5f, -30f, 2.5f, 40f, FloorAlt);     // coursive centrale
            Floor(s, -8f, -42f, 8f, -30f, FloorColor);           // quartiers
            Floor(s, -16f, -24f, -2.5f, -8f, Prim.Hex("#D5DBE0")); // labo (sol clair)
            Floor(s, 2.5f, -24f, 16f, -8f, Prim.Hex("#3E4A3C"));   // botanique
            Floor(s, -16f, -4f, -2.5f, 12f, Prim.Hex("#2D2A27"));  // machines
            Floor(s, 2.5f, -4f, 18f, 14f, FloorColor);           // station de tir
            Floor(s, -14f, 18f, -2.5f, 23f, FloorAlt);      // coursive transversale
            Floor(s, -52f, 6f, -14f, 36f, Prim.Hex("#33373D")); // hangar
            Floor(s, -12f, 40f, 12f, 56f, Prim.Hex("#232A36")); // passerelle

            // Coursive centrale.
            WallZ(s, -2.5f, -30f, 40f, WallH, G(-16f), G(4f), G(20.5f, 5f, WallH));
            WallZ(s, 2.5f, -30f, 40f, WallH, G(-16f), G(5f));
            // Quartiers.
            WallX(s, -30f, -8f, 8f, WallH, G(0f));
            WallZ(s, -8f, -42f, -30f, WallH);
            WallZ(s, 8f, -42f, -30f, WallH);
            WallX(s, -42f, -8f, 8f, WallH);
            // Laboratoire.
            WallX(s, -8f, -16f, -2.5f, WallH);
            WallX(s, -24f, -16f, -2.5f, WallH);
            WallZ(s, -16f, -24f, -8f, WallH);
            // Botanique.
            WallX(s, -8f, 2.5f, 16f, WallH);
            WallX(s, -24f, 2.5f, 16f, WallH);
            WallZ(s, 16f, -24f, -8f, WallH);
            // Salle des machines.
            WallX(s, 12f, -16f, -2.5f, WallH);
            WallX(s, -4f, -16f, -2.5f, WallH);
            WallZ(s, -16f, -4f, 12f, WallH);
            // Station de tir.
            WallX(s, 14f, 2.5f, 18f, WallH);
            WallX(s, -4f, 2.5f, 18f, WallH);
            WallZ(s, 18f, -4f, 14f, WallH);
            // Coursive transversale.
            WallX(s, 18f, -14f, -2.5f, WallH);
            WallX(s, 23f, -14f, -2.5f, WallH);
            // Hangar (plus haut), grande ouverture sur l'espace à l'ouest.
            WallZ(s, -14f, 6f, 36f, 9f, G(20.5f));
            WallX(s, 36f, -52f, -14f, 9f);
            WallX(s, 6f, -52f, -14f, 9f);
            WallZ(s, -52f, 6f, 36f, 9f, G(21f, 26f, 8f));
            // Passerelle (la baie vitrée est construite avec la salle).
            WallX(s, 40f, -12f, 12f, WallH, G(0f));
            WallZ(s, -12f, 40f, 56f, WallH);
            WallZ(s, 12f, 40f, 56f, WallH);

            // Portes.
            var doors = Group("Portes", s);
            Door(doors, new Vector3(0f, 0f, -30f), true, "Quartiers d'équipage");
            Door(doors, new Vector3(-2.5f, 0f, -16f), false, "Laboratoire");
            Door(doors, new Vector3(2.5f, 0f, -16f), false, "Botanique");
            Door(doors, new Vector3(-2.5f, 0f, 4f), false, "Salle des machines");
            Door(doors, new Vector3(2.5f, 0f, 5f), false, "Station de tir");
            Door(doors, new Vector3(-14f, 0f, 20.5f), false, "Hangar", true, true, "Hangar verrouillé : autorisation du commandant requise.");
            Door(doors, new Vector3(0f, 0f, 40f), true, "Passerelle");
        }

        // ================================================================== salles

        static void BuildCorridor()
        {
            var c = Group("Coursive");
            // Bandes lumineuses au sol et lumières.
            Prim.Make(PrimitiveType.Cube, "FloorStrip", c, new Vector3(-2.05f, 0.01f, 5f), new Vector3(0.1f, 0.02f, 70f), Cyan * 0.7f, false, null, true);
            Prim.Make(PrimitiveType.Cube, "FloorStrip", c, new Vector3(2.05f, 0.01f, 5f), new Vector3(0.1f, 0.02f, 70f), Cyan * 0.7f, false, null, true);
            for (int i = 0; i < 14; i++)
                Prim.Make(PrimitiveType.Cube, "FloorPlate", c, new Vector3(0, 0.005f, -27.5f + i * 5f), new Vector3(3.6f, 0.01f, 0.08f), Trim, false);
            foreach (var z in new[] { -24f, -10f, 6f, 22f, 34f })
                PointLight(c, new Vector3(0, 3.5f, z), new Color(0.75f, 0.85f, 1f), 11f, 1.1f);
            PointLight(c, new Vector3(-8f, 3.5f, 20.5f), new Color(0.75f, 0.85f, 1f), 9f, 1f);

            // Robot de nettoyage qui patrouille dans la coursive.
            var bot = Group("Robot de nettoyage", c);
            bot.position = new Vector3(1.3f, 0f, -27f);
            Prim.Make(PrimitiveType.Cylinder, "Base", bot, new Vector3(0, 0.15f, 0), new Vector3(0.7f, 0.15f, 0.7f), White, false);
            Prim.Make(PrimitiveType.Sphere, "Dome", bot, new Vector3(0, 0.35f, 0), new Vector3(0.55f, 0.35f, 0.55f), Metal, false);
            Prim.Make(PrimitiveType.Sphere, "Eye", bot, new Vector3(0, 0.4f, 0.24f), Vector3.one * 0.12f, Cyan, false, null, true);
            var patrol = bot.gameObject.AddComponent<Patrol>();
            patrol.pointA = new Vector3(1.3f, 0f, -27f);
            patrol.pointB = new Vector3(1.3f, 0f, 37f);
            patrol.speed = 1.6f;
            patrol.pause = 2f;
        }

        static void BuildQuarters()
        {
            var q = Group("Quartiers d'équipage");
            PointLight(q, new Vector3(0, 3.5f, -36f), new Color(1f, 0.85f, 0.65f), 12f, 1.2f);

            foreach (var x in new[] { -6.6f, 6.6f })
                foreach (var z in new[] { -40f, -36.5f })
                {
                    var b = Group("Couchette", q);
                    b.position = new Vector3(x, 0, z);
                    for (int lvl = 0; lvl < 2; lvl++)
                    {
                        float y = lvl * 1.4f;
                        Prim.Make(PrimitiveType.Cube, "Frame", b, new Vector3(0, y + 0.3f, 0), new Vector3(1.3f, 0.25f, 2.3f), Metal);
                        Prim.Make(PrimitiveType.Cube, "Mattress", b, new Vector3(0, y + 0.5f, 0), new Vector3(1.15f, 0.18f, 2.1f), Prim.Hex("#4A5D7A"), false);
                        Prim.Make(PrimitiveType.Cube, "Pillow", b, new Vector3(0, y + 0.65f, 0.75f), new Vector3(0.8f, 0.14f, 0.4f), White, false);
                    }
                    foreach (var pz in new[] { -1.1f, 1.1f })
                        Prim.Make(PrimitiveType.Cube, "Post", b, new Vector3(x > 0 ? -0.6f : 0.6f, 1.1f, pz), new Vector3(0.08f, 2.2f, 0.08f), Trim, false);
                }

            for (int i = 0; i < 6; i++)
            {
                float x = -3.75f + i * 1.5f;
                Prim.Make(PrimitiveType.Cube, "Locker", q, new Vector3(x, 1.1f, -41.4f), new Vector3(1.3f, 2.2f, 0.7f), Prim.Hex("#7C8A99"));
                Prim.Make(PrimitiveType.Cube, "LockerLight", q, new Vector3(x + 0.45f, 1.3f, -41.04f), new Vector3(0.06f, 0.3f, 0.02f), i % 3 == 0 ? Orange : Green, false, null, true);
            }

            Prim.Make(PrimitiveType.Cylinder, "TableLeg", q, new Vector3(0, 0.45f, -37f), new Vector3(0.25f, 0.45f, 0.25f), Trim);
            Prim.Make(PrimitiveType.Cube, "Table", q, new Vector3(0, 0.92f, -37f), new Vector3(2.2f, 0.08f, 1.3f), White);
            foreach (var p in new[] { new Vector3(-1.5f, 0, -37f), new Vector3(1.5f, 0, -37f) })
                Prim.Make(PrimitiveType.Cylinder, "Stool", q, p + Vector3.up * 0.3f, new Vector3(0.5f, 0.3f, 0.5f), Metal);
            Prim.Make(PrimitiveType.Cylinder, "Mug", q, new Vector3(0.5f, 1.03f, -37.2f), new Vector3(0.14f, 0.08f, 0.14f), Orange, false);
        }

        static void BuildLab()
        {
            var l = Group("Laboratoire");
            PointLight(l, new Vector3(-9.25f, 3.5f, -16f), new Color(0.85f, 0.95f, 1f), 13f, 1.3f);

            // Paillasses le long des murs.
            Prim.Make(PrimitiveType.Cube, "BenchW", l, new Vector3(-15.15f, 0.5f, -16f), new Vector3(1.3f, 1f, 13f), White);
            Prim.Make(PrimitiveType.Cube, "BenchN", l, new Vector3(-9.5f, 0.5f, -8.85f), new Vector3(10f, 1f, 1.3f), White);
            for (int i = 0; i < 4; i++)
                Prim.Make(PrimitiveType.Cube, "Monitor", l, new Vector3(-15.5f, 1.45f, -21f + i * 3.3f), new Vector3(0.06f, 0.7f, 1.1f), Cyan, false, null, true);

            // Éprouvettes colorées.
            var colors = new[] { Purple, Green, Orange, Cyan, RedGlow };
            for (int i = 0; i < 14; i++)
                Prim.Make(PrimitiveType.Cylinder, "TestTube", l, new Vector3(-13f + i * 0.5f, 1.2f, -8.9f), new Vector3(0.12f, 0.2f, 0.12f), colors[i % colors.Length], false, null, true);
            // Microscope.
            Prim.Make(PrimitiveType.Cylinder, "MicroBase", l, new Vector3(-15f, 1.05f, -12.5f), new Vector3(0.5f, 0.05f, 0.5f), Trim, false);
            Prim.Make(PrimitiveType.Cylinder, "MicroTube", l, new Vector3(-15f, 1.4f, -12.5f), new Vector3(0.15f, 0.35f, 0.15f), Metal, false, new Vector3(20, 0, 0));

            // Cellule de confinement avec le cristal mystérieux.
            var pod = Group("Cellule de confinement", l);
            pod.position = new Vector3(-9.25f, 0, -16f);
            Prim.Make(PrimitiveType.Cylinder, "Base", pod, new Vector3(0, 0.25f, 0), new Vector3(2.6f, 0.25f, 2.6f), Trim);
            Prim.Make(PrimitiveType.Cylinder, "RingLow", pod, new Vector3(0, 0.6f, 0), new Vector3(2.2f, 0.06f, 2.2f), Cyan, false, null, true);
            Prim.Make(PrimitiveType.Cylinder, "RingHigh", pod, new Vector3(0, 2.9f, 0), new Vector3(2.2f, 0.06f, 2.2f), Cyan, false, null, true);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2f + Mathf.PI / 4f;
                Prim.Make(PrimitiveType.Cylinder, "Pillar", pod, new Vector3(Mathf.Cos(a) * 1.05f, 1.75f, Mathf.Sin(a) * 1.05f), new Vector3(0.1f, 1.15f, 0.1f), Metal, false);
            }
            var crystal = Prim.Make(PrimitiveType.Cube, "Crystal", pod, new Vector3(0, 1.75f, 0), new Vector3(0.6f, 0.9f, 0.6f), Purple, false, new Vector3(45, 0, 45), true);
            crystal.AddComponent<Spinner>().speed = 35f;
            var glow = PointLight(pod, pod.position + Vector3.up * 1.75f, Purple, 6f, 1.5f);
            var fl = pod.gameObject.AddComponent<Flicker>();
            fl.lightSource = glow;
            fl.flames = new Transform[0];
            fl.baseIntensity = 1.5f;
        }

        static void BuildBotany()
        {
            var b = Group("Botanique");
            PointLight(b, new Vector3(9.25f, 3.5f, -16f), new Color(1f, 0.95f, 0.75f), 13f, 1.2f);

            foreach (var z in new[] { -21.2f, -10.8f })
            {
                Prim.Make(PrimitiveType.Cube, "Planter", b, new Vector3(9.5f, 0.4f, z), new Vector3(10f, 0.8f, 1.5f), Prim.Hex("#6B4F35"));
                Prim.Make(PrimitiveType.Cube, "Soil", b, new Vector3(9.5f, 0.82f, z), new Vector3(9.7f, 0.05f, 1.3f), Prim.Hex("#3A2A1C"), false);
                Prim.Make(PrimitiveType.Cube, "UVLamp", b, new Vector3(9.5f, 3f, z), new Vector3(9.5f, 0.08f, 0.25f), Purple, false, null, true);
                for (int i = 0; i < 9; i++)
                {
                    var p = new Vector3(5.5f + i * 1f, 0.85f, z + Rand(-0.3f, 0.3f));
                    float h = Rand(0.3f, 0.9f);
                    Prim.Make(PrimitiveType.Cylinder, "Stem", b, p + Vector3.up * h / 2f, new Vector3(0.06f, h / 2f, 0.06f), Prim.Hex("#3D7A2E"), false);
                    var leaf = i % 3 == 0 ? Prim.Hex("#7CC24A") : Prim.Hex("#3F9E3A");
                    Prim.Make(PrimitiveType.Sphere, "Leaves", b, p + Vector3.up * h, Vector3.one * Rand(0.45f, 0.75f), leaf, false);
                    if (i % 4 == 1)
                        Prim.Make(PrimitiveType.Sphere, "Tomato", b, p + new Vector3(0.2f, h - 0.1f, 0.15f), Vector3.one * 0.16f, Prim.Hex("#E0402A"), false);
                }
            }
            PointLight(b, new Vector3(9.5f, 2.6f, -21.2f), Purple, 5f, 1.2f);
            PointLight(b, new Vector3(9.5f, 2.6f, -10.8f), Purple, 5f, 1.2f);

            // Arbre central dans un grand bac.
            var tree = Group("Arbre", b);
            tree.position = new Vector3(10f, 0, -16f);
            Prim.Make(PrimitiveType.Cylinder, "Pot", tree, new Vector3(0, 0.4f, 0), new Vector3(2.4f, 0.4f, 2.4f), Prim.Hex("#6B4F35"));
            Prim.Make(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0, 1.6f, 0), new Vector3(0.35f, 1f, 0.35f), Prim.Hex("#6A4A2A"), false);
            Prim.Make(PrimitiveType.Sphere, "Crown", tree, new Vector3(0, 3f, 0), new Vector3(2.4f, 1.8f, 2.4f), Prim.Hex("#3F9E3A"), false);
            Prim.Make(PrimitiveType.Sphere, "Crown2", tree, new Vector3(0.5f, 3.5f, 0.3f), new Vector3(1.4f, 1.1f, 1.4f), Prim.Hex("#7CC24A"), false);

            // Réservoir d'eau.
            Prim.Make(PrimitiveType.Cylinder, "WaterTank", b, new Vector3(14.6f, 1.5f, -22.6f), new Vector3(1.8f, 1.5f, 1.8f), Prim.Hex("#3A78B5"));
            Prim.Make(PrimitiveType.Cylinder, "Pipe", b, new Vector3(14.6f, 3.2f, -16f), new Vector3(0.2f, 6f, 0.2f), Metal, false, new Vector3(90, 0, 0));
        }

        static void BuildEngineRoom()
        {
            var e = Group("Salle des machines");
            PointLight(e, new Vector3(-5f, 3.5f, 10f), new Color(1f, 0.85f, 0.7f), 10f, 0.8f);

            // Réacteur à fusion.
            var r = Group("Réacteur", e);
            r.position = new Vector3(-9.25f, 0, 4f);
            Prim.Make(PrimitiveType.Cylinder, "Base", r, new Vector3(0, 0.3f, 0), new Vector3(4f, 0.3f, 4f), Trim);
            var core = Prim.Make(PrimitiveType.Cylinder, "Core", r, new Vector3(0, 2.1f, 0), new Vector3(1.4f, 1.6f, 1.4f), Orange, false, null, true);
            foreach (var y in new[] { 1.1f, 2.1f, 3.1f })
                Prim.Make(PrimitiveType.Cylinder, "Ring", r, new Vector3(0, y, 0), new Vector3(2.6f, 0.1f, 2.6f), Metal);
            Prim.Make(PrimitiveType.Cylinder, "Cap", r, new Vector3(0, 3.8f, 0), new Vector3(2.4f, 0.2f, 2.4f), Trim, false);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2f;
                Prim.Make(PrimitiveType.Cube, "Strut", r, new Vector3(Mathf.Cos(a) * 1.4f, 2f, Mathf.Sin(a) * 1.4f), new Vector3(0.2f, 3.6f, 0.2f), Metal, false);
            }
            var light = PointLight(r, r.position + Vector3.up * 2f, Orange, 14f, 2.5f);
            var fl = r.gameObject.AddComponent<Flicker>();
            fl.lightSource = light;
            fl.flames = new[] { core.transform };
            fl.baseIntensity = 2.5f;

            // Conduits le long du mur ouest.
            foreach (var y in new[] { 0.8f, 1.6f, 2.4f })
                Prim.Make(PrimitiveType.Cylinder, "Conduit", e, new Vector3(-15.5f, y, 4f), new Vector3(0.35f, 7.8f, 0.35f), y > 2f ? Orange : Metal, false, new Vector3(90, 0, 0));
            // Générateurs.
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3(-13.5f + i * 3.2f, 0, 11f);
                Prim.Make(PrimitiveType.Cube, "Generator", e, p + Vector3.up, new Vector3(2.4f, 2f, 1.3f), Metal);
                Prim.Make(PrimitiveType.Cube, "GenLight", e, p + new Vector3(0, 1.4f, -0.66f), new Vector3(1.8f, 0.08f, 0.02f), Orange, false, null, true);
            }
            Console(e, new Vector3(-4f, 0, -2.8f), 180f, 2.4f, Orange);
            Prim.Make(PrimitiveType.Cube, "Hazard", e, new Vector3(-9.25f, 0.01f, 4f), new Vector3(5.2f, 0.01f, 5.2f), Yellow, false);
        }

        static void BuildGunnery()
        {
            var g = Group("Station de tir");
            PointLight(g, new Vector3(5f, 3.5f, 5f), new Color(1f, 0.8f, 0.8f), 10f, 1.1f);
            PointLight(g, new Vector3(14f, 3.5f, 5f), new Color(1f, 0.95f, 0.9f), 10f, 1.3f);

            // Comptoir de la ligne de tir (empêche d'entrer dans le stand).
            Prim.Make(PrimitiveType.Cube, "Counter", g, new Vector3(7.5f, 0.45f, 5f), new Vector3(0.7f, 0.9f, 17.6f), Metal);
            Prim.Make(PrimitiveType.Cube, "CounterGlow", g, new Vector3(7.5f, 0.92f, 5f), new Vector3(0.72f, 0.04f, 17.6f), RedGlow, false, null, true);
            for (int i = 0; i < 4; i++)
                Prim.Make(PrimitiveType.Cube, "LaneLine", g, new Vector3(12.5f, 0.01f, 1.75f + i * 2.5f), new Vector3(9.5f, 0.01f, 0.06f), Yellow, false);

            // Mur du fond avec cercles lumineux.
            for (int i = 0; i < 5; i++)
                Prim.Make(PrimitiveType.Cylinder, "BackRing", g, new Vector3(17.75f, 2.8f, 0.5f + i * 2.5f), new Vector3(0.6f, 0.02f, 0.6f), RedGlow, false, new Vector3(0, 0, 90), true);

            // Cibles.
            for (int i = 0; i < 5; i++)
            {
                var t = Group("Cible", g);
                t.position = new Vector3(16.3f, 0, 0.5f + i * 2.5f);
                t.rotation = Quaternion.Euler(0, -90f, 0);
                Prim.Make(PrimitiveType.Cylinder, "Pole", t, new Vector3(0, 0.6f, 0), new Vector3(0.1f, 0.6f, 0.1f), Trim);
                Prim.Make(PrimitiveType.Cylinder, "Disc", t, new Vector3(0, 1.45f, 0), new Vector3(1.1f, 0.04f, 1.1f), White, true, new Vector3(90, 0, 0));
                Prim.Make(PrimitiveType.Cylinder, "Ring", t, new Vector3(0, 1.45f, 0.03f), new Vector3(0.78f, 0.03f, 0.78f), Security, false, new Vector3(90, 0, 0));
                Prim.Make(PrimitiveType.Cylinder, "Inner", t, new Vector3(0, 1.45f, 0.05f), new Vector3(0.48f, 0.03f, 0.48f), White, false, new Vector3(90, 0, 0));
                Prim.Make(PrimitiveType.Cylinder, "Bull", t, new Vector3(0, 1.45f, 0.07f), new Vector3(0.2f, 0.03f, 0.2f), RedGlow, false, new Vector3(90, 0, 0), true);
                t.gameObject.AddComponent<Health>().Init("Cible", 30f);
                var st = t.gameObject.AddComponent<ShootingTarget>();
                if (i == 1 || i == 3)
                {
                    st.moveAmplitude = 0.9f;
                    st.moveSpeed = 1.2f + i * 0.2f;
                }
            }

            // Postes de commande des tourelles.
            Console(g, new Vector3(5f, 0, -3.3f), 180f, 3f, RedGlow);
            Console(g, new Vector3(3.4f, 0, 12.6f), 0f, 1.6f, Cyan);
            Prim.Make(PrimitiveType.Cube, "TurretWindow", g, new Vector3(5f, 2.5f, -3.75f), new Vector3(3.5f, 1.2f, 0.05f), Prim.Hex("#0A1530"), false);
        }

        static void BuildBridge()
        {
            var b = Group("Passerelle");
            PointLight(b, new Vector3(0, 3.5f, 47f), new Color(0.6f, 0.75f, 1f), 16f, 1.4f);

            // Baie vitrée : muret, montants et linteau ; l'espace est visible au-delà.
            WallX(b, 56f, -12f, 12f, 1.1f);
            foreach (var x in new[] { -12f, -6f, 0f, 6f, 12f })
                Prim.Make(PrimitiveType.Cube, "WindowPillar", b, new Vector3(x, WallH / 2f, 56f), new Vector3(0.5f, WallH, 0.5f), Trim);
            Prim.Make(PrimitiveType.Cube, "WindowTop", b, new Vector3(0, WallH - 0.2f, 56f), new Vector3(24.4f, 0.4f, 0.5f), Trim, false);
            Prim.Make(PrimitiveType.Cube, "WindowGlow", b, new Vector3(0, 1.12f, 56f), new Vector3(24f, 0.04f, 0.45f), Cyan, false, null, true);

            // Fauteuil du commandant.
            var chair = Group("Fauteuil", b);
            chair.position = new Vector3(0, 0, 49.5f);
            Prim.Make(PrimitiveType.Cylinder, "Foot", chair, new Vector3(0, 0.25f, 0), new Vector3(0.5f, 0.25f, 0.5f), Trim);
            Prim.Make(PrimitiveType.Cube, "Seat", chair, new Vector3(0, 0.6f, 0), new Vector3(1.1f, 0.25f, 1f), Prim.Hex("#2B3A55"));
            Prim.Make(PrimitiveType.Cube, "Back", chair, new Vector3(0, 1.2f, -0.45f), new Vector3(1.1f, 1.1f, 0.2f), Prim.Hex("#2B3A55"), false);
            Prim.Make(PrimitiveType.Cube, "ArmL", chair, new Vector3(-0.6f, 0.85f, 0), new Vector3(0.15f, 0.1f, 0.9f), Command, false);
            Prim.Make(PrimitiveType.Cube, "ArmR", chair, new Vector3(0.6f, 0.85f, 0), new Vector3(0.15f, 0.1f, 0.9f), Command, false);

            // Table holographique avec une planète qui tourne.
            var holo = Group("Table holographique", b);
            holo.position = new Vector3(0, 0, 44.5f);
            Prim.Make(PrimitiveType.Cylinder, "Table", holo, new Vector3(0, 0.5f, 0), new Vector3(2.2f, 0.5f, 2.2f), Trim);
            Prim.Make(PrimitiveType.Cylinder, "Top", holo, new Vector3(0, 1.02f, 0), new Vector3(2f, 0.02f, 2f), Cyan, false, null, true);
            var planet = Group("Hologramme", holo);
            planet.localPosition = new Vector3(0, 1.9f, 0);
            Prim.Make(PrimitiveType.Sphere, "Planet", planet, Vector3.zero, Vector3.one * 0.8f, Prim.Hex("#4FD2C8"), false, null, true);
            Prim.Make(PrimitiveType.Cylinder, "Orbit", planet, Vector3.zero, new Vector3(1.6f, 0.005f, 1.6f), Cyan * 0.6f, false, new Vector3(18, 0, 0), true);
            Prim.Make(PrimitiveType.Sphere, "Moon", planet, new Vector3(0.8f, 0.2f, 0), Vector3.one * 0.18f, White, false, null, true);
            planet.gameObject.AddComponent<Spinner>().speed = 25f;

            // Consoles de pilotage et de communication, et postes latéraux.
            Console(b, new Vector3(-5f, 0, 52.6f), 180f, 3f, Cyan);
            Console(b, new Vector3(5f, 0, 52.6f), 180f, 3f, Cyan);
            Console(b, new Vector3(-11f, 0, 46f), 90f, 3.5f, Green);
            Console(b, new Vector3(11f, 0, 46f), -90f, 3.5f, Orange);
            PointLight(b, new Vector3(0, 1.9f, 44.5f), Cyan, 5f, 1.2f);
        }

        static void BuildHangar()
        {
            var h = Group("Hangar");
            foreach (var z in new[] { 12f, 28f })
                foreach (var x in new[] { -42f, -24f })
                    PointLight(h, new Vector3(x, 8f, z), new Color(0.9f, 0.95f, 1f), 22f, 1.3f);

            // Champ de force : laisse passer les vaisseaux (qui ne sont pas soumis à la physique), pas les piétons.
            Prim.Make(PrimitiveType.Cube, "Champ de force", h, new Vector3(-52f, 4f, 21f), new Vector3(0.15f, 8f, 26f), Prim.Hex("#2A7FFF") * 0.7f, true, null, true);
            Prim.Make(PrimitiveType.Cube, "FieldEmitterTop", h, new Vector3(-51.7f, 8.05f, 21f), new Vector3(0.4f, 0.15f, 26f), Cyan, false, null, true);
            Prim.Make(PrimitiveType.Cube, "FieldEmitterLow", h, new Vector3(-51.7f, 0.05f, 21f), new Vector3(0.4f, 0.1f, 26f), Cyan, false, null, true);

            // Chasseurs alignés face à la sortie, avec leur aire de stationnement.
            var stripes = new[] { Prim.Hex("#D8433A"), Prim.Hex("#3E8EDE"), Prim.Hex("#E3B341") };
            var zs = new[] { 12f, 21f, 30f };
            for (int i = 0; i < 3; i++)
            {
                var pad = new Vector3(-32f, 0, zs[i]);
                Prim.Make(PrimitiveType.Cube, "PadN", h, pad + new Vector3(0, 0.01f, 3.6f), new Vector3(9f, 0.01f, 0.15f), Yellow, false);
                Prim.Make(PrimitiveType.Cube, "PadS", h, pad + new Vector3(0, 0.01f, -3.6f), new Vector3(9f, 0.01f, 0.15f), Yellow, false);
                Prim.Make(PrimitiveType.Cube, "PadE", h, pad + new Vector3(4.5f, 0.01f, 0), new Vector3(0.15f, 0.01f, 7.2f), Yellow, false);
                Prim.Make(PrimitiveType.Cube, "LaunchLine", h, new Vector3(-44f, 0.01f, zs[i]), new Vector3(14f, 0.01f, 0.2f), Cyan * 0.8f, false, null, true);

                var parts = FighterFactory.Build("Chasseur " + (i + 1), h, pad, -90f, stripes[i]);
                var col = parts.root.AddComponent<BoxCollider>();
                col.center = new Vector3(0, 1.6f, -0.2f);
                col.size = new Vector3(7.4f, 2.4f, 6.2f);
                var f = parts.root.AddComponent<Fighter>();
                f.canopyPivot = parts.canopyPivot;
                f.gear = parts.gear.ToArray();
                f.engineGlows = parts.engineGlows.ToArray();
                f.interactRange = 5f;
            }

            // Caisses, réservoirs de carburant, chariot d'outils.
            Crate(h, new Vector3(-17.5f, 0, 33.5f), 1.2f, 10f);
            Crate(h, new Vector3(-18.9f, 0, 33.8f), 1f, -5f);
            Crate(h, new Vector3(-18f, 1.2f, 33.6f), 0.9f, 30f);
            Crate(h, new Vector3(-48f, 0, 34f), 1.2f, 0f);
            for (int i = 0; i < 3; i++)
                Prim.Make(PrimitiveType.Cylinder, "FuelTank", h, new Vector3(-17f - i * 1.4f, 1f, 8f), new Vector3(1.1f, 1f, 1.1f), Prim.Hex("#B8322C"));
            Prim.Make(PrimitiveType.Cube, "ToolCart", h, new Vector3(-22f, 0.5f, 26.5f), new Vector3(1.2f, 1f, 0.7f), Prim.Hex("#C0632B"));
            Prim.Make(PrimitiveType.Cube, "Tools", h, new Vector3(-22f, 1.05f, 26.5f), new Vector3(1f, 0.1f, 0.5f), Metal, false);
            Console(h, new Vector3(-15f, 0, 32f), -90f, 2.4f, Cyan);
        }

        static void BuildSpace()
        {
            // Étoiles tout autour du vaisseau (visibles par-dessus les murs et par la baie vitrée).
            var sky = Group("Espace");
            var center = new Vector3(-18f, 0f, 7f);
            for (int i = 0; i < 450; i++)
            {
                var dir = new Vector3(Rand(-1f, 1f), Rand(-1f, 1f), Rand(-1f, 1f));
                if (dir.sqrMagnitude < 0.01f) continue;
                dir.Normalize();
                if (dir.y > 0.35f) dir.y *= 0.3f; // peu d'étoiles au zénith pour ne pas encombrer la vue du dessus
                var p = center + dir.normalized * Rand(140f, 260f);
                float s = Rand(0.4f, 1.3f);
                var c = rng.NextDouble() < 0.15 ? Prim.Hex("#9FC4FF") : (rng.NextDouble() < 0.1 ? Prim.Hex("#FFD9A0") : Color.white);
                Prim.Make(PrimitiveType.Sphere, "Star", sky, p, Vector3.one * s, c, false, null, true);
            }
            // Une planète visible depuis la passerelle.
            Prim.Make(PrimitiveType.Sphere, "Planète", sky, new Vector3(60f, -40f, 320f), Vector3.one * 150f, Prim.Hex("#2E6FA8"), false);
            Prim.Make(PrimitiveType.Sphere, "Lune", sky, new Vector3(-90f, 10f, 260f), Vector3.one * 25f, Prim.Hex("#9A9A9A"), false);
        }

        // ================================================================== joueur et équipage

        static GameObject BuildPlayer()
        {
            Vector3 pos; Quaternion rot;
            Spawn(PlayerId, out pos, out rot);
            // Combinaison de pilote orange.
            var parts = CharacterFactory.Build("Pilote", pos, Prim.Hex("#D9772B"), 1f, true, Prim.Hex("#3A3F47"));
            parts.root.transform.SetParent(gameplay, true);
            parts.root.transform.rotation = rot;

            var go = parts.root;
            CharacterFactory.AddController(go);
            var pc = go.AddComponent<PlayerController>();
            pc.Visual = parts.visual;
            pc.SpawnPoint = pos;
            var shooter = go.AddComponent<PlayerShooter>();
            shooter.weaponPivot = parts.weaponPivot;
            shooter.muzzle = parts.muzzle;
            go.AddComponent<PlayerInteractor>();
            return go;
        }

        static NPC MakeNPC(string name, string role, Color body, Color accent, float size = 1f, float wander = 0f)
        {
            Vector3 pos; Quaternion rot;
            Spawn(name, out pos, out rot);
            var parts = CharacterFactory.Build(name, pos, body, size, false, accent);
            parts.root.transform.SetParent(gameplay, true);
            parts.root.transform.rotation = rot;
            var col = parts.root.AddComponent<CapsuleCollider>();
            col.height = 2f * size;
            col.radius = 0.45f * size;
            col.center = new Vector3(0, size, 0);
            var npc = parts.root.AddComponent<NPC>();
            npc.npcName = name;
            npc.role = role;
            npc.wanderRadius = wander;
            return npc;
        }

        static Dialogue Talk(string speaker, params string[] lines)
        {
            var d = new Dialogue();
            foreach (var l in lines) d.Say(speaker, l);
            return d;
        }

        static void BuildCrew()
        {
            var q = QuestManager.Instance;
            var uniform = Prim.Hex("#2A3142");

            // --- Passerelle -----------------------------------------------------------------
            var vega = MakeNPC("Commandant Vega", "Commandant de l'Aurore", Prim.Hex("#1E2433"), Command);
            Prim.Make(PrimitiveType.Cube, "Cap", vega.transform.Find("Visual"), new Vector3(0, 2.02f, 0.05f), new Vector3(0.8f, 0.12f, 0.9f), Command, false);
            q.commander = vega.transform;
            q.commanderName = "Commandant Vega";
            vega.markerProvider = () => q.State == QuestState.FindCommander ? "!" : null;
            vega.dialogueProvider = () =>
            {
                if (q.State == QuestState.FindCommander)
                    return new Dialogue()
                        .Say("Commandant Vega", "Ah, vous voilà, pilote. Bienvenue sur la passerelle de l'Aurore.")
                        .Say("Commandant Vega", "Il y a une heure, nos capteurs ont capté un signal de détresse dans le champ d'astéroïdes de Kepler-9.")
                        .Say("Commandant Vega", "J'ai envoyé un escadron de reconnaissance. Il leur manque un pilote... et c'est vous.")
                        .Say("Commandant Vega", "Prenez un chasseur dans le hangar et rejoignez-les au point de rendez-vous.")
                        .Choice("À vos ordres, commandant !", () =>
                        {
                            q.Accept();
                            DialogueManager.Instance.Open(new Dialogue()
                                .Say("Commandant Vega", "J'ai déverrouillé l'accès au hangar. Il est à bâbord, au bout de la coursive transversale.")
                                .Say("Commandant Vega", "Le chef Doyle a préparé les chasseurs. Bonne chasse, pilote."), vega.transform);
                        })
                        .Choice("Laissez-moi un instant.", () =>
                        {
                            DialogueManager.Instance.Open(new Dialogue()
                                .Say("Commandant Vega", "Ne tardez pas trop. L'escadron compte sur vous."), vega.transform);
                        });
                if (q.State == QuestState.GoToHangar)
                    return Talk("Commandant Vega", "Le hangar est à bâbord : prenez la coursive transversale, à gauche avant la passerelle. Qu'attendez-vous ?");
                return Talk("Commandant Vega", "Bonne chance.");
            };

            var okoro = MakeNPC("Lt. Okoro", "Pilote de l'Aurore", uniform, Command);
            okoro.dialogueProvider = () => Talk("Lt. Okoro",
                "Je pilote l'Aurore. Nous croisons vers le système Kepler, cap au 2-7-0, vitesse stable.",
                "Un vaisseau de cette taille, c'est comme un paquebot : il faut anticiper chaque virage dix minutes à l'avance.",
                "Vous, en chasseur, vous pouvez tourner sur une pièce de monnaie. Je vous envie parfois.");

            var sato = MakeNPC("Enseigne Sato", "Officier des communications", uniform, Command);
            sato.dialogueProvider = () => Talk("Enseigne Sato",
                "Je surveille toutes les fréquences. Le signal de détresse se répète toutes les quarante secondes.",
                "C'est un vieux protocole de l'Alliance... Plus personne ne l'utilise depuis vingt ans. Bizarre, non ?");

            // --- Station de tir --------------------------------------------------------------
            var kova = MakeNPC("Sergent Kova", "Officier de tir", uniform, Security);
            kova.dialogueProvider = () =>
            {
                var d = new Dialogue()
                    .Say("Sergent Kova", "Station de tir. D'ici, on commande les quatre tourelles laser de l'Aurore.")
                    .Say("Sergent Kova", "Et ce stand sert à garder la main. Clic gauche pour tirer avec ton pistolaser, vise les cibles au fond.");
                if (ShootingTarget.Hits >= 10) d.Say("Sergent Kova", ShootingTarget.Hits + " cibles ! Pas mal du tout. Tu veux un poste de canonnier ?");
                else if (ShootingTarget.Hits > 0) d.Say("Sergent Kova", ShootingTarget.Hits + " cible(s) pour l'instant. Continue, il en faut dix pour m'impressionner.");
                return d;
            };

            var mills = MakeNPC("Canonnier Mills", "Canonnier", uniform, Security);
            mills.dialogueProvider = () => Talk("Canonnier Mills",
                "Je calibre les tourelles. Si un astéroïde s'approche à moins de deux kilomètres : pouf, de la poussière.",
                "Le plus dur, c'est les quarts de nuit. Il ne se passe jamais rien... jusqu'au moment où il se passe quelque chose.");

            // --- Laboratoire -----------------------------------------------------------------
            var maris = MakeNPC("Dr Maris", "Scientifique en chef", White, Science);
            maris.dialogueProvider = () => Talk("Dr Maris",
                "Laboratoire scientifique. Ce cristal vient d'une lune de glace : il émet un rayonnement que personne n'arrive à expliquer.",
                "Depuis ce matin, il pulse au même rythme que le signal de détresse. Toutes les quarante secondes.",
                "Si vous trouvez quoi que ce soit là-bas, ramenez-moi des relevés. S'il vous plaît.");

            var theo = MakeNPC("Théo", "Assistant de laboratoire", White, Science, 0.95f, 1.2f);
            theo.dialogueProvider = () => Talk("Théo",
                "Je classe les échantillons... Six cents tubes, chacun étiqueté à la main.",
                "Le docteur Maris ne fait pas confiance aux ordinateurs. Elle dit qu'ils « oublient avec trop d'assurance ».");

            // --- Botanique -------------------------------------------------------------------
            var ama = MakeNPC("Ama", "Botaniste", Prim.Hex("#3F5A3A"), Botany);
            ama.dialogueProvider = () => Talk("Ama",
                "Bienvenue dans la serre ! Ces plantes produisent un tiers de l'oxygène du vaisseau.",
                "Les lampes violettes imitent le soleil d'une planète que ces graines n'ont jamais vue.",
                "Et les tomates du réfectoire, c'est moi aussi. Ne crois jamais quelqu'un qui te dit qu'elles sont synthétiques.");

            var pico = MakeNPC("Pico", "Robot jardinier", Prim.Hex("#9AA3AD"), Botany, 0.6f, 1.5f);
            pico.dialogueProvider = () => Talk("Pico",
                "Bip. Arrosage en cours. Humidité : 68 %. Bip boup.",
                "Analyse du visiteur : aucune feuille détectée. Arrosage annulé. Bip.");

            // --- Salle des machines ----------------------------------------------------------
            var rhea = MakeNPC("Rhea", "Ingénieure en chef", Prim.Hex("#4A3A2E"), Engineering);
            rhea.dialogueProvider = () => Talk("Rhea",
                "Salle des machines. Ce réacteur à fusion alimente tout : moteurs, boucliers, lumière... et la machine à café.",
                "Il tourne à 72 % de sa puissance. Si tu entends une alarme, cours. Dans l'autre sens.",
                "Les chasseurs ont leurs propres petits réacteurs. Doyle me les rend toujours avec des rayures.");

            var bram = MakeNPC("Bram", "Technicien", Prim.Hex("#4A3A2E"), Engineering, 1f, 1.5f);
            bram.dialogueProvider = () => Talk("Bram",
                "Je vérifie les conduits de plasma, un par un.",
                "Une micro-fuite et c'est toute une coursive qui sent le métal chaud pendant une semaine.");

            // --- Hangar ----------------------------------------------------------------------
            var doyle = MakeNPC("Chef Doyle", "Chef mécanicien", Prim.Hex("#5A4A3A"), Engineering);
            doyle.dialogueProvider = () =>
            {
                if (q.State == QuestState.FindCommander)
                    return Talk("Chef Doyle", "Personne ne touche à mes chasseurs sans un ordre du commandant. Personne.");
                return Talk("Chef Doyle",
                    "Le commandant m'a prévenu. Prends celui que tu veux, ils ont tous le plein.",
                    "Approche-toi d'un chasseur et appuie sur E pour monter à bord. Il décollera tout seul et sortira par le champ de force.",
                    "Et rends-le-moi sans rayures, cette fois.");
            };

            var nyx = MakeNPC("Nyx", "Pilote de chasse", Prim.Hex("#D9772B"), Command);
            nyx.dialogueProvider = () => Talk("Nyx",
                "Moi c'est Nyx, ton ailière. Le reste de l'escadron est déjà dehors.",
                "On se retrouve au point de rendez-vous. Dernier arrivé paie sa tournée au réfectoire !");

            // --- Coursive --------------------------------------------------------------------
            var lou = MakeNPC("Enseigne Lou", "Officier de quart", uniform, Security);
            lou.dialogueProvider = () =>
            {
                if (q.State == QuestState.FindCommander)
                    return Talk("Enseigne Lou",
                        "Bonjour pilote ! Le commandant ? Sur la passerelle, tout au bout de la coursive, au nord.",
                        "Appuie sur E devant une porte pour l'ouvrir. Elles se referment toutes seules derrière toi.");
                return Talk("Enseigne Lou", "Le hangar est à bâbord, par la coursive transversale. Bon vol !");
            };
        }
    }
}
