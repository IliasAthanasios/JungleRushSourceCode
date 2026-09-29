using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class UIGradient : BaseMeshEffect
{
    public Color colorLeft = Color.red;
    public Color colorRight = Color.green;
    [Range(0, 1)] public float ratio = 1f; 
    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        var vertexList = new System.Collections.Generic.List<UIVertex>();
        vh.GetUIVertexStream(vertexList);

        Color currentEndColor = Color.Lerp(colorLeft, colorRight, ratio);

        for (int i = 0; i < vertexList.Count; i++)
        {
            UIVertex v = vertexList[i];
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
