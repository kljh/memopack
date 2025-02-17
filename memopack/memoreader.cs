using System.Text;


namespace MemoPack
{

// Insigned integers are  error prone in code because it's far easier for an expression to underflow than it is to overflow.
using Ptr     = Int64;   // used in the code for calculation
using MemoPtr = UInt32;  // used in the file

public class MemoReader : IDisposable
{
    private BinaryReader br;
    private Ptr top;  // position for next item to write
    private MemoReader? any_offset_reader;

    public MemoReader(BinaryReader br)
    {
        this.br = br;
    }

    public MemoReader AtOffset(Ptr offset)
    {
        // allocate only once
        if (any_offset_reader == null)
            any_offset_reader = new MemoReader(br);

        any_offset_reader.top = offset;
        return any_offset_reader;
    }

    public void Dispose()
    { }

    private void Align(int elementSize, int prefixSize = 0)
    {
        Ptr pos = top + prefixSize;
        if (pos % elementSize != 0)
            pos = pos - pos % elementSize + elementSize;
        top = pos - prefixSize;

        br.BaseStream.Seek(top, SeekOrigin.Begin);
    }

#region Raw Value

    /*
    public void Write(double[] vec)
    {
        Align(sizeof(double), sizeof(MemoPtr));

        int n = vec.Length;
        bw.Write((MemoPtr)n);
        foreach (var val in vec)
            bw.Write(val);
        top += sizeof(MemoPtr) + n * sizeof(double);
    }

    public void Write(int[] vec)
    {
        Align(sizeof(int), sizeof(MemoPtr));

        int n = vec.Length;
        bw.Write((MemoPtr)n);
        foreach (var val in vec)
            bw.Write(val);
        top += sizeof(MemoPtr) + n * sizeof(int);
    }

    */

    public string ReadStringAt(MemoPtr ptr)
    {
        return AtOffset(ptr).ReadString();
    }

    public string ReadString()
    {
        Align(sizeof(MemoPtr));

        int n = (int)br.ReadUInt32();
        var bytes = br.ReadBytes(n);
        var res = Encoding.UTF8.GetString(bytes);

        top += sizeof(MemoPtr) + n + 1;
        return res;
    }

    public double ReadDouble()
    {
        Align(sizeof(double));
        var res = br.ReadDouble();
        top += sizeof(double);
        return res;
    }

    public float ReadSingle()
    {
        Align(sizeof(float));
        var res = br.ReadSingle();
        top += sizeof(float);
        return res;
    }

    public long ReadInt64()
    {
        Align(sizeof(long));
        var res = br.ReadInt64();
        top += sizeof(long);
        return res;
    }

    public int ReadInt32()
    {
        Align(sizeof(int));
        var res = br.ReadInt32();
        top += sizeof(int);
        return res;
    }

    public short ReadInt16()
    {
        Align(sizeof(short));
        var res = br.ReadInt16();
        top += sizeof(short);
        return res;
    }

    public byte ReadByte()
    {
        Align(sizeof(byte));
        var res = br.ReadByte();
        top += sizeof(byte);
        return res;
    }

    public uint ReadUInt32()
    {
        Align(sizeof(uint));
        var res = br.ReadUInt32();
        top += sizeof(uint);
        return res;
    }


    public double[] ReadDoubleArray(uint n)
    {
        Align(sizeof(double));

        double[] res = new double[n];
        for (var i=0; i<n; i++)
            res[i] = br.ReadDouble();

        top += n * sizeof(double);
        return res;
    }

    public long[] ReadInt64Array(uint n)
    {
        Align(sizeof(long));

        long[] res = new long[n];
        for (var i=0; i<n; i++)
            res[i] = br.ReadInt64();

        top += n * sizeof(long);
        return res;
    }

    public int[] ReadInt32Array(uint n)
    {
        Align(sizeof(int));

        int[] res = new int[n];
        for (var i=0; i<n; i++)
            res[i] = br.ReadInt32();

        top += n * sizeof(int);
        return res;
    }

    public uint[] ReadUInt32Array(uint n)
    {
        Align(sizeof(uint));

        uint[] res = new uint[n];
        for (var i=0; i<n; i++)
            res[i] = br.ReadUInt32();

        top += n * sizeof(uint);
        return res;
    }

