using GameDev.Slice.BreakIns;
using GameDev.World.Portals;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameDev.Core.Scenes;

namespace GameDev.Slice.Dwellings
{
    /// <summary>
    /// Every dwelling's street door is locked (S5c.5; 10.3 §6.1): when a scene loads, each door that leads into the shared
    /// Dwelling scene gets a <see cref="LockedDoor"/> with its dwelling's id and its Lock by the household's wealth, the same
    /// lock through the street door and a back door. The door out of a dwelling is never locked. Each home's door is also
    /// made known to <see cref="GameDev.Slice.People.SliceHomes"/>, and a resident's report of a break-in waiting from
    /// inside is taken to the nearest Warden once the street is loaded (10.3 §6.2).
    /// </summary>
    public static class DwellingLocks
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            SceneTransition.TravelCompleted -= OnTravelled;
            SceneTransition.TravelCompleted += OnTravelled;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                Lock(SceneManager.GetSceneAt(i));
            }
        }

        private static void OnLoaded(Scene scene, LoadSceneMode mode) => Lock(scene);

        // The street stays loaded behind a dwelling, switched off (R1), so the way back is no load: the report waits for it.
        private static void OnTravelled(TravelRequest request) => Report();

        internal static bool DenOpen()
        {
            var alchemy = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Alchemy : null;
            return alchemy != null && alchemy.KnowsDen && alchemy.IsOpen(GameDev.Slice.Alchemy.BenchKind.Den);
        }

        /// <summary>Locks the dwellings' doors in <paramref name="scene"/>; how many were locked now.</summary>
        public static int Lock(Scene scene)
        {
            if (!scene.isLoaded)
            {
                return 0;
            }

            var locked = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var portal in root.GetComponentsInChildren<Portal>(true))
                {
                    var target = portal.Target;
                    if (target.ArrivalPortalId != DwellingRuntime.ArrivalPortalId || !DwellingKey.TryParse(target.Context, out var key)
                        || portal.GetComponent<LockedDoor>() != null)
                    {
                        continue;
                    }

                    if (DwellingResidents.IsHome(key))
                    {
                        GameDev.Slice.People.SliceHomes.Know(key.DwellingId, key.DoorX / 10f, key.DoorZ / 10f, (int)key.Wealth);
                    }

                    var lockedDoor = portal.gameObject.AddComponent<LockedDoor>();
                    lockedDoor.Configure(key.DwellingId, key.Lock);
                    if (key.DwellingId == GameDev.Slice.Alchemy.SliceAlchemy.DenDwelling)
                    {
                        // Veinmarket lets in one who heard of the den while it brews (SLICE_TRIAL §4); to anyone else, and
                        // by day, its door is a lock like any other, and going through it a break-in.
                        lockedDoor.OpenForGood = DenOpen;
                    }

                    locked++;
                }
            }

            return locked;
        }

        /// <summary>
        /// A resident's report of a break-in (10.3 §6.2), taken to the nearest Warden once the street with the dwelling's
        /// door stands awake again; only a street with that door takes it, never the Dwelling scene itself.
        /// </summary>
        public static bool Report()
        {
            var world = SliceWorldDriver.Running;
            if (world == null || world.BreakIns == null || world.Fights == null || !world.BreakIns.HasReport)
            {
                return false;
            }

            foreach (var portal in Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude))
            {
                var target = portal.Target;
                if (target.ArrivalPortalId != DwellingRuntime.ArrivalPortalId || !DwellingKey.TryParse(target.Context, out var key)
                    || !world.BreakIns.ReportedAt(key.DoorX, key.DoorZ))
                {
                    continue;
                }

                return world.BreakIns.TakeReport(out _, out var since) && world.Fights.ReportCrimeFrom(portal.transform.position, since);
            }

            return false;
        }
    }
}
