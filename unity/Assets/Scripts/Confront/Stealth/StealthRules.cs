using System;
using GameDev.Player.Progression;
using UnityEngine;

namespace GameDev.Confront.Stealth
{
    /// <summary>
    /// 10.3's numbers (owner-approved 2026-11-03 unless marked an authoring default): Stealth Mode, the Vision Cone, the
    /// Notice Meter with Darkness and Alert, and what the character can read of them. The stealth attack's numbers are
    /// on <c>FightRules</c> with the rest of a blow's. Immutable data.
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Confront/Stealth Rules", fileName = "StealthRules")]
    public sealed class StealthRules : ScriptableObject
    {
        [Tooltip("07.1 §4's curve, for the check a cone touching a hidden character makes in a fight (10.3 §8).")]
        [SerializeField] private ActionCheckRules checks;

        [Header("Stealth Mode (10.3 §1)")]
        [Tooltip("A crouching character's speed as a share of walking speed.")]
        [SerializeField] private float crouchSpeed = 0.75f;

        [Tooltip("AP to enter Stealth Mode in a fight (authoring default, 10.2 §5b).")]
        [SerializeField] private int enterAp = 3;

        [Tooltip("AP for two cells crouching in a fight.")]
        [SerializeField] private int apPerTwoCells = 3;

        [Header("Vision Cone (10.3 §2)")]
        [Tooltip("The cone's reach, cells: 15 by the owner's direction of 2026-11-12, 10 before.")]
        [SerializeField] private int coneCells = 15;

        [Tooltip("The cone's whole angle, degrees: the World Inhabitants setting's middle until that slider exists (authoring default; 60–130° range).")]
        [SerializeField] private float coneDegrees = 95f;

        [Tooltip("Height of an observer's eyes and of a crouching hider's head over its feet, m, for the line of sight (authoring default).")]
        [SerializeField] private float eyeHeight = 1.6f;
        [SerializeField] private float crouchHeight = 0.9f;

        [Header("Notice Meter (10.3 §3)")]
        [Tooltip("Seconds to fill at Δ 0, at the distance factor 1.")]
        [SerializeField] private float fillSeconds = 3f;

        [Tooltip("Each step of Δ multiplies the time to fill by this.")]
        [SerializeField] private float fillBase = 1.5f;

        [Tooltip("The distance factor is `near + d × perCell`, d in cells.")]
        [SerializeField] private float distanceNear = 0.5f;
        [SerializeField] private float distancePerCell = 0.1f;

        [Tooltip("Δ is held between −limit and +limit.")]
        [SerializeField] private int deltaLimit = 4;

        [Tooltip("An Alert observer's Perception is multiplied by this.")]
        [SerializeField] private float alertPerception = 1.25f;

        [Tooltip("Seconds for a full meter to empty outside every cone.")]
        [SerializeField] private float fadeSeconds = 8f;

        [Tooltip("Seconds for a full meter to empty when the observer had noticed the character and lost it.")]
        [SerializeField] private float searchFadeSeconds = 16f;

        [Tooltip("World ticks an observer stays Alert after its meter is empty.")]
        [SerializeField] private int alertTicks = 10;

        [Header("Darkness (10.3 §3)")]
        [SerializeField] private int shadowDarkness = 2;
        [SerializeField] private int nightStreetDarkness = 4;
        [SerializeField] private int unlitRoomDarkness = 6;

        [Tooltip("A street at night counts as dark this far from a lit lamp, m (authoring default).")]
        [SerializeField] private float lampReach = 6f;

        [Header("What the character reads (10.3 §4)")]
        [SerializeField] private int meterFromPerception = 4;
        [SerializeField] private int conesFaintFromIntelligence = 3;
        [SerializeField] private int conesClearFromIntelligence = 6;

        public ActionCheckRules Checks => checks;
        public float CrouchSpeed => crouchSpeed;
        public int EnterAp => enterAp;
        public int ApPerTwoCells => apPerTwoCells;
        public int ConeCells => coneCells;
        public float ConeDegrees => coneDegrees;
        public float EyeHeight => eyeHeight;
        public float CrouchHeight => crouchHeight;
        public float FadeSeconds => fadeSeconds;
        public float SearchFadeSeconds => searchFadeSeconds;
        public int AlertTicks => alertTicks;
        public int ShadowDarkness => shadowDarkness;
        public int NightStreetDarkness => nightStreetDarkness;
        public int UnlitRoomDarkness => unlitRoomDarkness;
        public float LampReach => lampReach;
        public int MeterFromPerception => meterFromPerception;
        public int ConesFaintFromIntelligence => conesFaintFromIntelligence;
        public int ConesClearFromIntelligence => conesClearFromIntelligence;

        /// <summary>
        /// 10.3 §3: `Δ = (Dexterity + Stealth skill + Darkness) / 2 − Perception`, held between −4 and +4, with the
        /// observer's Perception × 1.25 when it is Alert.
        /// </summary>
        public double Delta(int dexterity, int stealth, int darkness, int perception, bool alert)
        {
            var seen = alert ? perception * alertPerception : perception;
            return Math.Clamp((dexterity + stealth + darkness) / 2d - seen, -deltaLimit, deltaLimit);
        }

        /// <summary>10.3 §3: `Time to fill = 3 s × 1.5^Δ × (0.5 + d / 10)`, d the distance in cells.</summary>
        public double TimeToFill(double delta, double cells) =>
            fillSeconds * Math.Pow(fillBase, delta) * (distanceNear + Math.Max(0d, cells) * distancePerCell);

        /// <summary>
        /// 10.3 §8: a cone touching a hidden character in a fight is a check on 07.1's curve at §3's Δ without distance:
        /// Δ −2 is 50 %, +2 is 95 %.
        /// </summary>
        public double TurnCheckChance(double delta) => ActionCheck.BaseChance(checks, delta);

        /// <summary>10.3 §2: whether a point lies within an observer's cone, by flat distance and angle; sight is the caller's.</summary>
        public bool InCone(Vector3 eye, Vector3 forward, Vector3 point, float cellSize)
        {
            var to = point - eye;
            to.y = 0f;
            forward.y = 0f;
            var reach = coneCells * cellSize;
            if (to.sqrMagnitude > reach * reach)
            {
                return false;
            }

            return to.sqrMagnitude < 1e-6f || forward.sqrMagnitude < 1e-6f || Vector3.Angle(forward, to) <= coneDegrees * 0.5f;
        }

        /// <summary>10.3 §4: the Notice Meter over an NPC is shown from Perception 4.</summary>
        public bool ShowsMeter(int perception) => perception >= meterFromPerception;

        /// <summary>10.3 §4: the cones' opacity: none below Intelligence 3, faint at 3 and clearer up to 6 (the steps between are an authoring default).</summary>
        public float ConeOpacity(int intelligence)
        {
            if (intelligence < conesFaintFromIntelligence)
            {
                return 0f;
            }

            return Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(conesFaintFromIntelligence, conesClearFromIntelligence, intelligence));
        }

        /// <summary>10.3 §8: AP for a crouching walk of this many cells in a fight, 3 AP for two, a last odd cell rounded up.</summary>
        public int CrouchAp(int cells) => (cells * apPerTwoCells + 1) / 2;
    }
}
