using System;
using System.Collections.Generic;
using GameDev.Slice.People;
using GameDev.Slice.Save;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// The fights' host in a Suspend Save (17 X.3): what lives on between fights, kept exactly. The counters that key its
    /// draws, the deaths the goddess counts, the wounds shots left on people, the fallen with their day, the Wardens left
    /// standing where an Assault was, and a Warden's errand to take up the fallen. Nothing of a fight is kept: a save is
    /// written outside one, and outside an Alarm's answer too (<see cref="AlarmUnderWay"/>).
    /// </summary>
    public sealed partial class SliceConfront
    {
        /// <summary>
        /// Whether an Alarm's answer is under way, during which a Suspend Save waits (17 X.3, authoring default): an Assault,
        /// Wardens or the church's people running to a place or looking round there, a resident gone for help, or a search
        /// after a Break Away.
        /// </summary>
        public bool AlarmUnderWay =>
            Assaulting || _late.Count > 0 || _runners.Count > 0 || _searching.Exists(s => s != null) || _church.Exists(b => b != null)
            || _returning.Exists(r => r.Body != null && LooksRound(r));

        [Serializable]
        private sealed class SavedHost
        {
            public int nextId;
            public int fights;
            public int shots;
            public int shotDeaths;
            public int lastRound;
            public Vector3 assaultAt;
            public List<long> deathTicks = new List<long>();
            public List<SavedWound> wounds = new List<SavedWound>();
            public List<SavedFallen> fallen = new List<SavedFallen>();
            public List<SavedPost> standing = new List<SavedPost>();
            public string errandWarden = string.Empty;
            public List<string> errandBodies = new List<string>();
            public Vector3 errandPost;
            public Quaternion errandFacing;
        }

        [Serializable]
        private sealed class SavedWound
        {
            public string body;
            public double lost;
        }

        [Serializable]
        private sealed class SavedFallen
        {
            public string body;
            public long tick;
            public bool sent;
        }

        [Serializable]
        private sealed class SavedPost
        {
            public string body;
            public Vector3 post;
            public Quaternion facing;
            public long lookUntil;
        }

        public string CaptureSave()
        {
            var saved = new SavedHost
            {
                nextId = _nextId,
                fights = _fights,
                shots = _shots,
                shotDeaths = _shotDeaths,
                lastRound = _lastRound,
                assaultAt = _assaultAt,
                deathTicks = new List<long>(_deathTicks),
            };

            foreach (var pair in _wounds)
            {
                if (pair.Key != null)
                {
                    saved.wounds.Add(new SavedWound { body = SavedObjects.PathOf(pair.Key), lost = pair.Value });
                }
            }

            foreach (var f in _fallen)
            {
                if (f.Body != null)
                {
                    saved.fallen.Add(new SavedFallen { body = SavedObjects.PathOf(f.Body.transform), tick = f.Tick, sent = f.Sent });
                }
            }

            foreach (var r in _returning)
            {
                if (r.Body != null)
                {
                    saved.standing.Add(new SavedPost { body = SavedObjects.PathOf(r.Body), post = r.Post, facing = r.PostFacing, lookUntil = r.LookUntil });
                }
            }

            if (_errand != null)
            {
                saved.errandWarden = SavedObjects.PathOf(_errand.transform);
                saved.errandBodies = _errand.Bodies.FindAll(b => b != null).ConvertAll(b => SavedObjects.PathOf(b.transform));
                saved.errandPost = _errand.Post;
                saved.errandFacing = _errand.Facing;
            }

            return JsonUtility.ToJson(saved);
        }

        /// <summary>Puts the host back once the saved people stand and lie where they were.</summary>
        public void RestoreSave(string payload)
        {
            var saved = JsonUtility.FromJson<SavedHost>(payload);
            _nextId = saved.nextId;
            _fights = saved.fights;
            _shots = saved.shots;
            _shotDeaths = saved.shotDeaths;
            _lastRound = saved.lastRound;
            _assaultAt = saved.assaultAt;
            _deathTicks.Clear();
            _deathTicks.AddRange(saved.deathTicks);

            _wounds.Clear();
            foreach (var w in saved.wounds)
            {
                var body = SavedObjects.FindPath(w.body);
                if (body != null)
                {
                    _wounds[body] = w.lost;
                }
            }

            _fallen.Clear();
            foreach (var f in saved.fallen)
            {
                var body = SavedObjects.FindPath(f.body);
                if (body != null)
                {
                    _fallen.Add(new Fallen { Body = body.gameObject, Tick = f.tick, Sent = f.sent });
                }
            }

            _late.Clear();
            _returning.Clear();
            foreach (var r in saved.standing)
            {
                var body = SavedObjects.FindPath(r.body);
                if (body != null)
                {
                    _returning.Add(new Coming { Body = body, Post = r.post, PostFacing = r.facing, LookUntil = r.lookUntil, OnlyLooks = true });
                }
            }

            if (_errand != null)
            {
                Destroy(_errand);
            }

            _errand = null;
            var warden = string.IsNullOrEmpty(saved.errandWarden) ? null : SavedObjects.FindPath(saved.errandWarden);
            if (warden != null)
            {
                var bodies = new List<GameObject>();
                foreach (var id in saved.errandBodies)
                {
                    var body = SavedObjects.FindPath(id);
                    if (body != null)
                    {
                        bodies.Add(body.gameObject);
                    }
                }

                if (bodies.Count > 0)
                {
                    _errand = WardenErrand.Send(warden, bodies, saved.errandPost, saved.errandFacing);
                }
            }
        }
    }
}
