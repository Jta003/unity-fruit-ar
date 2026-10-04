using UnityEngine;
using UnityEngine.UI;

// Lightweight UGUI corner brackets for the camera scan guide.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ScanFocusGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float inset = 3f;
        float length = Mathf.Min(rect.width, rect.height) * .2f;
        float left = rect.xMin + inset;
        float right = rect.xMax - inset;
        float bottom = rect.yMin + inset;
        float top = rect.yMax - inset;
        float width = Mathf.Clamp(Mathf.Min(rect.width, rect.height) * .018f, 4f, 7f);
        Color glow = new Color(0.35f, 1f, 0.62f, .22f);
        Color stroke = new Color(1f, 1f, 1f, .97f);

        AddCorner(vh, new Vector2(left, top), new Vector2(left + length, top), new Vector2(left, top - length), width, glow, stroke);
        AddCorner(vh, new Vector2(right, top), new Vector2(right - length, top), new Vector2(right, top - length), width, glow, stroke);
        AddCorner(vh, new Vector2(left, bottom), new Vector2(left + length, bottom), new Vector2(left, bottom + length), width, glow, stroke);
        AddCorner(vh, new Vector2(right, bottom), new Vector2(right - length, bottom), new Vector2(right, bottom + length), width, glow, stroke);
    }

    private void AddCorner(VertexHelper vh, Vector2 corner, Vector2 horizontalEnd, Vector2 verticalEnd, float width, Color glow, Color stroke)
    {
        AddLine(vh, corner, horizontalEnd, width * 2.2f, glow);
        AddLine(vh, corner, verticalEnd, width * 2.2f, glow);
        AddLine(vh, corner, horizontalEnd, width, stroke);
        AddLine(vh, corner, verticalEnd, width, stroke);
    }

    private void AddLine(VertexHelper vh, Vector2 start, Vector2 end, float width, Color tint)
    {
        Vector2 half = (end - start).normalized;
        half = new Vector2(-half.y, half.x) * (width * .5f);
        Color vertexColor = tint * color;
        int first = vh.currentVertCount;
        AddVertex(vh, start + half, vertexColor);
        AddVertex(vh, end + half, vertexColor);
        AddVertex(vh, end - half, vertexColor);
        AddVertex(vh, start - half, vertexColor);
        vh.AddTriangle(first, first + 1, first + 2);
        vh.AddTriangle(first, first + 2, first + 3);
    }

    private static void AddVertex(VertexHelper vh, Vector2 position, Color tint)
    {
        var vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = tint;
        vh.AddVert(vertex);
    }
}
