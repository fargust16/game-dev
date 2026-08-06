using System.Collections.Generic;
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

                if (!poi.windowOpen)
                {
                    continue;
                }

                if (now < poi.windowEndHours)
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
                    EnsureWindowStarted(id);
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

            if (poi.id == PoiShellId.PoiC_SootStairTrace && !CanOpenTrace())
            {
                InteractionFeedback.Show(
                    Label(id),
                    "Ash and boot-scuffs, but nothing that resolves into a story yet. Aftermath from the arrest or collectors will wake this Trace.");
                return;
            }

            if (!poi.windowOpen)
            {
                EnsureWindowStarted(id);
            }

            if (WorldClock.Instance != null && WorldClock.Instance.WorldHours >= poi.windowEndHours)
            {
                ResolveUnattended(poi);
                return;
            }

            // S1: participation stub (full Decision Node is S2).
            poi.discovery = PoiDiscoveryState.Resolved;
            poi.windowOpen = false;
            poi.resolution = PoiResolutionKind.Participated;
            poi.aftermathText = ParticipatedText(id);
            ApplyAftermathFlags(id, unattended: false);
            MaybeUnlockTrace();
            InteractionFeedback.Show(Label(id) + " — participated", poi.aftermathText, 7f);
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

            var urgency = WindowUrgency(poi);
            var state = poi.discovery == PoiDiscoveryState.Marked ? "Marked" : "Hinted";
            return string.IsNullOrEmpty(urgency) ? $"{baseName} [{state}]" : $"{baseName} [{state}/{urgency}]";
        }

        private void BootstrapDefaults()
        {
            _pois[PoiShellId.PoiA_HarrowArrest] = new RuntimePoi
            {
                id = PoiShellId.PoiA_HarrowArrest,
                windowClass = ActiveWindowClass.Short,
                discovery = PoiDiscoveryState.Unknown
            };
            _pois[PoiShellId.PoiB_EleventhBellCollectors] = new RuntimePoi
            {
                id = PoiShellId.PoiB_EleventhBellCollectors,
                windowClass = ActiveWindowClass.Brief,
                discovery = PoiDiscoveryState.Unknown
            };
            _pois[PoiShellId.PoiC_SootStairTrace] = new RuntimePoi
            {
                id = PoiShellId.PoiC_SootStairTrace,
                windowClass = ActiveWindowClass.Standard,
                discovery = PoiDiscoveryState.Unknown
            };
        }

        private bool RaiseDiscovery(PoiShellId id, PoiDiscoveryState target)
        {
            if (id == PoiShellId.None || !_pois.TryGetValue(id, out var poi))
            {
                return false;
            }

            if (poi.discovery == PoiDiscoveryState.Resolved)
            {
                return false;
            }

            if (id == PoiShellId.PoiC_SootStairTrace && !CanOpenTrace())
            {
                return false;
            }

            if ((int)target <= (int)poi.discovery)
            {
                return false;
            }

            poi.discovery = target;
            EnsureWindowStarted(id);

            InteractionFeedback.Show(
                "Discovery",
                $"{Label(id)} is now {target}.",
                3.5f);
            return true;
        }

        private void EnsureWindowStarted(PoiShellId id)
        {
            if (!_pois.TryGetValue(id, out var poi))
            {
                return;
            }

            if (poi.discovery == PoiDiscoveryState.Resolved || poi.windowOpen)
            {
                return;
            }

            if (poi.discovery == PoiDiscoveryState.Unknown)
            {
                return;
            }

            if (id == PoiShellId.PoiC_SootStairTrace && !CanOpenTrace())
            {
                return;
            }

            var now = WorldClock.Instance != null ? WorldClock.Instance.WorldHours : 0f;
            poi.windowOpen = true;
            poi.windowStartHours = now;
            poi.windowEndHours = now + HoursForClass(poi.windowClass);
        }

        private void ResolveUnattended(RuntimePoi poi)
        {
            if (poi.discovery == PoiDiscoveryState.Resolved)
            {
                return;
            }

            poi.discovery = PoiDiscoveryState.Resolved;
            poi.windowOpen = false;
            poi.resolution = PoiResolutionKind.Unattended;
            poi.aftermathText = UnattendedText(poi.id);
            ApplyAftermathFlags(poi.id, unattended: true);
            MaybeUnlockTrace();
            InteractionFeedback.Show(
                Label(poi.id) + " — window closed",
                poi.aftermathText,
                8f);
            RefreshAllShellLabels();
        }

        private void ApplyAftermathFlags(PoiShellId id, bool unattended)
        {
            switch (id)
            {
                case PoiShellId.PoiA_HarrowArrest:
                    _aftermathFlags.Add(unattended ? "aftermath_harrow_sealed" : "aftermath_harrow_intervened");
                    break;
                case PoiShellId.PoiB_EleventhBellCollectors:
                    _aftermathFlags.Add(unattended ? "aftermath_collectors_took_girls" : "aftermath_collectors_contested");
                    break;
                case PoiShellId.PoiC_SootStairTrace:
                    _aftermathFlags.Add(unattended ? "aftermath_trace_cold" : "aftermath_trace_read");
                    break;
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

            if (trace.discovery != PoiDiscoveryState.Unknown)
            {
                return;
            }

            trace.discovery = PoiDiscoveryState.Hinted;
            InteractionFeedback.Show(
                "Trace wakes",
                "Soot Stair Trace is now Hinted — something left a readable residue under the factory wall.",
                5f);
        }

        private string WindowUrgency(RuntimePoi poi)
        {
            if (!poi.windowOpen || WorldClock.Instance == null)
            {
                return poi.windowClass.ToString();
            }

            var left = poi.windowEndHours - WorldClock.Instance.WorldHours;
            if (left <= 0f)
            {
                return "closing";
            }

            if (left < 2f)
            {
                return "urgent";
            }

            if (left < 6f)
            {
                return "soon";
            }

            return poi.windowClass.ToString();
        }

        private void RefreshAllShellLabels()
        {
            var shells = FindObjectsByType<PoiShellMarker>(FindObjectsSortMode.None);
            foreach (var shell in shells)
            {
                shell.RefreshPresentation();
            }
        }

        private static float HoursForClass(ActiveWindowClass windowClass)
        {
            switch (windowClass)
            {
                case ActiveWindowClass.Brief: return 4f;
                case ActiveWindowClass.Short: return 24f;
                case ActiveWindowClass.Standard: return 72f;
                case ActiveWindowClass.Long: return 168f;
                default: return 240f;
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
            public bool windowOpen;
            public float windowStartHours;
            public float windowEndHours;
            public PoiResolutionKind resolution;
            public string aftermathText;
        }
    }
}
