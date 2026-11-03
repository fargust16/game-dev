using System.Collections.Generic;
using GameDev.Decisions;
using GameDev.Interaction;
using UnityEngine;

namespace GameDev.World
{
    /// <summary>
    /// S1 POI runtime: discovery states, Active Windows, unattended Aftermath stubs.
    /// Design: 04 Points of Interest, 11 Vertical Slice (S1).
    /// </summary>
    public sealed class PoiDirector : MonoBehaviour
    {
        public static PoiDirector Instance { get; private set; }

        private readonly Dictionary<PoiShellId, RuntimePoi> _pois = new Dictionary<PoiShellId, RuntimePoi>();
        private readonly HashSet<string> _aftermathFlags = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BootstrapDefaults();
        }

        private void Start()
        {
            RefreshAllShellLabels();
        }

        private void Update()
        {
            if (WorldClock.Instance == null)
            {
                return;
            }

            var now = WorldClock.Instance.WorldHours;
            foreach (var pair in _pois)
            {
                var poi = pair.Value;
                if (poi.discovery == PoiDiscoveryState.Resolved)
                {
                    continue;
                }

                // 04: a window runs from the POI's appearance, never from its discovery.
                if (!poi.windowOpen && now >= poi.appearsAtHours)
                {
                    poi.windowOpen = true;
                    poi.windowStartHours = poi.appearsAtHours;
                    poi.windowEndHours = poi.appearsAtHours + poi.windowHours;
                }

                if (!poi.windowOpen || now < poi.windowEndHours)
                {
                    continue;
                }

                ResolveUnattended(poi);
            }
        }

        public bool HasAftermath(string flag) => _aftermathFlags.Contains(flag);

        public RuntimePoi Get(PoiShellId id)
        {
            return _pois.TryGetValue(id, out var poi) ? poi : null;
        }

        public void ApplyTips(PoiShellId[] hints, PoiShellId[] marks, string summary)
        {
            var changed = false;
            if (hints != null)
            {
                foreach (var id in hints)
                {
                    changed |= RaiseDiscovery(id, PoiDiscoveryState.Hinted);
                }
            }

            if (marks != null)
            {
                foreach (var id in marks)
                {
                    changed |= RaiseDiscovery(id, PoiDiscoveryState.Marked);
                }
            }

            if (!string.IsNullOrWhiteSpace(summary))
            {
                InteractionFeedback.Show("Tip", summary, 5f);
            }

            if (changed)
            {
                RefreshAllShellLabels();
            }
        }

        public void HandlePoiInteract(PoiShellId id)
        {
            if (!_pois.TryGetValue(id, out var poi))
            {
                return;
            }

            if (poi.discovery != PoiDiscoveryState.Resolved && !poi.windowOpen)
            {
                InteractionFeedback.Show(Label(id), "Nothing here draws your attention yet.");
                return;
            }

            if (poi.discovery == PoiDiscoveryState.Unknown)
            {
                InteractionFeedback.Show(
                    Label(id),
                    "You notice a tense place, but you have no useful tip yet. Check the notice board or ask locals.");
                return;
            }

            if (poi.discovery == PoiDiscoveryState.Resolved)
            {
                InteractionFeedback.Show(
                    Label(id),
                    string.IsNullOrEmpty(poi.aftermathText)
                        ? "This moment has already passed. Residue remains in the district."
                        : poi.aftermathText);
                return;
            }

            if (WorldClock.Instance != null && WorldClock.Instance.WorldHours >= poi.windowEndHours)
            {
                ResolveUnattended(poi);
                return;
            }

            // S2: POI A/B open Decision Node. POI C stays a Trace stub until S4 Bargain.
            if (id == PoiShellId.PoiA_HarrowArrest || id == PoiShellId.PoiB_EleventhBellCollectors)
            {
                if (DecisionNodeSession.Instance != null && DecisionNodeSession.Instance.TryOpen(id))
                {
                    return;
                }
            }

            poi.discovery = PoiDiscoveryState.Resolved;
            poi.windowOpen = false;
            poi.resolution = PoiResolutionKind.Participated;
            poi.aftermathText = ParticipatedText(id);
            ApplyAftermathFlags(id, unattended: false);
            MaybeUnlockTrace();
            InteractionFeedback.Show(Label(id) + " — participated", poi.aftermathText, 7f);
            RefreshAllShellLabels();
        }

