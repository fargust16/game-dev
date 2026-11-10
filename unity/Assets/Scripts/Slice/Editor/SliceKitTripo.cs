using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Kit;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The owner's Tripo furniture for the interiors (owner, 2026-11-09; INTERIOR_FURNISHING_RULES.md §10). Every model is
    /// normalised to 1 on its longest side, so each takes its size here, at game scale (×1.2 its true size, as the
    /// furniture of the kit standard). The survey (`SliceAssetSurvey.RunTripo`, `Logs/SliceSurvey/tripo_all_*.png`)
    /// found every piece facing +Z but the wall lantern, whose wall plate is on +X. They hold up to about 10 000 polygons
    /// each (the table lamp 14 410), so they furnish interiors only, never the city's open plan (owner). Sizes are authoring defaults.
    /// </summary>
    public static partial class SliceKitCatalogue
    {
        /// <summary>
        /// Tripo things standing on a set's base, by file: the longest side, m, and the turns about X and Z that lay a flat
        /// thing down (the parchment stands upright in its file, the magnifying glass leans). Authoring defaults.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, (float Longest, float Pitch, float Roll)> TripoThings = new Dictionary<string, (float, float, float)>
        {
            ["decorative_table_lamp_3d_model"] = (0.6f, 0f, 0f),
            ["aged_parchment_3d_model"] = (0.34f, -90f, 0f), // its written face (+Z) turned up
            ["vintage_gramophone_3d_model"] = (0.65f, 0f, 0f),
            ["sewing_machine_3d_model"] = (0.46f, 0f, 0f),
            ["classical_bust_3d_model"] = (0.55f, 0f, 0f),          // 0.45 m true, on its own small base
            ["ornate_ink_desk_3d_model_Clone1"] = (0.3f, 0f, 0f),    // two wells and a pen, 0.25 m true
            ["magnifying_glass_3d_model"] = (0.25f, -4f, -30f),      // measured: these turns lay it flattest (RunTripoProfiles)
        };

        /// <summary>
        /// Where a Tripo lamp's flame or bulb is, as a fraction of its seen bounds, by spec key or, for a thing on a set, by
        /// file. The models carry no light, so the wrapper adds a warm one there, as the pack's lamps carry theirs (rules G9);
        /// the builds then set its strength and reach with every lamp's. Authoring defaults.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, Vector3> TripoGlow = new Dictionary<string, Vector3>
        {
            ["TrSconce"] = new Vector3(0.5f, 0.9f, 0.6f),        // the candles on the arms' ends
            ["TrLantern"] = new Vector3(0.5f, 0.45f, 0.75f),     // the glass box at the bracket's end
            ["decorative_table_lamp_3d_model"] = new Vector3(0.5f, 0.75f, 0.5f), // under the shade
        };

        internal static void Glow(Transform parent, Bounds seen, Vector3 at)
        {
            var light = new GameObject("Lamp light").AddComponent<Light>();
            light.transform.SetParent(parent, true);
            light.transform.position = seen.min + Vector3.Scale(seen.size, at);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.75f, 0.45f);
            light.range = 6f;
            light.intensity = 2.5f;
            light.shadows = LightShadows.None;
        }

        private static IEnumerable<Spec> TripoSpecs()
        {
            const string pr = KitLayers.Prop, ng = KitLayers.NoGrid;

            Spec T(string key, string file, float? sizeY, Anchor anchor = Anchor.Centre, string layer = pr, Body body = Body.Box)
            {
                var spec = S(key, "Tripo", key.Substring(2), Tripo, Model(file), layer, body, anchor);
                spec.SizeY = sizeY;
                spec.BodyTop = ViBodyTop;
                if (anchor == Anchor.Back || anchor == Anchor.Face)
                {
                    spec.Face = anchor == Anchor.Back ? ViBack : ViFace;
                }

                return spec;
            }

            Spec Hung(string key, string file, float? sizeY)
            {
                var spec = T(key, file, sizeY, Anchor.Face, ng, Body.None);
                spec.Width = ViModule; // centred on its 3 m wall segment, as the pack's hung pieces
                spec.Out = HungOut;
                return spec;
            }

            // Things on a set: Victorian Interiors' small things keep their size; Tripo's take TripoThings'.
            Spec Set(Spec spec, params (string Folder, string File, float Fx, float Fz)[] on)
            {
                spec.On = on;
                return spec;
            }

            (string, string, float, float) Vi(string file, float fx, float fz) => (SliceKitCatalogue.Vi, file, fx, fz);
            (string, string, float, float) Tr(string file, float fx, float fz) => (Tripo, Model(file), fx, fz);

            // Against a wall. True sizes: a wardrobe 1.9 m high, a chest of drawers 0.88 m, a writing desk with its
            // gallery 0.95 m, a nightstand 0.67 m, a cheval glass 1.55 m, a folding screen 1.6 m, a narrow glazed cabinet
            // 1.65 m. The owner's fifth play test (2026-11-09) found the piano very small beside the player (1.8 m): its
            // carved top makes it tall for its keys, which stand at 0.41 of its height (measured, pt5b lineup), so it is
            // sized by its keys, 0.82 m, a real upright's 0.72 m at game scale. The tall pieces were measured against the
            // player in the same lineup and raised a little, to stand over a man's head as their true ones do.
            yield return T("TrWardrobe", "antique_wardrobe", 2.4f, Anchor.Back);
            yield return Set(T("TrNightstand", "antique_wood_nightstand", 0.8f, Anchor.Back), Vi("Candle_1", 0.3f, 0.6f), Vi("Book_1", 0.72f, 0.6f));
            yield return Set(T("TrNightstandLamp", "antique_wood_nightstand", 0.8f, Anchor.Back), Tr("decorative_table_lamp", 0.5f, 0.55f));
            yield return Set(T("TrDresser", "antique_wooden_dresser", 1.05f, Anchor.Back), Vi("Brush_1", 0.3f, 0.6f), Vi("Box_2", 0.72f, 0.6f));
            // The rich desk's inkstand and magnifying glass are the owner's of 2026-11-10 (rules §10), in the place of the
            // pack's ink and pen.
            yield return Set(T("TrDesk", "antique_wooden_desk", 1.15f, Anchor.Back),
                Tr("decorative_table_lamp", 0.14f, 0.6f), Tr("aged_parchment", 0.48f, 0.72f), Tr("ornate_ink_desk_3d_model_Clone1", 0.8f, 0.62f),
                Tr("magnifying_glass", 0.62f, 0.8f));
            yield return T("TrMirror", "ornate_mirror", 2f, Anchor.Back);
            yield return T("TrScreen", "ornate_wooden_screen", 2f, Anchor.Back);
            yield return T("TrPiano", "ornate_piano", 2f, Anchor.Back);
            yield return T("TrDisplay", "furniture", 2.1f, Anchor.Back);

            // In the room. True sizes: a wing chair 1.05 m high, a coat stand 1.7 m, a floor vase 0.75 m, a log basket
            // 0.55 m, a stand of fire irons 0.67 m, a pedestal table 0.67 m. The armchair matches Victorian Interiors' own
            // (1.4 m), which stands beside it in the same rooms.
            yield return T("TrArmchair", "red_velvet_armchair", 1.4f);
            yield return T("TrCoatRack", "wooden_coat_rack", 2.05f);
            yield return T("TrVase", "antique_vase", 0.9f);
            yield return T("TrLogHolder", "log_holder", 0.65f);
            yield return T("TrFireTools", "fireplace_tools_set", 0.8f);
            yield return Set(T("TrGramophone", "wooden_round_table", 0.8f), Tr("vintage_gramophone", 0.5f, 0.5f));
            yield return Set(F2("TrSewing", "Table_2"), Tr("sewing_machine", 0.5f, 0.5f));

            // On a wall's face, centred on its 3 m segment. True sizes: a regulator clock 0.9 m, a two-arm sconce 0.45 m
            // across, a bracket lantern 0.5 m.
            yield return Hung("TrWallClock", "ornate_grandfather_clock", 1.1f); // the file's name, but a wall clock: no plinth
            var sconce = Hung("TrSconce", "brass_wall_sconce", null);
            sconce.SizeX = 0.55f;
            yield return sconce;
            var lantern = Hung("TrLantern", "antique_wall_lantern", 0.6f);
            lantern.ModelYaw = 90f; // survey: its wall plate is on +X
            yield return lantern;

            // Outside, the one exception (99 VS-SIGN (b), owner 2026-11-09): a blank board under an iron bracket, about 1 m
            // high true, hung on a shop's facade beside its door by VeilmouthSigns. Its back is the bracket's wall plate.
            var sign = T("TrSign", "wooden_sign", 1.2f, Anchor.Face, ng, Body.None);
            sign.ModelYaw = 90f; // survey: its wall plate is on +X, as the lantern's
            sign.Face = 0f;      // on the facade itself, which has no infill plane
            sign.Width = 1;
            yield return sign;

            // Under a ceiling higher than a storey only (rules G9): 1.2 m true.
            var chandelier = T("TrChandelier", "ornate_chandelier", 1.4f, Anchor.Centre, ng, Body.None);
            chandelier.Top = true;
            yield return chandelier;

            // The owner's pieces of 2026-11-10 (rules §10). True sizes: a nightstand cabinet 0.67 m, as the nightstand, so
            // the two are alike beside a bed; an ornate floor vase 0.75 m; a gas radiator 0.48 m high, low enough to stand
            // under a window (G2: under 0.6 m at game scale); an umbrella stand 0.6 m. The bust stands on the gramophone's
            // pedestal table. The barometer, 0.38 m across, hangs as the wall clock does.
            yield return Set(T("TrCabinet", "antique_wooden_cabinet_3d_model_Clone1", 0.8f, Anchor.Back), Vi("Candle_1", 0.3f, 0.6f), Vi("Book_1", 0.72f, 0.6f));
            yield return Set(T("TrCabinetLamp", "antique_wooden_cabinet_3d_model_Clone1", 0.8f, Anchor.Back), Tr("decorative_table_lamp", 0.5f, 0.55f));
            yield return T("TrVaseOrnate", "ornate_vase", 0.9f);
            // On its brackets, 5 cm off the wall (authoring default): a window's frame stands 3.7 cm into the room (measured, kit check).
            var radiator = T("TrRadiator", "ornate_radiator", 0.58f, Anchor.Back);
            radiator.Face = ViBack + 0.05f;
            yield return radiator;
            yield return T("TrUmbrellas", "decorative_vase_3d_model_Clone1", 0.72f);
            yield return Set(T("TrBust", "wooden_round_table", 0.8f), Tr("classical_bust", 0.5f, 0.5f));
            yield return Hung("TrBarometer", "antique_barometer", 0.45f);

            // A sewing table: the pack's small table, the Tripo machine on it.
            Spec F2(string key, string viFile)
            {
                var spec = S(key, "Tripo", key.Substring(2), SliceKitCatalogue.Vi, viFile, pr, Body.Box, Anchor.Centre);
                spec.BodyTop = ViBodyTop;
                return spec;
            }
        }

        /// <summary>A Tripo file's model name: the bridge's "_3d_model" suffix, unless the name holds it already (a "_Clone1").</summary>
        private static string Model(string file) => file.Contains("_3d_model") ? file : file + "_3d_model";

        /// <summary>
        /// The street lamp's glass (city plan §30.1). The owner's Tripo lamp is one mesh with one material, so its panes
        /// cannot glow alone. Measured on the normalised model (`SliceAssetSurvey.RunTripoProfiles`): the lantern's panes
        /// run from 0.745 to 0.840 up it, their faces 0.0335 out from its axis at the foot and 0.060 at the head. The glass
        /// is eight quads of our own just outside them, two on each face either side of its middle bar and clear of its
        /// corners, in a material whose name holds "glass", which `VeilmouthMystic` lights at night. A child of the model,
        /// so it takes the model's size.
        /// </summary>
        internal static void LampGlass(GameObject model)
        {
            const float foot = 0.750f, head = 0.835f, paneFoot = 0.745f, outAtFoot = 0.0335f, slope = 0.279f, off = 0.0015f, bar = 0.005f, edge = 0.8f;
            KitStandIns.EnsureFolder(Folder + "/Street");
            var meshPath = Folder + "/Street/LampGlass_Mesh.asset";
            var mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "LampGlass" };
                UnityEditor.AssetDatabase.CreateAsset(mesh, meshPath);
            }

            float Out(float y) => outAtFoot + (slope * (y - paneFoot)) + off;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            foreach (var n in new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left })
            {
                var t = Vector3.Cross(Vector3.up, -n); // the viewer's right, looking at the face from outside
                foreach (var (s0, s1, inner0) in new[] { (-edge, -bar, false), (bar, edge, true) })
                {
                    // The span from the middle bar, fixed, to the corner's side, which follows the face's taper.
                    Vector3 At(float y, float s, bool fixedSpan) => (n * Out(y)) + (t * (fixedSpan ? s : s * Out(y))) + (Vector3.up * y);
                    var k = vertices.Count;
                    vertices.AddRange(new[] { At(foot, s0, inner0), At(foot, s1, !inner0), At(head, s1, !inner0), At(head, s0, inner0) });
                    triangles.AddRange(new[] { k, k + 3, k + 2, k, k + 2, k + 1 });
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            UnityEditor.EditorUtility.SetDirty(mesh);

            // Built in the wrapper's frame, upright, where the profile was measured, then hung under the model keeping its
            // place: the Tripo model's own root is turned -90° about X (Z up), so the model's local axes would lay it flat.
            var glass = new GameObject("Lantern glass");
            glass.transform.SetParent(model.transform.parent, false);
            glass.transform.SetParent(model.transform, true);
            glass.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = glass.AddComponent<MeshRenderer>();
            var material = Colour("Kit_LampGlass", new Color(0.32f, 0.33f, 0.31f), false); // by day a dark pane
            material.SetFloat("_Smoothness", 0.85f);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>The Tripo interior pieces' keys, so a check can find them all.</summary>
        public static IEnumerable<string> TripoKeys => TripoSpecs().Select(s => s.Key);
    }
}
