#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Slice.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>Loose props (kit standard §4, owner 2026-11-06): they rest where they stand, and the player pushes them.</summary>
    public sealed class LoosePropsTests : SliceInputTestBase
    {
        private const float Rest = 0.15f; // m a woken prop may settle

        [UnityTest]
        public IEnumerator InTheTavernWokenPropsRestWhereTheyStand() => Settles("Assets/Scenes/Slice/Interiors/SootAndBell.unity");

        [UnityTest]
        public IEnumerator InTheApothecaryWokenPropsRestWhereTheyStand() => Settles("Assets/Scenes/Slice/Interiors/HarrowApothecary.unity");

        [UnityTest]
        public IEnumerator InVeilmouthWokenPropsRestWhereTheyStand() => Settles("Assets/Scenes/Slice/Veilmouth.unity");

        [UnityTest]
        public IEnumerator ThePlayerPushesAChair()
        {
            yield return LoadScene("Assets/Scenes/Slice/Interiors/SootAndBell.unity");
            var chair = Object.FindObjectsByType<LooseProp>(FindObjectsInactive.Exclude)
                .Where(p => !p.IsThing && Mathf.Approximately(p.GetComponent<Rigidbody>().mass, 6f)).OrderBy(p => p.name).FirstOrDefault();
            Assert.IsNotNull(chair, "the tavern has a chair");
            var player = Object.FindAnyObjectByType<PushLooseProps>();
            Assert.IsNotNull(player, "the player pushes props");
            var controller = player.GetComponent<CharacterController>();

            // On a floor of its own, away from the room's furniture, the player walks into the chair from 1.2 m.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(500f, 49.5f, 500f);
            floor.transform.localScale = new Vector3(20f, 1f, 20f);
            var body = chair.GetComponent<Rigidbody>();
            chair.transform.position = new Vector3(500f, 50.02f, 500f);
            body.position = chair.transform.position;
            Physics.SyncTransforms();
            var centre = body.worldCenterOfMass;
            var toward = Vector3.forward;
            controller.enabled = false;
            player.transform.position = new Vector3(centre.x, 50.05f, centre.z) - (toward * 1.2f);
            controller.enabled = true;
            yield return new WaitForFixedUpdate();
            var start = chair.transform.position;
            for (var t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                controller.Move(((toward * 2f) + (Vector3.down * 0.5f)) * Time.deltaTime);
                yield return null;
            }

            var moved = Vector3.ProjectOnPlane(chair.transform.position - start, Vector3.up).magnitude;
            Assert.Greater(moved, 0.1f, $"{chair.name} is pushed");
        }

        private IEnumerator Settles(string scene)
        {
            yield return LoadScene(scene);
            var props = Object.FindObjectsByType<LooseProp>(FindObjectsInactive.Exclude);
            Assert.IsNotEmpty(props, "the scene has loose props");
            foreach (var thing in props.Where(p => p.IsThing))
            {
                Assert.IsNull(thing.transform.parent != null ? thing.transform.parent.GetComponentInParent<KitBlock>() : null,
                    $"{thing.name} has left its set");
            }

            var start = props.Select(p => p.transform.position).ToArray();
            foreach (var prop in props)
            {
                prop.GetComponent<Rigidbody>().WakeUp();
            }

            yield return new WaitForSeconds(2f);
            var faults = props.Select((p, i) => (p, d: Vector3.Distance(p.transform.position, start[i])))
                .Where(x => x.d > Rest).Select(x => $"{x.p.name} at {start[System.Array.IndexOf(props, x.p)]} moved {x.d:0.00} m").ToList();
            CollectionAssert.IsEmpty(faults, $"{faults.Count} of {props.Length} props did not rest");
        }
    }
}
#endif
