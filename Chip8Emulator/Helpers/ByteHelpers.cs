using System;

namespace Chip8Emulator.Helpers
{
    public static class ByteHelpers
    {
        /**
         * The zero bit is the MSB and the eigth bit the LSB.
         */
        public static bool GetBit(this byte b, int index)
        {
            if (index < 0 || index >= 8) throw new ArgumentOutOfRangeException(nameof(index));
            return ((b >> (7 - index)) & 1) == 1;
            //return ((byte)(1 << (8 - (index + 1))) & b) == 1;
        }
    }
}
