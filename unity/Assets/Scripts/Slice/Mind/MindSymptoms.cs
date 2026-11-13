using System;
using GameDev.Slice.Fights;
using GameDev.Slice.Stealth;
using GameDev.World.Seed;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Random = System.Random;

namespace GameDev.Slice.Mind
{
    /// <summary>What a troubled mind perceives (07.2 §3.4 *Sanity in the slice*).</summary>
    public enum MindSymptom
    {
        /// <summary>Mild disturbance and below: a voice at the edge of hearing, with no words.</summary>
        Whisper,

        /// <summary>Significant and below: a figure at the edge of the screen, gone when the camera turns.</summary>
        Glimpse,

        /// <summary>Severe and below: a Notice Meter fills from nobody.</summary>
        FalseAlarm,

        /// <summary>Critical: whispers over the lines of a dialogue.</summary>
        DialogueWhisper,
    }

    /// <summary>
    /// The symptoms of a troubled mind in the scene (S5b.6; 07.2 §3.4). Each band below Stable keeps the symptoms of those
    /// above it and adds its own: whispers, then glimpses, then false alarms, then at Critical the colour drains from the
    /// world and whispers speak over a dialogue's lines. A symptom deceives perception only: it writes no note, moves no
    /// trace and puts no words in a person's mouth, so the glimpse has no collider and the false meter no observer. Each
    /// comes at a seeded, uneven interval from the owner's tuning page, in real seconds, and none while the world is paused
    /// but the dialogue's whispers, a dialogue pausing it.
    /// </summary>
    public sealed class MindSymptoms : MonoBehaviour
    {
        private const int Rate = 22050;
        private const float GlimpseDistance = 9f;   // m from the camera
        private const float GlimpseTurn = 6f;       // °: a turn of the camera beyond this and the figure is gone
        private const float AlarmFill = 2.5f;       // s for the false meter to fill
        private const float AlarmHold = 4f;         // s the false meter stays

        private SliceWorldDriver _world;
        private Random _rolls;
        private readonly float[] _next = new float[4];
        private readonly int[] _counts = new int[4];
        private AudioSource _voice;
        private AudioClip _whisper;
        private GameObject _figure;
        private Vector3 _figureLook;
        private float _figureUntil;
        private Volume _drain;
        private ColorAdjustments _adjust;

        public void Configure(SliceWorldDriver world, IWorldSeed seed)
        {
            _world = world;
            _rolls = seed != null ? seed.Fork(SliceSeedStreams.Mind) : new Random(0);
            for (var i = 0; i < _next.Length; i++)
            {
                _next[i] = -1f;
            }
        }

        /// <summary>Whether a band shows a symptom: each keeps those of the bands above it.</summary>
        public static bool Allows(SanityBand band, MindSymptom symptom)
        {
            switch (symptom)
            {
                case MindSymptom.Whisper: return band >= SanityBand.Mild;
                case MindSymptom.Glimpse: return band >= SanityBand.Significant;
                case MindSymptom.FalseAlarm: return band >= SanityBand.Severe;
                default: return band >= SanityBand.Critical;
            }
        }

        /// <summary>How many times a symptom has come (for tests).</summary>
        public int Count(MindSymptom symptom) => _counts[(int)symptom];

        /// <summary>Whether the figure stands at the edge of the screen now.</summary>
        public bool GlimpseShown => _figure != null && _figure.activeSelf;

        /// <summary>How far the colour has drained, 0 to 1.</summary>
        public float ColourDrain => _drain != null ? _drain.weight : 0f;

        private SanityBand Band => _world != null && _world.Mind != null ? _world.Mind.Band : SanityBand.Stable;

