using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Fabrique les personnages : une capsule pour le corps, deux sphères pour les yeux
    /// (pour voir dans quelle direction ils regardent), un uniforme et un pistolaser optionnels.
    /// La racine est aux pieds du personnage (y = 0).
    /// </summary>
    public static class CharacterFactory
    {
        public class Parts
        {
            public GameObject root;
            public Transform visual;
            public Renderer body;
            public Transform weaponPivot;
            public Transform muzzle;
        }

        /// <summary>
        /// Personnage en capsule. <paramref name="accent"/> colore les épaulettes et la ceinture de l'uniforme
        /// (couleur du département). <paramref name="withPistol"/> ajoute un pistolaser dans la main droite.
        /// </summary>
        public static Parts Build(string name, Vector3 position, Color bodyColor, float size = 1f, bool withPistol = false, Color? accent = null)
        {
            var p = new Parts();
            p.root = new GameObject(name);
            p.root.transform.position = position;

            p.visual = new GameObject("Visual").transform;
            p.visual.SetParent(p.root.transform, false);
            p.visual.localScale = Vector3.one * size;

            var body = Prim.Make(PrimitiveType.Capsule, "Body", p.visual, new Vector3(0, 1f, 0), Vector3.one, bodyColor, false);
            p.body = body.GetComponent<Renderer>();

            var eyeColor = new Color(0.08f, 0.08f, 0.1f);
            Prim.Make(PrimitiveType.Sphere, "EyeL", p.visual, new Vector3(-0.18f, 1.55f, 0.42f), Vector3.one * 0.14f, eyeColor, false);
            Prim.Make(PrimitiveType.Sphere, "EyeR", p.visual, new Vector3(0.18f, 1.55f, 0.42f), Vector3.one * 0.14f, eyeColor, false);

            if (accent.HasValue)
            {
                Prim.Make(PrimitiveType.Cube, "ShoulderL", p.visual, new Vector3(-0.38f, 1.42f, 0), new Vector3(0.3f, 0.08f, 0.5f), accent.Value, false, new Vector3(0, 0, 20));
                Prim.Make(PrimitiveType.Cube, "ShoulderR", p.visual, new Vector3(0.38f, 1.42f, 0), new Vector3(0.3f, 0.08f, 0.5f), accent.Value, false, new Vector3(0, 0, -20));
                Prim.Make(PrimitiveType.Cylinder, "Belt", p.visual, new Vector3(0, 0.85f, 0), new Vector3(1.03f, 0.06f, 1.03f), accent.Value, false);
                Prim.Make(PrimitiveType.Cube, "Badge", p.visual, new Vector3(-0.2f, 1.25f, 0.47f), new Vector3(0.12f, 0.12f, 0.03f), new Color(1f, 0.85f, 0.3f), false, new Vector3(0, 0, 45));
            }

            if (withPistol)
            {
                // Le pivot est à la main droite ; il recule à chaque tir.
                p.weaponPivot = new GameObject("WeaponPivot").transform;
                p.weaponPivot.SetParent(p.visual, false);
                p.weaponPivot.localPosition = new Vector3(0.5f, 1.15f, 0.3f);
                var dark = new Color(0.2f, 0.22f, 0.26f);
                Prim.Make(PrimitiveType.Cube, "Grip", p.weaponPivot, new Vector3(0, -0.1f, 0), new Vector3(0.08f, 0.2f, 0.1f), dark, false, new Vector3(-15, 0, 0));
                Prim.Make(PrimitiveType.Cube, "Frame", p.weaponPivot, new Vector3(0, 0.02f, 0.12f), new Vector3(0.1f, 0.12f, 0.34f), new Color(0.75f, 0.78f, 0.82f), false);
                Prim.Make(PrimitiveType.Cylinder, "Barrel", p.weaponPivot, new Vector3(0, 0.03f, 0.33f), new Vector3(0.06f, 0.08f, 0.06f), dark, false, new Vector3(90, 0, 0));
                Prim.Make(PrimitiveType.Cube, "Cell", p.weaponPivot, new Vector3(0, 0.1f, 0.08f), new Vector3(0.05f, 0.04f, 0.16f), new Color(0.3f, 0.9f, 1f), false);
                p.muzzle = new GameObject("Muzzle").transform;
                p.muzzle.SetParent(p.weaponPivot, false);
                p.muzzle.localPosition = new Vector3(0, 0.03f, 0.43f);
            }

            return p;
        }

        public static CharacterController AddController(GameObject root, float size = 1f)
        {
            var cc = root.AddComponent<CharacterController>();
            cc.height = 2f * size;
            cc.radius = 0.5f * size;
            cc.center = new Vector3(0, 1f * size, 0);
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.4f;
            return cc;
        }
    }
}
