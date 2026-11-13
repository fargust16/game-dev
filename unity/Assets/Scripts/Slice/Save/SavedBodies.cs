using System;
using System.Collections.Generic;
using GameDev.Locomotion;
using GameDev.Slice.People;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Save
{
    /// <summary>
    /// Every person of the loaded scenes in a Suspend Save (17 X.3; the owner's answer to S5-7: one killed does not rise
    /// again, and each stands where it stood): a walker, a named person, a Warden on post, by the body that carries it.
    /// Its place and facing, whether it is out in the world, and whether it lies fallen (<see cref="FallenBody"/>), with
    /// the fall it fell by.
    /// </summary>
    public static class SavedBodies
    {
        private static readonly int DownHash = Animator.StringToHash("Down");

        [Serializable]
        private sealed class Saved
        {
            public List<Body> bodies = new List<Body>();
        }

        [Serializable]
        private sealed class Body
        {
            public string id;
            public Vector3 at;
            public Quaternion facing;
            public bool active;
            public bool down;
            public float death;
        }

        public static string Capture()
        {
            var saved = new Saved();
            foreach (var body in People())
            {
                var fallen = body.GetComponent<FallenBody>();
                saved.bodies.Add(new Body
                {
                    id = SavedObjects.PathOf(body),
                    at = body.position,
                    facing = body.rotation,
                    active = body.gameObject.activeSelf,
                    down = fallen != null,
                    death = fallen != null ? fallen.Death : 0f,
                });
            }

            return JsonUtility.ToJson(saved);
        }

        /// <summary>Puts back the people of <paramref name="scene"/>, after its walkers took up what they were doing.</summary>
        public static void Restore(string payload, Scene scene)
        {
            foreach (var b in JsonUtility.FromJson<Saved>(payload).bodies)
            {
                if (SavedObjects.SceneOf(b.id) != scene.name)
                {
                    continue;
                }

                var body = SavedObjects.FindPath(b.id);
                if (body == null)
                {
                    continue;
                }

                body.gameObject.SetActive(b.active);
                body.SetPositionAndRotation(b.at, b.facing);
                FallenBody.Mark(body, b.down, b.death);
                if (b.down)
                {
                    body.GetComponent<FallenBody>().Lay();
                }
            }
        }

        /// <summary>
        /// Every person's body, the player's left out: one with a walker or a named person, or whose controller carries the
        /// fight's Down (read from the asset's name while its scene is switched off: the Wardens'), by the body with its capsule.
        /// </summary>
        private static List<Transform> People()
        {
            var people = new List<Transform>();
            var seen = new HashSet<Transform>();
            foreach (var animator in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (animator.runtimeAnimatorController == null || !animator.gameObject.scene.IsValid()
                    || animator.GetComponentInParent<PlayerLocomotion>(true) != null)
                {
                    continue;
                }

                var walker = animator.GetComponentInParent<TownsfolkWalker>(true);
                var named = animator.GetComponentInParent<NamedPerson>(true);
                if (walker == null && named == null && !Fights(animator))
                {
                    continue;
                }

                var body = walker != null ? walker.transform : named != null ? named.transform : Capsule(animator.transform);
                if (seen.Add(body))
                {
                    people.Add(body);
                }
            }

            return people;
        }

        private static bool Fights(Animator animator)
        {
            if (!animator.isActiveAndEnabled)
            {
                return animator.runtimeAnimatorController.name.StartsWith("Warden", StringComparison.Ordinal);
            }

            if (!animator.isHuman)
            {
                return false;
            }

            foreach (var p in animator.parameters)
            {
                if (p.nameHash == DownHash)
                {
                    return true;
                }
            }

            return false;
        }

        private static Transform Capsule(Transform model)
        {
            for (var t = model; t != null; t = t.parent)
            {
                if (t.GetComponent<CapsuleCollider>() != null)
                {
                    return t;
                }
            }

            return model;
        }
    }
}