        private void Update()
        {
            if (_world == null || _world.Mind == null || SliceDeathScreen.Shown)
            {
                Hide();
                Drain(false);
                return;
            }

            var band = Band;
            Drain(band >= SanityBand.Critical);
            WatchFigure();
            var now = Time.unscaledTime;
            var talking = _world.TalkOpen;
            var paused = _world.Clock != null && _world.Clock.IsPaused;
            for (var i = 0; i < _next.Length; i++)
            {
                var symptom = (MindSymptom)i;
                var allowed = Allows(band, symptom) && (symptom == MindSymptom.DialogueWhisper ? talking : !paused);
                if (!allowed)
                {
                    _next[i] = -1f;
                    continue;
                }

                if (_next[i] < 0f)
                {
                    _next[i] = now + Interval(symptom); // a band newly reached: its first symptom after a while
                }
                else if (now >= _next[i])
                {
                    Provoke(symptom);
                    _next[i] = now + Interval(symptom);
                }
            }
        }

        /// <summary>The symptom now, whatever the band (the interval's work, and tests).</summary>
        public void Provoke(MindSymptom symptom)
        {
            var done = symptom switch
            {
                MindSymptom.Glimpse => Glimpse(),
                MindSymptom.FalseAlarm => FalseAlarm(),
                _ => Whisper(symptom == MindSymptom.DialogueWhisper),
            };
            if (done)
            {
                _counts[(int)symptom]++;
            }
        }

        private float Interval(MindSymptom symptom)
        {
            var tuning = SliceTuning.Current;
            var range = symptom switch
            {
                MindSymptom.Whisper => tuning.WhisperSeconds,
                MindSymptom.Glimpse => tuning.GlimpseSeconds,
                MindSymptom.FalseAlarm => tuning.FalseAlarmSeconds,
                _ => tuning.DialogueWhisperSeconds,
            };
            var low = Mathf.Max(0.5f, Mathf.Min(range.x, range.y));
            var high = Mathf.Max(low, Mathf.Max(range.x, range.y));
            return low + ((float)_rolls.NextDouble() * (high - low));
        }

        private bool Whisper(bool overTalk)
        {
            if (_voice == null)
            {
                _voice = gameObject.AddComponent<AudioSource>();
                _voice.playOnAwake = false;
                _voice.spatialBlend = 0f;
                _voice.ignoreListenerPause = true;
                _whisper = WhisperClip(_rolls.Next());
            }

            _voice.panStereo = (float)((_rolls.NextDouble() * 1.4d) - 0.7d);
            _voice.pitch = 0.85f + ((float)_rolls.NextDouble() * 0.3f);
            _voice.PlayOneShot(_whisper, SliceTuning.Current.WhisperVolume * (overTalk ? 1.2f : 1f));
            return true;
        }

        /// <summary>A breath of filtered noise in a few syllables, with no words: made once, seeded.</summary>
        private static AudioClip WhisperClip(int seed)
        {
            var rolls = new Random(seed);
            var length = (int)(Rate * 1.8f);
            var samples = new float[length];
            var syllables = 6 + rolls.Next(3);
            var low = 0f;
            var lower = 0f;
            for (var i = 0; i < length; i++)
            {
                var noise = (float)((rolls.NextDouble() * 2d) - 1d);
                low += 0.35f * (noise - low);
                lower += 0.04f * (low - lower);
                var t = (float)i / length;
                var syllable = Mathf.Sin(t * syllables * Mathf.PI);
                var fade = Mathf.Sin(t * Mathf.PI);
                samples[i] = (low - lower) * syllable * syllable * fade * 1.6f;
            }

            var clip = AudioClip.Create("MindWhisper", length, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private bool Glimpse()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            var half = Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect) * 0.5f;
            var side = _rolls.Next(2) == 0 ? -1f : 1f;
            var look = camera.transform.forward;
            var flat = new Vector3(look.x, 0f, look.z);
            if (flat.sqrMagnitude < 0.01f)
            {
                return false;
            }

            var eye = camera.transform.position;
            var towards = Vector3.zero;
            var distance = 0f;
            for (var tries = 0; tries < 2 && distance < 3f; tries++, side = -side)
            {
                towards = Quaternion.AngleAxis(side * (half - 4f), Vector3.up) * flat.normalized;
                distance = Physics.Raycast(eye, towards, out var wall, GlimpseDistance, ~0, QueryTriggerInteraction.Ignore)
                    ? wall.distance - 0.6f
                    : GlimpseDistance;
            }

            if (distance < 3f)
            {
                return false; // walls near on both edges: nowhere to stand
            }

            var at = eye + (towards * distance);
            if (Physics.Raycast(at + (Vector3.up * 3f), Vector3.down, out var ground, 12f, ~0, QueryTriggerInteraction.Ignore))
            {
                at = ground.point;
            }
            else
            {
                at.y -= 1.6f;
            }

            Figure().transform.SetPositionAndRotation(at, Quaternion.LookRotation(-towards, Vector3.up));
            _figure.SetActive(true);
            _figureLook = look;
            _figureUntil = Time.unscaledTime + Mathf.Max(1f, SliceTuning.Current.GlimpseHold);
            return true;
        }

