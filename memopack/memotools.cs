namespace MemoPack
{

public class MemoTools
{
    public static byte[] BoolToByteArray(IEnumerable<bool> bools)
    {
        int nbBytes = bools.Count() / 8 + ((bools.Count() % 8) == 0 ? 0 : 1);
        byte[] bytes = new byte[nbBytes];

        int iByte = 0;
        uint bits = 0;
        uint mask = 0x1;
        foreach (var bit in bools) {
            if (bit)
                bits = bits | mask;
            mask = mask << 1;

            // push if one full byte
            if (mask == 0x100)
            {
                bytes[iByte++] = (byte)bits;
                bits = 0;
                mask = 0x1;
            }
        }
        // push last partial byte
        if (mask != 0x1)
            bytes[iByte++] = (byte)bits;

        return bytes;
    }

    public static bool[] ByteToBoolArray(byte[] bytes, uint nbBools)
    {
        bool[] bools = new bool[nbBools];

        uint[] masks = { 0x1, 0x2, 0x4, 0x8, 0x10, 0x20, 0x40, 0x80 };
        for (uint i=0; i<nbBools; i++)
            bools[i] = (bytes[i/8] & masks[i%8]) != 0;

        return bools;
    }

}
}