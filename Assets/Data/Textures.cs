using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class RadixBitmap
{
    public string Name { get; internal set; }
    public int Width { get; internal set; }
    public int Height { get; internal set; }
    public byte[,] Pixels { get; internal set; }

    private Texture2D texture;
    public Texture2D Texture
    {
        get
        {
            if (texture)
                return texture;

            texture = new Texture2D(Width, Height, TextureFormat.ARGB32, false);
            texture.filterMode = FilterMode.Point;
            Color32[] colors = new Color32[Width * Height];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    colors[y * Width + x] = TextureManager.Palettes[0][Pixels[x, y]];
            texture.SetPixels32(colors);
            texture.Apply(false);
            return texture;
        }
    }
}

public class TextureManager
{
    private static bool TexturesLoaded = false;
    private static List<RadixBitmap> Textures = new List<RadixBitmap>();
    public static readonly List<Color32[]> Palettes = new List<Color32[]>();

    private static Color32[] LoadPalette(string filename)
    {
        Color32[] pal = new Color32[256];

        using (MemoryStream ms = ResourceManager.OpenRead(filename))
        using (BinaryReader br = new BinaryReader(ms))
        {
            for (int i = 0; i < 256; i++)
            {
                uint cr = br.ReadByte();
                uint cg = br.ReadByte();
                uint cb = br.ReadByte();
                cr *= 4; if (cr > 255) cr = 255;
                cg *= 4; if (cg > 255) cg = 255;
                cb *= 4; if (cb > 255) cb = 255;
                pal[i] = new Color32((byte)cr, (byte)cg, (byte)cb, 255);
            }
        }

        return pal;
    }

    public static void Load()
    {
        if (TexturesLoaded)
            return;

        TexturesLoaded = true;

        // load palette1. we only have palette1 for now.
        Palettes.Add(LoadPalette("Palette[1]"));

        Resource.Entry ent = ResourceManager.FindEntry("WallBitmaps");
        using (MemoryStream ms = ResourceManager.OpenRead(ent))
        using (BinaryReader br = new BinaryReader(ms))
        {
            uint count = br.ReadUInt16();
            uint roffset = br.ReadUInt32();
            // now, Radix is so stupid that "roffset" is actually an absolute offset in radix.dat
            // so we need to know the offset of the original entry
            uint foffs = roffset - ent.Offset;
            for (uint i = 0; i < count; i++)
            {
                ms.Position = foffs + i * 40;
                
                string imgnb = Utils.GetNullTermString(br.ReadBytes(32));
                uint r_offset = br.ReadUInt32();
                uint r_width = br.ReadUInt16();
                uint r_height = br.ReadUInt16();

                //Debug.LogFormat("found texture {0} ({1}x{2})", imgnb, r_width, r_height);

                // r_offset is an absolute offset in radix.dat as well
                ms.Position = r_offset - ent.Offset;
                // read in the pixels
                RadixBitmap bmp = new RadixBitmap();
                bmp.Width = (int)r_width;
                bmp.Height = (int)r_height;
                bmp.Name = imgnb;
                bmp.Pixels = new byte[bmp.Width, bmp.Height];

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        bmp.Pixels[x, y] = br.ReadByte(); // palette index here
                    }
                }

                Textures.Add(bmp);
            }
        }
    }

    public static RadixBitmap GetTextureById(int num)
    {
        Load();

        if (num < 0 || num >= Textures.Count)
            return null;
        return Textures[num];
    }

    public static RadixBitmap GetTextureByName(string name)
    {
        Load();

        for (int i = 0; i < Textures.Count; i++)
        {
            if (Textures[i].Name.ToLowerInvariant() == name.ToLowerInvariant())
                return Textures[i];
        }

        return null;
    }
}
