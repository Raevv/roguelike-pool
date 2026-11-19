using UnityEditor;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Scene view tools for a selected <see cref="Pocket"/> (the filled zones themselves come from Pocket.OnDrawGizmos):
    /// one draggable dot per radius (drawn hole, colored-ball capture, cue-ball capture) and a block listing the values.
    /// Dragging a dot edits the Inspector value (Radius, Object Capture Scale, Cue Capture Scale), with Undo.
    /// Dashed circles show the arcade assist ranges (blue = aspiration, purple = rebound funnel).
    /// </summary>
    [CustomEditor(typeof(Pocket)), CanEditMultipleObjects]
    public class PocketEditor : Editor
    {
        static readonly Color AssistColor = new Color(0.4f, 0.8f, 1f, 0.8f);
        static readonly Color FunnelColor = new Color(0.8f, 0.5f, 1f, 0.8f);

        void OnSceneGUI()
        {
            var p = (Pocket)target;
            Vector3 c = p.transform.position;

            // Dots point toward the table center so they stay over the felt, spread so they don't overlap.
            var table = p.GetComponentInParent<PoolTable>();
            Vector3 inward = table != null ? table.transform.position - c : Vector3.left;
            inward.z = 0f;
            if (inward.sqrMagnitude < 1e-6f) inward = Vector3.left;
            inward.Normalize();

            Handles.color = AssistColor;
            DrawDashedCircle(c, p.radius * p.assistRadiusScale);
            Handles.color = FunnelColor;
            DrawDashedCircle(c, p.radius * p.funnelRadiusScale);

            // Drawn hole radius (also the trigger collider).
            float hole = RadiusDot(c, Rotate(inward, 0f), p.radius, Pocket.HoleGizmoColor);
            if (hole != p.radius)
            {
                Undo.RecordObject(p, "Rayon de la poche");
                p.radius = Mathf.Max(0.05f, hole);
                if (p.TryGetComponent(out CircleCollider2D circle))
                {
                    Undo.RecordObject(circle, "Rayon de la poche");
                    circle.radius = p.radius;
                    Mark(circle);
                }
                Mark(p);
            }

            // Colored capture radius (edits objectCaptureScale).
            float obj = RadiusDot(c, Rotate(inward, 45f), p.ObjectCaptureRadius, Pocket.ObjectGizmoColor);
            if (obj != p.ObjectCaptureRadius)
            {
                Undo.RecordObject(p, "Capture des couleurs");
                p.objectCaptureScale = Mathf.Max(0.05f, obj / p.radius);
                Mark(p);
            }

            // Cue capture radius (edits cueCaptureScale, a fraction of the colored capture, 0.1–1 like its Range).
            float cue = RadiusDot(c, Rotate(inward, -45f), p.CueCaptureRadius, Pocket.CueGizmoColor);
            if (cue != p.CueCaptureRadius)
            {
                Undo.RecordObject(p, "Capture de la blanche");
                p.cueCaptureScale = Mathf.Clamp(cue / p.ObjectCaptureRadius, 0.1f, 1f);
                Mark(p);
            }

            // Values block beside the pocket, over the felt (outside the dashed ranges, clear of the move tool).
            Vector2 gui = HandleUtility.WorldToGUIPoint(c + inward * (p.radius * Mathf.Max(p.assistRadiusScale, p.funnelRadiusScale) + 0.3f));
            Handles.BeginGUI();
            var rows = new (string text, Color color)[]
            {
                ($"Trou {p.radius:0.00}", Pocket.HoleGizmoColor),
                ($"Couleurs {p.ObjectCaptureRadius:0.00} (x{p.objectCaptureScale:0.00})", Pocket.ObjectGizmoColor),
                ($"Blanche {p.CueCaptureRadius:0.00} (x{p.cueCaptureScale:0.00})", Pocket.CueGizmoColor),
                ($"Aspiration {p.radius * p.assistRadiusScale:0.00}", AssistColor),
                ($"Rebonds {p.radius * p.funnelRadiusScale:0.00}", FunnelColor),
            };
            const float w = 190f, lh = 16f;
            float x = inward.x < 0f ? gui.x - w : gui.x;          // grow away from the pocket
            float y = inward.y < 0f ? gui.y : gui.y - rows.Length * lh; // GUI y points down
            EditorGUI.DrawRect(new Rect(x - 4f, y - 2f, w, rows.Length * lh + 4f), new Color(0f, 0f, 0f, 0.6f));
            for (int i = 0; i < rows.Length; i++)
                GUI.Label(new Rect(x, y + i * lh, w, lh), rows[i].text, LabelStyle(rows[i].color));
            Handles.EndGUI();
        }

        /// <summary>A dot on the circle of radius r along dir; returns the new radius (unchanged if not dragged).</summary>
        static float RadiusDot(Vector3 c, Vector3 dir, float r, Color color)
        {
            Vector3 pos = c + dir * r;
            float size = HandleUtility.GetHandleSize(pos) * 0.08f;
            Handles.color = color;
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.FreeMoveHandle(pos, size, Vector3.zero, Handles.DotHandleCap);
            if (!EditorGUI.EndChangeCheck()) return r;
            Vector3 d = moved - c;
            d.z = 0f;
            return Mathf.Max(0.01f, Vector3.Dot(d, dir)); // projected on the dot's direction: sideways drags don't jump
        }

        static void DrawDashedCircle(Vector3 c, float r)
        {
            const int n = 64;
            for (int i = 0; i < n; i += 2)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                Handles.DrawLine(c + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)) * r, c + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * r);
            }
        }

        static void Mark(Object o)
        {
            EditorUtility.SetDirty(o);
            PrefabUtility.RecordPrefabInstancePropertyModifications(o);
        }

        static Vector3 Rotate(Vector3 v, float degrees) => Quaternion.Euler(0f, 0f, degrees) * v;

        static GUIStyle LabelStyle(Color color)
        {
            var s = new GUIStyle(EditorStyles.boldLabel);
            s.normal.textColor = color;
            return s;
        }
    }
}
