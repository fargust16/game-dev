using UnityEngine;

namespace GameDev.Cameras
{
    /// <summary>
    /// A view the third-person camera blends into while a weapon is aimed (10.2 §5e): the camera leaves its orbit for
    /// <see cref="Eye"/>, keeps the look the mouse gives it, and narrows to <see cref="FieldOfView"/>, all by
    /// <see cref="Weight"/>. Once placed it tells the view, so the view can set what it shows along the camera's line.
    /// </summary>
    public interface IAimView
    {
        /// <summary>0 in the orbit, 1 fully at the eye.</summary>
        float Weight { get; }

        /// <summary>Where the camera stands when fully aimed, in the world.</summary>
        Vector3 Eye { get; }

        /// <summary>The camera's vertical field of view when fully aimed, degrees.</summary>
        float FieldOfView { get; }

        /// <summary>The share of the mouse's turn while aimed.</summary>
        float MouseShare { get; }

        /// <summary>The camera has been placed this frame.</summary>
        void Placed(Camera camera);
    }
}
