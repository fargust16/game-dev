using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.VictorianKit
{
    /// <summary>
    /// The rooms built behind one house's windows (VicRooms), drawn only while a camera is near them: on within
    /// <see cref="OnDistance"/> of their box, off beyond <see cref="OffDistance"/>, left as they are between. The window
    /// glass is opaque from its _RoomFar, 46 m, so the switch happens behind opaque glass and is never seen. The distance
    /// is the camera's to the nearest point of the rooms' box, so the lodBias, the field of view and the house's size
    /// change nothing (Plans/VEILMOUTH_REFERENCE_PLAN.md R2 step 3).
    /// </summary>
    /// <remarks>
    /// Each camera that draws decides before it culls: in play the main camera, in the editor every game and scene view
    /// camera. Turning off uses <see cref="Renderer.forceRenderingOff"/>, which is never saved, so the editor's switching
    /// leaves no change in a scene or a prefab, and static batching keeps its batches.
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class VicRoomsSwitch : MonoBehaviour
    {
        /// <summary>
        /// Measured, m: the glass is opaque from 46 m (_RoomFar of every window material), and the farthest window of the
        /// city's 55 house types lies 1.28 m outside its rooms' box (KitHouseLodTests, 2026-11-08), so 46 + 1.28 rounded
        /// up: no glass in front of the rooms is clear yet when they come on.
        /// </summary>
        public const float OnDistance = 48f;

        /// <summary>Authoring default, m: 5 m beyond <see cref="OnDistance"/>, so a camera that stops at the line does not flicker the rooms.</summary>
        public const float OffDistance = 53f;

        [SerializeField, Tooltip("The rooms' box in this object's own space, set when the house is baked.")]
        private Bounds _box;

        private static readonly List<VicRoomsSwitch> Live = new List<VicRoomsSwitch>();
        private Renderer[] _renderers;
        private int _state = -1; // -1 not decided yet, 0 off, 1 on

        /// <summary>The rooms' box in this object's own space.</summary>
        public Bounds Box
        {
            get => _box;
            set => _box = value;
        }

        /// <summary>Whether the rooms are drawn now (true until the first camera decides).</summary>
        public bool Shown => _state != 0;

        /// <summary>The distance, m, from <paramref name="eye"/> to the nearest point of the rooms' box.</summary>
        public float DistanceFrom(Vector3 eye) => Mathf.Sqrt(_box.SqrDistance(transform.InverseTransformPoint(eye))) * transform.lossyScale.x;

        /// <summary>Turns the rooms on or off for a camera at <paramref name="eye"/>, keeping their state between the two distances.</summary>
        public void Decide(Vector3 eye)
        {
            var d = DistanceFrom(eye);
            Show(_state == 1 ? d <= OffDistance : d <= OnDistance);
        }

        // In play the rooms never change, and the city comes back on at every door: the renderers are read once.
        private void OnEnable()
        {
            if (_renderers == null || !Application.isPlaying)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
            }

            if (Live.Count == 0)
            {
                RenderPipelineManager.beginCameraRendering += BeforeCamera;
            }

            Live.Add(this);
        }

        // The state is kept: the city switched off behind an interior comes back with its rooms as they were.
        private void OnDisable()
        {
            Live.Remove(this);
            if (Live.Count == 0)
            {
                RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            }
        }

        private static void BeforeCamera(ScriptableRenderContext context, Camera camera)
        {
            var decides = Application.isPlaying
                ? camera == Camera.main
                : camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView;
            if (!decides)
            {
                return;
            }

            var eye = camera.transform.position;
            foreach (var rooms in Live)
            {
                rooms.Decide(eye);
            }
        }

        private void Show(bool on)
        {
            var state = on ? 1 : 0;
            if (state == _state)
            {
                return;
            }

            _state = state;
            foreach (var r in _renderers)
            {
                if (r != null)
                {
                    r.forceRenderingOff = !on;
                }
            }
        }
    }
}
