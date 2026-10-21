using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Fabrique les personnages : une capsule pour le corps, deux sphères pour les yeux
    /// (pour voir dans quelle direction ils regardent) et une épée optionnelle.
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
        }

        public static Parts Build(string name, Vector3 position, Color bodyColor, float size = 1f, bool withSword = false, Color? swordColor = null)
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

            if (withSword)
            {
                // Le pivot est à l'épaule droite ; on le fait tourner pour animer les coups.
                p.weaponPivot = new GameObject("WeaponPivot").transform;
                p.weaponPivot.SetParent(p.visual, false);
                p.weaponPivot.localPosition = new Vector3(0.55f, 1.15f, 0.1f);
                var blade = swordColor ?? new Color(0.8f, 0.82f, 0.88f);
                Prim.Make(PrimitiveType.Cube, "Handle", p.weaponPivot, new Vector3(0, 0, 0.15f), new Vector3(0.08f, 0.08f, 0.3f), new Color(0.35f, 0.2f, 0.1f), false);
                Prim.Make(PrimitiveType.Cube, "Guard", p.weaponPivot, new Vector3(0, 0, 0.32f), new Vector3(0.3f, 0.06f, 0.06f), new Color(0.6f, 0.5f, 0.2f), false);
                Prim.Make(PrimitiveType.Cube, "Blade", p.weaponPivot, new Vector3(0, 0, 0.85f), new Vector3(0.1f, 0.03f, 1.0f), blade, false);
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
