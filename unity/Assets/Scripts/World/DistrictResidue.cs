using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World
{
    /// <summary>
    /// District residue flags from Decision Nodes / Aftermath. Affects rumours and labels.
    /// </summary>
    public sealed class DistrictResidue : MonoBehaviour
    {
        public static DistrictResidue Instance { get; private set; }

        private readonly HashSet<string> _flags = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public bool Has(string flag) => _flags.Contains(flag);

        public void Add(params string[] flags)
        {
            if (flags == null)
            {
                return;
            }

            foreach (var flag in flags)
            {
                if (!string.IsNullOrWhiteSpace(flag))
                {
                    _flags.Add(flag);
                }
            }

            RefreshPresentation();
        }

        public string BuildMerrinPostRumour()
        {
            if (Has("rumour_merrin_harrow_saved"))
            {
                return "Merrin (later): Harrow's still mixing — says he owes a stranger from the ridge.";
            }

            if (Has("rumour_merrin_seal"))
            {
                return "Merrin (later): Brass seal on Harrow's door. Wardens drinking like they earned it.";
            }

            if (Has("rumour_merrin_whisper"))
            {
                return "Merrin (later): Someone heard 'crowned buyer' near Tincture. Keep your voice down.";
            }

            if (Has("rumour_merrin_rescue"))
            {
                return "Merrin (later): A mill girl got free on Loom Lane. Coach men left angry.";
            }

            if (Has("rumour_merrin_trail"))
            {
                return "Merrin (later): Whisper says the coach went coal-stair way, not the ridge.";
            }

            if (Has("rumour_merrin_missed_arrest") || Has("rumour_merrin_missed_collectors"))
            {
                return "Merrin (later): You missed the night's noise. The street remembers anyway.";
            }

            return "";
        }

        public string BuildHarrowLabelSuffix()
        {
            if (Has("residue_harrow_sealed") || Has("aftermath_harrow_sealed"))
            {
                return " [sealed]";
            }

            if (Has("residue_harrow_open") || Has("aftermath_harrow_intervened"))
            {
                return " [open/debt]";
            }

            return "";
        }

        public string BuildLoomLabelSuffix()
        {
            if (Has("residue_girls_freed"))
            {
                return " [rescued]";
            }

            if (Has("residue_collectors_took_girls") || Has("aftermath_collectors_took_girls"))
            {
                return " [emptied]";
            }

            if (Has("residue_followed_coach") || Has("residue_stair_lead"))
            {
                return " [trailed]";
            }

            return "";
        }

        private void RefreshPresentation()
        {
            var landmarks = FindObjectsByType<LandmarkMarker>();
            foreach (var landmark in landmarks)
            {
                var labelTf = landmark.transform.Find("WorldLabel");
                if (labelTf == null)
                {
                    continue;
                }

                var tm = labelTf.GetComponent<TextMesh>();
                if (tm == null)
                {
                    continue;
                }

                if (landmark.LandmarkId == LandmarkId.HarrowAndTincture)
                {
                    tm.text = "POI A — Harrow" + BuildHarrowLabelSuffix();
                }
                else if (landmark.LandmarkId == LandmarkId.LoomLane)
                {
                    tm.text = "Loom Lane" + BuildLoomLabelSuffix();
                }
                else if (landmark.LandmarkId == LandmarkId.AlleyMouth)
                {
                    tm.text = "POI B — Alley" + BuildLoomLabelSuffix();
                }
            }
        }
    }
}
