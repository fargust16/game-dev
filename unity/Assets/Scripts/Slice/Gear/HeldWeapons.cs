using GameDev.Confront.Rules;
using UnityEngine;

namespace GameDev.Slice.Gear
{
    /// <summary>
    /// The weapons a body holds (10.2 §5c): its Weapon Set's models in its hands while drawn, none while sheathed, as a
    /// sheathed weapon is out of sight in the slice. Each model is set in its fist by the hand's own bones, the same in
    /// every pose: the grip a little in from the knuckles toward the palm, the weapon leaving the fist on the index
    /// finger's side and turning its edge the knuckles' way, a shield's face the back of the left hand's.
    /// </summary>
    public sealed class HeldWeapons : MonoBehaviour
    {
        public const string MainName = "Held_Main";
        public const string OffName = "Held_Off";

        /// <summary>Authoring default, m: the grip's middle lies this far in front of the palm's bones, as a tool's (SliceTools).</summary>
        public const float PalmDepth = 0.03f;

        private Animator _animator;
        private GameObject _main;
        private GameObject _off;
        private GameObject _mainSource;
        private GameObject _offSource;

        /// <summary>True while the weapons are in the hands.</summary>
        public bool Drawn { get; private set; }

        /// <summary>The model in the right hand, or null.</summary>
        public GameObject Main => _main;

        /// <summary>The model in the left hand, a shield or the second weapon, or null.</summary>
        public GameObject Off => _off;

        /// <summary>The body's held weapons, added the first time they are asked for.</summary>
        public static HeldWeapons Of(Component body)
        {
            var held = body.GetComponent<HeldWeapons>();
            return held != null ? held : body.gameObject.AddComponent<HeldWeapons>();
        }

        /// <summary>The body changed under the root (S5b.1): the held models went with the old hands, and the next draw finds the new ones.</summary>
        public void Rebind()
        {
            _animator = null;
            _main = _off = _mainSource = _offSource = null;
            Drawn = false;
        }

        /// <summary>The set's models into the hands: the weapon in the right, a shield or the second weapon in the left; fists and a Cartridge Pouch show none.</summary>
        public void Draw(WeaponModels models, WeaponSet set)
        {
            var main = models != null ? models.For(set.Weapon) : null;
            var off = models == null ? null : set.OffWeapon != null ? models.For(set.OffWeapon) : models.For(set.OffHand);
            Put(ref _main, ref _mainSource, main, true, MainName);
            Put(ref _off, ref _offSource, off, false, OffName);
            Drawn = true;
        }

        /// <summary>The weapons out of sight.</summary>
        public void Sheathe()
        {
            if (_main != null)
            {
                _main.SetActive(false);
            }

            if (_off != null)
            {
                _off.SetActive(false);
            }

            Drawn = false;
        }

        private Animator Animator => _animator != null ? _animator : _animator = GetComponentInChildren<Animator>();

        private void Put(ref GameObject held, ref GameObject source, GameObject prefab, bool right, string name)
        {
            if (held != null && prefab != source)
            {
                if (Application.isPlaying)
                {
                    Destroy(held);
                }
                else
                {
                    DestroyImmediate(held);
                }

                held = null;
            }

            source = prefab;
            if (prefab == null || Animator == null || !Animator.isHuman)
            {
                return;
            }

            if (held == null)
            {
                var hand = Animator.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
                if (hand == null)
                {
                    return;
                }

                held = Instantiate(prefab, hand);
                held.name = name;
                var grip = Grip(Animator, right);
                held.transform.SetLocalPositionAndRotation(grip.position, grip.rotation);
                held.transform.localScale = Vector3.one / Mathf.Max(1e-4f, hand.lossyScale.x);
            }

            held.SetActive(true);
        }

        /// <summary>
        /// Where a held model's origin and axes stand in the hand's own space: the grip <see cref="PalmDepth"/> in front of
        /// the palm, 0.6 of the way from the wrist to the middle knuckle; +Z from the little finger's knuckle to the index
        /// finger's, +Y the knuckles' way. The knuckles stand where they stand in any pose, so this holds in every one.
        /// </summary>
        public static Pose Grip(Animator animator, bool right)
        {
            var hand = animator.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            var index = animator.GetBoneTransform(right ? HumanBodyBones.RightIndexProximal : HumanBodyBones.LeftIndexProximal);
            var little = animator.GetBoneTransform(right ? HumanBodyBones.RightLittleProximal : HumanBodyBones.LeftLittleProximal);
            if (little == null)
            {
                little = animator.GetBoneTransform(right ? HumanBodyBones.RightRingProximal : HumanBodyBones.LeftRingProximal);
            }

            var middle = animator.GetBoneTransform(right ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal);
            if (middle == null)
            {
                middle = index;
            }

            Vector3 fingers, across;
            if (index != null && little != null && middle != null)
            {
                fingers = (middle.position - hand.position).normalized;
                across = Vector3.ProjectOnPlane(index.position - little.position, fingers).normalized;
            }
            else
            {
                // No finger bones: the forearm's way, the thumb's side up across it.
                var forearm = animator.GetBoneTransform(right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);
                fingers = (hand.position - forearm.position).normalized;
                across = Vector3.ProjectOnPlane(Vector3.up, fingers).normalized;
            }

            var palm = right ? Vector3.Cross(fingers, across) : Vector3.Cross(across, fingers);
            var knuckle = middle != null ? middle.position : hand.position + (fingers * 0.08f);
            var at = Vector3.Lerp(hand.position, knuckle, 0.6f) + (palm * PalmDepth);
            var rotation = Quaternion.LookRotation(across, fingers);
            return new Pose(hand.InverseTransformPoint(at), Quaternion.Inverse(hand.rotation) * rotation);
        }
    }
}
