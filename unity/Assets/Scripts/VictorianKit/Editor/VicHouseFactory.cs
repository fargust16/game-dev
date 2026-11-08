using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDev.VictorianKit.EditorTools
{
    /// <summary>A kit manifest as the package writes it (Runtime/Manifests/House_*.json).</summary>
    [Serializable]
    public sealed class VicManifest
    {
        public string name;
        public VicManifestPiece[] pieces;

        public static VicManifest Load(string path) => JsonUtility.FromJson<VicManifest>(File.ReadAllText(path));

        public IEnumerable<VicPiece> Pieces() => pieces.Select(p => new VicPiece(p.id, p.position[0], p.position[1], p.position[2], p.yaw, false));
    }

    [Serializable]
    public sealed class VicManifestPiece
    {
        public string id;
        public float[] position;
        public float yaw;
        public string[] materials;
    }

    /// <summary>
    /// Turns placed modules into GameObjects. Each module stays its own prefab instance (no merged mesh, no baked atlas).
    /// <para>
    /// Measured on 2026-11-08 (Logs/VictorianKit/modules.tsv): Unity imports each FBX with a root turned 270° about X and
    /// scaled 100, and its mesh holds the Blender point (x, y, z) as (−x, y, z) / 100, so the prefab shows (−x, z, −y):
    /// the importer's handedness flip. A kit point keeps its Blender look in Unity as (x, z, y), and a turn θ about
    /// Blender Z is a turn −θ about Unity Y; the module's own import turn is kept under a further 180°. The package's
    /// Build Prefabs sets the turn to Euler(0, yaw, 0) and so drops the import turn: its house prefabs lie flat.
    /// </para>
    /// </summary>
    public static class VicHouseFactory
    {
        /// <summary>A kit position (Blender metres) in the house's Unity space.</summary>
        public static Vector3 ToUnity(double x, double y, double z) => new Vector3((float)x, (float)z, (float)y);

        /// <summary>Builds a house root at the origin with one prefab instance per piece; materials by <paramref name="paint"/>.</summary>
        public static GameObject Build(string name, IEnumerable<VicPiece> pieces, Func<VicPiece, Material, Material> paint = null)
        {
            var root = new GameObject(name);
            var cache = new Dictionary<string, GameObject>();
            var n = 0;
            foreach (var p in pieces)
            {
                if (!cache.TryGetValue(p.Id, out var prefab))
                {
                    prefab = VicKitPaths.LoadModule(p.Id);
                    if (prefab == null)
                    {
                        throw new InvalidOperationException($"The kit has no module {p.Id}.");
                    }

                    cache[p.Id] = prefab;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                go.name = $"{p.Id}_{n++:000}";
                go.transform.localPosition = ToUnity(p.X, p.Y, p.Z);
                go.transform.localRotation = Quaternion.Euler(0f, 180f - (float)p.Yaw, 0f) * prefab.transform.localRotation;
                go.transform.localScale = prefab.transform.localScale;
                if (paint != null)
                {
                    foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        var mats = r.sharedMaterials;
                        var changed = false;
                        for (var i = 0; i < mats.Length; i++)
                        {
                            var m = paint(p, mats[i]);
                            if (m != null && m != mats[i])
                            {
                                mats[i] = m;
                                changed = true;
                            }
                        }

                        if (changed)
                        {
                            r.sharedMaterials = mats;
                        }
                    }
                }
            }

            return root;
        }
    }
}
