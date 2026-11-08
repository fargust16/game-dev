using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameDev.VictorianKit.EditorTools
{
    /// <summary>Stills of the open scene for checking the streets, written as PNG to Logs/VictorianKit.</summary>
    public static class VicShots
    {
        public static Color Sky = new Color(0.62f, 0.72f, 0.84f);

        /// <summary>A perspective view from <paramref name="from"/> toward <paramref name="to"/>.</summary>
        public static void Eye(string name, Vector3 from, Vector3 to, float fieldOfView = 55f, int width = 1600, int height = 900)
        {
            var camera = Make();
            camera.fieldOfView = fieldOfView;
            camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from));
            Shoot(camera, name, width, height);
        }

        /// <summary>An orthographic view straight down onto <paramref name="centre"/>, <paramref name="half"/> metres each way.</summary>
        public static void Top(string name, Vector3 centre, float half, int size = 1400)
        {
            var camera = Make();
            camera.orthographic = true;
            camera.orthographicSize = half;
            camera.transform.SetPositionAndRotation(new Vector3(centre.x, 200f, centre.z), Quaternion.LookRotation(Vector3.down, Vector3.forward));
            camera.farClipPlane = 400f;
            Shoot(camera, name, size, size);
        }

        /// <summary>A plan section: an orthographic view straight down from height <paramref name="cut"/>, seeing only what lies below it.</summary>
        public static void Section(string name, Vector3 centre, float cut, float half, int size = 1200)
        {
            var camera = Make();
            camera.orthographic = true;
            camera.orthographicSize = half;
            camera.transform.SetPositionAndRotation(new Vector3(centre.x, cut, centre.z), Quaternion.LookRotation(Vector3.down, Vector3.forward));
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = cut + 2f;

            // The rooms behind the windows cull by screen size, which an orthographic view of a whole street falls under.
            var bias = QualitySettings.lodBias;
            QualitySettings.lodBias = 1000f;
            Shoot(camera, name, size, size);
            QualitySettings.lodBias = bias;
        }

        private static Camera Make()
        {
            var camera = new GameObject("VicShotCamera").AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 1000f;
            camera.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            camera.backgroundColor = Sky;
            camera.allowHDR = true;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            return camera;
        }

        private static void Shoot(Camera camera, string name, int width, int height)
        {
            Directory.CreateDirectory(VicKitPaths.LogFolder);
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(VicKitPaths.LogFolder, name + ".png"), image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(camera.gameObject);
        }
    }
}
