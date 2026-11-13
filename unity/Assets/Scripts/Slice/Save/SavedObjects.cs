using System;
using System.Collections.Generic;
using System.Globalization;
using GameDev.Core.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Save
{
    /// <summary>
    /// The things of the loaded scenes a Suspend Save keeps (17 X.3) and the ids it finds them by again: a placed thing's
    /// path in its scene, each step a name and its place among its siblings, the same on every run of the same scene.
    /// Every id begins with its scene's name, so the things of a city are put back before an interior is entered.
    /// </summary>
    public static class SavedObjects
    {
        /// <summary>The path id of <paramref name="t"/>: its scene, then each step from the root, name#index.</summary>
        public static string PathOf(Transform t)
        {
            if (t == null)
            {
                return null;
            }

            var steps = new List<string>();
            for (var x = t; x != null; x = x.parent)
            {
                steps.Add(x.name + "#" + x.GetSiblingIndex().ToString(CultureInfo.InvariantCulture));
            }

            steps.Reverse();
            return t.gameObject.scene.name + "/" + string.Join("/", steps);
        }

        /// <summary>The scene an id belongs to.</summary>
        public static string SceneOf(string id)
        {
            var slash = id != null ? id.IndexOf('/') : -1;
            return slash > 0 ? id.Substring(0, slash) : string.Empty;
        }

        /// <summary>The thing a path id names, in its loaded scene, active or not; null when it is gone.</summary>
        public static Transform FindPath(string id)
        {
            var scene = SceneManager.GetSceneByName(SceneOf(id));
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            var steps = id.Substring(scene.name.Length + 1).Split('/');
            Transform at = null;
            foreach (var step in steps)
            {
                var hash = step.LastIndexOf('#');
                if (hash < 0 || !int.TryParse(step.Substring(hash + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var index))
                {
                    return null;
                }

                var name = step.Substring(0, hash);
                at = at == null ? Root(scene, name, index) : Child(at, name, index);
                if (at == null)
                {
                    return null;
                }
            }

            return at;
        }

        /// <summary>Every saved thing of the loaded scenes, active or not, by its id.</summary>
        public static List<ISavedObject> All()
        {
            var found = new List<ISavedObject>();
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour is ISavedObject saved && behaviour.gameObject.scene.IsValid())
                {
                    found.Add(saved);
                }
            }

            return found;
        }

        /// <summary>Each saved thing's state by its id; a thing with nothing to keep gives null and is left out.</summary>
        public static string Capture()
        {
            var entries = new List<KeyValuePair<string, string>>();
            var seen = new HashSet<string>();
            foreach (var saved in All())
            {
                var id = saved.SaveId;
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                {
                    continue; // two things of one id: the first is kept, the second starts as the scene has it
                }

                var state = saved.CaptureState();
                if (state != null)
                {
                    entries.Add(new KeyValuePair<string, string>(id, state));
                }
            }

            return SaveText.Join(entries);
        }

        /// <summary>
        /// Puts back the saved things of <paramref name="scene"/>: every one lets go of what it holds first, then the
        /// knocked props stand where they lay, then the rest, so a person sits on a chair standing where it stood.
        /// </summary>
        public static void Restore(string payload, Scene scene)
        {
            var saved = new Dictionary<string, string>();
            foreach (var entry in SaveText.Split(payload))
            {
                saved[entry.Key] = entry.Value;
            }

            var here = All().FindAll(s => s is Component c && c.gameObject.scene == scene);
            foreach (var thing in here)
            {
                thing.ClearState();
            }

            here.Sort((a, b) => Order(a).CompareTo(Order(b)));
            foreach (var thing in here)
            {
                if (saved.TryGetValue(thing.SaveId, out var state))
                {
                    try
                    {
                        thing.RestoreState(state);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Save] {thing.SaveId} could not be put back: {e.Message}");
                    }
                }
            }
        }

        private static int Order(ISavedObject thing) => thing is Kit.LooseProp ? 0 : 1;

        private static Transform Root(Scene scene, string name, int index)
        {
            Transform byName = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != name)
                {
                    continue;
                }

                if (root.transform.GetSiblingIndex() == index)
                {
                    return root.transform;
                }

                byName ??= root.transform;
            }

            return byName;
        }

        private static Transform Child(Transform parent, string name, int index)
        {
            if (index < parent.childCount && parent.GetChild(index).name == name)
            {
                return parent.GetChild(index);
            }

            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
