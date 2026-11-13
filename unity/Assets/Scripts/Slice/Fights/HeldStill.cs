using System.Collections.Generic;
using GameDev.Slice.People;
using GameDev.Slice.Stealth;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// 05.1 *Вне боя* (S5b.8): a person an ability holds out of a fight. Asleep by Дрёма, it lies down until its ticks run
    /// out, a fight takes it, or a noticed theft or a failed Чужой сон wakes it; frozen by Стой!, it stands for its ticks
    /// and calls no help. Its walk and its eyes stop meanwhile, so it neither walks on nor notices; both come back as it
    /// is let go, the walk only when no fight holds it.
    /// </summary>
    public sealed class HeldStill : MonoBehaviour
    {
        private static readonly List<HeldStill> Holding = new List<HeldStill>();
        private static readonly int DownHash = Animator.StringToHash("Down");
        private static readonly int IdleState = Animator.StringToHash("Idle");

        private TownsfolkWalker _walker;
        private StealthObserver _eyes;
        private Animator _animator;
        private bool _on;
        private bool _walked;
        private bool _watched;
        private bool _laid;

        /// <summary>Everyone held now.</summary>
        public static IReadOnlyList<HeldStill> All => Holding;

        /// <summary>Asleep, lying; otherwise frozen, standing.</summary>
        public bool Asleep { get; private set; }

        /// <summary>The world tick it is let go at.</summary>
        public long Until { get; private set; }

        /// <summary>Whether it holds its person now.</summary>
        public bool On => _on;

        /// <summary>Holds <paramref name="body"/> asleep or frozen until the tick <paramref name="until"/>; a hold already on it is replaced.</summary>
        public static HeldStill Hold(Transform body, bool asleep, long until)
        {
            var held = body.GetComponent<HeldStill>();
            if (held == null)
            {
                held = body.gameObject.AddComponent<HeldStill>();
            }

            held.Begin(asleep, until);
            return held;
        }

        /// <summary>The hold on <paramref name="body"/> or on the person it is part of, while one is on; otherwise null.</summary>
        public static HeldStill Of(Transform body)
        {
            var held = body != null ? body.GetComponentInParent<HeldStill>() : null;
            return held != null && held._on ? held : null;
        }

        public static bool Sleeping(Transform body) => Of(body) is HeldStill held && held.Asleep;

        public static bool Frozen(Transform body) => Of(body) is HeldStill held && !held.Asleep;

        private void Begin(bool asleep, long until)
        {
            if (_on)
            {
                Restore(false);
            }

            Asleep = asleep;
            Until = until;
            _walker = GetComponent<TownsfolkWalker>();
            _eyes = GetComponentInChildren<StealthObserver>();
            _animator = GetComponentInChildren<Animator>();

            // The eyes close first: their OnDisable lets a walk they had paused go on, and the walk stops next.
            _watched = _eyes != null && _eyes.enabled;
            if (_watched)
            {
                _eyes.enabled = false;
            }

            _walked = _walker != null && _walker.enabled;
            if (_walked)
            {
                _walker.enabled = false;
            }

            _laid = asleep && _animator != null && System.Array.Exists(_animator.parameters, p => p.nameHash == DownHash);
            if (_laid)
            {
                _animator.SetBool(DownHash, true);
            }
            else if (_animator != null && _animator.HasState(0, IdleState))
            {
                _animator.CrossFadeInFixedTime(IdleState, 0.15f, 0);
            }

            _on = true;
            if (!Holding.Contains(this))
            {
                Holding.Add(this);
            }
        }

        /// <summary>
        /// Lets it go: its eyes open, it stands up, and it walks again unless <paramref name="keepStill"/>, when a fight
        /// takes it. True when it was walking before the hold.
        /// </summary>
        public bool Release(bool keepStill = false)
        {
            if (!_on)
            {
                return false;
            }

            _on = false;
            Holding.Remove(this);
            return Restore(keepStill);
        }

        private bool Restore(bool keepStill)
        {
            var walked = _walked;
            if (_laid && _animator != null)
            {
                _animator.SetBool(DownHash, false);
            }

            if (_watched && _eyes != null)
            {
                _eyes.enabled = true;
            }

            if (walked && !keepStill && _walker != null)
            {
                _walker.enabled = true;
            }

            _laid = _watched = _walked = false;
            return walked;
        }

        private void Update()
        {
            var world = SliceWorldDriver.Running;
            if (_on && world != null && world.Clock != null && world.Clock.Tick >= Until)
            {
                Release();
            }
        }

        private void OnDestroy() => Holding.Remove(this);
    }
}
