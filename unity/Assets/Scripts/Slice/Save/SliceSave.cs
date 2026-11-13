using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using GameDev.Core.Save;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Gear;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.Slice.Save
{
    /// <summary>
    /// The slice's Suspend Save (owner, 2026-11-04: one Suspend Save; the owner's answer to S5-7, 2026-11-13: the world's
    /// exact state; 17 X.3). "Save and quit" on the Esc menu writes the run as it is: the clock, the character, the bag and
    /// what is worn, the world state, and every person and beast where it stands and what it is doing, the dead lying
    /// where they fell. It is refused in a fight, a conversation, a Reaction Check or work under way, on a journey, during
    /// an Alarm's answer, and wherever a later system says so (sleep, Assimilation). On the next start the slice offers to
    /// continue it: the data comes back first, then the city is loaded again in the dark so each of its things starts from
    /// the restored world, then the people take up where they were, then the interior the player was in, then the player.
    /// A release build spends the save as it loads; a development build keeps it, for trying the same moment again.
    /// </summary>
    public sealed class SliceSave : MonoBehaviour
    {
        private const int Version = 1;

        // Parts of the sections several systems share.
        private const string SheetPart = "sheet";
        private const string StaminaPart = "stamina";
        private const string IdentityPart = "identity";
        private const string BagPart = "bag";
        private const string OutfitPart = "outfit";
        private const string ObjectsPart = "objects";
        private const string BodiesPart = "bodies";
        private const string ConfrontPart = "confront";
        private const string BeastsPart = "beasts";
        private const string AccusationPart = "accusation";
        private const string GatheringPart = "gathering";

        private static bool _offered;

        private SliceWorldDriver _world;
        private string _cityScene;

        /// <summary>
        /// Further reasons a save waits, each the key of its Screen Message or null: sleep and Assimilation add theirs
        /// (S5b.5, S5c.2).
        /// </summary>
        public List<Func<string>> Refusals { get; } = new List<Func<string>>();

        /// <summary>True while a Continue puts the run back.</summary>
        public bool Loading { get; private set; }

        /// <summary>Raised once a Continue has put everything back and the world runs again.</summary>
        public event Action Restored;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _offered = false;

        public void Configure(SliceWorldDriver world, string cityScene)
        {
            _world = world;
            _cityScene = cityScene;
        }

        /// <summary>Whether the slice should offer the saved run now: once a launch, when a save is on disk.</summary>
        public bool ShouldOffer()
        {
            // A batch run (the tests) never meets the player's own save, only one a test put in its own slot.
            if (_offered || !SliceSaveSlot.Exists || (Application.isBatchMode && !SliceSaveSlot.IsCustomPath))
            {
                return false;
            }

            _offered = true;
            return true;
        }

        /// <summary>Discards the saved run for a new one; a development build keeps it.</summary>
        public void Decline()
        {
            if (!Debug.isDebugBuild)
            {
                SliceSaveSlot.Delete();
            }
        }

        // ------------------------------------------------------------------------------------------- saving

        /// <summary>The key of the reason a save must wait now, or null when it may be written.</summary>
        public string RefusalKey()
        {
            var w = _world;
            if (w == null || Loading)
            {
                return "ui.save.refused.busy";
            }

            if (w.Fights != null && w.Fights.InFight)
            {
                return "ui.save.refused.fight";
            }

            if (w.TalkOpen || (w.Accusation != null && w.Accusation.IsOpen))
            {
                return "ui.save.refused.talk";
            }

            if ((w.Reaction != null && w.Reaction.Running) || (w.Work != null && w.Work.Busy))
            {
                return "ui.save.refused.work";
            }

            if (SceneTransition.IsTraveling)
            {
                return "ui.save.refused.busy";
            }

            if ((w.Fights != null && w.Fights.AlarmUnderWay) || (w.Beasts != null && w.Beasts.Alarmed)
                || (w.Accusation != null && w.Accusation.Busy) || FindAnyObjectByType<GameDev.Slice.Fights.HelpRun>() != null)
            {
                return "ui.save.refused.alarm";
            }

            foreach (var refusal in Refusals)
            {
                var key = refusal();
                if (key != null)
                {
                    return key;
                }
            }

            return null;
        }

        /// <summary>The run as it is now, every section.</summary>
        public SaveFile Capture() => SaveAssembler.Capture(Sections());

        /// <summary>Writes the save; the key of the reason it must wait, or null once it is on disk.</summary>
        public string Write()
        {
            var refused = RefusalKey();
            if (refused != null)
            {
                return refused;
            }

            SliceSaveSlot.Write(Capture());
            Debug.Log($"[Save] the Suspend Save is written at tick {_world.Clock.Tick}: {SliceSaveSlot.Path}");
            return null;
        }

        // ------------------------------------------------------------------------------------------- continuing

        /// <summary>Continues the saved run on disk; false when there is none or it cannot be read.</summary>
        public bool Continue()
        {
            var file = SliceSaveSlot.Read();
            if (file == null || Loading)
            {
                return false;
            }

            StartCoroutine(Load(file));
            return true;
        }

        /// <summary>Puts <paramref name="file"/> back, as a Continue does; the coroutine ends when the world runs again.</summary>
        public IEnumerator Load(SaveFile file)
        {
            Loading = true;
            var hold = _world.Clock.Pause();
            try
            {
                // The data first: every system's numbers, before any of the scene's things look at them.
                SaveAssembler.Restore(file, PhaseOne());
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] the Suspend Save cannot be read by this build: {e.Message}");
                hold.Dispose();
                Loading = false;
                yield break;
            }

            var people = Parts(file, SaveSchema.WorldPeople);
            Use(people, BeastsPart, p => _world.Beasts?.RestorePacks(p));
            Use(people, AccusationPart, p =>
            {
                if (_world.Accusation != null)
                {
                    _world.Accusation.QuietUntil = long.Parse(p, CultureInfo.InvariantCulture);
                }
            });
            Use(people, GatheringPart, p =>
            {
                if (_world.Gathering != null)
                {
                    _world.Gathering.Strokes = int.Parse(p, CultureInfo.InvariantCulture);
                }
            });
            var place = JsonUtility.FromJson<SavedPlace>(Section(file, SaveSchema.PlayerPlace) ?? "{}");

            // The city again, in the dark, each of its things starting from the restored world.
            Time.timeScale = 1f;
            while (SceneTransition.IsTraveling)
            {
                yield return null;
            }

            if (!SceneTransition.Restart(_cityScene))
            {
                Debug.LogError($"[Save] '{_cityScene}' cannot be loaded again.");
            }

            yield return Arrived();
            var city = SceneManager.GetSceneByName(_cityScene);
            Use(people, ObjectsPart, p => SavedObjects.Restore(p, city));
            Use(people, BodiesPart, p => SavedBodies.Restore(p, city));
            Use(people, ConfrontPart, p => _world.Fights?.RestoreSave(p));

            if (place.inside && !string.IsNullOrEmpty(place.scene) && place.scene != _cityScene)
            {
                if (SceneTransition.Travel(new TravelRequest(place.scene, place.portal, string.IsNullOrEmpty(place.context) ? null : place.context)))
                {
                    yield return Arrived();
                    var inside = SceneManager.GetSceneByName(place.scene);
                    Use(people, ObjectsPart, p => SavedObjects.Restore(p, inside));
                    Use(people, BodiesPart, p => SavedBodies.Restore(p, inside));
                }
            }

            var player = FindAnyObjectByType<PlayerLocomotion>();
            _world.WearLook(player);
            if (player != null && place.placed)
            {
                player.Teleport(place.at, Quaternion.Euler(0f, place.yaw, 0f));
            }

            if (_world.Hiding != null)
            {
                _world.Hiding.SetActive(place.hiding);
            }

            hold.Dispose();
            Loading = false;
            if (!Debug.isDebugBuild)
            {
                SliceSaveSlot.Delete();
            }

            Debug.Log($"[Save] the Suspend Save is back at tick {_world.Clock.Tick}, in {SceneManager.GetActiveScene().name}.");
            Restored?.Invoke();
        }

        /// <summary>The journey's end, and two frames more, so every thing of the new scene has started.</summary>
        private static IEnumerator Arrived()
        {
            while (SceneTransition.IsTraveling)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }

        // ------------------------------------------------------------------------------------------- the sections

        [Serializable]
        private sealed class SavedPlace
        {
            public bool placed;
            public bool inside;
            public string scene;
            public string portal;
            public string context;
            public Vector3 at;
            public float yaw;
            public bool hiding;
        }

        private IEnumerable<ISaveContributor> Sections()
        {
            foreach (var s in PhaseOne())
            {
                yield return s;
            }

            yield return new TextSection(SaveSchema.PlayerPlace, CapturePlace, _ => { });
            yield return new SavePartsSection(SaveSchema.WorldPeople, Version, new ISavePart[]
            {
                new SavePart(ObjectsPart, SavedObjects.Capture, _ => { }),
                new SavePart(BodiesPart, SavedBodies.Capture, _ => { }),
                new SavePart(ConfrontPart, () => _world.Fights != null ? _world.Fights.CaptureSave() : string.Empty, _ => { }),
                new SavePart(BeastsPart, () => _world.Beasts != null ? _world.Beasts.CapturePacks() : string.Empty, _ => { }),
                new SavePart(AccusationPart, () => (_world.Accusation != null ? _world.Accusation.QuietUntil : long.MinValue).ToString(CultureInfo.InvariantCulture), _ => { }),
                new SavePart(GatheringPart, () => (_world.Gathering != null ? _world.Gathering.Strokes : 0).ToString(CultureInfo.InvariantCulture), _ => { }),
            });
        }

        /// <summary>The sections restored before the city loads again: the seed checked, then every system's numbers.</summary>
        private List<ISaveContributor> PhaseOne()
        {
            var w = _world;
            var list = new List<ISaveContributor>
            {
                new TextSection(SaveSchema.WorldSeed, () => w.Seed.Value.ToString(CultureInfo.InvariantCulture), CheckSeed),
                new WorldStateSaveContributor(w.State),
            };

            if (w.Clock is GameClock clock)
            {
                list.Add(new GameClockSaveContributor(clock));
            }

            var sheet = new CharacterSheetSaveContributor(w.Character);
            var character = new List<ISavePart>
            {
                new SavePart(SheetPart, sheet.CapturePayload, p => sheet.RestorePayload(sheet.SectionVersion, p)),
                new SavePart(IdentityPart, () => JsonUtility.ToJson(w.Identity), p => w.Identity = JsonUtility.FromJson<GameDev.Slice.Creation.CharacterIdentity>(p)),
            };
            if (w.Stamina != null)
            {
                character.Add(new SavePart(StaminaPart, w.Stamina.Capture, w.Stamina.Restore));
            }

            list.Add(new SavePartsSection(SaveSchema.PlayerCharacter, Version, character));
            if (w.Bag != null)
            {
                var inventory = new List<ISavePart> { new SavePart(BagPart, CaptureBag, RestoreBag) };
                if (w.Outfit != null)
                {
                    inventory.Add(new SavePart(OutfitPart, CaptureOutfit, RestoreOutfit));
                }

                list.Add(new SavePartsSection(SaveSchema.PlayerInventory, Version, inventory));
            }

            return list;
        }

        private void CheckSeed(string payload)
        {
            if (!int.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed) || seed != _world.Seed.Value)
            {
                throw new SaveVersionException($"The save was made with seed {payload}; this slice runs seed {_world.Seed.Value}.");
            }
        }

        private string CapturePlace()
        {
            var active = SceneManager.GetActiveScene().name;
            var player = FindAnyObjectByType<PlayerLocomotion>();
            var arrived = SceneTransition.Arrived;
            var inside = active != _cityScene && arrived.SceneName == active;
            return JsonUtility.ToJson(new SavedPlace
            {
                placed = player != null,
                inside = inside,
                scene = active,
                portal = inside ? arrived.ArrivalPortalId : string.Empty,
                context = inside ? arrived.Context ?? string.Empty : string.Empty,
                at = player != null ? player.transform.position : Vector3.zero,
                yaw = player != null ? player.transform.eulerAngles.y : 0f,
                hiding = _world.Hiding != null && _world.Hiding.Active,
            });
        }

        [Serializable]
        private sealed class SavedBag
        {
            public List<string> ids = new List<string>();
            public List<int> counts = new List<int>();
            public List<GameDev.Items.ItemPiece> pieces = new List<GameDev.Items.ItemPiece>();
            public int bronze;
        }

        private string CaptureBag()
        {
            var bag = _world.Bag;
            var saved = new SavedBag { bronze = bag.Bronze, pieces = new List<GameDev.Items.ItemPiece>(bag.Pieces) };
            foreach (var (item, count) in bag.Stacks)
            {
                saved.ids.Add(item.id);
                saved.counts.Add(count);
            }

            return JsonUtility.ToJson(saved);
        }

        private void RestoreBag(string payload)
        {
            var saved = JsonUtility.FromJson<SavedBag>(payload);
            var stacks = new List<(string, int)>();
            for (var i = 0; i < saved.ids.Count && i < saved.counts.Count; i++)
            {
                stacks.Add((saved.ids[i], saved.counts[i]));
            }

            _world.Bag.Restore(stacks, saved.pieces, saved.bronze);
        }

        [Serializable]
        private sealed class SavedOutfit
        {
            public List<int> slots = new List<int>();
            public List<int> pieces = new List<int>();
        }

        private string CaptureOutfit()
        {
            var saved = new SavedOutfit();
            foreach (var (slot, piece) in _world.Outfit.Capture())
            {
                saved.slots.Add((int)slot);
                saved.pieces.Add(piece);
            }

            return JsonUtility.ToJson(saved);
        }

        private void RestoreOutfit(string payload)
        {
            var saved = JsonUtility.FromJson<SavedOutfit>(payload);
            var worn = new List<(WearSlot, int)>();
            for (var i = 0; i < saved.slots.Count && i < saved.pieces.Count; i++)
            {
                worn.Add(((WearSlot)saved.slots[i], saved.pieces[i]));
            }

            _world.Outfit.Restore(worn);
        }

        private static string Section(SaveFile file, string key)
        {
            var section = file.sections.Find(s => s.key == key);
            if (section != null && section.version > Version)
            {
                throw new SaveVersionException($"Section '{key}' has version {section.version}; this build reads up to {Version}.");
            }

            return section?.payload;
        }

        private static Dictionary<string, string> Parts(SaveFile file, string key)
        {
            var parts = new Dictionary<string, string>();
            foreach (var entry in SaveText.Split(Section(file, key)))
            {
                parts[entry.Key] = entry.Value;
            }

            return parts;
        }

        private static void Use(Dictionary<string, string> parts, string key, Action<string> restore)
        {
            if (parts.TryGetValue(key, out var payload) && !string.IsNullOrEmpty(payload))
            {
                restore(payload);
            }
        }

        /// <summary>A section of one plain payload, written and read by two functions.</summary>
        private sealed class TextSection : ISaveContributor
        {
            private readonly Func<string> _capture;
            private readonly Action<string> _restore;

            public TextSection(string key, Func<string> capture, Action<string> restore)
            {
                SectionKey = key;
                _capture = capture;
                _restore = restore;
            }

            public string SectionKey { get; }

            public int SectionVersion => Version;

            public string CapturePayload() => _capture();

            public void RestorePayload(int version, string payload) => _restore(payload);
        }
    }
}
