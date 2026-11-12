using System;
using GameDev.Cameras;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Slice.Fights;
using UnityEngine;

namespace GameDev.Slice.Gear
{
    /// <summary>
    /// 10.2 §5e, the Aimed Shot (owner, 2026-11-12; PB8b). Out of a fight, with a firearm drawn, the right mouse button
    /// held raises the firearm to the eye as at a shooting range: the camera leaves its orbit for the eye and narrows,
    /// the firearm is set on the camera's line by its own rear and front sight, measured on its model, so the front sight
    /// stands at the screen's centre and is the only mark; the hands hold it there, the upper body in the firearm's aim
    /// clip, and the body faces the aim and walks any way (<see cref="PlayerLocomotion.AimFacing"/>). A click fires a
    /// projectile from the muzzle at what the screen's centre shows (<see cref="SliceProjectile"/>), which is a shot heard
    /// 30 cells round; the next shot waits the firearm's loading, 1.5 s for a pistol and 2.5 s for a rifle, the load clip
    /// playing over the walk and the firearm leaving the eye meanwhile. A shot without the aim goes the same way, the body
    /// turned to it first. What the projectile touches is told to <see cref="Touched"/>.
    /// <para>
    /// It runs before the camera, which reads the eye, and does its work on the bones once the camera is placed
    /// (<see cref="IAimView.Placed"/>), after the Animator's pose. The camera's numbers are on the tuning page.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerAim : MonoBehaviour, IAimView
    {
        /// <summary>The player controller's layer for the upper body's aim and load (SliceClips).</summary>
        public const string Layer = "Aim";

        public const string AimState = "Aim";
        public const string PistolLoadState = "PistolLoad";
        public const string RifleLoadState = "RifleLoad";

        /// <summary>The held firearm's children, measured on its model by <c>SliceWeapons.Sights</c>.</summary>
        public const string RearSight = "RearSight";

        public const string FrontSight = "FrontSight";
        public const string Muzzle = "Muzzle";

        /// <summary>A long firearm's forend, where the left hand holds it; a pistol has none and is held in one hand.</summary>
        public const string Forend = "Forend";

        /// <summary>10.2 §5e, authoring defaults: out of a fight the next shot waits the firearm's loading, s.</summary>
        public const float PistolLoadSeconds = 1.5f;

        public const float RifleLoadSeconds = 2.5f;

        private const float LoadFade = 0.15f;

        private PlayerArms _arms;
        private PlayerLocomotion _player;
        private Animator _animator;
        private ThirdPersonCamera _camera;
        private int _layer = -1;
        private float _view;
        private float _sights;
        private float _upper;
        private float _loadLeft;
        private Vector3 _eye;
        private Vector3 _eyeVelocity;
        private bool _eyeSet;
        private GameObject _gun;
        private Transform _rear;
        private Transform _front;
        private Transform _muzzle;
        private Transform _forend;
        private Vector3 _gripAt;
        private Quaternion _gripTurn = Quaternion.identity;
        private bool _moved;

        /// <summary>The projectile touched something, or nothing: the collider (null for none), where, and where the character fired from.</summary>
        public Action<Collider, Vector3, Vector3> Touched { get; set; }

        /// <summary>For the tests: holds the aim as the right button would, or lets it go; null leaves it to the button.</summary>
        public bool? HoldOverride { get; set; }

        /// <summary>True while the aim is held and can be.</summary>
        public bool Held { get; private set; }

        /// <summary>True once the firearm stands on the sight line.</summary>
        public bool Aimed => _sights >= 0.999f;

        /// <summary>True while the firearm loads after a shot.</summary>
        public bool Loading => _loadLeft > 0f;

        /// <summary>How far the firearm has come up to the eye, 0 to 1.</summary>
        public float Raised => _sights;

        /// <summary>The last projectile fired.</summary>
        public SliceProjectile LastShot { get; private set; }

        public float Weight => _view;

        public Vector3 Eye => _eye;

        public float FieldOfView => Rifle ? Tuning.RifleFieldOfView : Tuning.PistolFieldOfView;

        public float MouseShare => Tuning.MouseShare;

        private static SliceTuning.Aiming Tuning => SliceTuning.Current.Aim;

        private bool Rifle => _arms != null && _arms.Motion == FightBeats.Rifle;

        private bool Firearm => _arms != null && (_arms.Motion == FightBeats.Pistol || _arms.Motion == FightBeats.Rifle);

        public void Configure(PlayerArms arms)
        {
            _arms = arms;
        }

        /// <summary>
        /// A click with the firearm drawn out of a fight: the projectile from the muzzle at what the screen's centre shows,
        /// heard as a shot; false while the firearm loads or no firearm is in hand.
        /// </summary>
        public bool Fire()
        {
            if (!Find() || Loading || !Firearm || !Gun())
            {
                return false;
            }

            var view = Camera.main;
            var ray = view != null ? view.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)) : new Ray(_player.transform.position + Vector3.up * 1.5f, _player.transform.forward);
            var point = AimPoint(ray, Tuning.BulletReach);
            if (!Aimed)
            {
                // Not at the eye: the body turns to the shot first.
                var turn = Vector3.ProjectOnPlane(point - _player.transform.position, Vector3.up);
                if (turn.sqrMagnitude > 1e-4f)
                {
                    _player.transform.rotation = Quaternion.LookRotation(turn.normalized, Vector3.up);
                }
            }

