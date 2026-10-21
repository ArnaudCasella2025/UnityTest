using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Petits utilitaires pour fabriquer le monde à partir de formes primitives.
    /// Les matériaux sont clonés depuis le matériau par défaut des primitives,
    /// ce qui fonctionne quel que soit le pipeline de rendu (Built-in, URP...).
    /// </summary>
    public static class Prim
    {
        static Material baseMaterial;
        static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();

        public static Material Mat(Color color)
        {
            Material m;
            if (cache.TryGetValue(color, out m) && m != null) return m;

            if (baseMaterial == null)
            {
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseMaterial = tmp.GetComponent<Renderer>().sharedMaterial;
                Object.Destroy(tmp);
            }

            m = new Material(baseMaterial) { color = color };
            cache[color] = m;
            return m;
        }

        /// <summary>Crée une primitive positionnée, colorée, éventuellement sans collider.</summary>
        public static GameObject Make(PrimitiveType type, string name, Transform parent,
            Vector3 localPos, Vector3 scale, Color color, bool collider = true, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            if (!collider) RemoveCollider(go);
            else if (type == PrimitiveType.Cylinder || (type == PrimitiveType.Sphere && !IsUniform(scale)))
            {
                // Le collider par défaut d'un cylindre est une capsule, et celui d'une sphère
                // étirée reste une sphère : on les remplace par un MeshCollider convexe fidèle à la forme.
                RemoveCollider(go);
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                mc.convex = true;
            }
            return go;
        }

        public static void RemoveCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null)
            {
                // Désactivé immédiatement (Destroy est différé à la fin de la frame).
                c.enabled = false;
                Object.Destroy(c);
            }
        }

        static bool IsUniform(Vector3 v)
        {
            return Mathf.Approximately(v.x, v.y) && Mathf.Approximately(v.y, v.z);
        }

        public static Color Hex(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString(hex, out c);
            return c;
        }
    }
}