        public void CompleteFromDecision(PoiShellId id, bool withdrawLike, string choiceTitle, string detail)
        {
            if (!_pois.TryGetValue(id, out var poi) || poi.discovery == PoiDiscoveryState.Resolved)
            {
                return;
            }

            poi.discovery = PoiDiscoveryState.Resolved;
            poi.windowOpen = false;
            poi.resolution = withdrawLike ? PoiResolutionKind.Unattended : PoiResolutionKind.Participated;
            poi.aftermathText = $"{choiceTitle}\n{detail}";
            ApplyAftermathFlags(id, unattended: withdrawLike);
            MaybeUnlockTrace();
            RefreshAllShellLabels();
        }

        public string BuildShellLabel(PoiShellId id)
        {
            if (!_pois.TryGetValue(id, out var poi))
            {
                return id.ToString();
            }

            var baseName = ShortName(id);
            if (poi.discovery == PoiDiscoveryState.Unknown)
            {
                return $"{baseName} [?]";
            }

            if (poi.discovery == PoiDiscoveryState.Resolved)
            {
                return $"{baseName} [Resolved]";
            }

            // 04 rule 5: a Marked POI shows its urgency in its source's words, never as a countdown.
            return poi.discovery == PoiDiscoveryState.Marked
                ? $"{baseName} [Marked: {poi.urgencyWords}]"
                : $"{baseName} [Hinted]";
        }

        /// <summary>
        /// Slice times, 11 Vertical Slice (authoring defaults, 2026-11-03): A 14:00–20:00 on day 1,
        /// B 23:00–02:00 the same night, C from its appearance; the slice has no invasion yet, so C keeps a Standard window.
        /// </summary>
        private void BootstrapDefaults()
        {
            _pois[PoiShellId.PoiA_HarrowArrest] = new RuntimePoi
            {
                id = PoiShellId.PoiA_HarrowArrest,
                windowClass = ActiveWindowClass.Short,
                appearsAtHours = 14f,
                windowHours = 6f,
                urgencyWords = "before the whistle",
                discovery = PoiDiscoveryState.Unknown
            };
            _pois[PoiShellId.PoiB_EleventhBellCollectors] = new RuntimePoi
            {
                id = PoiShellId.PoiB_EleventhBellCollectors,
                windowClass = ActiveWindowClass.Brief,
                appearsAtHours = 23f,
                windowHours = 3f,
                urgencyWords = "tonight",
                discovery = PoiDiscoveryState.Unknown
            };
            _pois[PoiShellId.PoiC_SootStairTrace] = new RuntimePoi
            {
                id = PoiShellId.PoiC_SootStairTrace,
                windowClass = ActiveWindowClass.Standard,
                appearsAtHours = float.PositiveInfinity, // appears when an A or B aftermath wakes it
                windowHours = 72f,
                urgencyWords = "before the rain",
                discovery = PoiDiscoveryState.Unknown
            };
        }

        private bool RaiseDiscovery(PoiShellId id, PoiDiscoveryState target)
        {
            if (id == PoiShellId.None || !_pois.TryGetValue(id, out var poi))
            {
                return false;
            }

            // A tip cannot name what has not appeared yet.
            if (poi.discovery == PoiDiscoveryState.Resolved || !poi.windowOpen)
            {
                return false;
            }

            if ((int)target <= (int)poi.discovery)
            {
                return false;
            }

            poi.discovery = target;

            InteractionFeedback.Show(
                "Discovery",
                $"{Label(id)} is now {target}.",
                3.5f);
            return true;
        }

        private void ResolveUnattended(RuntimePoi poi)
        {
            if (poi.discovery == PoiDiscoveryState.Resolved)
            {
                return;
            }

            var known = poi.discovery != PoiDiscoveryState.Unknown;
            poi.discovery = PoiDiscoveryState.Resolved;
            poi.windowOpen = false;
            poi.resolution = PoiResolutionKind.Unattended;
            poi.aftermathText = UnattendedText(poi.id);
            ApplyAftermathFlags(poi.id, unattended: true);
            MaybeUnlockTrace();

            // A POI the player never heard of leaves only residue, no announcement.
            if (known)
            {
                InteractionFeedback.Show(
                    Label(poi.id) + " — window closed",
                    poi.aftermathText,
                    8f);
            }

            RefreshAllShellLabels();
        }

