using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ClipperLib;

using TriangleNet.Geometry;
using TriangleNet.Data;

public class TriangulatorHelper
{
    // returns list of list of vertices, each one being a triangle
    public static List<List<Vector2>> MakeTriangles(List<List<Vector2>> loops)
    {
        //Debug.LogFormat("loop1 = {0}, loopL = {1}", loops[0].Count, loops[loops.Count - 1].Count);

        // first off, the loops should be sorted by area, so we can just go from the start expecting the first polygon to be the largest one and last the smallest.
        List<List<Vector2>> polygons = new List<List<Vector2>>();
        Dictionary<List<Vector2>, List<List<Vector2>>> holes = new Dictionary<List<Vector2>, List<List<Vector2>>>();
        //for (int i = loops.Count - 1; i >= 0; i--)
        for (int i = 0; i < loops.Count; i++)
        {
            List<Vector2> loop = loops[i];
            // first we check if this loop intersects any polygon. if it does, add hole to dictionary. if it doesn't, just add polygon.
            bool anyHole = false;

            for (int j = 0; j < polygons.Count; j++)
            {
                if (CheckIntersection(polygons[j], loop))
                {
                    //Debug.LogFormat("adding hole (len = {0}) to polygon (len = {1})", loop.Count, polygons[j].Count);
                    //Debug.LogFormat("polygon {0} intersects loop {1}", j, i);
                    if (!holes.ContainsKey(polygons[j]))
                        holes[polygons[j]] = new List<List<Vector2>>();
                    holes[polygons[j]].Add(((IEnumerable<Vector2>)loop).Reverse().ToList());
                    anyHole = true;
                    break;
                }
            }

            if (!anyHole)
            {
                //Debug.LogFormat("adding polygon (len = {0})", loop.Count);
                polygons.Add(loop);
            }
            //break;
        }

        List<List<Vector2>> output = new List<List<Vector2>>();
        
        for (int i = 0; i < polygons.Count; i++)
        {
            List<Vector2> polygon = polygons[i];

            if (polygon.Count < 3)
                continue;
            //Debug.LogFormat("polygon points = {0}", polygon.Count);

            InputGeometry input = new InputGeometry();
            for (int j = 0; j < polygon.Count; j++)
                input.AddPoint(polygon[j].x, polygon[j].y);

            // add segments
            for (int j = 0; j < polygon.Count; j++)
                input.AddSegment(j, (j + 1) % polygon.Count, 0);

            if (holes.ContainsKey(polygon))
            {
                for (int j = 0; j < holes[polygon].Count; j++)
                {
                    List<Vector2> hole = holes[polygon][j];

                    // add holes segments
                    int basePt = input.points.Count;
                    float cx = 0;
                    float cy = 0;
                    for (int k = 0; k < hole.Count; k++)
                    {
                        input.AddPoint(hole[k].x, hole[k].y);
                        cx += hole[k].x; cy += hole[k].y;
                    }
                    cx /= hole.Count;
                    cy /= hole.Count;
                    for (int k = 0; k < hole.Count; k++)
                        input.AddSegment(basePt + k, basePt + ((k + 1) % hole.Count), j + 1);
                    input.AddHole(cx, cy);
                }
            }

            if (input.points.Count < 3)
                continue;

            // 
            TriangleNet.Mesh mesh = new TriangleNet.Mesh();
            mesh.Triangulate(input);

            // put triangles to output
            for (int j = 0; j < mesh.Triangles.Count; j++)
            {
                Triangle mtri = mesh.Triangles.ElementAt(j);
                List<Vector2> triangle = new List<Vector2>();
                for (int k = 0; k < 3; k++)
                    triangle.Add(new Vector2((float)mtri.vertices[k].x, (float)mtri.vertices[k].y));
                output.Add(triangle);
            }
        }

        return output;
    }

    public static int CheckIntersectionWithAny(List<Vector2> loop, List<List<Vector2>> loops)
    {
        int rv = 0;
        for (int i = 0; i < loops.Count; i++)
            if (CheckIntersection(loop, loops[i])) rv++;
        return rv;
    }

    public static bool PointInPolygon(List<Vector2> poly, Vector2 point)
    {
        var coef = poly.Skip(1).Select((p, i) =>
                                        (point.y - poly[i].y) * (p.x - poly[i].x)
                                      - (point.x - poly[i].x) * (p.y - poly[i].y))
                                .ToList();

        if (coef.Any(p => p == 0))
            return true;

        for (int i = 1; i < coef.Count(); i++)
        {
            if (coef[i] * coef[i - 1] < 0)
                return false;
        }
        return true;
    }

    public static bool CheckIntersection(List<Vector2> loop1, List<Vector2> loop2)
    {
        List<List<IntPoint>> solution = new List<List<IntPoint>>();

        List<List<IntPoint>> subj = new List<List<IntPoint>>();
        subj.Add(new List<IntPoint>());
        List<List<IntPoint>> clip = new List<List<IntPoint>>();
        clip.Add(new List<IntPoint>());

        for (int i = 0; i < loop1.Count; i++)
            subj[0].Add(new IntPoint(loop1[i].x, loop1[i].y));

        for (int i = 0; i < loop2.Count; i++)
            clip[0].Add(new IntPoint(loop2[i].x, loop2[i].y));

        Clipper c = new Clipper();
        c.AddPaths(subj, PolyType.ptSubject, true);
        c.AddPaths(clip, PolyType.ptClip, true);

        return c.Execute(ClipType.ctIntersection, solution) && solution.Count > 0;
        /*foreach (Vector2 p in loop2)
            if (PointInPolygon(loop1, p))
                return true;
        return false;*/
    }

