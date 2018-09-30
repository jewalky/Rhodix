using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

public class ResourceException : Exception
{
    public ResourceException() : base() { }
    public ResourceException(string msg) : base(msg) { }
}

public class Resource : IDisposable
{
    public class Entry
    {
        public string Name;
        public uint Offset;
        public uint Size;
        public ushort Unk0;
        public uint Unk1;
    }

    internal FileStream ResStream = null;
    internal List<Entry> Entries = new List<Entry>();

    public Resource(string realFilename)
    {
        ResStream = File.OpenRead(realFilename);
        BinaryReader br = new BinaryReader(ResStream);

        string radix_header = Utils.GetNullTermString(br.ReadBytes(11));
        if (radix_header != "NSRes:Radix")
            throw new ResourceException("Invalid file header");

        ushort radix_unk0 = br.ReadUInt16();
        uint radix_unk1 = br.ReadUInt32();
        uint radix_countfiles = br.ReadUInt32();
        uint radix_fatoffset = br.ReadUInt32();

        ResStream.Position = radix_fatoffset;
        for (uint i = 0; i < radix_countfiles; i++)
        {
            Entry ent = new Entry();
            ent.Name = Utils.GetNullTermString(br.ReadBytes(32));
            ent.Offset = br.ReadUInt32();
            ent.Size = br.ReadUInt32();
            ent.Unk0 = br.ReadUInt16();
            ent.Unk1 = br.ReadUInt32();
            Entries.Add(ent);
        }
    }

    public void Dispose()
    {
        if (ResStream != null)
            ResStream.Dispose();
        ResStream = null;
    }
}

public class ResourceManager
{
    private static Resource RadixDat = null;

    public static void InitResources()
    {
        if (RadixDat != null)
            return;

        try
        {
            RadixDat = new Resource("radix.dat");
        }
        catch (Exception e)
        {
            RadixDat = null;
            throw e;
        }
    }

    public static Resource.Entry FindEntry(string filename)
    {
        InitResources();

        filename = filename.ToLower();
        for (int i = 0; i < RadixDat.Entries.Count; i++)
        {
            Resource.Entry ent = RadixDat.Entries[i];
            if (ent.Name.ToLower().Equals(filename))
                return ent;
        }

        return null;
    }

    public static MemoryStream OpenRead(Resource.Entry ent)
    {
        byte[] buf = new byte[ent.Size];

        RadixDat.ResStream.Position = ent.Offset;
        if (RadixDat.ResStream.Read(buf, 0, (int)ent.Size) != (int)ent.Size)
            return null;

        MemoryStream ms = new MemoryStream(buf);
        return ms;
    }

    public static MemoryStream OpenRead(string filename)
    {
        InitResources();

        filename = filename.ToLower();
        for (int i = 0; i < RadixDat.Entries.Count; i++)
        {
            Resource.Entry ent = RadixDat.Entries[i];
            if (ent.Name.ToLower().Equals(filename))
                return OpenRead(ent);
        }

        return null;
    }
}