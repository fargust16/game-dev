using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// World-space tree height targets and trunk-only collision helpers.
    /// Character ≈ 1.8 m → mature trees should read 3–5× taller (≈12–25 m+).
    /// </summary>
    public static class TreePlacement
    {
        public const float CharacterHeightMeters = 1.8f;
        public const float MatureHeightMinMeters = 12f;
        public const float MatureHeightMaxMeters = 25f;
        public const float YoungHeightMinMeters = 8f;
        public const float YoungHeightMaxMeters = 14f;

        /// <summary>Trunk capsule radius as a fraction of measured crown width (clamped).</summary>
        public const float TrunkRadiusFractionOfWidth = 0.07f;
        public const float TrunkRadiusMinMeters = 0.22f;
        public const float TrunkRadiusMaxMeters = 0.55f;

        /// <summary>Capsule height as a fraction of final tree height (lower trunk only).</summary>
        public const float TrunkHeightFraction = 0.42f;

        public static float PickTargetHeightMeters(bool young, System.Random rng)
        {
            var min = young ? YoungHeightMinMeters : MatureHeightMinMeters;
            var max = young ? YoungHeightMaxMeters : MatureHeightMaxMeters;
            return Mathf.Lerp(min, max, (float)rng.NextDouble());
        }

        /// <summary>
        /// Uniform scale so renderer bounds height ≈ <paramref name="targetHeightMeters"/>.
        /// Returns the applied scale (1 if bounds unusable).
        /// </summary>
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
        /// Strip crown-sized colliders and attach a thin Y-axis capsule on the trunk only so the
        /// player can walk under the canopy up to the bole.
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
            var worldWidth = Mathf.Max(bounds.size.x, bounds.size.z);
            var worldRadius = Mathf.Clamp(
                worldWidth * TrunkRadiusFractionOfWidth,
                TrunkRadiusMinMeters,
                TrunkRadiusMaxMeters);
            var worldCapsuleHeight = Mathf.Clamp(
                worldHeight * TrunkHeightFraction,
                worldRadius * 2.2f,
                worldHeight * 0.55f);

            var capsule = instance.AddComponent<CapsuleCollider>();
            capsule.direction = 1; // Y
            capsule.radius = worldRadius / lossy;
            capsule.height = worldCapsuleHeight / lossy;
            // Sit on the ground: capsule bottom ≈ bounds.min.y.
            var worldCenterY = bounds.min.y + worldCapsuleHeight * 0.5f;
            var localCenter = instance.transform.InverseTransformPoint(
                new Vector3(bounds.center.x, worldCenterY, bounds.center.z));
            capsule.center = localCenter;
        }
    }
}
