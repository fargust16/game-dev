using System.Collections.Generic;
using System.Linq;
using GameDev.World.Portals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The Soot & Bell's back door to its yard (owner, VI-8 second follow-up: a door leads out or goes). The kit's tavern
    /// has a door in its yard side, marked <see cref="VeilmouthKitHouses.YardDoorMarker"/> when it is baked, and a portal
    /// leaf is set in that door, paired with the interior's yard door (`soot-and-bell.yard.inside`). The city builder sets
    /// it with the house; <see cref="Place"/> sets it again in a scene that is the owner's, touching only its own leaf.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthYardDoor.Place
    /// </summary>
    internal static class VeilmouthYardDoor
    {
        private const string HouseName = "The Soot & Bell";
        public const string PortalId = VeilmouthScene.SootAndBellId + ".yard";
        public const string InsidePortalId = VeilmouthScene.SootAndBellId + ".yard.inside";

        [MenuItem("GameDev/Slice/Place the Soot and Bell's Yard Door")]
        public static void Place()
        {
            var kit = SliceKitCatalogue.Load();
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
            foreach (var old in all.Where(t => t != null && t.name == $"Portal - {PortalId}").ToList())
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var house = all.First(t => t != null && t.name == HouseName);
            var portal = Leaf(house, kit, house.parent).GetComponent<Portal>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var pose = portal.ArrivalPose;
            Debug.Log($"[VeilmouthYardDoor] the yard door at {portal.transform.position:F2}, turn {portal.transform.eulerAngles.y:F0}; " +
                      $"a player coming out stands at {pose.position:F2} facing {pose.forward:F1}.");
        }

        /// <summary>
        /// The leaf in the house's yard door: centred on the marker at its sill, its local +Z out into the yard, where a
        /// player coming out arrives.
        /// </summary>
        internal static GameObject Leaf(Transform house, Dictionary<string, SliceKitCatalogue.Entry> kit, Transform parent)
        {
            var marker = house.Cast<Transform>().First(t => t.name == VeilmouthKitHouses.YardDoorMarker);
            var start = marker.position - (marker.right * 1.5f);
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(kit["PortalLeaf"].Prefab, parent);
            leaf.transform.SetPositionAndRotation(new Vector3(Mathf.Round(start.x), Mathf.Round(marker.position.y * 10f) / 10f, Mathf.Round(start.z)),
                marker.rotation);
            var portal = leaf.GetComponent<Portal>();
            portal.Configure(PortalId, SliceTestScenes.SceneName(VeilmouthScene.SootAndBellScenePath), InsidePortalId, true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
            leaf.name = $"Portal - {PortalId}";
            return leaf;
        }
    }
}
