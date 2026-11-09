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
        /// Tripo things standing on a set's base, by file: the longest side, m, and a turn about X that lays a flat
        /// thing down (the parchment stands upright in its file). Authoring defaults.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, (float Longest, float Pitch)> TripoThings = new Dictionary<string, (float, float)>
        {
            ["decorative_table_lamp_3d_model"] = (0.6f, 0f),
            ["aged_parchment_3d_model"] = (0.34f, -90f), // its written face (+Z) turned up
            ["vintage_gramophone_3d_model"] = (0.65f, 0f),
            ["sewing_machine_3d_model"] = (0.46f, 0f),
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
                var spec = S(key, "Tripo", key.Substring(2), Tripo, file + "_3d_model", layer, body, anchor);
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
            (string, string, float, float) Tr(string file, float fx, float fz) => (Tripo, file + "_3d_model", fx, fz);

            // Against a wall. True sizes: a wardrobe 1.9 m high, a chest of drawers 0.88 m, a writing desk with its
            // gallery 0.95 m, a nightstand 0.67 m, a cheval glass 1.55 m, a folding screen 1.6 m, an upright piano 1.2 m,
            // a narrow glazed cabinet 1.65 m.
            yield return T("TrWardrobe", "antique_wardrobe", 2.3f, Anchor.Back);
            yield return Set(T("TrNightstand", "antique_wood_nightstand", 0.8f, Anchor.Back), Vi("Candle_1", 0.3f, 0.6f), Vi("Book_1", 0.72f, 0.6f));
            yield return Set(T("TrNightstandLamp", "antique_wood_nightstand", 0.8f, Anchor.Back), Tr("decorative_table_lamp", 0.5f, 0.55f));
            yield return Set(T("TrDresser", "antique_wooden_dresser", 1.05f, Anchor.Back), Vi("Brush_1", 0.3f, 0.6f), Vi("Box_2", 0.72f, 0.6f));
            yield return Set(T("TrDesk", "antique_wooden_desk", 1.15f, Anchor.Back),
                Tr("decorative_table_lamp", 0.14f, 0.6f), Tr("aged_parchment", 0.48f, 0.72f), Vi("Ink", 0.76f, 0.62f), Vi("Pen", 0.82f, 0.66f));
            yield return T("TrMirror", "ornate_mirror", 1.85f, Anchor.Back);
            yield return T("TrScreen", "ornate_wooden_screen", 1.9f, Anchor.Back);
            yield return T("TrPiano", "ornate_piano", 1.45f, Anchor.Back);
            yield return T("TrDisplay", "furniture", 2f, Anchor.Back);

            // In the room. True sizes: a wing chair 1.05 m high, a coat stand 1.5 m, a floor vase 0.75 m, a log basket
            // 0.55 m, a stand of fire irons 0.67 m, a pedestal table 0.67 m.
            yield return T("TrArmchair", "red_velvet_armchair", 1.25f);
            yield return T("TrCoatRack", "wooden_coat_rack", 1.8f);
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

            // Under a ceiling higher than a storey only (rules G9): 1.2 m true.
            var chandelier = T("TrChandelier", "ornate_chandelier", 1.4f, Anchor.Centre, ng, Body.None);
            chandelier.Top = true;
            yield return chandelier;

            // A sewing table: the pack's small table, the Tripo machine on it.
            Spec F2(string key, string viFile)
            {
                var spec = S(key, "Tripo", key.Substring(2), SliceKitCatalogue.Vi, viFile, pr, Body.Box, Anchor.Centre);
                spec.BodyTop = ViBodyTop;
                return spec;
            }
        }

        /// <summary>The Tripo interior pieces' keys, so a check can find them all.</summary>
        public static IEnumerable<string> TripoKeys => TripoSpecs().Select(s => s.Key);
    }
}
