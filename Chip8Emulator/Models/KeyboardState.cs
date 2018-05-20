using System;

namespace Chip8Emulator.Models
{
    public class KeyboardState
    {
        private readonly bool[] _keyStates;

        public KeyboardState(bool[] keyStates)
        {
            if (keyStates.Length != 16)
            {
                throw new ArgumentException("There should be 16 keys on the keyboard", nameof(keyStates));
            }
            _keyStates = keyStates;
        }

        public bool IsKeyDown(byte key)
        {
            return _keyStates[key % 16];
        }
    }
}
