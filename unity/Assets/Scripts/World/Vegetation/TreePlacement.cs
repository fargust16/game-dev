using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// World-space tree height targets and trunk collision helpers.
    /// Capsules are fitted from mid-trunk mesh vertices (not crown AABB). Unity can also use
    /// <see cref="MeshCollider"/> on a full render mesh, but that includes the canopy and is too
    /// heavy for thousands of trees — so we fit capsules to the bole geometry instead.
    /// </summary>
    public static class TreePlacement
    {
        public const float CharacterHeightMeters = GameDev.World.WorldScale.CharacterHeightMeters;
        public const float MatureHeightMinMeters = GameDev.World.WorldScale.TreeMatureMinMeters;
        public const float MatureHeightMaxMeters = GameDev.World.WorldScale.TreeMatureMaxMeters;
        public const float YoungHeightMinMeters = GameDev.World.WorldScale.TreeYoungMinMeters;
        public const float YoungHeightMaxMeters = GameDev.World.WorldScale.TreeYoungMaxMeters;

        public const float TrunkRadiusMinMeters = 0.10f;
        public const float TrunkRadiusMaxMeters = 0.38f;
        public const float TrunkHeightFraction = 0.45f;

        /// <summary>Last fitted world radius (metres) — for editor diagnostics.</summary>
        public static float LastFittedRadiusMeters { get; private set; }

        /// <summary>Last fitted capsule world height (metres).</summary>
        public static float LastFittedHeightMeters { get; private set; }

        public static float PickTargetHeightMeters(bool young, System.Random rng)
        {
            var min = young ? YoungHeightMinMeters : MatureHeightMinMeters;
            var max = young ? YoungHeightMaxMeters : MatureHeightMaxMeters;
            return Mathf.Lerp(min, max, (float)rng.NextDouble());
        }

        public static float ApplyWorldHeight(GameObject instance, float targetHeightMeters)
        {
            if (instance == null || targetHeightMeters <= 0.01f)
            {
                return 1f;
            }

            instance.transform.localScale = Vector3.one;
            var height = MeasureRendererHeight(instance);
            if (height < 0.05f)
            {
                instance.transform.localScale = Vector3.one * (targetHeightMeters / 5f);
                return instance.transform.localScale.x;
            }

            var scale = targetHeightMeters / height;
            instance.transform.localScale = Vector3.one * scale;
            return scale;
        }

        public static float MeasureRendererHeight(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                return 0f;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.size.y;
        }

        /// <summary>
        /// Strip prefab colliders and attach a pivot-centred capsule fitted to mid-trunk vertices.
        /// Optionally adds a short base capsule when root flare is wider than the bole (fruit trees).
        /// </summary>
        public static void EnsureTrunkCollider(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            var existing = instance.GetComponentsInChildren<Collider>(includeInactive: true);
            for (var i = 0; i < existing.Length; i++)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(existing[i]);
                }
                else
                {
                    Object.DestroyImmediate(existing[i]);
                }
            }

            var renderers = instance.GetComponentsInChildren<Renderer>();
            Bounds bounds;
            if (renderers != null && renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
            else
            {
                bounds = new Bounds(instance.transform.position, new Vector3(1f, 12f, 1f));
            }

            var lossy = Mathf.Max(instance.transform.lossyScale.x, 0.01f);
            var worldHeight = Mathf.Max(1f, bounds.size.y);
            SampleTrunkRadii(instance, bounds, out var boleRadius, out var rootRadius);

            var worldRadius = Mathf.Clamp(boleRadius, TrunkRadiusMinMeters, TrunkRadiusMaxMeters);
            var worldCapsuleHeight = Mathf.Clamp(
                worldHeight * TrunkHeightFraction,
                worldRadius * 2.5f,
                worldHeight * 0.55f);

            LastFittedRadiusMeters = worldRadius;
            LastFittedHeightMeters = worldCapsuleHeight;

            var capsule = instance.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.radius = worldRadius / lossy;
            capsule.height = worldCapsuleHeight / lossy;
            var worldCenterY = bounds.min.y + worldCapsuleHeight * 0.5f;
            var pivot = instance.transform.position;
            capsule.center = instance.transform.InverseTransformPoint(
                new Vector3(pivot.x, worldCenterY, pivot.z));

            // Fruit-tree root flare: short extra capsule at the ground so the player cannot
            // walk through exposed roots while the bole capsule stays thin.
            if (rootRadius > worldRadius * 1.25f && rootRadius > 0.14f)
            {
                var baseRadius = Mathf.Clamp(rootRadius * 0.85f, worldRadius, TrunkRadiusMaxMeters);
                var baseHeight = Mathf.Clamp(worldHeight * 0.12f, baseRadius * 2.1f, 1.6f);
                var baseCapsule = instance.AddComponent<CapsuleCollider>();
                baseCapsule.direction = 1;
                baseCapsule.radius = baseRadius / lossy;
                baseCapsule.height = baseHeight / lossy;
                var baseCenterY = bounds.min.y + baseHeight * 0.45f;
                baseCapsule.center = instance.transform.InverseTransformPoint(
                    new Vector3(pivot.x, baseCenterY, pivot.z));
            }
        }

        private static void SampleTrunkRadii(
            GameObject instance, Bounds worldBounds, out float boleRadius, out float rootRadius)
        {
            boleRadius = 0.2f;
            rootRadius = 0f;
            var pivot = instance.transform.position;
            var minY = worldBounds.min.y;
            var h = Mathf.Max(worldBounds.size.y, 0.1f);
            // Skip ground roots (0–8%) and canopy (>40%); bole lives in the mid band.
            var boleMinY = minY + h * 0.10f;
            var boleMaxY = minY + h * 0.38f;
            var rootMaxY = minY + h * 0.08f;

            var boleSamples = new List<float>(128);
            var rootSamples = new List<float>(64);
            var filters = instance.GetComponentsInChildren<MeshFilter>();
            for (var f = 0; f < filters.Length; f++)
            {
                var mesh = filters[f].sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                var verts = mesh.vertices;
                var xf = filters[f].transform;
                var step = Mathf.Max(1, verts.Length / 320);
                for (var i = 0; i < verts.Length; i += step)
                {
                    var w = xf.TransformPoint(verts[i]);
                    var dx = w.x - pivot.x;
                    var dz = w.z - pivot.z;
                    var r = Mathf.Sqrt(dx * dx + dz * dz);
                    if (w.y >= boleMinY && w.y <= boleMaxY)
                    {
                        boleSamples.Add(r);
                    }
                    else if (w.y <= rootMaxY)
                    {
                        rootSamples.Add(r);
                    }
                }
            }

            if (boleSamples.Count >= 6)
            {
                boleSamples.Sort();
                // Median — max would pick needles/leaf cards that poke the trunk band.
                boleRadius = boleSamples[boleSamples.Count / 2];
                // Soft upper trim: ignore extreme outliers above 90th percentile.
                var p90 = boleSamples[(int)(boleSamples.Count * 0.9f)];
                boleRadius = Mathf.Min(boleRadius * 1.15f, p90);
            }
            else if (boleSamples.Count > 0)
            {
                boleSamples.Sort();
                boleRadius = boleSamples[boleSamples.Count / 2];
            }
            else
            {
                boleRadius = Mathf.Clamp(h * 0.015f, TrunkRadiusMinMeters, TrunkRadiusMaxMeters);
            }

            if (rootSamples.Count >= 4)
            {
                rootSamples.Sort();
                // Prefer lower quartile of roots so one wild root card doesn't balloon the capsule.
                rootRadius = rootSamples[rootSamples.Count / 4];
                var rootMed = rootSamples[rootSamples.Count / 2];
                rootRadius = Mathf.Lerp(rootRadius, rootMed, 0.5f);
            }
        }
    }
}