    public bool[] ReadBoolArray(uint nbBools)
    {
        Align(sizeof(byte));

        int nbBytes = (int)( nbBools / 8 + ((nbBools % 8) == 0 ? 0 : 1) );

        byte[] bytes = br.ReadBytes(nbBytes);
        bool[] bools = MemoTools.ByteToBoolArray(bytes, nbBools);

        top += nbBytes;
        return bools;
    }

#endregion

#region Decorated value

    public object? ReadTaggedAt(MemoPtr ptr)
    {
        if (ptr == 0)
            return null;

        var res = ReadInlineTagged(ptr);
        if (res != null)
            return res;  // value was inlined

        return AtOffset(ptr).ReadTagged();
    }

    public object? ReadInlineTagged(MemoPtr ptr)
    {
        // Tagged value inlined in Ptr are marked with 0xF higher quartet.");
        if ((ptr & 0xF0000000) == 0)
            return null;

        uint typ = (ptr >> 24) & 0xF;
        uint val = ptr & 0xFFFFFF;

        var intFrom24bit = (uint ui) => {
            if ((ui & 0x800000) == 0) {
                // positive int (prepend with 0x0)
                return (int)( 0x00FFFFFF & ui );
            } else {
                // negative int (prepend with 0xF)
                return (int)( 0xFF000000 | ui );
            }
        };

        switch (typ) {
            case 0x8:
            {
                if (val == 0)
                    return string.Empty;
                else
                    return new string((char)val, 1);
            }

            case 0x9:
                return (double)intFrom24bit(val);
            case 0xA:
                return (float)intFrom24bit(val);
            case 0xB:
                return (long)intFrom24bit(val);
            case 0xC:
                return (int)intFrom24bit(val);
            case 0xD:
                return (ulong)val;
            case 0xE:
                return (uint)val;
        }

        return null;
    }

    public object? ReadTagged()
    {
        byte typ = ReadByte();
        while (typ == '\0')
            typ = ReadByte();

        if (typ == MemoPack.TYPE_TXT_PTR)
            return ReadStringAt(ReadUInt32());

        if (typ == MemoPack.TYPE_TXT)
            return ReadString();
        if (typ == MemoPack.TYPE_F64)
            return ReadDouble();
        if (typ == MemoPack.TYPE_I64)
            return ReadInt64();
        if (typ == MemoPack.TYPE_I32)
            return ReadInt32();
        if (typ == MemoPack.TYPE_TRUE)
            return true;
        if (typ == MemoPack.TYPE_FALSE)
            return false;
        if (typ == MemoPack.TYPE_NULL)
            return null;

        if (typ == MemoPack.TYPE_DICT || typ == MemoPack.TYPE_SORTED_DICT)
            return ReadTaggedDict();
        if (typ == MemoPack.TYPE_ARRAY)
            return ReadTaggedArray();

        throw new Exception($"Unhandled type {typ}  / '{(char)typ}'");
    }

    public object ReadTaggedDict()
    {
        byte keyType = ReadByte();
        byte valType = ReadByte();
        uint n = ReadUInt32();

        if (keyType != MemoPack.TYPE_TXT)
            throw new Exception($"Unhandled key type {keyType}  / '{(char)keyType}'");
        if (valType != MemoPack.TYPE_UNTYPED)
            throw new Exception($"Unhandled value type {valType}  / '{(char)valType}'");

        string[]  keys = ReadUInt32Array(n).Select(offset => ReadStringAt(offset)).ToArray();
        object?[] vals = ReadUInt32Array(n).Select(offset => ReadTaggedAt(offset)).ToArray();

        return keys.Zip(vals, (k, v) => new { k, v })
              .ToDictionary(kv => kv.k, kv => kv.v);
    }

    public object ReadTaggedArray()
    {
        byte typ = ReadByte();
        MemoPtr n = ReadUInt32();

        if (typ == MemoPack.TYPE_UNTYPED)
            return ReadUInt32Array(n).Select(offset => ReadTaggedAt(offset)).ToArray();
        if (typ == MemoPack.TYPE_TXT_PTR)
            return ReadUInt32Array(n).Select(offset => ReadStringAt(offset)).ToArray();
        if (typ == MemoPack.TYPE_F64)
            return ReadDoubleArray(n);
        if (typ == MemoPack.TYPE_I64)
            return ReadInt64Array(n);
        if (typ == MemoPack.TYPE_I32)
            return ReadInt32Array(n);
        if (typ == MemoPack.TYPE_BOOL)
            return ReadBoolArray(n);

        throw new Exception($"Unhandled array type {typ} / '{(char)typ}'");
    }


#endregion

}

}