    private static float GetLineLoopLength(List<Vector2> loop)
    {
        float len = 0;
        for (int i = 1; i < loop.Count; i++)
        {
            len += new Vector2(loop[i].x - loop[i - 1].x,
                               loop[i].y - loop[i - 1].y).magnitude;
        }

        return len;
    }

    private static float GetLineLoopArea(List<Vector2> loop)
    {
        List<Vector2> points = new List<Vector2>();
        points.AddRange(loop);
        points.Add(loop[0]);
        var area = Mathf.Abs(points.Take(points.Count - 1)
           .Select((p, i) => (points[i + 1].x - p.x) * (points[i + 1].y + p.y))
           .Sum() / 2);

        return area;
    }

    // has parts of Level
    public static List<List<Vector2>> GetLineLoops(LevelSector sec)
    {
        List<List<Vector2>> output = new List<List<Vector2>>();

        HashSet<LevelWall> checkedwalls = new HashSet<LevelWall>();
        while (true)
        {
            List<Vector2> poly = new List<Vector2>();
            LevelWall firstwall = null;
            for (int j = 0; j < sec.Walls.Count; j++)
            {
                if (checkedwalls.Contains(sec.Walls[j]))
                    continue;
                firstwall = sec.Walls[j];
                break;
            }

            if (firstwall == null)
                break;

            // take next line loop (polygon)

            LevelWall curwall = firstwall;
            do
            {
                checkedwalls.Add(curwall);

                LevelWall nextwall = null;
                for (int j = 0; j < sec.Walls.Count; j++)
                {
                    LevelWall checkwall = sec.Walls[j];
                    if (checkwall.GetV1(sec) == curwall.GetV2(sec))
                    {
                        nextwall = checkwall;
                        break;
                    }
                }

                poly.Add(new Vector2(curwall.GetV1(sec).X, curwall.GetV1(sec).Y));
                curwall = nextwall;
            }
            while (curwall != firstwall && curwall != null);
            output.Add(poly);
        }

        if (output[output.Count - 1].Count <= 0)
            output.RemoveAt(output.Count - 1);

        // sort loops
        output = output.OrderBy(o => GetLineLoopArea(o)).Reverse().ToList();
        
        for (int i = 0; i < output.Count; i++)
        {
            //Debug.LogFormat("loop {0}, lines = {1}, len = {2}, area = {3}", i, output[i].Count, GetLineLoopLength(output[i]), GetLineLoopArea(output[i]));
        }

        return output;
    }
}

public class Triangulator
{
    private List<Vector2> m_points = new List<Vector2>();

    public Triangulator(Vector2[] points)
    {
        m_points = new List<Vector2>(points);
    }

    public int[] Triangulate()
    {
        List<int> indices = new List<int>();

        int n = m_points.Count;
        if (n < 3)
            return indices.ToArray();

        int[] V = new int[n];
        if (Area() > 0)
        {
            for (int v = 0; v < n; v++)
                V[v] = v;
        }
        else {
            for (int v = 0; v < n; v++)
                V[v] = (n - 1) - v;
        }

        int nv = n;
        int count = 2 * nv;
        for (int m = 0, v = nv - 1; nv > 2;)
        {
            if ((count--) <= 0)
                return indices.ToArray();

            int u = v;
            if (nv <= u)
                u = 0;
            v = u + 1;
            if (nv <= v)
                v = 0;
            int w = v + 1;
            if (nv <= w)
                w = 0;

            if (Snip(u, v, w, nv, V))
            {
                int a, b, c, s, t;
                a = V[u];
                b = V[v];
                c = V[w];
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
                m++;
                for (s = v, t = v + 1; t < nv; s++, t++)
                    V[s] = V[t];
                nv--;
                count = 2 * nv;
            }
        }

        indices.Reverse();
        return indices.ToArray();
    }

    private float Area()
    {
        int n = m_points.Count;
        float A = 0.0f;
        for (int p = n - 1, q = 0; q < n; p = q++)
        {
            Vector2 pval = m_points[p];
            Vector2 qval = m_points[q];
            A += pval.x * qval.y - qval.x * pval.y;
        }
        return (A * 0.5f);
    }

    private bool Snip(int u, int v, int w, int n, int[] V)
    {
        int p;
        Vector2 A = m_points[V[u]];
        Vector2 B = m_points[V[v]];
        Vector2 C = m_points[V[w]];
        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
            return false;
        for (p = 0; p < n; p++)
        {
            if ((p == u) || (p == v) || (p == w))
                continue;
            Vector2 P = m_points[V[p]];
            if (InsideTriangle(A, B, C, P))
                return false;
        }
        return true;
    }

    private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
        float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
        float cCROSSap, bCROSScp, aCROSSbp;

        ax = C.x - B.x; ay = C.y - B.y;
        bx = A.x - C.x; by = A.y - C.y;
        cx = B.x - A.x; cy = B.y - A.y;
        apx = P.x - A.x; apy = P.y - A.y;
        bpx = P.x - B.x; bpy = P.y - B.y;
        cpx = P.x - C.x; cpy = P.y - C.y;

        aCROSSbp = ax * bpy - ay * bpx;
        cCROSSap = cx * apy - cy * apx;
        bCROSScp = bx * cpy - by * cpx;

        return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
    }
}