        private void WatchFigure()
        {
            if (!GlimpseShown)
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null || Time.unscaledTime >= _figureUntil || Vector3.Angle(camera.transform.forward, _figureLook) > GlimpseTurn)
            {
                Hide();
            }
        }

        private void Hide()
        {
            if (_figure != null)
            {
                _figure.SetActive(false);
            }
        }

        /// <summary>A tall dark figure, a body and a head with no collider: it is perceived, never met.</summary>
        private GameObject Figure()
        {
            if (_figure != null)
            {
                return _figure;
            }

            _figure = new GameObject("MindGlimpse");
            _figure.transform.SetParent(transform, false);
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit");
            var ink = new Material(shader) { name = "MindGlimpse", color = new Color(0.02f, 0.02f, 0.025f, 1f) };
            Part(PrimitiveType.Capsule, new Vector3(0f, 0.8f, 0f), new Vector3(0.42f, 0.8f, 0.32f), ink);
            Part(PrimitiveType.Sphere, new Vector3(0f, 1.72f, 0f), new Vector3(0.24f, 0.28f, 0.24f), ink);
            _figure.SetActive(false);
            return _figure;
        }

        private void Part(PrimitiveType type, Vector3 at, Vector3 size, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }

            part.transform.SetParent(_figure.transform, false);
            part.transform.localPosition = at;
            part.transform.localScale = size;
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private bool FalseAlarm()
        {
            var camera = Camera.main;
            var hud = FindAnyObjectByType<StealthHud>();
            if (camera == null || hud == null)
            {
                return false;
            }

            var look = camera.transform.forward;
            var flat = new Vector3(look.x, 0f, look.z).normalized;
            var side = _rolls.Next(2) == 0 ? -1f : 1f;
            var towards = Quaternion.AngleAxis(side * (12f + ((float)_rolls.NextDouble() * 18f)), Vector3.up) * flat;
            var at = camera.transform.position + (towards * (10f + ((float)_rolls.NextDouble() * 6f)));
            hud.ShowPhantom(at, AlarmFill, AlarmFill + AlarmHold);
            return true;
        }

        private void Drain(bool on)
        {
            if (_drain == null)
            {
                if (!on)
                {
                    return;
                }

                var go = new GameObject("MindColourDrain");
                go.transform.SetParent(transform, false);
                _drain = go.AddComponent<Volume>();
                _drain.isGlobal = true;
                _drain.priority = 100f;
                _drain.weight = 0f;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _adjust = profile.Add<ColorAdjustments>(true);
                _drain.sharedProfile = profile;
            }

            _adjust.saturation.Override(Mathf.Clamp(SliceTuning.Current.ColourDrain, -100f, 0f));
            _drain.weight = Mathf.MoveTowards(_drain.weight, on ? 1f : 0f, Time.unscaledDeltaTime * 0.5f);
        }

        private void OnDestroy()
        {
            if (_drain != null && _drain.sharedProfile != null)
            {
                Destroy(_drain.sharedProfile);
            }
        }
    }
}
