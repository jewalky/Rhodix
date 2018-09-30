using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

public class LevelVertex
{
    public List<LevelWall> Walls = new List<LevelWall>();
    public float X;
    public float Y;
}

[Flags]
public enum LevelWallFlags
{
    CompleteWall = 0x01,
    FloorWall = 0x02,
    CeilingWall = 0x04, // 6 = classic doom doublesided wall, 1 = singlesided wall
    Found = 0x0800, // set when wall got found once
}

public class LevelWallSide
{
    public LevelWall Wall = null;
    public LevelVertex V1 = null;
    public LevelVertex V2 = null;
    public LevelSector Sector = null;
}

public class LevelWall
{
    public LevelWallFlags Flags = 0;
    public LevelVertex V1 = null;
    public LevelVertex V2 = null;
    public LevelWallSide Front = null;
    public LevelWallSide Back = null;

    public LevelVertex GetV1(LevelSector s)
    {
        return (Front == null || s == Front.Sector) ? V1 : V2;
    }

    public LevelVertex GetV2(LevelSector s)
    {
        return (Front == null || s == Front.Sector) ? V2 : V1;
    }

    public LevelWallSide GetSide(LevelSector s)
    {
        if (s == Front.Sector) return Front;
        else if (Back != null && s == Back.Sector) return Back;
        return null;
    }
}

public class LevelSector
{
    public class Triangle
    {
        public List<Vector2> Points = new List<Vector2>();

        public Vector2 Center
        {
            get
            {
                if (Points.Count <= 0)
                    return new Vector2(0, 0);

                float x = 0;
                float y = 0;
                for (int i = 0; i < Points.Count; i++)
                {
                    x += Points[i].x;
                    y += Points[i].y;
                }

                x /= Points.Count;
                y /= Points.Count;
                return new Vector2(x, y);
            }
        }
    }

    public class Plane
    {
        public int Texture;
        public float Z;
        public bool HasSlope;
        public int SlopeA; // 26
        public int SlopeB; // 2A
        public int SlopeC; // 2E
        public int SlopeD; // 32

        private int DMulScale3(int a, int b, int c, int d)
        {
            return (a * b + c * d) >> 3;
        }

        // this should return something based on slope later.
        public float ZatPoint(float x, float y)
        {
            if (!HasSlope)
                return Z;

            // ok. this is pure fucking magic.
            return (-(SlopeA * (int)x) - SlopeC * (int)y - SlopeD) / SlopeB;
        }
    }

    public string Tag;
    public List<LevelWall> Walls = new List<LevelWall>();

    public Plane Floor = new Plane();
    public Plane Ceiling = new Plane();

    public List<Triangle> Triangles = new List<Triangle>();
}

public class LevelException : Exception
{
    public LevelException() : base() { }
    public LevelException(string msg) : base(msg) { }
}

public class Level
{
    public List<LevelSector> Sectors = new List<LevelSector>();
    public List<LevelWall> Walls = new List<LevelWall>();
    public List<LevelVertex> Vertices = new List<LevelVertex>();

    private LevelVertex GetVertex(float x, float y, LevelWall wall)
    {
        for (int i = 0; i < Vertices.Count; i++)
        {
            if (Vertices[i].X == x &&
                Vertices[i].Y == y)
            {
                Vertices[i].Walls.Add(wall);
                return Vertices[i];
            }
        }

        LevelVertex nv = new LevelVertex();
        nv.X = x;
        nv.Y = y;
        nv.Walls.Add(wall);
        Vertices.Add(nv);
        return nv;
    }

    private void ReadSlope(MemoryStream ms, BinaryReader br, long curPos, LevelSector.Plane plane)
    {
        ms.Position = curPos;
        plane.SlopeA = br.ReadInt32();
        plane.SlopeB = br.ReadInt32();
        plane.SlopeC = br.ReadInt32();
        plane.SlopeD = br.ReadInt32();
        plane.HasSlope = true;
    }

