using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace MemoPack
{

// Insigned integers are  error prone in code because it's far easier for an expression to underflow than it is to overflow.
using Ptr     = Int64;   // used in the code for calculation
using MemoPtr = UInt32;  // used in the file

public class MemoWriter : IDisposable
{
    private BinaryWriter bw;
    private Ptr top;  // position for next item to write
    private Ptr start, stop = 0;

    private Dictionary<string, MemoPtr> interned_strings;
    private Dictionary<byte[], MemoPtr> interned_objects;

    public MemoWriter(BinaryWriter bw, bool allow_object_internalisation)
    {
        this.bw = bw;
        this.interned_strings = new();

        if (allow_object_internalisation)
           this.interned_objects = new ();
    }

    public MemoWriter(BinaryWriter bw, Ptr startPos, Ptr stopPos,
        Dictionary<string, MemoPtr> interned_strings,
        Dictionary<byte[], MemoPtr> interned_objects
        )
    {
        this.bw = bw;
        this.top = startPos;
        this.start = startPos;
        this.stop = stopPos;
        this.interned_strings = interned_strings;
    }

    public void Dispose()
    {
        if (stop !=0 && top>stop)
            throw new Exception($"MemoWriter bound to [ {start}, {stop} ] wrote up to {top}.");
    }

    MemoWriter BindWriter(int scopeSize)
    {
        Ptr startPos = this.top;
        Ptr stopPos = this.top + scopeSize;
        var boundWriter = new MemoWriter(bw, startPos, stopPos, interned_strings, interned_objects);

        // update top
        this.top = this.top + scopeSize;

        return boundWriter;
    }

    // Object internalisation optimisation
    // (to use as with struct build using BindWriter above, after it completes writting the memory block)
    // (will only work fine if we internalise variants with their type prefix)
    /*
    public MemoPtr GetInternedObjectPtr()
    {
        if (interned_objects == null)
            return 0;

        // should we check for a hash or directly for byte sequence ?
        byte[] hashBytes;
        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            hashBytes = md5.ComputeHash(memBytes, start, stop-start);

        if (interned_objects.ContainsKey(hashBytes))
        {
            throw new NotImplementedException("we can reset the global heap top to this.start");
        }
        else
        {
            interned_objects[hashBytes] = (MemoPtr)this.start;
        }
        return interned_objects[hashBytes];
    }
    */

    private MemoPtr Align(int elementSize, int prefixSize = 0)
    {
        Ptr pos = top + prefixSize;
        if (pos % elementSize != 0)
            pos = pos - pos % elementSize + elementSize;
        top = pos - prefixSize;

        bw.BaseStream.Seek(top, SeekOrigin.Begin);

        return (MemoPtr)top;
    }

#region Raw Value

    public void Write(double[] vec)
    {
        Align(sizeof(double), sizeof(MemoPtr));

        int n = vec.Length;
        bw.Write((MemoPtr)n);
        foreach (var val in vec)
            bw.Write(val);
        top += sizeof(MemoPtr) + n * sizeof(double);
    }

    public void Write(float[] vec)
    {
        Align(sizeof(float), sizeof(MemoPtr));

        int n = vec.Length;
        bw.Write((MemoPtr)n);
        foreach (var val in vec)
            bw.Write(val);
        top += sizeof(MemoPtr) + n * sizeof(float);
    }

    public void Write(long[] vec)
    {
        Align(sizeof(long), sizeof(MemoPtr));

        int n = vec.Length;
        bw.Write((MemoPtr)n);
        foreach (var val in vec)
            bw.Write(val);
        top += sizeof(MemoPtr) + n * sizeof(long);
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

    public void Write(bool[] vec)
    {
        Align(sizeof(bool), sizeof(MemoPtr));

        int n = vec.Length;
        bw.Write((MemoPtr)n);
        foreach (var val in vec)
            bw.Write(val);
        top += sizeof(MemoPtr) + n * sizeof(bool);
    }

    public MemoPtr Write(string val)
    {
        // string internalisation optimisation
        if (interned_strings.ContainsKey(val))
            return interned_strings[val];

        var pos = Align(sizeof(MemoPtr));
        interned_strings[val] = pos;

        var bytes = Encoding.UTF8.GetBytes(val);
        int n = bytes.Length;
        bw.Write((MemoPtr)n);
        bw.Write(bytes);
        bw.Write('\0');

        top += sizeof(MemoPtr) + n + 1;
        return pos;
    }

    public void Write(double val)
    {
        Align(sizeof(double));
        bw.Write(val);
        top += sizeof(double);
    }

    public void Write(float val)
    {
        Align(sizeof(float));
        bw.Write(val);
        top += sizeof(float);
    }

    public void Write(long val)
    {
        Align(sizeof(long));
        bw.Write(val);
        top += sizeof(long);
    }

    public void Write(int val)
    {
        Align(sizeof(int));
        bw.Write(val);
        top += sizeof(int);
    }

    public void Write(short val)
    {
        Align(sizeof(short));
        bw.Write(val);
        top += sizeof(short);
    }

    public void Write(byte val)
    {
        Align(sizeof(byte));
        bw.Write(val);
        top += sizeof(byte);
    }

    public void Write(uint val)
    {
        Align(sizeof(uint));
        bw.Write(val);
        top += sizeof(uint);
    }

#endregion

#region Decorated value

    public MemoPtr WriteTagged(object val)
    {
        if (val is Dictionary<string, object>)
            return WriteTagged((Dictionary<string, object>)val);

        if (val is string)
            return WriteTagged((string)val);
        if (val is double)
            return WriteTagged((double)val);
        if (val is long)
            return WriteTagged((long)val);
        if (val is int)
            return WriteTagged((int)val);
        if (val is bool)
            return WriteTagged((bool)val);

        if (val is string[])
            return WriteTagged((string[])val);
        if (val is double[])
            return WriteTagged((double[])val);
        if (val is long[])
            return WriteTagged((long[])val);
        if (val is int[])
            return WriteTagged((int[])val);
        if (val is bool[])
            return WriteTagged((bool[])val);

        if (val is JObject) {
            var val2 = (val as JObject).ToObject<Dictionary<string, object>>();
            return WriteTagged(val2);
        }

        if (val is JArray) {
            var val2 = (val as JArray).ToObject<object[]>();
            return WriteTagged(val2);
        }

        if (val is null)
            return WriteTaggedNull();

        throw new Exception($"Unhandled type {val.GetType().Name}");
    }

    public MemoPtr WriteTaggedNull()
    {
        var pos = Align(sizeof(byte));
        Write(MemoPack.TYPE_NULL);
        return pos;
    }

    public MemoPtr WriteTagged(Dictionary<string, object> dict)
    {
        var pos = Align(sizeof(MemoPtr), 3 * sizeof(byte));

        var n = dict.Count;
        Write(MemoPack.TYPE_DICT);
        Write(MemoPack.TYPE_TXT);
        Write(MemoPack.TYPE_UNTYPED);
        Write((MemoPtr)dict.Count);

        using (var x = BindWriter(2 * n * sizeof(MemoPtr)))
        {
            foreach (var kv in dict)
                x.Write(Write(kv.Key));           // write offset to string written offline
            foreach (var kv in dict)
                x.Write(WriteTagged(kv.Value));   // write offset to value written offline
        }

        return pos;
    }

    public MemoPtr WriteTagged(object[] vec)
    {
        var pos = Align(sizeof(MemoPtr), 2 * sizeof(byte));

        Write(MemoPack.TYPE_ARRAY);
        Write(MemoPack.TYPE_UNTYPED);
        Write((MemoPtr)vec.Length);

        using (var x = BindWriter(vec.Length * sizeof(MemoPtr)))
        {
            foreach (var val in vec)
                x.Write(WriteTagged(val));   // write offset to value written offline
        }

        return pos;
    }


    public MemoPtr WriteTagged(string[] vec)
    {
        var pos = Align(sizeof(MemoPtr), 2 * sizeof(byte));

        Write(MemoPack.TYPE_ARRAY);
        Write(MemoPack.TYPE_TXT_PTR);
        Write((MemoPtr)vec.Length);

        using (var x = BindWriter(vec.Length * sizeof(MemoPtr)))
        {
            foreach (var val in vec)
                x.Write(WriteTagged(val));   // write offset to value written offline
        }

        return pos;
    }

    public MemoPtr WriteTagged(double[] vec)
    {
        var pos = Align(sizeof(double), 2 * sizeof(byte));

        Write(MemoPack.TYPE_ARRAY);
        Write(MemoPack.TYPE_F64);
        Write(vec);

        return pos;
    }

    public MemoPtr WriteTagged(long[] vec)
    {
        var pos = Align(sizeof(long), 2 * sizeof(byte));

        Write(MemoPack.TYPE_ARRAY);
        Write(MemoPack.TYPE_I64);
        Write(vec);

        return pos;
    }

    public MemoPtr WriteTagged(int[] vec)
    {
        var pos = Align(sizeof(int), 2 * sizeof(byte));

        Write(MemoPack.TYPE_ARRAY);
        Write(MemoPack.TYPE_I32);
        Write(vec);

        return pos;
    }

    public MemoPtr WriteTagged(bool[] vec)
    {
        var pos = Align(sizeof(bool), 2 * sizeof(byte));

        Write(MemoPack.TYPE_ARRAY);
        Write(MemoPack.TYPE_BOOL);
        Write(vec);

        return pos;
    }

    public MemoPtr WriteTagged(string val)
    {
        var pos = Align(sizeof(MemoPtr), sizeof(byte));

        // string internalisation optimisation
        if (interned_strings.ContainsKey(val)) {
            MemoPtr strAddr = interned_strings[val];

            Write(MemoPack.TYPE_TXT_PTR);
            Write(strAddr);

            return pos;
        }

        Write(MemoPack.TYPE_TXT);
        Write(val);

        return pos;
    }

    public MemoPtr WriteTagged(double val)
    {
        var pos = Align(sizeof(double), sizeof(byte));

        Write(MemoPack.TYPE_F64);
        Write(val);

        return pos;
    }

    public MemoPtr WriteTagged(long val)
    {
        var pos = Align(sizeof(long), sizeof(byte));

        Write(MemoPack.TYPE_I64);
        Write(val);

        return pos;
    }

    public MemoPtr WriteTagged(int val)
    {
        var pos = Align(sizeof(int), sizeof(byte));

        Write(MemoPack.TYPE_I32);
        Write(val);

        return pos;
    }

    public MemoPtr WriteTagged(bool val)
    {
        var pos = Align(sizeof(byte));

        Write(val
            ? MemoPack.TYPE_TRUE
            : MemoPack.TYPE_FALSE);

        return pos;
    }

#endregion

}
}