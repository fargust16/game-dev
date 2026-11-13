using System.Collections.Generic;
using GameDev.Slice.Fights;
using GameDev.Slice.Stealth;
using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// A person asleep at home by its hours (S5c.5; 10.3 §6.1, 18): it lies down where it was laid and watches with half
    /// its Perception, so a burglar in Stealth Mode fills its meter slower; anything that wakes it gives it back. Unlike
    /// Дрёма's hold (<see cref="HeldStill"/>), its eyes stay on. Чужой сон reaches it as any sleeper.
    /// </summary>
    public sealed class Slumber : MonoBehaviour
    {
        private static readonly List<Slumber> Sleepers = new List<Slumber>();
        private static readonly int DownHash = Animator.StringToHash("Down");

        private StealthObserver _eyes;
        private int _perception = -1;

        /// <summary>Everyone asleep at home now.</summary>
        public static IReadOnlyList<Slumber> All => Sleepers;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Sleepers.Clear();

        /// <summary>Lays <paramref name="body"/> down asleep.</summary>
        public static Slumber Lay(Transform body)
        {
            var slumber = body.GetComponent<Slumber>();
            return slumber != null ? slumber : body.gameObject.AddComponent<Slumber>();
        }

        /// <summary>The slumber of <paramref name="body"/> or of the person it is part of, or null.</summary>
        public static Slumber Of(Transform body) => body != null ? body.GetComponentInParent<Slumber>() : null;

        /// <summary>Asleep at home or by Дрёма.</summary>
        public static bool Asleep(Transform body) => HeldStill.Sleeping(body) || Of(body) != null;

        private void OnEnable()
        {
            Sleepers.Add(this);
            Down(true);
            _eyes = GetComponent<StealthObserver>();
            if (_eyes != null && _perception < 0)
            {
                _perception = _eyes.Perception;
                _eyes.Perception = Mathf.Max(1, _perception / 2); // 10.3 §6.1: half its Perception
            }
        }

        private void OnDisable() => Sleepers.Remove(this);

        private void Update()
        {
            var fights = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Fights : null;
            if (fights != null && fights.InFight)
            {
                Wake(); // a fight takes it, as it takes one held by Дрёма; a blow at it is struck at a sleeper first
            }
        }

        /// <summary>It wakes: stands up and watches with its whole Perception.</summary>
        public void Wake()
        {
            Down(false);
            if (_eyes != null && _perception >= 0)
            {
                _eyes.Perception = _perception;
            }

            _perception = -1;
            Sleepers.Remove(this);
            Destroy(this);
        }

        private void Down(bool down)
        {
            var animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                return;
            }

            foreach (var p in animator.parameters)
            {
                if (p.nameHash == DownHash)
                {
                    animator.SetBool(DownHash, down);
                    return;
                }
            }
        }
    }
}
