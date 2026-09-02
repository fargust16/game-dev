using GameDev.World.Buildings.Shell;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// Spawns greybox placeholders for property boundaries, gates, secondary pads,
    /// and SettlementStreet dress props (S8).
    /// </summary>
    public static class PropertyFeatureSpawner
    {
        public static GameObject SpawnAll(ISettlementPropertyLayout layout, Transform parent)
        {
            var root = new GameObject($"PropertyFeatures_{layout.SettlementId}");
            if (parent != null)
                root.transform.SetParent(parent, false);

            for (var i = 0; i < layout.Properties.Count; i++)
            {
                var prop = layout.Properties[i];
                var propRoot = new GameObject($"Prop_{i:00}_{prop.ArchetypeId}");
                propRoot.transform.SetParent(root.transform, false);
                SpawnBoundaries(prop, propRoot.transform);
                SpawnSecondary(prop, propRoot.transform);
                SpawnProps(prop, propRoot.transform);
            }

            if (layout is SettlementFabricLayout fabric)
                SpawnStreetProps(fabric, root.transform);

            return root;
        }

        private static void SpawnBoundaries(PropertySpec prop, Transform parent)
        {
            for (var i = 0; i < prop.Boundaries.Count; i++)
            {
                var seg = prop.Boundaries[i];
                var length = seg.LengthMeters;
                if (length <= 0.05f)
                    continue;

                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = seg.IsGate
                    ? $"Gate_{seg.Edge}"
                    : $"Boundary_{seg.Style}_{seg.Edge}_{i:00}";
                go.transform.SetParent(parent, false);

                var delta = seg.EndWorld - seg.StartWorld;
                var dir = delta / length;
                // Keep fence upright; elongate cube local +X along the edge.
                var rot = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);
                var center = (seg.StartWorld + seg.EndWorld) * 0.5f
                             + Vector3.up * (seg.HeightMeters * 0.5f);

                go.transform.SetPositionAndRotation(center, rot);
                go.transform.localScale = new Vector3(
                    length,
                    seg.HeightMeters,
                    seg.ThicknessMeters);

                var r = go.GetComponent<Renderer>();
                if (r != null)
                    r.sharedMaterial = GeneratedTintMaterials.Get(ColorFor(seg));

                var col = go.GetComponent<Collider>();
                if (col != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        Object.DestroyImmediate(col);
                    else
#endif
                        Object.Destroy(col);
                }
            }
        }

        private static void SpawnSecondary(PropertySpec prop, Transform parent)
        {
            if (!prop.HasSecondaryBuilding)
                return;

            var pad = prop.SecondaryBuildingPad;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "SecondaryBuilding";
            go.transform.SetParent(parent, false);
            var yaw = Quaternion.Euler(0f, pad.YawDegrees, 0f);
            var height = ModularGrid.StoreyMeters * 1.2f;
            var center = pad.Origin
                         + yaw * new Vector3(pad.WidthMeters * 0.5f, height * 0.5f, pad.DepthMeters * 0.5f);
            go.transform.SetPositionAndRotation(center, yaw);
            go.transform.localScale = new Vector3(pad.WidthMeters, height, pad.DepthMeters);

            var r = go.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = GeneratedTintMaterials.Get(new Color(0.42f, 0.36f, 0.3f, 1f));
        }

        private static void SpawnProps(PropertySpec prop, Transform parent)
        {
            if (prop.Props == null)
                return;

            for (var i = 0; i < prop.Props.Count; i++)
            {
                var p = prop.Props[i];
                var go = GameObject.CreatePrimitive(PrimitiveFor(p.Kind));
                go.name = $"Prop_{p.Semantic}_{p.Kind}_{i:00}";
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(
                    p.Position,
                    Quaternion.Euler(0f, p.YawDegrees, 0f));
                go.transform.localScale = p.Scale;

                var r = go.GetComponent<Renderer>();
                if (r != null)
                    r.sharedMaterial = GeneratedTintMaterials.Get(p.Tint);

                var col = go.GetComponent<Collider>();
                if (col != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        Object.DestroyImmediate(col);
                    else
#endif
                        Object.Destroy(col);
                }
            }
        }

        private static void SpawnStreetProps(SettlementFabricLayout fabric, Transform parent)
        {
            if (fabric.StreetProps == null || fabric.StreetProps.Count == 0)
                return;

            var streetRoot = new GameObject("StreetProps");
            streetRoot.transform.SetParent(parent, false);

            for (var i = 0; i < fabric.StreetProps.Count; i++)
            {
                var p = fabric.StreetProps[i];
                var go = GameObject.CreatePrimitive(PrimitiveForStreet(p.Kind));
                go.name = $"Street_{p.StreetKind}_{p.Kind}_{i:00}";
                go.transform.SetParent(streetRoot.transform, false);
                go.transform.SetPositionAndRotation(
                    p.Position,
                    Quaternion.Euler(0f, p.YawDegrees, 0f));
                go.transform.localScale = p.Scale;

                var r = go.GetComponent<Renderer>();
                if (r != null)
                    r.sharedMaterial = GeneratedTintMaterials.Get(p.Tint);

                var col = go.GetComponent<Collider>();
                if (col != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        Object.DestroyImmediate(col);
                    else
#endif
                        Object.Destroy(col);
                }
            }
        }

        private static PrimitiveType PrimitiveFor(PropertyPropKind kind)
        {
            switch (kind)
            {
                case PropertyPropKind.Barrel:
                case PropertyPropKind.HedgeOrb:
                    return PrimitiveType.Cylinder;
                case PropertyPropKind.GardenLamp:
                case PropertyPropKind.WardMarker:
                case PropertyPropKind.Statue:
                    return PrimitiveType.Capsule;
                default:
                    return PrimitiveType.Cube;
            }
        }

        private static PrimitiveType PrimitiveForStreet(StreetPropKind kind)
        {
            switch (kind)
            {
                case StreetPropKind.StreetLamp:
                    return PrimitiveType.Capsule;
                case StreetPropKind.Bollard:
                    return PrimitiveType.Cylinder;
                default:
                    return PrimitiveType.Cube;
            }
        }

        private static Color ColorFor(PropertyBoundarySegment seg)
        {
            if (seg.IsGate)
                return new Color(0.55f, 0.35f, 0.2f, 1f);
            switch (seg.Style)
            {
                case BoundaryStyle.LowFence: return new Color(0.55f, 0.48f, 0.38f, 1f);
                case BoundaryStyle.IronFence: return new Color(0.35f, 0.38f, 0.42f, 1f);
                case BoundaryStyle.Wall: return new Color(0.62f, 0.58f, 0.52f, 1f);
                default: return Color.gray;
            }
        }
    }
}
