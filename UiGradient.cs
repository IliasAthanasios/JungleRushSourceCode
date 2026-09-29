using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class UIGradient : BaseMeshEffect
{
    public Color colorLeft = Color.red;
    public Color colorRight = Color.green;
    [Range(0, 1)] public float ratio = 1f; // Sync this with slider.normalizedValue

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        var vertexList = new System.Collections.Generic.List<UIVertex>();
        vh.GetUIVertexStream(vertexList);

        // The color at the right edge of the CURRENT fill
        Color currentEndColor = Color.Lerp(colorLeft, colorRight, ratio);

        for (int i = 0; i < vertexList.Count; i++)
        {
            UIVertex v = vertexList[i];
            // Vertices 0, 1, 5 are the left side; 2, 3, 4 are the right side
            bool isLeft = (i % 6 == 0 || i % 6 == 1 || i % 6 == 5);
            v.color = isLeft ? colorLeft : currentEndColor;
            vertexList[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(vertexList);
    }

    public void Refresh(float r)
    {
        ratio = r;
        if (graphic != null) graphic.SetVerticesDirty();
    }
}