            var muzzle = _muzzle != null ? _muzzle.position : _gun.transform.position;
            var from = _player.transform.position;
            var touched = Touched;
            LastShot = SliceProjectile.Fire(muzzle, point - muzzle, Tuning.BulletSpeed, Tuning.BulletGravityShare, Tuning.BulletReach, _player.transform,
                (collider, at) => touched?.Invoke(collider, at, from));
            SliceAlarm.Raise(new Alarm(AlarmKind.Shot, from, _player.transform));
            Load();
            return true;
        }

        private void Update()
        {
            if (!Find())
            {
                return;
            }

            var input = GameInputService.Instance;
            var wants = HoldOverride ?? (input != null && input.AimHeld);
            Held = wants && CanAim();
            var dt = Time.deltaTime;
            var rate = dt / Mathf.Max(0.01f, Tuning.RaiseSeconds);
            if (_loadLeft > 0f)
            {
                // Sheathed, or drawn by a fight, which loads on its own turns: the loading out of a fight is over.
                _loadLeft = _arms != null && _arms.Drawn ? _loadLeft - dt : 0f;
                if (_loadLeft <= 0f)
                {
                    _loadLeft = 0f;
                    Play(AimState);
                }
            }

            _view = Mathf.MoveTowards(_view, Held ? 1f : 0f, rate);
            _sights = Mathf.MoveTowards(_sights, Held && !Loading ? 1f : 0f, rate);
            _upper = Mathf.MoveTowards(_upper, Held || Loading ? 1f : 0f, rate);
            if (_layer >= 0 && _animator.isActiveAndEnabled)
            {
                _animator.SetLayerWeight(_layer, Smooth(_upper));
            }

            if (Held && _camera != null)
            {
                _player.AimFacing = Quaternion.Euler(0f, _camera.Yaw, 0f) * Vector3.forward;
            }
            else
            {
                _player.AimFacing = null;
            }
        }

        private void LateUpdate()
        {
            // The firearm back in the hand's grip, where the last frame's aim moved it from; the Animator has put the
            // arms back already.
            if (_moved && _gun != null)
            {
                _gun.transform.SetLocalPositionAndRotation(_gripAt, _gripTurn);
            }

            _moved = false;
            Untwist(Smooth(_upper));

            // Before the camera: the eye, by the head as the Animator posed it, in the body's own facing.
            if (_player == null || _animator == null || _view <= 0f)
            {
                _eyeSet = false;
                return;
            }

            var head = _animator.GetBoneTransform(HumanBodyBones.Head);
            if (head == null)
            {
                return;
            }

            var at = head.position + (_player.transform.rotation * Tuning.EyeFromHead);
            if (!_eyeSet)
            {
                _eye = at;
                _eyeVelocity = Vector3.zero;
                _eyeSet = true;
            }
            else
            {
                _eye = Vector3.SmoothDamp(_eye, at, ref _eyeVelocity, Mathf.Max(0.0001f, Tuning.EyeSmoothingSeconds));
            }
        }

        /// <summary>
        /// The pack's side and slanting walks turn the hips toward the step (Walk01_Right by about 60°, measured on the
        /// clips' body rotation), and the Aim layer poses the upper body on the hips, so it would turn with them: the
        /// spine's bones share the turn back to the body's facing, which is the aim's, the legs keeping their walk. An
        /// Animator that did not pose the bones this frame (its body unseen, culled) left last frame's turn on them, so
        /// that turn is taken off first and never adds up.
        /// </summary>
        private void Untwist(float weight)
        {
            if (_spineMoved)
            {
                for (var i = 0; i < _spine.Length; i++)
                {
                    if (_spine[i] != null && Quaternion.Angle(_spine[i].localRotation, _spineSet[i]) < 0.01f)
                    {
                        _spine[i].localRotation = _spineWas[i];
                    }
                }

                _spineMoved = false;
            }

            if (weight <= 0f || _player == null || _animator == null || !_animator.isHuman)
            {
                return;
            }

            var leftLeg = _animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var rightLeg = _animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            if (leftLeg == null || rightLeg == null)
            {
                return;
            }

            var hips = Vector3.Cross(rightLeg.position - leftLeg.position, Vector3.up);
            var facing = _player.transform.forward;
            hips.y = 0f;
            facing.y = 0f;
            if (hips.sqrMagnitude < 1e-6f || facing.sqrMagnitude < 1e-6f)
            {
                return;
            }

            var bones = 0;
            for (var i = 0; i < _spine.Length; i++)
            {
                _spine[i] = _animator.GetBoneTransform(SpineBones[i]);
                bones += _spine[i] != null ? 1 : 0;
            }

            var share = Quaternion.AngleAxis(Vector3.SignedAngle(hips, facing, Vector3.up) * weight / Mathf.Max(1, bones), Vector3.up);
            for (var i = 0; i < _spine.Length; i++)
            {
                var bone = _spine[i];
                if (bone != null)
                {
                    _spineWas[i] = bone.localRotation;
                    bone.rotation = share * bone.rotation;
                    _spineSet[i] = bone.localRotation;
                }
            }

            _spineMoved = true;
        }

        private static readonly HumanBodyBones[] SpineBones = { HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest };
        private readonly Transform[] _spine = new Transform[3];
        private readonly Quaternion[] _spineWas = new Quaternion[3];
        private readonly Quaternion[] _spineSet = new Quaternion[3];
        private bool _spineMoved;

        /// <summary>The camera stands at the eye: the firearm onto its line, and the hands onto the firearm.</summary>
        public void Placed(Camera camera)
        {
            if (_sights <= 0f || camera == null || !Gun())
            {
                return;
            }

            var hand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            var upper = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var lower = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            if (hand == null || upper == null || lower == null)
            {
                return;
            }

            // The firearm on the sight line: its rear sight so far ahead of the eye, its front sight further on the camera's
            // axis, its top the camera's up.
            var gun = _gun.transform;
            var look = camera.transform;
            var sight = (_front.localPosition - _rear.localPosition).normalized;
            var top = Vector3.ProjectOnPlane(Vector3.forward, sight).normalized;
            var onLine = Quaternion.LookRotation(look.forward, look.up) * Quaternion.Inverse(Quaternion.LookRotation(sight, top));
            var reach = Rifle ? Tuning.RifleEyeToRearSight : Tuning.PistolEyeToRearSight;
            var onLineAt = look.position + (look.forward * reach) - (onLine * (_rear.localPosition * gun.lossyScale.x));

            var share = Smooth(_sights);
            var heldAt = gun.position;
            var heldTurn = gun.rotation;
            var at = Vector3.Lerp(heldAt, onLineAt, share);
            var turn = Quaternion.Slerp(heldTurn, onLine, share);

            // A long firearm is held in both hands: the left one where the clip has it, going to the forend as it comes up.
            var left = _forend != null ? _animator.GetBoneTransform(HumanBodyBones.LeftHand) : null;
            var leftFrom = left != null ? left.position : Vector3.zero;
            var leftFromTurn = left != null ? left.rotation : Quaternion.identity;

            // The right hand where it holds the firearm there, then the firearm exactly on the line, reached or not.
            var handTurn = turn * Quaternion.Inverse(gun.localRotation);
            var handAt = at - (handTurn * Vector3.Scale(gun.localPosition, hand.lossyScale));
            Reach(upper, lower, hand, handAt);
            hand.rotation = handTurn;
            gun.SetPositionAndRotation(at, turn);
            _moved = true;

            if (left != null)
            {
                var leftUpper = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                var leftLower = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                if (leftUpper != null && leftLower != null)
                {
                    // Under the forend, palm up, the knuckles toward the muzzle and the fingers round its right side.
                    var grip = HeldWeapons.Grip(_animator, false);
                    var gripTurn = Quaternion.LookRotation(turn * Vector3.up, turn * Vector3.left);
                    var holdTurn = gripTurn * Quaternion.Inverse(grip.rotation);
                    var holdAt = _forend.position - (holdTurn * Vector3.Scale(grip.position, left.lossyScale));
                    Reach(leftUpper, leftLower, left, Vector3.Lerp(leftFrom, holdAt, share));
                    left.rotation = Quaternion.Slerp(leftFromTurn, holdTurn, share);
                }
            }
        }

        /// <summary>
        /// A two-bone reach: the upper and lower bones turned so that <paramref name="end"/> stands at
        /// <paramref name="target"/>, or as near as the limb's length allows, the elbow kept on the side it bent to.
        /// </summary>
        public static void Reach(Transform upper, Transform lower, Transform end, Vector3 target)
        {
            var a = upper.position;
            var b = lower.position;
            var c = end.position;
            var upperLength = (b - a).magnitude;
            var lowerLength = (c - b).magnitude;
            if (upperLength < 1e-4f || lowerLength < 1e-4f)
            {
                return;
            }

            var toTarget = target - a;
            var distance = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(upperLength - lowerLength) + 1e-4f, (upperLength + lowerLength) * 0.999f);
            var way = toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : (c - a).normalized;
            var bend = Vector3.ProjectOnPlane(b - a, way);
            if (bend.sqrMagnitude < 1e-8f)
            {
                bend = Vector3.ProjectOnPlane(Vector3.down, way);
            }

            var cos = ((upperLength * upperLength) + (distance * distance) - (lowerLength * lowerLength)) / (2f * upperLength * distance);
            var angle = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f)) * Mathf.Rad2Deg;
            var axis = Vector3.Cross(way, bend).normalized;
            var elbow = a + (Quaternion.AngleAxis(angle, axis) * way * upperLength);
            upper.rotation = Quaternion.FromToRotation(b - a, elbow - a) * upper.rotation;
            b = lower.position;
            c = end.position;
            lower.rotation = Quaternion.FromToRotation(c - b, target - b) * lower.rotation;
        }

        private bool CanAim() =>
            _arms != null && _arms.Drawn && Firearm && !_arms.Striking && _player.enabled && !_player.InWater && _player.State != PlayerLocomotion.Move.Crouched
            && _player.State != PlayerLocomotion.Move.Climbing && _camera != null && _camera.isActiveAndEnabled && _layer >= 0;

        /// <summary>The firearm loads: its clip over the upper body, the next shot after its seconds.</summary>
        private void Load()
        {
            _loadLeft = Rifle ? RifleLoadSeconds : PistolLoadSeconds;
            Play(Rifle ? RifleLoadState : PistolLoadState);
        }

        private void Play(string state)
        {
            var hash = Animator.StringToHash(state);
            if (_layer >= 0 && _animator.isActiveAndEnabled && _animator.HasState(_layer, hash))
            {
                _animator.CrossFadeInFixedTime(hash, LoadFade, _layer);
            }
        }

        /// <summary>The nearest thing the screen's centre shows within reach, no trigger and not the character; the reach's end with none.</summary>
        private Vector3 AimPoint(Ray ray, float reach)
        {
            var nearest = reach;
            var point = ray.origin + (ray.direction * reach);
            foreach (var hit in Physics.RaycastAll(ray, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance < nearest && !hit.collider.transform.IsChildOf(_player.transform))
                {
                    nearest = hit.distance;
                    point = hit.point;
                }
            }

            return point;
        }

        /// <summary>The firearm in the right hand and its marks; false with none, or with a model whose sights are not measured.</summary>
        private bool Gun()
        {
            var held = _player != null ? _player.GetComponent<HeldWeapons>() : null;
            var main = held != null ? held.Main : null;
            if (main != _gun)
            {
                _gun = main;
                _rear = main != null ? main.transform.Find(RearSight) : null;
                _front = main != null ? main.transform.Find(FrontSight) : null;
                _muzzle = main != null ? main.transform.Find(Muzzle) : null;
                _forend = main != null ? main.transform.Find(Forend) : null;
                _moved = false;
                if (main != null)
                {
                    _gripAt = main.transform.localPosition;
                    _gripTurn = main.transform.localRotation;
                }
            }

            return _gun != null && _gun.activeInHierarchy && _rear != null && _front != null;
        }

        private bool Find()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
                _animator = _player != null ? _player.GetComponentInChildren<Animator>() : null;
                _layer = _animator != null ? _animator.GetLayerIndex(Layer) : -1;
                _gun = null;
            }

            if (_camera == null || !_camera.isActiveAndEnabled)
            {
                var main = Camera.main;
                var found = main != null ? main.GetComponent<ThirdPersonCamera>() : null;
                if (found != _camera)
                {
                    if (_camera != null && ReferenceEquals(_camera.Aim, this))
                    {
                        _camera.Aim = null;
                    }

                    _camera = found;
                    if (_camera != null)
                    {
                        _camera.Aim = this;
                    }
                }
            }

            return _player != null && _animator != null;
        }

        private void OnDisable()
        {
            if (_player != null)
            {
                _player.AimFacing = null;
            }

            if (_camera != null && ReferenceEquals(_camera.Aim, this))
            {
                _camera.Aim = null;
            }

            _view = _sights = _upper = 0f;
        }

        private static float Smooth(float t) => t * t * (3f - (2f * t));
    }
}
