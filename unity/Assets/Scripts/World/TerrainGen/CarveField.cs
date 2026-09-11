using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Stage 3C: how much ground the valley corridors remove, at any world point.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stage 3 is subtractive: <c>H3 = H2 - C</c>. The Stage 2 massif stays the envelope, ridge tops
    /// <em>are</em> the Stage 2 surface, and nothing here can raise ground.
    /// </para>
    /// <para>
    /// <b>The carve is keyed to arc length along a valley's own trajectory.</b> That is the whole
    /// correction over the previous two attempts. Both of those parameterised the valley by the
    /// massif's transverse flank coordinate, which is capped at the toe and has no meaning past it -
    /// so the support was zero on every one of its own boundaries, and a non-negative function that
    /// vanishes on the whole boundary of a simply-connected region has closed level sets. Each
    /// corridor was a bump, by theorem. Running the profile along a trajectory that reaches the coast
    /// moves the downstream boundary of the support onto the shoreline, where the coastal gate is
    /// independently zero, so the valley ends where the land does instead of healing in mid-slope.
    /// </para>
    /// <para>
    /// Depth is two additive terms, each continuous on its own:
    /// </para>
    /// <code>
    /// C = ( alpha * uplift(p) * Rise(a) + incision * Trough(a) ) * Across(w) * gate
    /// </code>
    /// <para>
    /// The first cuts into the mountain and is bounded by a fraction of the mountain's own uplift, so
    /// it can never reach Stage 1 and it fades to nothing at the toe on its own. The second is the
    /// trough, which is what continues across the island floor - the only term allowed below Stage 1,
    /// bounded by <see cref="RidgeSettings.CorridorIncisionMeters"/>, and zero at both ends of the
    /// trajectory. At the toe the first term has gone and the second is at full strength: the join is
    /// an interior point of one curve, and there is no seam there because nothing switches.
    /// </para>
    /// </remarks>
    public sealed class CarveField
    {
        /// <summary>
        /// Across the corridor: a floor of finite width, steepening, zero at the corridor edge.
        /// </summary>
        /// <remarks>
        /// Evaluated at <c>1 - w</c>, so it is one on the floor and zero on the boundary the corridor
        /// shares with its neighbour. Smoothstep between control points gives zero slope at that
        /// boundary, which is what makes the carve C1 where two corridors meet rather than merely
        /// continuous.
        /// </remarks>
        public static readonly ProfileCurve AcrossProfile = new ProfileCurve(
            new[] { 0f, 0.55f, 0.8f, 1f },
            new[] { 0f, 0.55f, 1f, 1f });

        /// <summary>Down the massif part: nothing at the head, full by the upper flank.</summary>
        /// <remarks>
        /// The crest is the divide, so a corridor has no depth there - the relief principles' "valleys
        /// need headwaters" rule holding by arithmetic. Rising only; the uplift factor takes this term
        /// to zero at the toe on its own.
        /// </remarks>
        public static readonly ProfileCurve RiseProfile = new ProfileCurve(
            new[] { 0f, 0.45f, 1f },
            new[] { 0f, 1f, 1f });

        /// <summary>
        /// The trough along the whole trajectory: zero at the head, full at the toe, zero at the end.
        /// </summary>
        /// <remarks>
        /// Parameterised by arc length over the entire trajectory, which is what carries the valley
        /// past the massif. Its maximum sits at the toe so that everything downstream of the toe is
        /// monotone decreasing, which is the design's one drainage rule.
        /// </remarks>
        public static readonly ProfileCurve TroughProfile = new ProfileCurve(
            new[] { 0f, 0.5f, 1f },
            new[] { 0f, 1f, 0f });

        /// <summary>
        /// The same trough for a corridor that ends by joining another, which does not fade out.
        /// </summary>
        /// <remarks>
        /// A tributary does not shallow to nothing where it meets its trunk - it hands its water over.
        /// Ending at full strength and letting the smooth maximum with the trunk carry it on is what
        /// makes the junction continuous; the fading profile would leave the tributary stopping dead in
        /// open ground, which is exactly how the previous pass rendered every merge.
        /// </remarks>
        public static readonly ProfileCurve TroughToMergeProfile = new ProfileCurve(
            new[] { 0f, 0.5f, 1f },
            new[] { 0f, 1f, 1f });

        private readonly RidgeSkeleton ridges;
        private readonly RidgeSettings settings;
        private readonly CoastQuery coast;
        private readonly float gateMeters;
        private readonly float blendMeters;

        private readonly CorridorIndex index;

        public CarveField(
            RidgeSkeleton ridges,
            CoastQuery coast,
            RidgeSettings settings,
            MassifSettings massifSettings)
        {
            settings.Clamp();
            massifSettings.Clamp();

            this.ridges = ridges;
            this.coast = coast;
            this.settings = settings;
            gateMeters = Mathf.Max(1f, massifSettings.CoastGateMeters);
            blendMeters = Mathf.Max(1f, massifSettings.MassifBlendMeters);

            index = new CorridorIndex(ridges.Valleys, settings);
        }

        /// <summary>How deep the valley structure cuts at a world point, metres, never negative.</summary>
        public float Depth(float worldX, float worldZ) =>
            Depth(worldX, worldZ, Gate(worldX, worldZ), coast != null && coast.IsInside(worldX, worldZ));

        /// <summary>The same carve for a caller that already knows the gate and the land test.</summary>
        /// <remarks>
        /// The bulk path, and a bounded one: the bucket index returns the handful of corridors whose
        /// support could reach this point, and only those are projected onto. Nothing iterates the
        /// network and nothing consults a raster.
        /// </remarks>
        public float Depth(float worldX, float worldZ, float gate, bool isLand)
        {
            if (gate <= 0f || !isLand)
            {
                return 0f;
            }

            var p = new Vector2(worldX, worldZ);
            var candidates = index.At(p);
            if (candidates == null)
            {
                return 0f;
            }

            var depth = 0f;
            for (var i = 0; i < candidates.Count; i++)
            {
                var one = DepthOf(ridges.Valleys[candidates[i]], p);
                if (one <= 0f)
                {
                    continue;
                }

                // Two corridors overlap only where their continuations converge on the island floor.
                // The smooth maximum Stage 2B already uses to join two massifs joins them here for the
                // same reason: a hard maximum would leave a crease along the line where they are equal.
                depth = depth <= 0f
                    ? one
                    : MountainUpliftModel.SmoothMax(depth, one, blendMeters);
            }

            return Mathf.Max(0f, depth * gate);
        }

        /// <summary>How deep one corridor cuts at a point, before the coastal gate.</summary>
        public float DepthOf(ValleyCorridor corridor, Vector2 p)
        {
            if (!corridor.HasTrajectory || corridor.LengthMeters <= 1e-3f)
            {
                return 0f;
            }

            if (!corridor.Project(p, out var arc, out var across))
            {
                return 0f;
            }

            var halfWidth = HalfWidthAt(corridor, arc);
            if (across >= halfWidth)
            {
                return 0f;
            }

            var w = Mathf.Clamp01(across / halfWidth);
            var a = Mathf.Clamp01(arc / corridor.LengthMeters);

            var massif = ridges.Ownership.Massifs[corridor.MassifIndex];

            // Where the massif part of the trajectory has got to, as a fraction of its own length.
            var toe = corridor.ToeArcMeters;
            var rise = toe > 1e-3f
                ? RiseProfile.Evaluate(Mathf.Clamp01(arc / toe))
                : 1f;

            // The mountain term: a fraction of the uplift the massif itself put here. Bounded by that
            // uplift, so it cannot reach the Stage 1 surface, and it fades with the mountain.
            var uplift = UpliftAt(massif, p);
            var alpha = settings.ValleyDepthPerCrest * ReliefOf(massif, corridor);
            var mountain = alpha * uplift * rise;

            // The trough term: the only part allowed below Stage 1, and only inside this corridor.
            var profile = corridor.Termination == ValleyTermination.Merge
                ? TroughToMergeProfile
                : TroughProfile;
            var trough = settings.CorridorIncisionMeters * profile.Evaluate(a);

            return (mountain + trough) * AcrossProfile.Evaluate(1f - w);
        }

        /// <summary>The corridor's half-width at an arc position, metres.</summary>
        /// <remarks>
        /// Constant: the width the corridor had at the massif toe, carried the whole way. A corridor
        /// that narrowed along its length would put a second longitudinal profile into the geometry,
        /// and the depth profile is already the one that decides how the valley fades.
        /// </remarks>
        private static float HalfWidthAt(ValleyCorridor corridor, float arcMeters) =>
            corridor.HalfWidthMeters;

        /// <summary>The massif's own uplift at a world point, metres.</summary>
        private static float UpliftAt(MountainMassif massif, Vector2 p) =>
            massif.Evaluate(p.x, p.y);

        /// <summary>How high the crest is above this corridor's head, as a fraction of the peak.</summary>
        private static float ReliefOf(MountainMassif massif, ValleyCorridor corridor) =>
            massif.CrestHeightMeters > 1e-3f
                ? Mathf.Clamp01(massif.CrestAt(corridor.HeadS) / massif.CrestHeightMeters)
                : 1f;

        /// <summary>The coastal gate for a known inland distance, metres.</summary>
        public float GateFor(float inlandMeters)
        {
            if (inlandMeters <= 0f)
            {
                return 0f;
            }

            var u = Mathf.Clamp01(inlandMeters / gateMeters);
            return u * u * (3f - 2f * u);
        }

        private float Gate(float worldX, float worldZ) =>
            coast == null ? 0f : GateFor(coast.SignedDistance(worldX, worldZ));

        /// <summary>
        /// A uniform bucket index over the world: which corridors could reach a given point.
        /// </summary>
        /// <remarks>
        /// The design's own acceleration structure. It holds ids, not terrain, so it is not a raster
        /// mask: the geometry stays curve-based and this only avoids projecting onto every corridor
        /// for every sample.
        /// </remarks>
        private sealed class CorridorIndex
        {
            private const float BucketMeters = 64f;

            private readonly List<int>[] buckets;
            private readonly int width;
            private readonly int height;

            public CorridorIndex(IReadOnlyList<ValleyCorridor> corridors, RidgeSettings settings)
            {
                width = Mathf.CeilToInt(WorldScale.WorldExtentXMeters / BucketMeters) + 1;
                height = Mathf.CeilToInt(WorldScale.WorldExtentZMeters / BucketMeters) + 1;
                buckets = new List<int>[width * height];

                for (var i = 0; i < corridors.Count; i++)
                {
                    var corridor = corridors[i];
                    if (!corridor.HasTrajectory)
                    {
                        continue;
                    }

                    var bounds = corridor.Bounds(corridor.HalfWidthMeters + BucketMeters);
                    var x0 = Clamp(Mathf.FloorToInt(bounds.xMin / BucketMeters), width);
                    var x1 = Clamp(Mathf.CeilToInt(bounds.xMax / BucketMeters), width);
                    var z0 = Clamp(Mathf.FloorToInt(bounds.yMin / BucketMeters), height);
                    var z1 = Clamp(Mathf.CeilToInt(bounds.yMax / BucketMeters), height);

                    for (var z = z0; z <= z1; z++)
                    {
                        for (var x = x0; x <= x1; x++)
                        {
                            var slot = z * width + x;
                            buckets[slot] ??= new List<int>(2);
                            buckets[slot].Add(i);
                        }
                    }
                }
            }

            public List<int> At(Vector2 p)
            {
                var x = Clamp(Mathf.FloorToInt(p.x / BucketMeters), width);
                var z = Clamp(Mathf.FloorToInt(p.y / BucketMeters), height);
                return buckets[z * width + x];
            }

            private static int Clamp(int value, int limit) => Mathf.Clamp(value, 0, limit - 1);
        }
    }
}
