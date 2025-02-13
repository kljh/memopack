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

    public class MemoWriterInternals
    {
        public MemoWriterInternals(bool allow_object_internalisation) {
            this.interned_strings = new();
            if (allow_object_internalisation)
                this.interned_objects = new ();
        }

        public  Dictionary<string, MemoPtr> interned_strings;
        public  Dictionary<byte[], MemoPtr>? interned_objects;
    }

    private MemoWriterInternals internals;

    public MemoWriter(BinaryWriter bw, bool allow_object_internalisation)
    {
        this.bw = bw;
        this.internals = new MemoWriterInternals(allow_object_internalisation);
    }

    public MemoWriter(BinaryWriter bw, Ptr startPos, Ptr stopPos,
        MemoWriterInternals internals
        )
    {
        this.bw = bw;
        this.top = startPos;
        this.start = startPos;
        this.stop = stopPos;
        this.internals = internals;
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
        var boundWriter = new MemoWriter(bw, startPos, stopPos, internals);

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
        byte[] bytes = MemoTools.BoolToByteArray(vec);
        bw.Write((MemoPtr)n);
        bw.Write(bytes);
        top += sizeof(MemoPtr) + bytes.Length;
    }

    public MemoPtr Write(string val)
    {
        // string internalisation optimisation
        if (internals.interned_strings.ContainsKey(val))
            return internals.interned_strings[val];

        var pos = Align(sizeof(MemoPtr));
        internals.interned_strings[val] = pos;

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

    public MemoPtr WriteInlineTagged(object? val)
    {
        // When we write a pointer to a tagged value, footprint can be as big as :
        // pointer (4 bytes) tag value (1 type byte + 8 bytes for a double + possibly 7 byte to align)
        // for a double value as common as zero

        // Inlining means we can encode in the address the most common values.
        // We set the highest quartet of the address to 0xF to denote inline values (we lose 4GB/16 = 250MB of addressable space).
        // (Reminder : because we are using little endian integers, the highest quartet is on the 4th byte.)
        // We use the lowest quartet of the highest byte to encode what the inline value is :
        //
        //  0xF8    str     (on 3 bytes: one or two chars, null terminated)
        //  0xF9    f64     (if contains an int, that can be encode on a 3 byte int)
        //  0XFA    f32     (idem)
        //  0xFB    int64   (if can be encoded on a 3 bytes int)
        //  0xFC    int32   (idem)
        //  0xFD    uint32  (if can be encoded on a 3 bytes uint)
        //  0xFE    uint64  (idem)
        //  0xFF    all tagged value types that fit on 3 bytes : type tag first byte (lowest byte), value in 2nd and 3rd byte

        if (val is null)
            return (MemoPtr)0;


        MemoPtr uiVal = 0;
        if (val is string)
        {
            string sVal = (string)val;
            if (sVal.Length == 0)
                return 0xF8000000;
            if (sVal.Length == 1)
                return 0xF8000000 | sVal[0];
        }

        if (val is double)
        {
            uiVal = (MemoPtr)((int)(double)val) & 0xFFFFFF;
            if ((double)val == (double)uiVal)
                return 0xF9000000 | uiVal;
        }

        if (val is float)
        {
            uiVal = (MemoPtr)((int)(float)val) & 0xFFFFFF;
            if ((float)val == (float)uiVal)
                return 0xFA000000 | uiVal;
        }

        if (val is long)
        {
            uiVal = (MemoPtr)((long)val) & 0xFFFFFF;
            if ((long)val == (long)uiVal)
                return 0xFB000000 | uiVal;
        }

        if (val is int)
        {
            uiVal = (MemoPtr)((int)val) & 0xFFFFFF;
            if ((int)val == (int)uiVal)
                return 0xFC000000 | uiVal;
        }

        if (val is ulong)
        {
            uiVal = (MemoPtr)((ulong)val) & 0xFFFFFF;
            if ((ulong)val == (ulong)uiVal)
                return 0xFD000000 | uiVal;
        }

        if (val is uint)
        {
            uiVal = (MemoPtr)((uint)val) & 0xFFFFFF;
            if ((uint)val == (uint)uiVal)
                return 0xFE000000 | uiVal;
        }

        return WriteTagged(val);
    }

    public MemoPtr WriteTagged(object? val)
    {
        if (val is null)
            return WriteTaggedNull();

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

        if (val is Dictionary<string, object>) {
            var dic = (Dictionary<string, object>)val;
            return WriteTagged(dic);
        }

        if (val is Dictionary<object, object>) {
            var kvs = (Dictionary<object, object>)val;
            var dic = new Dictionary<string, object>(kvs.Select(kv => new KeyValuePair<string, object>(kv.Key?.ToString() ?? "", kv.Value)));
            return WriteTagged(dic);
        }

        if (val is object[]) {
            var vec = (object[])val;
            return WriteTagged(vec);
        }

        if (val is JObject) {
            var obj = ((JObject)val).ToObject<Dictionary<string, object>>();
            if (obj == null)
                throw new NullReferenceException();
            return WriteTagged(obj);
        }

        if (val is JArray) {
            var vec = ((JArray)val).ToObject<object[]>();
            if (vec == null)
                throw new NullReferenceException();
            return WriteTagged(vec);
        }

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
                x.Write(WriteInlineTagged(kv.Value));   // write offset to value written offline
        }

        return pos;
    }

    public MemoPtr WriteTagged(object[] vec)
    {
        var pos = Align(sizeof(MemoPtr), 2 * sizeof(byte) + sizeof(MemoPtr));

        Write(MemoPack.TYPE_ARRAY);
        Write(MemoPack.TYPE_UNTYPED);
        Write((MemoPtr)vec.Length);

        using (var x = BindWriter(vec.Length * sizeof(MemoPtr)))
        {
            foreach (var val in vec)
                x.Write(WriteInlineTagged(val));   // write offset to value written offline
        }

        return pos;
    }


    public MemoPtr WriteTagged(string[] vec)
    {
        var pos = Align(sizeof(MemoPtr), 2 * sizeof(byte) + sizeof(MemoPtr));

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
        var pos = Align(sizeof(double), 2 * sizeof(byte) + sizeof(MemoPtr));

        Write(MemoPack.TYPE_ARRAY);
        Write(MemoPack.TYPE_F64);
        Write(vec);

        return pos;
    }

    public MemoPtr WriteTagged(long[] vec)
    {
        var pos = Align(sizeof(long), 2 * sizeof(byte) + sizeof(MemoPtr));

        Write(MemoPack.TYPE_ARRAY);
        Write(MemoPack.TYPE_I64);
        Write(vec);

        return pos;
    }

    public MemoPtr WriteTagged(int[] vec)
    {
        var pos = Align(sizeof(int), 2 * sizeof(byte) + sizeof(MemoPtr));

        Write(MemoPack.TYPE_ARRAY);
        Write(MemoPack.TYPE_I32);
        Write(vec);

        return pos;
    }

    public MemoPtr WriteTagged(bool[] vec)
    {
        // for T[], we align of max(sizeof(MemoPtr), sizeof(T)) because we store the array size first
        var pos = Align(sizeof(MemoPtr), 2 * sizeof(byte) + sizeof(MemoPtr));

        Write(MemoPack.TYPE_ARRAY);
        Write(MemoPack.TYPE_BOOL);
        Write(vec);

        return pos;
    }

    public MemoPtr WriteTagged(string val)
    {
        var pos = Align(sizeof(MemoPtr), sizeof(byte));

        // string internalisation optimisation
        if (internals.interned_strings.ContainsKey(val)) {
            MemoPtr strAddr = internals.interned_strings[val];

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