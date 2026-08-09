using UnityEngine;

namespace GameDev.World.Buildings.Shell
{
    public static class ShellPlaceholderFactory
    {
        public static GameObject SpawnShellRoot(ShellInstance shell, Transform parent, string name)
        {
            var root = new GameObject(name);
            if (parent != null)
                root.transform.SetParent(parent, false);

            var yaw = Quaternion.Euler(0f, shell.Plot.YawDegrees, 0f);
            root.transform.SetPositionAndRotation(shell.Plot.Origin, yaw);

            for (var i = 0; i < shell.Pieces.Count; i++)
            {
                var p = shell.Pieces[i];
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = p.PieceId;
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = p.LocalPosition;
                go.transform.localEulerAngles = p.LocalEuler;
                go.transform.localScale = p.LocalScale;
                var r = go.GetComponent<Renderer>();
                if (r != null)
                {
                    var mat = CreateTintMaterial(p.Tint);
                    // Openings are thin facade plaques — draw after walls so they are not hidden.
                    if (IsOpeningCategory(p.Category))
                        mat.renderQueue = 3001;
                    r.sharedMaterial = mat;
                }

                // Drop colliders on decorative small pieces to keep sandbox light.
                if (p.Category == ExteriorPieceCategory.Trim
                    || p.Category == ExteriorPieceCategory.Window
                    || p.Category == ExteriorPieceCategory.AtticWindow
                    || p.Category == ExteriorPieceCategory.Door
                    || p.Category == ExteriorPieceCategory.Shopfront
                    || p.Category == ExteriorPieceCategory.Chimney
                    || p.Category == ExteriorPieceCategory.Fence
                    || p.Category == ExteriorPieceCategory.Stoop
                    || p.Category == ExteriorPieceCategory.Corner)
                {
                    var col = go.GetComponent<Collider>();
                    if (col != null)
                    {
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                            Object.DestroyImmediate(col);
                        else
                            Object.Destroy(col);
#else
                        Object.Destroy(col);
#endif
                    }
                }
            }

            var debug = root.AddComponent<ShellDebugGizmo>();
            debug.Configure(shell);
            return root;
        }

        private static bool IsOpeningCategory(ExteriorPieceCategory c)
        {
            return c == ExteriorPieceCategory.Window
                   || c == ExteriorPieceCategory.Door
                   || c == ExteriorPieceCategory.Shopfront
                   || c == ExteriorPieceCategory.AtticWindow;
        }

        private static Material CreateTintMaterial(Color tint)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            var mat = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", tint);
            else
                mat.color = tint;
            return mat;
        }
    }
}
