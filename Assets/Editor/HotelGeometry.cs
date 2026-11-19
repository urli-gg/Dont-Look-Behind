using System.Collections.Generic;
using UnityEngine;

internal sealed class HotelGeometry
{
    private readonly List<Vector3> vertices = new();
    private readonly List<Vector2> uv = new();
    private readonly Dictionary<string, List<int>> groups = new();

    public IEnumerable<string> Surfaces => groups.Keys;

    public Mesh ToMesh(string name)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.subMeshCount = groups.Count;
        int index = 0;
        foreach (var triangles in groups.Values)
            mesh.SetTriangles(triangles, index++);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void Face(string material, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 tiling)
    {
        if (!groups.TryGetValue(material, out var indices))
        {
            indices = new List<int>();
            groups.Add(material, indices);
        }

        int index = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
        uv.Add(Vector2.zero);
        uv.Add(new Vector2(0f, tiling.y));
        uv.Add(tiling);
        uv.Add(new Vector2(tiling.x, 0f));
        indices.Add(index);
        indices.Add(index + 1);
        indices.Add(index + 2);
        indices.Add(index);
        indices.Add(index + 2);
        indices.Add(index + 3);
    }

    public HotelGeometry Box(string material, float x, float y, float z, float width, float height, float depth, float tile = 1f)
    {
        float l = x - width / 2f, r = x + width / 2f;
        float b = y - height / 2f, t = y + height / 2f;
        float f = z - depth / 2f, k = z + depth / 2f;
        Vector3 a = new(l, b, f), c = new(r, b, f), e = new(l, t, f), g = new(r, t, f);
        Vector3 d = new(r, b, k), h = new(r, t, k), i = new(l, b, k), j = new(l, t, k);

        Face(material, a, e, g, c, new Vector2(width, height) * tile);
        Face(material, d, h, j, i, new Vector2(width, height) * tile);
        Face(material, i, j, e, a, new Vector2(depth, height) * tile);
        Face(material, c, g, h, d, new Vector2(depth, height) * tile);
        Face(material, e, j, h, g, new Vector2(width, depth) * tile);
        Face(material, i, a, c, d, new Vector2(width, depth) * tile);
        return this;
    }

    public HotelGeometry Lathe(string material, Vector3 origin, Vector2[] profile, int sides = 16,
        float start = 0f, float end = 360f, bool sealCuts = false)
    {
        for (int face = 0; face < sides; face++)
        {
            float a = Mathf.Lerp(start, end, (float)face / sides) * Mathf.Deg2Rad;
            float b = Mathf.Lerp(start, end, (face + 1f) / sides) * Mathf.Deg2Rad;
            for (int row = 0; row < profile.Length - 1; row++)
                Face(material, Point(origin, profile[row], a), Point(origin, profile[row + 1], a),
                    Point(origin, profile[row + 1], b), Point(origin, profile[row], b), new Vector2(0.16f, 0.20f));
        }

        if (sealCuts)
        {
            float a = start * Mathf.Deg2Rad, b = end * Mathf.Deg2Rad;
            for (int row = 0; row < profile.Length - 1; row++)
            {
                Vector2 low = profile[row], high = profile[row + 1];
                Vector2 lowInside = new(Mathf.Max(0f, low.x - 0.018f), low.y);
                Vector2 highInside = new(Mathf.Max(0f, high.x - 0.018f), high.y);
                Face(material, Point(origin, low, a), Point(origin, lowInside, a),
                    Point(origin, highInside, a), Point(origin, high, a), Vector2.one);
                Face(material, Point(origin, low, b), Point(origin, high, b),
                    Point(origin, highInside, b), Point(origin, lowInside, b), Vector2.one);
            }
        }
        return this;
    }

    private static Vector3 Point(Vector3 origin, Vector2 profile, float angle) =>
        origin + new Vector3(Mathf.Cos(angle) * profile.x, profile.y, Mathf.Sin(angle) * profile.x);
}