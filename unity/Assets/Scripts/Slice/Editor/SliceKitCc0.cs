using System.Collections.Generic;
using GameDev.Slice.Kit;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The CC0 models of Blender's asset libraries (owner, 2026-11-11): rileyb3d, Charge, Poly Haven and ambientCG, each
    /// CC0-1.0, baked for Unity by <c>tools/blender/assets/cc0_export.py</c> and bound to URP Lit by
    /// <see cref="SliceCc0Import"/>. They come at their true size in metres, so loose props take the kit's game scale
    /// (<see cref="FurnitureScale"/>) as every other pack's. What each replaces or fills is in SLICE_ASSET_MAP.md §4 and
    /// SLICE_MODELS_WANTED.md. Sizes are authoring defaults.
    /// </summary>
    public static partial class SliceKitCatalogue
    {
        private const string Cc0 = SliceCc0Import.Root;

        /// <summary>
        /// The anvil on its block, m to its highest point, about a real anvil's on its block. The blacksmith's blow comes
        /// lower with the forge hammer (0.66 m over his soles, 2026-11-11), so SliceBlacksmith sets the block into the
        /// ground by the difference, at most 0.15 m. Authoring default, checked by the blacksmith's report.
        /// </summary>
        internal const float AnvilHeight = 0.70f;

        /// <summary>CC0 things standing on a set's base, by file: the longest side, m, at game scale (authoring defaults).</summary>
        internal static readonly IReadOnlyDictionary<string, float> Cc0Things = new Dictionary<string, float>
        {
            ["DutchOven"] = 0.38f,  // a cast-iron pot of 0.32 m with its handles
            ["TeaKettle"] = 0.31f,  // a kettle of 0.26 m
            ["BreadLoaf"] = 0.30f,  // ambientCG's loaf
            ["Apple"] = 0.09f,
            ["ChessSet"] = 0.50f,   // the board with its pieces, on the saloon's round table
            ["PocketWatch"] = 0.08f, // its glass left out by the export, so the dial shows
            ["Spectacles"] = 0.16f,  // their lenses left out by the export
        };

        /// <summary>
        /// The pinnace's draught, m: its keel lies 2.40 m under the waterline at its file's origin (measured on the hull's
        /// mesh in Blender, 2026-11-11), so the ship floats with its foot this far under the sea's 24 m.
        /// </summary>
        internal const float ShipDraught = 2.40f;

        /// <summary>
        /// Batch: writes again, keeping their GUIDs, the wrappers whose model became a CC0 one or whose set took CC0
        /// things (2026-11-11), and writes the new ones.
        /// </summary>
        public static void RebuildCc0() => LoadRebuilding(
            "Anvil", "SmithHammer", "Crate", "Box", "Barrel", "Chest", "ViKitchenTable", "ViNightstand", "ViDesk", "ViTableChess",
            "Sacks", "SacksSmall", "Handcart", "WoodCart", "HayBale", "QuenchBucket", "CandlePlate", "RopeCoil", "StepLadder",
            "Boulder", "Dartboard", "DampStain", "CoalHoleCover", "SewerCover", "Ship");

        /// <summary>
        /// Batch, with graphics, after <see cref="RebuildCc0"/> and the city's rebuild: the smith set at his anvil again, the
        /// seven interiors given their CC0 pieces in place and drawn, and the city's CC0 views.
        /// </summary>
        public static void FinishCc0()
        {
            SliceBlacksmith.Run();
            VeilmouthInteriors.EnrichAndRender();
            VeilmouthRenders.RenderCc0();
        }

        private static IEnumerable<Spec> Cc0Specs()
        {
            const string pr = KitLayers.Prop, ng = KitLayers.NoGrid;

            Spec C(string key, string family, string file, string layer = pr, Body body = Body.Box, Anchor anchor = Anchor.Centre) =>
                S(key, family, key, Cc0, file, layer, body, anchor);

            // Cargo, only where the port works and in stores (city plan §25.6): two piles of burlap sacks (rileyb3d).
            yield return Game(C("Sacks", "Props", "BurlapSacksA"));
            yield return Game(C("SacksSmall", "Props", "BurlapSacksB"));
            // A two-wheeled handcart for the quay and the warehouses (rileyb3d), and a farm wagon without its horse at
            // its true 5.2 m, a vehicle and not a loose prop.
            yield return Game(C("Handcart", "Props", "Handcart"));
            yield return C("WoodCart", "Props", "WoodCart");
            // A baled block of hay for the hamlet's farm yards (rileyb3d).
            yield return Game(C("HayBale", "Props", "HayBale"));
            // The smith's quench bucket beside his anvil (Charge): its file is not at true scale, so it is sized, 0.42 m.
            var bucket = C("QuenchBucket", "Market", "IronBucket");
            bucket.SizeY = 0.42f;
            yield return bucket;
            // Melted candles on an iron plate (Charge), round the cellar's ritual circle: seen, never solid.
            yield return Game(C("CandlePlate", "Ritual", "CandlePlate", ng, Body.None));
            // A coil of rope on the quay and the pier (rileyb3d), low, a body like any cargo.
            yield return Game(C("RopeCoil", "Props", "RopeCoil"));
            // A wooden stepladder for a store, at its true 1.34 m (Poly Haven).
            yield return C("StepLadder", "Props", "StepLadder");
            // A boulder of the land outside the walls (Poly Haven), Full Cover as the forest's trunks: wrapped for the
            // land pass (city plan §26.2 12b), placed by none yet.
            yield return C("Boulder", "Rocks", "Boulder", KitLayers.Structure);

            // A dartboard for the Soot & Bell's public bar, hung like a picture on its 3 m wall segment (Poly Haven).
            var dart = C("Dartboard", "Interior", "Dartboard", ng, Body.None, Anchor.Face);
            dart.Width = ViModule;
            dart.Face = ViFace;
            yield return dart;
            // Damp running down a cellar's brick from above (ambientCG Leaking002 as an upright cut-out square, 2 m), on the
            // brick a little in front of its line; the timbers stand before it.
            var damp = C("DampStain", "Interior", "DampStain", ng, Body.None, Anchor.Face);
            damp.Face = ViStoneInfill + 0.005f;
            yield return damp;
            // Covers set into the paving (ambientCG, flat cut-out squares): a coal hole's plate by a house's door and a
            // sewer's on a carriageway (VeilmouthCity.Covers).
            yield return C("CoalHoleCover", "Street", "CoalHoleCover", ng, Body.None);
            yield return C("SewerCover", "Street", "SewerCover", ng, Body.None);

            // A pinnace under bare yards moored at Tipsy Pier (Poly Haven, 39 m over its bowsprit, 120,000 triangles; city
            // plan §12: no ship stood until a model came). Its body stops at its rail, so the camera never meets its masts.
            var ship = S("Ship", "Port", "Pinnace", Cc0, "ShipPinnace", KitLayers.Structure, Body.Box, Anchor.Centre);
            ship.BodyTop = 9f;
            yield return ship;
        }
    }
}
