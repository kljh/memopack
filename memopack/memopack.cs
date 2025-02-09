namespace MemoPack
{
// https://flatbuffers.dev/internals/

public class MemoPack
{
    // Text : size u32, utf8 char[], '\0'
    public static readonly byte TYPE_TXT = (byte)'a';
    public static readonly byte TYPE_TXT_PTR = (byte)'A';

    // Boolean
    public static readonly byte TYPE_BOOL = (byte)'b';
    public static readonly byte TYPE_FALSE = (byte)'0';  // number '0'
    public static readonly byte TYPE_TRUE = (byte)'1';  // number '1'


    // Number : IEEE 754
    public static readonly byte TYPE_F64  = (byte)'d';
    public static readonly byte TYPE_F128 = (byte)'e';
    public static readonly byte TYPE_F32  = (byte)'f';


    // Integrer : lower case = little endian
    public static readonly byte TYPE_I8  = (byte)'g';
    public static readonly byte TYPE_I16 = (byte)'h';
    public static readonly byte TYPE_I32 = (byte)'i';
    public static readonly byte TYPE_I64 = (byte)'j';

    public static readonly byte TYPE_U8  = (byte)'x';
    public static readonly byte TYPE_U16 = (byte)'y';
    public static readonly byte TYPE_U32 = (byte)'u';
    public static readonly byte TYPE_U64 = (byte)'z';


    // Struct record (fixed size) + <ID>
    public static readonly byte TYPE_RECORD = (byte)'!';

    // Struct record (variable size / sparse) + <ID> + <VTABLE>
    public static readonly byte TYPE_SPARSE = (byte)'?';


    // Null
    public static readonly byte TYPE_NULL = (byte)'-';

    // Untyped
    public static readonly byte TYPE_UNTYPED = (byte)'_';


    // Reference (pointer) + <V> : size u32
    public static readonly byte TYPE_PTR = (byte)'&';


    // Array + <V> : size u32, data K[]
    public static readonly byte TYPE_ARRAY = (byte)'*';


    // Dict + <K, V> : size u32, keys K[], values V[]
    // Key type is a pair (hash, key) for hach sorted dict
    public static readonly byte TYPE_DICT = (byte)':';
    public static readonly byte TYPE_SORTED_DICT = (byte)'<';
    public static readonly byte TYPE_HASH_DICT = (byte)'#';


   // Padding (to align addresses)
    public static readonly byte TYPE_PADDING = (byte)'\0';


}
}