    public Level(string filename)
    {
        // 0..0x48 = header
        // 0x49.. = data
        using (MemoryStream ms = ResourceManager.OpenRead(filename))
        using (BinaryReader br = new BinaryReader(ms))
        {
            ms.Position = 0;
            uint level_magic = br.ReadUInt32();
            if (level_magic != 0xFFFFFEE7)
                throw new LevelException("Invalid level header");

            ms.Position = 0x1D;
            int level_numwalls = br.ReadInt32();
            int level_numsectors = br.ReadInt32();

            Debug.LogFormat("numwalls = {0}, numsectors = {1}, level = {2}", level_numwalls, level_numsectors, filename);

            Dictionary<int, int> sectorRemap = new Dictionary<int, int>();
            Dictionary<LevelSector, LevelSector> sectorRemapRef = new Dictionary<LevelSector, LevelSector>();

            ms.Position = 0x49;
            for (int i = 0; i < level_numsectors; i++)
            {
                // sector size = 0x8E
                long curPos = ms.Position;
                long nextPos = ms.Position + 0x8E;

                LevelSector sec = new LevelSector();

                ms.Position = curPos + 2;
                sec.Tag = Utils.GetNullTermString(br.ReadBytes(0x1A));
                // for now ignore everything else
                int sec_texfloor = br.ReadInt16();
                int sec_texceiling = br.ReadInt16();
                sec.Floor.Z = br.ReadInt16();
                sec.Ceiling.Z = br.ReadInt16();
                sec.Floor.Texture = sec_texfloor;
                sec.Ceiling.Texture = sec_texceiling;

                // 00: ?? ??
                // 02: name (0x1A long), also known as tag (used in scripting?)
                // 1C: ## ## (floor texture)
                // 1E: ## ## (ceiling texture)
                // // what the fuck is so important in sector structure that it takes 0x6E bytes to store?
                ms.Position = curPos + 0x25;
                byte sec_flags = br.ReadByte();
                string flagsstr = "";
                for (int j = 0; j < 8; j++)
                {
                    if ((sec_flags & (byte)(1<<j)) != 0)
                    {
                        if (flagsstr != "")
                            flagsstr += ", ";
                        if (((1 << j) & 4) != 0)
                            flagsstr += string.Format("floorslope");
                        else if (((1 << j) & 8) != 0)
                            flagsstr += string.Format("ceilingslope");
                        else flagsstr += string.Format("unknown:{0:X2}", (byte)(1 << j));
                    }
                }
                if (flagsstr != "") flagsstr = " (" + flagsstr + ")";
                Debug.LogFormat("sector {0}, flags {1}{2}", sec.Tag, sec_flags, flagsstr);

                if ((sec_flags & 4) != 0) // floor slope
                {
                    //
                    ReadSlope(ms, br, curPos+0x26, sec.Floor);
                }

                if ((sec_flags & 8) != 0) // ceiling slope
                {
                    //
                    ReadSlope(ms, br, curPos+0x36, sec.Ceiling);
                }

                // check if such sector already exists
                sectorRemap[i] = i;
                sectorRemapRef[sec] = sec;
                for (int j = 0; j < Sectors.Count; j++)
                {
                    LevelSector other = Sectors[j];
                    if (other.Floor.Z == sec.Floor.Z &&
                        other.Ceiling.Z == sec.Ceiling.Z)
                    {
                        sectorRemap[i] = j;
                        sectorRemapRef[sec] = other;
                        break;
                    }
                }

                Sectors.Add(sec);

                ms.Position = nextPos;
            }

            for (int i = 0; i < level_numwalls; i++)
            {
                // wall size = 0x56
                long curPos = ms.Position;
                long nextPos = ms.Position + 0x56;

                LevelWall wall = new LevelWall();

                ms.Position = curPos + 0x0A;

                wall.V1 = GetVertex(br.ReadInt32(), br.ReadInt32(), wall);
                wall.V2 = GetVertex(br.ReadInt32(), br.ReadInt32(), wall);

                ms.Position = curPos + 0x1A;
                short wall_ofront = br.ReadInt16();
                short wall_oback = br.ReadInt16();
                short wall_front = wall_ofront;
                short wall_back = wall_oback;

                if (wall_front < 0)
                {
                    wall.Front = null;
                }
                else
                {
                    wall.Front = new LevelWallSide();
                    wall.Front.Wall = wall;
                    wall.Front.V1 = wall.V1;
                    wall.Front.V2 = wall.V2;
                    wall.Front.Sector = Sectors[wall_front];
                    wall.Front.Sector.Walls.Add(wall);
                }

                if (wall_back < 0)
                {
                    wall.Back = null;
                }
                else
                {
                    wall.Back = new LevelWallSide();
                    wall.Back.Wall = wall;
                    wall.Back.V1 = wall.V2;
                    wall.Back.V2 = wall.V1;
                    wall.Back.Sector = Sectors[wall_back];
                    wall.Back.Sector.Walls.Add(wall);
                }

                ms.Position = curPos + 0x4E;
                int wall_texfront = br.ReadInt16(); // this is offset into wall textures, to be changed to a pointer later
                int wall_texback = br.ReadInt16();

                ms.Position = curPos + 0x48;
                wall.Flags = (LevelWallFlags)br.ReadUInt32();
                // for now ignore everything else
                Walls.Add(wall);

                //Debug.LogFormat("Wall  Front = {0},  Back = {1},  TextureFront = {2},  TextureBack = {3},  Flags = {4}\nV1 = {5},{6}  V2 = {7},{8}", wall_front, wall_back, wall_texfront, wall_texback, wall.Flags.ToString(), wall.V1.X, wall.V1.Y, wall.V2.X, wall.V2.Y);

                ms.Position = nextPos;
            }

            // handle unclosed sectors
            // if sector is not closed, we have to find a wall that references a similar sector (using sectorRemap) and remap.
            for (int i = 0; i < Sectors.Count; i++)
            {
                LevelSector sec = Sectors[i];
                if (sec.Walls.Count <= 0)
                    continue;

                bool isclosed = true;
                int closedloops = 0;
                HashSet<LevelWall> checkedwalls = new HashSet<LevelWall>();
                while (true)
                {
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

                        if (nextwall == null) // sector is not closed. try to find a remapped wall here
                        {
                            for (int j = 0; j < Walls.Count; j++)
                            {
                                LevelWall rwall = Walls[j];
                                if (checkedwalls.Contains(rwall) || sec.Walls.Contains(rwall))
                                    continue;
                                LevelSector remappedFront = rwall.Front != null ? sectorRemapRef[rwall.Front.Sector] : null;
                                LevelSector remappedBack = rwall.Back != null ? sectorRemapRef[rwall.Back.Sector] : null;
                                LevelSector remappedSelf = sectorRemapRef[sec];
                                if ((remappedFront == remappedSelf && rwall.V1 == curwall.GetV2(sec)) ||
                                    (remappedBack == remappedSelf && rwall.V2 == curwall.GetV2(sec)))
                                {
                                    // add this wall to sector walls
                                    if (remappedFront == remappedSelf)
                                    {
                                        rwall.Front.Sector.Walls.Remove(rwall);
                                        rwall.Front.Sector = sec;
                                        sec.Walls.Add(rwall);
                                    }
                                    else
                                    {
                                        rwall.Back.Sector.Walls.Remove(rwall);
                                        rwall.Back.Sector = sec;
                                        sec.Walls.Add(rwall);
                                    }
                                    nextwall = rwall;
                                }
                            }
                        }

                        curwall = nextwall;
                    }
                    while (curwall != firstwall && curwall != null);

                    if (curwall == null)
                    {
                        //Debug.LogFormat("sector {0} is not closed", i);
                        isclosed = false;
                    }
                    else closedloops++;
                }
                if (!isclosed)
                {
                    //Debug.LogFormat("but has {0} closed loops", closedloops);
                }
            }

            // based on lines and sectors, build GL nodes like structure
            for (int i = 0; i < Sectors.Count; i++)
            {
                LevelSector sec = Sectors[i];

                if (sec.Walls.Count <= 0)
                    continue;

                //if (i != 56)
                //    continue;

                //BayazitDecomposer
                //List<List<Vector2>> polygons = TriangulatorHelper.GetLineLoops(sec);
                try
                {
                    List<List<Vector2>> polygons = TriangulatorHelper.MakeTriangles(TriangulatorHelper.GetLineLoops(sec));

                    //Triangulation triangulation = Triangulation.Create(sec);

                    // these polygons should be convex
                    /*for (int j = 0; j < triangulation.Vertices.Count; j += 3)
                    {
                        LevelSector.Triangle tri = new LevelSector.Triangle();
                        tri.Points.Add(new Vector2(triangulation.Vertices[j].x, triangulation.Vertices[j].y));
                        tri.Points.Add(new Vector2(triangulation.Vertices[j+1].x, triangulation.Vertices[j+1].y));
                        tri.Points.Add(new Vector2(triangulation.Vertices[j+2].x, triangulation.Vertices[j+2].y));
                        sec.Triangles.Add(tri);
                    }*/
                    for (int j = 0; j < polygons.Count; j++)
                    {
                        LevelSector.Triangle tri = new LevelSector.Triangle();
                        tri.Points.AddRange(polygons[j]);
                        sec.Triangles.Add(tri);
                    }
                }
                catch (NotSupportedException e)
                {
                    /* ... */
                    sec.Triangles.Clear();
                }
            }
        }
    }
}