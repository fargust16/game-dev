using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.Slice.People
{
    /// <summary>
    /// The people's hours in the loaded scenes (S5c.3; 18 *Townsfolk in the slice*, SLICE_PEOPLE §4): each named person
    /// stands at the post its hours name and nowhere else, fewer walkers walk at night, and the drinkers and worshippers
    /// sit only while their place's keeper keeps it. A change happens out of the player's sight: one the player watches
    /// waits up to <see cref="SliceTuning.ScheduleWaitTicks"/>, and a scene just loaded takes the hour at once. Nothing
    /// changes during a fight. A named person who has fallen is gone for good (<see cref="GoneKey"/>): its body is left
    /// as it lies and none of its posts stands again. A residue that keeps a person away holds its post
    /// (<see cref="PersonPost.Held"/>), at once.
    /// </summary>
    public sealed class PersonHours : MonoBehaviour
    {
        /// <summary>
        /// Whether the hours are kept. PlayMode tests of other systems switch them off, so every person stands where its
        /// scene placed it (its primary post) and every walker walks, whatever the hour.
        /// </summary>
        public static bool Live = true;

        /// <summary>Authoring default, m: how far the player's eye reaches for the hours; a person farther off comes and goes unseen.</summary>
        public const float SightReach = 40f;

        private readonly List<PersonPost> _posts = new List<PersonPost>();
        private readonly List<TownsfolkWalker> _walkers = new List<TownsfolkWalker>();
        private readonly List<WorldResidue> _residues = new List<WorldResidue>();
        private readonly Dictionary<Object, long> _waited = new Dictionary<Object, long>();
        private SliceWorldDriver _world;
        private long _applied = -1;
        private bool _dirty;
        private int _sight;

        public static string GoneKey(string person) => $"person.gone.{person}";

        /// <summary>How many changes the last pass made, and how many waited for the player to look away.</summary>
        public int Changed { get; private set; }

        public int Waiting { get; private set; }

        public void Configure(SliceWorldDriver world)
        {
            _world = world;
            _sight = LayerMask.GetMask("Ground", "Structure", "Prop", "Interactable");
            SceneManager.sceneLoaded += OnLoaded;
            SceneManager.sceneUnloaded += OnUnloaded;
            Collect();
            Apply(true);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneUnloaded -= OnUnloaded;
        }

        private void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            Collect();
            Apply(true);
        }

        private void OnUnloaded(Scene scene)
        {
            _dirty = true;
        }

        /// <summary>A residue changed what it holds: the next frame applies it.</summary>
        public void Refresh() => _dirty = true;

        /// <summary>Whether <paramref name="person"/> has fallen, for good.</summary>
        public bool Gone(string person) => _world != null && _world.State.IsSet(GoneKey(person));

        /// <summary>
        /// Whether a keeper is at its work now, by its hours: not gone, not held away by a residue in the loaded scenes, and
        /// its hours naming its work post; always while the hours are not kept.
        /// </summary>
        public bool AtWork(string person)
        {
            if (!Live || !SliceHours.Keeps(person))
            {
                return true;
            }

            return !Gone(person) && !_posts.Any(p => p != null && p.Person == person && p.Held)
                   && SliceHours.PostAt(person, _world.Clock.MinuteOfDay) == SliceHours.Work;
        }

        private void Update()
        {
            if (_world == null || _world.Clock == null)
            {
                return;
            }

            var tick = _world.Clock.Tick;
            if (tick != _applied || _dirty)
            {
                Apply(false);
            }
        }

        private void Collect()
        {
            _posts.Clear();
            _posts.AddRange(FindObjectsByType<PersonPost>(FindObjectsInactive.Include));
            _walkers.Clear();
            _walkers.AddRange(FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Include));
            _residues.Clear();
            _residues.AddRange(FindObjectsByType<WorldResidue>(FindObjectsInactive.Include));
        }

        /// <summary>
        /// Puts every person of the loaded scenes where the hour wants it; <paramref name="atOnce"/> ignores the player's
        /// sight, as a scene just loaded does.
        /// </summary>
        public void Apply(bool atOnce)
        {
            if (_world == null || _world.Clock == null)
            {
                return;
            }

            _applied = _world.Clock.Tick;
            _dirty = false;
            if (_posts.Any(p => p == null) || _walkers.Any(w => w == null) || _residues.Any(r => r == null))
            {
                Collect();
            }

            var fight = _world.Fights != null && _world.Fights.InFight;
            var minute = _world.Clock.MinuteOfDay;
            var changed = 0;
            var waiting = 0;
            HoldByResidues();

            foreach (var post in _posts)
            {
                if (post.GetComponent<FallenBody>() != null)
                {
                    // A body lies where it fell; once the fight is over, the person is gone for good.
                    if (!fight && Live && SliceHours.Keeps(post.Person))
                    {
                        _world.State.Set(GoneKey(post.Person), 1);
                    }

                    continue;
                }

                var present = !Live || !SliceHours.Keeps(post.Person)
                    ? post.Primary
                    : !Gone(post.Person) && SliceHours.PostAt(post.Person, minute) == post.Post;
                var want = present && !post.Held;
                if (want == post.gameObject.activeSelf)
                {
                    _waited.Remove(post);
                    continue;
                }

                // A held person leaves at once, as the residue always did; anything else waits out of a fight and out of sight.
                var heldAway = present && post.Held;
                if (!heldAway && (fight || (!atOnce && Watched(post, post.transform))))
                {
                    waiting++;
                    continue;
                }

                post.gameObject.SetActive(want);
                _waited.Remove(post);
                changed++;
            }

            foreach (var walker in _walkers)
            {
                if (!Kept(walker))
                {
                    continue;
                }

                var want = Wanted(walker, minute);
                if (want == walker.gameObject.activeSelf || (!want && !(walker.isActiveAndEnabled && walker.FreeToGoHome)))
                {
                    _waited.Remove(walker);
                    continue;
                }

                if (fight || (!atOnce && Watched(walker, walker.transform)))
                {
                    waiting++;
                    continue;
                }

                walker.gameObject.SetActive(want);
                _waited.Remove(walker);
                changed++;
            }

            Changed = changed;
            Waiting = waiting;
            if (changed > 0)
            {
                _world.PeopleChanged();
            }
        }

        /// <summary>The residues standing now hold the posts they hide, instead of switching them off themselves.</summary>
        private void HoldByResidues()
        {
            foreach (var post in _posts)
            {
                post.Held = false;
            }

            foreach (var residue in _residues)
            {
                if (residue == null || !residue.IsShowing)
                {
                    continue;
                }

                foreach (var hidden in residue.Hides)
                {
                    if (hidden != null && hidden.TryGetComponent<PersonPost>(out var post))
                    {
                        post.Held = true;
                    }
                }
            }
        }

        /// <summary>
        /// A walker the hours keep: one that walks a route or sits at a place, standing (no fallen body), and no residue's
        /// to show or hide.
        /// </summary>
        private bool Kept(TownsfolkWalker walker)
        {
            if (walker == null || walker.GetComponent<FallenBody>() != null || (!walker.Roams && walker.Home == null))
            {
                return false;
            }

            foreach (var residue in _residues)
            {
                if (residue != null && (residue.Shows.Contains(walker.gameObject) || residue.Hides.Contains(walker.gameObject)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Wanted(TownsfolkWalker walker, int minute)
        {
            if (!Live)
            {
                return true;
            }

            if (walker.Roams)
            {
                return !(SliceHours.IsNight(minute) && SliceHours.HomeAtNight(walker.RouteSeed));
            }

            var keeper = SliceHours.PlaceKeeper(walker.gameObject.scene.name);
            return keeper == null || SliceHours.PostAt(keeper, minute) == SliceHours.Work;
        }

        /// <summary>
        /// Whether the player watches the body now: near, on the screen and with nothing solid between, and it has not yet
        /// waited its longest, counted in the world's ticks from the first pass that found it watched.
        /// </summary>
        private bool Watched(Object key, Transform body)
        {
            var camera = Camera.main;
            if (camera == null || body == null)
            {
                return false;
            }

            var at = body.position + (Vector3.up * 1.2f);
            var from = camera.transform.position;
            if ((at - from).sqrMagnitude > SightReach * SightReach)
            {
                return false;
            }

            var view = camera.WorldToViewportPoint(at);
            if (view.z <= 0f || view.x < -0.05f || view.x > 1.05f || view.y < -0.05f || view.y > 1.05f)
            {
                return false;
            }

            if (Physics.Linecast(from, at, _sight != 0 ? _sight : Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            var now = _world.Clock.Tick;
            if (!_waited.TryGetValue(key, out var since))
            {
                since = now;
                _waited[key] = now;
            }

            return now - since < SliceTuning.Current.ScheduleWaitTicks;
        }
    }
}
