﻿using UnityEngine;

namespace My_Utils.Lean_Tween.Visual
{
    public static class DrawingHelper
    {
        private static Texture2D aaLineTex = null;
        private static Texture2D lineTex = null;
        private static Material blitMaterial = null;
        private static Material blendMaterial = null;
        private static Rect lineRect = new Rect(0, 0, 1, 1);

        public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width, bool antiAlias)
        {
            // Normally the static initializer does this, but to handle texture reinitialization
            // after editor play mode stops we need this check in the Editor.
#if UNITY_EDITOR
            if (!lineTex)
            {
                Initialize();
            }
#endif

            // Note that theta = atan2(dy, dx) is the angle we want to rotate by, but instead
            // of calculating the angle we just use the sine (dy/len) and cosine (dx/len).
            float dx = pointB.x - pointA.x;
            float dy = pointB.y - pointA.y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);

            // Early out on tiny lines to avoid divide by zero.
            // Plus what's the point of drawing a line 1/1000th of a pixel long??
            if (len < 0.001f)
            {
                return;
            }

            // Pick texture and material (and tweak width) based on anti-alias setting.
            Texture2D tex;
            Material mat;
            if (antiAlias)
            {
                // Multiplying by three is fine for anti-aliasing width-1 lines, but make a wide "fringe"
                // for thicker lines, which may or may not be desirable.
                width = width * 3.0f;
                tex = aaLineTex;
                mat = blendMaterial;
            }
            else
            {
                tex = lineTex;
                mat = blitMaterial;
            }

            float wdx = width * dy / len;
            float wdy = width * dx / len;

            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.m00 = dx;
            matrix.m01 = -wdx;
            matrix.m03 = pointA.x + 0.5f * wdx;
            matrix.m10 = dy;
            matrix.m11 = wdy;
            matrix.m13 = pointA.y - 0.5f * wdy;

            // Use GL matrix and Graphics.DrawTexture rather than GUI.matrix and GUI.DrawTexture,
            // for better performance. (Setting GUI.matrix is slow, and GUI.DrawTexture is just a
            // wrapper on Graphics.DrawTexture.)
            GL.PushMatrix();
            GL.MultMatrix(matrix);
            Graphics.DrawTexture(lineRect, tex, lineRect, 0, 0, 0, 0, color, mat);
            GL.PopMatrix();
        }

        // Other than method name, DrawBezierLine is unchanged from Linusmartensson's original implementation.
        public static void DrawBezierLine(Vector2 start, Vector2 startTangent, Vector2 end, Vector2 endTangent, Color color, float width, bool antiAlias, int segments)
        {
            Vector2 lastV = CubeBezier(start, startTangent, end, endTangent, 0);
            for (int i = 1; i < segments; ++i)
            {
                Vector2 v = CubeBezier(start, startTangent, end, endTangent, i / (float)segments);
                DrawingHelper.DrawLine(lastV, v, color, width, antiAlias);
                lastV = v;
            }
        }

        private static Vector2 CubeBezier(Vector2 s, Vector2 st, Vector2 e, Vector2 et, float t)
        {
            float rt = 1 - t;
            return rt * rt * rt * s + 3 * rt * rt * t * st + 3 * rt * t * t * et + t * t * t * e;
        }

        // This static initializer works for runtime, but apparently isn't called when
        // Editor play mode stops, so DrawLine will re-initialize if needed.
        static DrawingHelper()
        {
            Initialize();
        }

        private static void Initialize()
        {
            if (lineTex == null)
            {
                lineTex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                lineTex.hideFlags = HideFlags.HideAndDontSave;
                lineTex.SetPixel(0, 1, Color.white);
                lineTex.Apply();
            }
            if (aaLineTex == null)
            {
                // TODO: better anti-aliasing of wide lines with a larger texture? or use Graphics.DrawTexture with border settings
                aaLineTex = new Texture2D(1, 3, TextureFormat.ARGB32, false);
                aaLineTex.SetPixel(0, 0, new Color(1, 1, 1, 0));
                aaLineTex.SetPixel(0, 1, Color.white);
                aaLineTex.SetPixel(0, 2, new Color(1, 1, 1, 0));
                aaLineTex.Apply();
            }

            // GUI.blitMaterial and GUI.blendMaterial are used internally by GUI.DrawTexture,
            // depending on the alphaBlend parameter. Use reflection to "borrow" these references.
            blitMaterial = (Material)typeof(GUI).GetMethod("get_blitMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
            blendMaterial = (Material)typeof(GUI).GetMethod("get_blendMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
        }
    }
}