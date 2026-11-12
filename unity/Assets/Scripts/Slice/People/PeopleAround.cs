using System.Collections.Generic;
using GameDev.Locomotion;
using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// The people of the loaded scenes a blow can reach and the Wardens among them (10.2 §5c): every human body whose
    /// controller carries the fight, a walker or a named person by its own root, never one out of sight inside a door.
    /// Any other person is the body that carries its capsule, as a Warden on post, a prefab whose model is a child.
    /// </summary>
    public static class PeopleAround
    {
        private static readonly int DownHash = Animator.StringToHash("Down");

        /// <summary>Every person in sight who can fight: its root, the walker's or the named person's where it has one.</summary>
        public static List<Transform> InSight()
        {
            var people = new List<Transform>();
            foreach (var animator in Object.FindObjectsByType<Animator>(FindObjectsInactive.Exclude))
            {
                if (!animator.isHuman || animator.runtimeAnimatorController == null || !Fights(animator))
                {
                    continue;
                }

                var walker = animator.GetComponentInParent<TownsfolkWalker>();
                if (walker != null && walker.Inside)
                {
                    continue;
                }

                var named = animator.GetComponentInParent<NamedPerson>();
                var body = walker != null ? walker.transform : named != null ? named.transform : Body(animator.transform);
                if (!people.Contains(body))
                {
                    people.Add(body);
                }
            }

            return people;
        }

        /// <summary>A Warden: a body in a Warden's uniform, its controller the Wardens' (SliceClips).</summary>
        public static bool IsWarden(Transform body)
        {
            var animator = body != null ? body.GetComponentInChildren<Animator>() : null;
            var controller = animator != null ? animator.runtimeAnimatorController : null;
            return controller != null && controller.name.StartsWith("Warden", System.StringComparison.Ordinal);
        }

        /// <summary>The Wardens in sight, nearest <paramref name="to"/> first.</summary>
        public static List<Transform> Wardens(Vector3 to)
        {
            var wardens = InSight().FindAll(IsWarden);
            wardens.Sort((a, b) => (a.position - to).sqrMagnitude.CompareTo((b.position - to).sqrMagnitude));
            return wardens;
        }

        /// <summary>Whether <paramref name="c"/> is part of a person, the player's too: people stop no sight and are no ground.</summary>
        public static bool IsPerson(Collider c) =>
            c != null && (c.GetComponentInParent<Animator>() != null || c.GetComponentInParent<TownsfolkWalker>() != null || c.GetComponentInParent<PlayerLocomotion>() != null
                          || (c is CapsuleCollider && c.GetComponentInChildren<Animator>() != null));

        /// <summary>The highest surface under <paramref name="at"/> within reach that is no person; <paramref name="at"/> itself when none.</summary>
        public static Vector3 Ground(Vector3 at, float above = 2f, float depth = 5f)
        {
            var best = float.MaxValue;
            var y = at.y;
            foreach (var hit in Physics.RaycastAll(at + (Vector3.up * above), Vector3.down, depth, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance < best && !IsPerson(hit.collider))
                {
                    best = hit.distance;
                    y = hit.point.y;
                }
            }

            return new Vector3(at.x, y, at.z);
        }

        // The nearest of the model and its parents that carries a capsule: the whole person moves with it.
        private static Transform Body(Transform model)
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

        private static bool Fights(Animator animator)
        {
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == DownHash)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
