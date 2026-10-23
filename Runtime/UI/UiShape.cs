using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Aurumworks
{
    /// <summary>How a UiShape's corners are drawn.</summary>
    public enum UiCornerStyle
    {
        /// <summary>Cut off at 45°.</summary>
        Cut,
        Round,
    }

    /// <summary>Which corners of a UiShape are cut or rounded; the others stay square.</summary>
    [Flags]
    public enum UiCorners
    {
        None = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomRight = 4,
        BottomLeft = 8,
        All = TopLeft | TopRight | BottomRight | BottomLeft,
    }

    /// <summary>
    /// A rectangle with cut or rounded corners, filled or as a frame of Border width. Panels, cards
    /// and buttons are drawn with it, so a game needs no sprite art for them.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class UiShape : MaskableGraphic
    {
        private const float RoundStep = 3f; // a rounded corner gets a segment per this much radius
        private const int MaxRoundSegments = 10;

        [SerializeField] private UiCornerStyle _style = UiCornerStyle.Cut;
        [SerializeField] private UiCorners _corners = UiCorners.TopLeft | UiCorners.BottomRight;
        [SerializeField] private float _corner = 12f;
        [Tooltip("0 for a filled shape; otherwise the width of the frame.")]
        [SerializeField] private float _border;

        private readonly List<Vector2> _outer = new(), _inner = new(), _points = new();

        public UiCornerStyle Style
        {
            get => _style;
            set
            {
                _style = value;
                SetVerticesDirty();
            }
        }

        public UiCorners Corners
        {
            get => _corners;
            set
            {
                _corners = value;
                SetVerticesDirty();
            }
        }

        /// <summary>The size of the cut, or the radius.</summary>
        public float Corner
        {
            get => _corner;
            set
            {
                _corner = value;
                SetVerticesDirty();
            }
        }

        /// <summary>0 for a filled shape; otherwise the width of the frame.</summary>
        public float Border
        {
            get => _border;
            set
            {
                _border = value;
                SetVerticesDirty();
            }
        }

        //----------

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float corner = Mathf.Min(_corner, rect.width / 2f, rect.height / 2f);
            int segments = _style == UiCornerStyle.Round
                ? Mathf.Clamp(Mathf.CeilToInt(corner / RoundStep), 2, MaxRoundSegments)
                : 1;
            Outline(_outer, rect, corner, segments);
            if (_border <= 0f)
            {
                Fan(vh, _outer, color);
                return;
            }

            float border = Mathf.Min(_border, rect.width / 2f, rect.height / 2f);
            var hole = new Rect(rect.x + border, rect.y + border, rect.width - 2f * border, rect.height - 2f * border);
            // A cut's inner edge runs parallel to its outer one; a round corner keeps its centre.
            float inset = _style == UiCornerStyle.Round ? border : border * 0.414f;
            Outline(_inner, hole, Mathf.Max(0f, corner - inset), segments);
            Ring(vh, _outer, _inner, color);
        }

        // The outline, clockwise from the top edge's left end. Every corner gives the same number of
        // points whatever its size, so a frame's two outlines pair up.
        private void Outline(List<Vector2> outline, Rect rect, float size, int segments)
        {
            outline.Clear();
            _points.Clear();
            AddCorner(_points, UiCorners.TopLeft, new Vector2(rect.xMin, rect.yMax), new Vector2(1f, -1f), 180f, size, segments);
            outline.Add(_points[_points.Count - 1]);
            AddCorner(outline, UiCorners.TopRight, new Vector2(rect.xMax, rect.yMax), new Vector2(-1f, -1f), 90f, size, segments);
            AddCorner(outline, UiCorners.BottomRight, new Vector2(rect.xMax, rect.yMin), new Vector2(-1f, 1f), 0f, size, segments);
            AddCorner(outline, UiCorners.BottomLeft, new Vector2(rect.xMin, rect.yMin), new Vector2(1f, 1f), -90f, size, segments);
            for (int i = 0; i < _points.Count - 1; i++)
            {
                outline.Add(_points[i]);
            }
        }

        // One corner's points, clockwise: two for a cut, segments + 1 for an arc. inward points from
        // the corner into the rect; startAngle is where the arc starts, seen from its centre.
        private void AddCorner(List<Vector2> points, UiCorners which, Vector2 at, Vector2 inward, float startAngle, float size,
            int segments)
        {
            if ((_corners & which) == 0)
            {
                points.Add(at);
                return;
            }

            if (_style == UiCornerStyle.Cut)
            {
                // Clockwise, the top-left and bottom-right corners leave along a vertical edge.
                var alongX = new Vector2(inward.x * size, 0f);
                var alongY = new Vector2(0f, inward.y * size);
                bool verticalFirst = inward.x * inward.y < 0f;
                points.Add(at + (verticalFirst ? alongY : alongX));
                points.Add(at + (verticalFirst ? alongX : alongY));
                return;
            }

            Vector2 centre = at + inward * size;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (startAngle - 90f * i / segments) * Mathf.Deg2Rad;
                points.Add(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size);
            }
        }

        //----------

        /// <summary>Adds a convex polygon as a triangle fan.</summary>
        public static void Fan(VertexHelper vh, IReadOnlyList<Vector2> points, Color32 color)
        {
            int start = vh.currentVertCount;
            for (int i = 0; i < points.Count; i++)
            {
                vh.AddVert(points[i], color, Vector2.zero);
            }

            for (int i = 1; i < points.Count - 1; i++)
            {
                vh.AddTriangle(start, start + i, start + i + 1);
            }
        }

        /// <summary>Adds the band between two polygons with the same number of points.</summary>
        public static void Ring(VertexHelper vh, IReadOnlyList<Vector2> outer, IReadOnlyList<Vector2> inner, Color32 color)
        {
            int start = vh.currentVertCount;
            int count = outer.Count;
            for (int i = 0; i < count; i++)
            {
                vh.AddVert(outer[i], color, Vector2.zero);
                vh.AddVert(inner[i], color, Vector2.zero);
            }

            for (int i = 0; i < count; i++)
            {
                int outer0 = start + 2 * i, inner0 = outer0 + 1;
                int outer1 = start + 2 * ((i + 1) % count), inner1 = outer1 + 1;
                vh.AddTriangle(outer0, outer1, inner1);
                vh.AddTriangle(outer0, inner1, inner0);
            }
        }
    }
}
