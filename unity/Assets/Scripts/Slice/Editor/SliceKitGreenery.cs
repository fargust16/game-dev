using System.Collections.Generic;
using GameDev.Slice.Kit;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The city's greenery and the roadside's fences as kit blocks (VEILMOUTH_CITY_PLAN.md §25, owner 2026-11-09): the
    /// Stylized Trees Pack's trees and bushes in the city's muted green (<c>City*</c>, retinted on wrapping), clipped
    /// hedges, and Roadside Tales' walls, fences and posts for outside the walls.
    /// </summary>
    public static partial class SliceKitCatalogue
    {
        private const string Roadside = "Assets/EmaceArt/Fences 2026/Prefabs";

        /// <summary>A city tree's height, m: under the eaves of a three-storey row, the forest's are 9–13 m (§25.1; authoring default).</summary>
        public const float CityTreeHeight = 8f;

        /// <summary>
        /// A hedge block, m: one metre of line with the bush stretched to 1.3 m along it, so neighbours overlap 0.15 m
        /// at each end and the hedge has no hole; 0.9 m high, low cover like a railing; 0.7 m deep. Authoring defaults.
        /// </summary>
        public const float HedgeLength = 1.3f, HedgeHeight = 0.9f, HedgeDepth = 0.7f;

        private static IEnumerable<Spec> GreenerySpecs()
        {
            const string pr = KitLayers.Prop;
            foreach (var n in new[] { "Birch 001", "Birch 002", "Birch 003", "Warped 001", "Warped 002", "Warped 003" })
            {
                var part = n.Replace(" 00", "");
                var tree = S("CityTree_" + part, "Tree", "City" + part, Trees + "/Trees", n, KitLayers.Vegetation, Body.Trunk, Anchor.Pivot);
                tree.SizeY = CityTreeHeight;
                yield return tree;
            }

            // Garden bushes, in the Polytope shrub's place inside the walls.
            foreach (var n in new[] { "Bush 006", "Bush 011", "Bush 015" })
            {
                var bush = S("CityBush_" + n.Substring(5), "Bush", "City" + n.Substring(5), Trees + "/Bushes", n, KitLayers.NoGrid, Body.None, Anchor.Pivot);
                bush.SizeY = 1.4f;
                yield return bush;
            }

            // Hedges: the pack's two compact round bushes (measured from their meshes' bounds, 1.7 m across at 1.7 m high),
            // clipped to a metre of line.
            foreach (var n in new[] { "Bush 019", "Bush 020" })
            {
                var hedge = S("CityHedge_" + n.Substring(5), "Hedge", "City" + n.Substring(5), Trees + "/Bushes", n, pr, Body.Box, Anchor.Linear);
                hedge.Width = 1;
                hedge.SizeX = HedgeLength;
                hedge.SizeY = HedgeHeight;
                hedge.SizeZ = HedgeDepth;
                hedge.Uniform = false;
                yield return hedge;
            }

            // Roadside Tales (§25.5), measured from the pack's meshes with tools/assets/fbx_sections.py: the dry-stone
            // wall is 3.31 m long over 6.33 m of fallen stones and is joined every 3 m, as the pack's demo pen joins it
            // every 3.28 m; the wooden fences are scaled along to 2 m from 2.21, 1.56 and 2.38 m, and held at low cover.
            var wall = S("RoadWall", "Roadside", "DryStoneWall", Roadside, "EA03_Fence_Cobble_01b_PRE", pr, Body.Box, Anchor.Linear);
            wall.Width = 3;
            yield return wall;
            foreach (var (key, file, part, height) in new[]
                     {
                         ("FieldFenceA", "EA03_Fence_ByAFieldField_01a_PRE", "FieldA", 1f), ("FieldFenceB", "EA03_Fence_ByAFieldField_01b_PRE", "FieldB", 1f),
                         ("FieldFenceC", "EA03_Fence_ByAFieldField_01c_PRE", "FieldC", 1f),
                         ("PlankFenceA", "EA03_Fence_Plank_01a_PRE", "PlankA", 1.2f), ("PlankFenceB", "EA03_Fence_Plank_01b_PRE", "PlankB", 1.2f),
                         ("PlankFenceC", "EA03_Fence_Plank_01c_PRE", "PlankC", 1.2f),
                         ("BedFenceA", "EA03_Fence_VegetablePatch_02a_PRE", "BedA", 0.8f), ("BedFenceB", "EA03_Fence_VegetablePatch_02b_PRE", "BedB", 0.8f),
                     })
            {
                var fence = S(key, "Roadside", part, Roadside, file, pr, Body.Box, Anchor.Linear);
                fence.Width = 2;
                fence.SizeX = 2f;
                fence.SizeY = height;
                fence.Uniform = false;
                yield return fence;
            }

            var post = S("RoadPost", "Roadside", "Post", Roadside, "EA03_Wooden_Pin_01j_PRE", KitLayers.NoGrid, Body.None, Anchor.Pivot);
            yield return post;
        }
    }
}