        private void ApplyAftermathFlags(PoiShellId id, bool unattended)
        {
            string flag = null;
            switch (id)
            {
                case PoiShellId.PoiA_HarrowArrest:
                    flag = unattended ? "aftermath_harrow_sealed" : "aftermath_harrow_intervened";
                    break;
                case PoiShellId.PoiB_EleventhBellCollectors:
                    flag = unattended ? "aftermath_collectors_took_girls" : "aftermath_collectors_contested";
                    break;
                case PoiShellId.PoiC_SootStairTrace:
                    flag = unattended ? "aftermath_trace_cold" : "aftermath_trace_read";
                    break;
            }

            if (flag == null)
            {
                return;
            }

            _aftermathFlags.Add(flag);
            if (DistrictResidue.Instance != null)
            {
                DistrictResidue.Instance.Add(flag);
                if (unattended && id == PoiShellId.PoiA_HarrowArrest)
                {
                    DistrictResidue.Instance.Add("residue_harrow_sealed", "rumour_merrin_missed_arrest");
                }

                if (unattended && id == PoiShellId.PoiB_EleventhBellCollectors)
                {
                    DistrictResidue.Instance.Add("residue_collectors_took_girls", "rumour_merrin_missed_collectors");
                }
            }
        }

        private bool CanOpenTrace()
        {
            return _aftermathFlags.Contains("aftermath_harrow_sealed")
                   || _aftermathFlags.Contains("aftermath_harrow_intervened")
                   || _aftermathFlags.Contains("aftermath_collectors_took_girls")
                   || _aftermathFlags.Contains("aftermath_collectors_contested");
        }

        private void MaybeUnlockTrace()
        {
            if (!CanOpenTrace())
            {
                return;
            }

            if (!_pois.TryGetValue(PoiShellId.PoiC_SootStairTrace, out var trace))
            {
                return;
            }

            if (trace.discovery != PoiDiscoveryState.Unknown || trace.windowOpen)
            {
                return;
            }

            var now = WorldClock.Instance != null ? WorldClock.Instance.WorldHours : 0f;
            trace.appearsAtHours = now;
            trace.windowOpen = true;
            trace.windowStartHours = now;
            trace.windowEndHours = now + trace.windowHours;
            trace.discovery = PoiDiscoveryState.Hinted;
            InteractionFeedback.Show(
                "Trace wakes",
                "Soot Stair Trace is now Hinted — something left a readable residue under the factory wall.",
                5f);
        }

        private void RefreshAllShellLabels()
        {
            var shells = FindObjectsByType<PoiShellMarker>();
            foreach (var shell in shells)
            {
                shell.RefreshPresentation();
            }
        }

        private static string Label(PoiShellId id) => ShortName(id);

        private static string ShortName(PoiShellId id)
        {
            switch (id)
            {
                case PoiShellId.PoiA_HarrowArrest: return "POI A — Harrow";
                case PoiShellId.PoiB_EleventhBellCollectors: return "POI B — Alley";
                case PoiShellId.PoiC_SootStairTrace: return "POI C — Stairs";
                default: return id.ToString();
            }
        }

        private static string ParticipatedText(PoiShellId id)
        {
            switch (id)
            {
                case PoiShellId.PoiA_HarrowArrest:
                    return "You step into the brass-number seizure. Full Decision Node comes in S2 — for now the shop remembers you interfered.";
                case PoiShellId.PoiB_EleventhBellCollectors:
                    return "You contest the eleventh-bell collectors. S2 will open Approach choices; tonight the lane knows you were seen.";
                case PoiShellId.PoiC_SootStairTrace:
                    return "You read the soot-stair Trace. A Bargain tease waits in S4; residue is logged.";
                default:
                    return "You participate.";
            }
        }

        private static string UnattendedText(PoiShellId id)
        {
            switch (id)
            {
                case PoiShellId.PoiA_HarrowArrest:
                    return "Unattended: Wardens finish the seizure. Harrow's front is sealed; neighbourhood talk turns sharp by morning.";
                case PoiShellId.PoiB_EleventhBellCollectors:
                    return "Unattended: coach-wheels leave Loom Lane quieter. Someone is missing — Aftermath sticks to the alley.";
                case PoiShellId.PoiC_SootStairTrace:
                    return "Unattended: rain and soot erase the clear reading. The Trace goes cold.";
                default:
                    return "The moment passed without you.";
            }
        }

        public sealed class RuntimePoi
        {
            public PoiShellId id;
            public PoiDiscoveryState discovery;
            public ActiveWindowClass windowClass;
            public float appearsAtHours;
            public float windowHours;
            public string urgencyWords;
            public bool windowOpen;
            public float windowStartHours;
            public float windowEndHours;
            public PoiResolutionKind resolution;
            public string aftermathText;
        }
    }
}
