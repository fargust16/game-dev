using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Core.Localization;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// CR5, what the four new abilities look like (05.1, 10.2 §5f *In the slice*), each in its turn among the beats:
    /// Пелена a slow cloud of haze hung on its Partial Obstacle, gone with it; Гнёт a dark burst at its point, then each
    /// one it struck hit or fallen at once, said in one line; Рывок a puff where the caster was and where it stands
    /// behind the enemy; Двойник a still copy, the caster's body baked in its pose, while the caster shows only its
    /// shadow. Presentation only: the turns have already decided everything.
    /// </summary>
    public sealed partial class SliceConfront
    {
        // Authoring defaults: the haze's grey and the burst's dark lilac, the ability tint of the hotbar; the cloud's and
        // the puffs' particles, how many, how long they live, s, and how large, m.
        private static readonly Color HazeColour = new Color(0.78f, 0.79f, 0.8f, 0.42f);
        private static readonly Color BurstColour = new Color(0.55f, 0.46f, 0.72f, 0.6f);
        private static readonly Color PuffColour = new Color(0.72f, 0.7f, 0.78f, 0.5f);
        private const float HazeLife = 4f;
        private const float HazeRate = 14f;
        private const float PuffLife = 0.9f;

        private readonly Dictionary<Combatant, Transform> _copies = new Dictionary<Combatant, Transform>();
        private readonly Dictionary<Renderer, ShadowCastingMode> _ghosted = new Dictionary<Renderer, ShadowCastingMode>();
        private readonly List<(Combatant Target, StrikeResult Result)> _pressHits = new List<(Combatant, StrikeResult)>();

        /// <summary>A combatant's body: a person's, or a copy's while it stands.</summary>
        private Transform BodyOf(Combatant c)
        {
            if (c == null)
            {
                return null;
            }

            if (_bodies.TryGetValue(c, out var body) && body != null)
            {
                return body;
            }

            return _copies.TryGetValue(c, out var copy) ? copy : null;
        }

        /// <summary>An ability took its place: its look, queued among the beats.</summary>
        private void OnAbilityAt(Combatant user, FightAbility ability, CombatCell cell)
        {
            var body = BodyOf(user);
            if (body == null || ability == null)
            {
                return;
            }

            var point = _grid.Center(cell);
            switch (ability.Effect)
            {
                case AbilityEffect.Screen:
                    var clouds = _turns.Clouds;
                    var cloud = clouds.Count > 0 ? clouds[clouds.Count - 1].Body : null;
                    var width = Mathf.Max(1, ability.Area) * _grid.CellSize;
                    _beats.Pose(body, () =>
                    {
                        Face(body, point);
                        if (cloud != null)
                        {
                            Particles(cloud.transform, point + (Vector3.up * 1.1f), new Vector3(width, 1.6f, width), HazeColour, HazeLife, 0, HazeRate, 1.4f, 2.6f);
                        }
                    });
                    break;
                case AbilityEffect.Press:
                    var size = Mathf.Max(1, ability.Area) * _grid.CellSize;
                    _beats.Pose(body, () =>
                    {
                        Face(body, point);
                        Particles(null, point + (Vector3.up * 0.9f), new Vector3(size, 1.2f, size), BurstColour, PuffLife, 40, 0f, 0.8f, 1.6f);
                    });
                    break;
                case AbilityEffect.Dash:
                    var facing = user.Facing;
                    _beats.Pose(body, () =>
                    {
                        Particles(null, body.position + (Vector3.up * 0.9f), new Vector3(0.6f, 1.4f, 0.6f), PuffColour, PuffLife, 16, 0f, 0.5f, 1f);
                        var look = new Vector3(facing.x, 0f, facing.y);
                        Place(body, point, look.sqrMagnitude > 0f ? Quaternion.LookRotation(look) : body.rotation);
                        Particles(null, point + (Vector3.up * 0.9f), new Vector3(0.6f, 1.4f, 0.6f), PuffColour, PuffLife, 16, 0f, 0.5f, 1f);
                    });
                    break;
                case AbilityEffect.Double:
                    var copy = user.Double;
                    _beats.Pose(body, () =>
                    {
                        if (copy != null && copy.Standing && !_copies.ContainsKey(copy))
                        {
                            _copies[copy] = Bake(body);
                        }

                        Ghost(body, true);
                        Particles(null, body.position + (Vector3.up * 0.9f), new Vector3(0.6f, 1.4f, 0.6f), PuffColour, PuffLife, 16, 0f, 0.5f, 1f);
                    });
                    break;
            }
        }

        /// <summary>A copy broke at a hit, after the blow is seen, or faded with its caster's veil.</summary>
        private void OnDoubleEnded(Combatant caster, Combatant copy, bool broke)
        {
            var body = BodyOf(caster) ?? BodyOf(copy);
            if (body == null || !_turns.IsRunning)
            {
                DropCopy(copy);
                return;
            }

            _beats.Pose(body, () =>
            {
                if (_copies.TryGetValue(copy, out var shown) && shown != null)
                {
                    Particles(null, shown.position + (Vector3.up * 0.9f), new Vector3(0.6f, 1.4f, 0.6f), PuffColour, PuffLife, broke ? 24 : 10, 0f, 0.5f, 1f);
                }

                DropCopy(copy);
                if (broke)
                {
                    _hud.Line(Loc.Format("ui.fight.log.double.broken", Name(caster)));
                }
            });
        }

        /// <summary>A fighter became unseen, or was seen again: said, and the caster's body shows only its shadow meanwhile.</summary>
        private void OnVeilChanged(Combatant c)
        {
            var body = BodyOf(c);
            var veiled = c.Fighter.Veiled > 0;
            if (body == null || !_turns.IsRunning)
            {
                if (!veiled && body != null)
                {
                    Ghost(body, false);
                }

                return;
            }

            _beats.Pose(body, () =>
            {
                Ghost(body, veiled);
                _hud.Line(Loc.Format(veiled ? "ui.fight.log.veiled" : "ui.fight.log.unveiled", Name(c)));
            });
        }

        /// <summary>05.1 Гнёт: each one struck is kept until the cast is told, then all are shown in one beat and one line.</summary>
        private void PressHit(Combatant user, Combatant target, FightAbility ability, StrikeResult result)
        {
            if (target != null)
            {
                _pressHits.Add((target, result));
                return;
            }

            var hits = new List<(Combatant Target, StrikeResult Result)>(_pressHits);
            _pressHits.Clear();
            var body = BodyOf(user);
            var line = Press(user, ability, hits);
            if (body == null)
            {
                _hud.Line(line);
                return;
            }

            _beats.Pose(body, () =>
            {
                foreach (var hit in hits)
                {
                    var struck = BodyOf(hit.Target);
                    if (hit.Target.DoubleOf == null)
                    {
                        _beats.Answer(struck, hit.Result);
                        BeastAnswer(struck, hit.Result);
                    }
                }

                _hud.Line(line);
            });
        }

        /// <summary>The line of a Гнёт: who cast it and what each one struck took.</summary>
        private string Press(Combatant user, FightAbility ability, List<(Combatant Target, StrikeResult Result)> hits)
        {
            var what = Loc.Get($"ability.{ability.Key}.name");
            if (hits.Count == 0)
            {
                return Loc.Format("ui.fight.log.press.none", Name(user), what);
            }

            var parts = new List<string>();
            foreach (var hit in hits)
            {
                var damage = Mathf.Max(0, Mathf.RoundToInt((float)hit.Result.Damage));
                parts.Add(hit.Target.DoubleOf != null ? Name(hit.Target)
                    : Loc.Format(hit.Result.Downed ? "ui.fight.log.press.down" : "ui.fight.log.press.hit", Name(hit.Target), damage));
            }

            return Loc.Format("ui.fight.log.press", Name(user), what, string.Join(", ", parts));
        }

        private void Face(Transform body, Vector3 point)
        {
            var look = point - body.position;
            look.y = 0f;
            if (look.sqrMagnitude > 1e-4f)
            {
                Place(body, body.position, Quaternion.LookRotation(look));
            }
        }

        /// <summary>
        /// 05.1 Двойник: the body baked in the pose it stands in, every skinned part and every held thing, as a still
        /// object of its own with the same materials.
        /// </summary>
        private static Transform Bake(Transform body)
        {
            var root = new GameObject("Double").transform;
            root.SetPositionAndRotation(body.position, body.rotation);
            foreach (var skinned in body.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!skinned.enabled || !skinned.gameObject.activeInHierarchy || skinned.sharedMesh == null)
                {
                    continue;
                }

                var mesh = new Mesh { name = "Double " + skinned.name };
                skinned.BakeMesh(mesh, true);
                Part(root, skinned.transform, mesh, skinned.sharedMaterials, false);
            }

            foreach (var plain in body.GetComponentsInChildren<MeshRenderer>())
            {
                if (plain.enabled && plain.gameObject.activeInHierarchy && plain.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
                {
                    Part(root, plain.transform, filter.sharedMesh, plain.sharedMaterials, true);
                }
            }

            return root;
        }

        private static void Part(Transform root, Transform from, Mesh mesh, Material[] materials, bool scaled)
        {
            var part = new GameObject(from.name);
            part.transform.SetPositionAndRotation(from.position, from.rotation);
            part.transform.localScale = scaled ? from.lossyScale : Vector3.one;
            part.transform.SetParent(root, true);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }

        private void DropCopy(Combatant copy)
        {
            if (copy == null || !_copies.TryGetValue(copy, out var shown))
            {
                return;
            }

            _copies.Remove(copy);
            DropBaked(shown);
        }

        /// <summary>The body shows only its shadow while it is unseen, and takes its own look back after.</summary>
        private void Ghost(Transform body, bool unseen)
        {
            foreach (var renderer in body.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                if (unseen)
                {
                    if (!_ghosted.ContainsKey(renderer))
                    {
                        _ghosted[renderer] = renderer.shadowCastingMode;
                        renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    }
                }
                else if (_ghosted.TryGetValue(renderer, out var mode))
                {
                    renderer.shadowCastingMode = mode;
                    _ghosted.Remove(renderer);
                }
            }
        }

        /// <summary>The fight is over: no copy stands and every body has its look back.</summary>
        private void ClearEffects()
        {
            foreach (var copy in new List<Combatant>(_copies.Keys))
            {
                DropCopy(copy);
            }

            foreach (var pair in _ghosted)
            {
                if (pair.Key != null)
                {
                    pair.Key.shadowCastingMode = pair.Value;
                }
            }

            _ghosted.Clear();
            _pressHits.Clear();
        }

        /// <summary>
        /// Particles of the haze material in a box: a slow cloud while <paramref name="rate"/> runs, hung on
        /// <paramref name="parent"/> and gone with it, or a burst of <paramref name="burst"/> that clears itself.
        /// </summary>
        private void Particles(Transform parent, Vector3 at, Vector3 box, Color colour, float life, int burst, float rate, float least, float most)
        {
            if (_settings == null || _settings.HazeMaterial == null)
            {
                return;
            }

            var go = new GameObject(rate > 0f ? "Haze Cloud" : "Puff");
            go.transform.position = at;
            if (parent != null)
            {
                go.transform.SetParent(parent, true);
            }

            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = rate > 0f;
            main.duration = rate > 0f ? HazeLife : 0.2f;
            main.startLifetime = life;
            main.startSpeed = 0.15f;
            main.startSize = new ParticleSystem.MinMaxCurve(least, most);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = colour;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            main.playOnAwake = false;
            if (rate > 0f)
            {
                main.prewarm = true;
            }

            var emission = particles.emission;
            emission.rateOverTime = rate;
            if (burst > 0)
            {
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });
            }

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;
            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _settings.HazeMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            particles.Play();
            if (rate <= 0f)
            {
                Destroy(go, life + 0.5f);
            }
        }
    }
}
