using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelView : MonoBehaviour
{
    private static LevelView _Instance = null;
    public static LevelView Instance
    {
        get
        {
            if (_Instance == null) _Instance = GameObject.FindObjectOfType<LevelView>();
            return _Instance;
        }
    }

    public Level CurrentLevel = null;

    private MeshRenderer Renderer = null;
    private MeshFilter Filter = null;

    public Texture TexPreview;

    // Use this for initialization
    private int drawAfter =- 1;
    public int DrawAfter
    {
        set
        {
            if (drawAfter != value)
            {
                drawAfter = value;
                SetMesh();
            }
        }
        get
        {
            return drawAfter;
        }
    }

    void Start ()
    {
        TexPreview = TextureManager.GetTextureByName("MetalAlienShape_#1#3").Texture;

        // rotate
        transform.Rotate(new Vector3(90, 0, 0));
        transform.localScale = new Vector3(1, -1, -1);

        CurrentLevel = new Level("WorldData[1][1]");

        Renderer = gameObject.AddComponent<MeshRenderer>();
        Filter = gameObject.AddComponent<MeshFilter>();

        SetMesh();
    }

    void SetMesh()
    {
        if (Filter.mesh)
            Destroy(Filter.mesh);

        List<Material> materials = new List<Material>();
        List<MeshTopology> topos = new List<MeshTopology>();
        materials.Add(new Material(MainCamera.MainShader)); // this is for outlines
        topos.Add(MeshTopology.Lines);
        //Renderer.materials = new Material[] { new Material(MainCamera.MainShader), new Material(MainCamera.MainShader) };

        Utils.MeshBuilder mb = new Utils.MeshBuilder();
        for (int i = 0; i < CurrentLevel.Sectors.Count; i++)
        {
            LevelSector sec = CurrentLevel.Sectors[i];

            if (drawAfter >= 0 && i < drawAfter)
                continue;

            int planemesh = mb.CurrentMesh+1;
            mb.CurrentMesh = 0;
            //if (sec.Walls.Count > 1) Debug.LogFormat("sector {0} has walls", i);
            for (int j = 0; j < sec.Walls.Count; j++)
            {
                LevelWall wall = sec.Walls[j];

                //if (j != 0) continue;

                if (wall.V1.X < 0 || wall.V1.Y < 0 ||
                    wall.V2.X < 0 || wall.V2.Y < 0) continue;

                Color color = new Color(1, 0, 0, 1);
                if (wall.Back != null)
                {
                    color.r = 0;
                    color.g = 1f;
                }

                //if (i == 0) color.g = color.b = 0;
                //else if (i == 10) color.g = color.r = 0;

                // sector 0 = 0
                // sector 10 = +8 (-8) floor height

                float TopFZ1 = wall.Front.Sector.Floor.ZatPoint(wall.V1.X, wall.V1.Y);
                if (wall.Back != null)
                    TopFZ1 = Mathf.Max(TopFZ1, wall.Back.Sector.Floor.ZatPoint(wall.V1.X, wall.V1.Y)); // top point at v1
                float BottomFZ1 = wall.Front.Sector.Floor.ZatPoint(wall.V1.X, wall.V1.Y);
                if (wall.Back != null)
                    BottomFZ1 = Mathf.Min(BottomFZ1, wall.Back.Sector.Floor.ZatPoint(wall.V1.X, wall.V1.Y));
                float TopFZ2 = wall.Front.Sector.Floor.ZatPoint(wall.V2.X, wall.V2.Y);
                if (wall.Back != null)
                    TopFZ2 = Mathf.Max(TopFZ2, wall.Back.Sector.Floor.ZatPoint(wall.V2.X, wall.V2.Y)); // top point at v2
                float BottomFZ2 = wall.Front.Sector.Floor.ZatPoint(wall.V2.X, wall.V2.Y);
                if (wall.Back != null)
                    BottomFZ2 = Mathf.Min(BottomFZ2, wall.Back.Sector.Floor.ZatPoint(wall.V2.X, wall.V2.Y));

                float TopCZ1 = wall.Front.Sector.Ceiling.ZatPoint(wall.V1.X, wall.V1.Y);
                if (wall.Back != null)
                    TopCZ1 = Mathf.Max(TopCZ1, wall.Back.Sector.Ceiling.ZatPoint(wall.V1.X, wall.V1.Y)); // top point at v1
                float BottomCZ1 = wall.Front.Sector.Ceiling.ZatPoint(wall.V1.X, wall.V1.Y);
                if (wall.Back != null)
                    BottomCZ1 = Mathf.Min(BottomCZ1, wall.Back.Sector.Ceiling.ZatPoint(wall.V1.X, wall.V1.Y));
                float TopCZ2 = wall.Front.Sector.Ceiling.ZatPoint(wall.V2.X, wall.V2.Y);
                if (wall.Back != null)
                    TopCZ2 = Mathf.Max(TopCZ2, wall.Back.Sector.Ceiling.ZatPoint(wall.V2.X, wall.V2.Y)); // top point at v2
                float BottomCZ2 = wall.Front.Sector.Ceiling.ZatPoint(wall.V2.X, wall.V2.Y);
                if (wall.Back != null)
                    BottomCZ2 = Mathf.Min(BottomCZ2, wall.Back.Sector.Ceiling.ZatPoint(wall.V2.X, wall.V2.Y));

                if (wall.Back != null)
                {
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V1.X, wall.V1.Y, TopFZ1);
                    mb.NextVertex();
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V2.X, wall.V2.Y, TopFZ2);
                    mb.NextVertex();

                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V1.X, wall.V1.Y, TopCZ1);
                    mb.NextVertex();
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V2.X, wall.V2.Y, TopCZ2);
                    mb.NextVertex();

                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V1.X, wall.V1.Y, BottomFZ1);
                    mb.NextVertex();
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V2.X, wall.V2.Y, BottomFZ2);
                    mb.NextVertex();

                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V1.X, wall.V1.Y, BottomCZ1);
                    mb.NextVertex();
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V2.X, wall.V2.Y, BottomCZ2);
                    mb.NextVertex();
                }
                else
                {
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V1.X, wall.V1.Y, TopFZ1);
                    mb.NextVertex();
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V2.X, wall.V2.Y, TopFZ2);
                    mb.NextVertex();

                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V1.X, wall.V1.Y, TopCZ1);
                    mb.NextVertex();
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V2.X, wall.V2.Y, TopCZ2);
                    mb.NextVertex();
                }

                // vertical lines
                if (wall.Back != null)
                {
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V1.X, wall.V1.Y, TopFZ1);
                    mb.NextVertex();
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V1.X, wall.V1.Y, BottomFZ1);
                    mb.NextVertex();

                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V2.X, wall.V2.Y, TopFZ2);
                    mb.NextVertex();
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V2.X, wall.V2.Y, BottomFZ2);
                    mb.NextVertex();

                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V1.X, wall.V1.Y, TopCZ1);
                    mb.NextVertex();
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V1.X, wall.V1.Y, BottomCZ1);
                    mb.NextVertex();

                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V2.X, wall.V2.Y, TopCZ2);
                    mb.NextVertex();
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V2.X, wall.V2.Y, BottomCZ2);
                    mb.NextVertex();
                }
                else
                {
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V1.X, wall.V1.Y, TopFZ1);
                    mb.NextVertex();
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V1.X, wall.V1.Y, BottomCZ1);
                    mb.NextVertex();

                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V2.X, wall.V2.Y, TopFZ2);
                    mb.NextVertex();
                    mb.CurrentColor = color;
                    mb.CurrentPosition = new Vector3(wall.V2.X, wall.V2.Y, BottomCZ2);
                    mb.NextVertex();
                }
            }

            // draw floor
            Material texfloor = new Material(MainCamera.MainShader);
            RadixBitmap bitmapfloor = TextureManager.GetTextureById(sec.Floor.Texture);
            Vector2 texmul = new Vector2(bitmapfloor.Width, bitmapfloor.Height);
            if (bitmapfloor != null)
            {
                texfloor.mainTexture = bitmapfloor.Texture;
            }

            Material texceiling = new Material(MainCamera.MainShader);
            RadixBitmap bitmapceiling = TextureManager.GetTextureById(sec.Ceiling.Texture);
            Vector2 texmul2 = new Vector2(bitmapceiling.Width, bitmapceiling.Height);
            if (bitmapceiling != null)
            {
                texceiling.mainTexture = bitmapceiling.Texture;
            }

            materials.Add(texfloor);
            materials.Add(texceiling);
            topos.Add(MeshTopology.Triangles);
            topos.Add(MeshTopology.Triangles);

            for (int p = 0; p < 2; p++)
            {
                mb.CurrentMesh = planemesh+p;
                LevelSector.Plane plane = p == 0 ? sec.Floor : sec.Ceiling;
                for (int j = 0; j < sec.Triangles.Count; j++)
                {
                    LevelSector.Triangle tri = sec.Triangles[j];

                    Color color = new Color(0.5f, 0.5f, 0.5f, 1);

                    if (tri.Points.Count != 3)
                    {
                        // now, we know that this mesh is NOT actual triangle list, it consists of points around the sector.
                        Vector2 center = tri.Center;

                        for (int k = 0; k < tri.Points.Count; k++)
                        {
                            // add center point
                            mb.CurrentColor = color;
                            mb.CurrentPosition = new Vector3(center.x, center.y, plane.ZatPoint(center.x, center.y));
                            mb.CurrentUV1 = new Vector2(center.x / texmul.x, center.y / texmul.y);
                            mb.NextVertex();

                            // add current point
                            int cIndex = k % tri.Points.Count;
                            mb.CurrentColor = color;
                            mb.CurrentPosition = new Vector3(tri.Points[cIndex].x, tri.Points[cIndex].y, plane.ZatPoint(tri.Points[cIndex].x, tri.Points[cIndex].y));
                            mb.CurrentUV1 = new Vector2(tri.Points[cIndex].x / texmul.x, tri.Points[cIndex].y / texmul.y);
                            mb.NextVertex();

                            // add next point
                            int nIndex = (k + 1) % tri.Points.Count;
                            mb.CurrentColor = color;
                            mb.CurrentPosition = new Vector3(tri.Points[nIndex].x, tri.Points[nIndex].y, plane.ZatPoint(tri.Points[nIndex].x, tri.Points[nIndex].y));
                            mb.CurrentUV1 = new Vector2(tri.Points[nIndex].x / texmul.x, tri.Points[nIndex].y / texmul.y);
                            mb.NextVertex();
                        }
                    }
                    else // it's a legit triangle, don't overcomplicate
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            mb.CurrentColor = color;
                            mb.CurrentPosition = new Vector3(tri.Points[k].x, tri.Points[k].y, plane.ZatPoint(tri.Points[k].x, tri.Points[k].y));
                            mb.CurrentUV1 = new Vector2(tri.Points[k].x / texmul.x, tri.Points[k].y / texmul.y);
                            mb.NextVertex();
                        }
                    }
                }
            }

            if (drawAfter >= 0 && i >= drawAfter && sec.Walls.Count > 0)
            {
                Debug.LogFormat("stopped rendering at {0}", i);
                break;
            }
        }

        Filter.mesh = mb.ToMesh(topos.ToArray());
        Renderer.materials = materials.ToArray();
    }

    // Update is called once per frame
    double lastMs = 0;
	void Update ()
    {
        /*
        double t = Time.realtimeSinceStartup;
        if ((int)t != (int)lastMs)
        {
            lastMs = t;
            DrawAfter = 56;
        }*/
	}
}
