using System.Linq;
using GameDev.World.Portals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The Soot & Bell's back door to its yard (owner, VI-8 second follow-up: a door leads out or goes). The house is
    /// baked again with the pack's own shop door in its yard side (<see cref="VictorianHouses.Spec.YardDoors"/>), and a
    /// portal leaf is set in that door, paired with the interior's yard door (`soot-and-bell.yard.inside`). The city
    /// scene is the owner's: this touches only its own leaf, and run again it replaces that leaf.
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
            VictorianHouses.BakeOne("Tavern6", "brick");
            var kit = SliceKitCatalogue.Load();
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
            foreach (var old in all.Where(t => t != null && t.name == $"Portal - {PortalId}").ToList())
            {
                Object.DestroyImmediate(old.gameObject);
            }

            // The door's middle: on the house's Left side (local x 0, east in the city), in its module counted from the
            // back; the leaf's local +Z points out into the yard, where a player coming out arrives.
            var house = all.First(t => t != null && t.name == HouseName);
            var module = VictorianHouses.Types.Single(t => t.Name == "Tavern6").YardDoors[0];
            var middle = house.TransformPoint(0f, 0f, (module + 0.5f) * VictorianHouses.Module);
            var rotation = Quaternion.LookRotation(-house.right, Vector3.up);
            var start = middle - (rotation * Vector3.right * 1.5f);
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(kit["PortalLeaf"].Prefab, house.parent);
            leaf.transform.SetPositionAndRotation(new Vector3(Mathf.Round(start.x), Mathf.Round(middle.y * 10f) / 10f, Mathf.Round(start.z)), rotation);
            var portal = leaf.GetComponent<Portal>();
            portal.Configure(PortalId, SliceTestScenes.SceneName(VeilmouthScene.SootAndBellScenePath), InsidePortalId, true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
            leaf.name = $"Portal - {PortalId}";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var pose = portal.ArrivalPose;
            Debug.Log($"[VeilmouthYardDoor] the yard door at {leaf.transform.position:F2}, turn {leaf.transform.eulerAngles.y:F0}; " +
                      $"a player coming out stands at {pose.position:F2} facing {pose.forward:F1}.");
        }
    }
}
