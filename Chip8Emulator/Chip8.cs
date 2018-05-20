using System;
using System.Diagnostics;
using Chip8Emulator.Helpers;
using Chip8Emulator.Models;

namespace Chip8Emulator
{
    public interface IChip8
    {
        void LoadGame(byte[] game);
        void ProcessOneStep(KeyboardState keyboardState);
        bool IsPixelSet(int x, int y);
        bool ShouldPlaySound();
    }

    // ReSharper disable InconsistentNaming
    public class Chip8 : IChip8
    {
        private const int ScreenWidth = 64;
        private const int ScreenHeight = 32;

        private readonly byte[] _memory = new byte[4096];
        private readonly ushort[] _stack = new ushort[16];
        private readonly bool[,] _screen = new bool[ScreenWidth, ScreenHeight];

        // Registers
        private byte[] V = new byte[17];
        private ushort I;
        private byte _soundRegister;
        private byte _delayRegister;
        private ushort PC;
        private byte SP;
        
        private const int _memoryStartLocation = 0x200;

        private readonly Random _random = new Random();
        private readonly Stopwatch stopwatch = new Stopwatch();

        public void LoadGame(byte[] game)
        {
            Reset();
            LoadCharacters();
            for (var i = 0; i < game.Length; i++)
            {
                _memory[_memoryStartLocation + i] = game[i];
            }
            
            PC = _memoryStartLocation;
        }

        private void Reset()
        {
            ClearScreen();
            for (var i = 0; i < V.Length; i++) V[i] = 0;
            for (var i = 0; i < _memory.Length; i++) _memory[i] = 0;
            for (var i = 0; i < _stack.Length; i++) _stack[i] = 0;
            I = 0;
            _soundRegister = 0;
            _delayRegister = 0;
            PC = 0;
            SP = 0;
        }

        public void ProcessOneStep(KeyboardState keyboardState)
        {
            //WriteState();
            //Console.WriteLine("FPS: " + _numberOfRuns / (_stopwatch.Elapsed.Seconds + 1));

            if (!stopwatch.IsRunning) stopwatch.Start();

            var opCode = GetOpCode();
            var nnn = (ushort) (opCode % (16 * 256));
            var n = opCode % 16;
            var x = opCode / 256 % 16;
            var y = opCode % 256 / 16;
            var kk = (byte) (opCode % 256);

            //Console.WriteLine(opCode.ToString("X4"));
            if (opCode == 0x00E0)
            {
                //Console.WriteLine("Clear Screen");
                ClearScreen();
                PC += 2;
            }
            else if (opCode == 0x00EE)
            {
                //Console.WriteLine("Return from subroutine");
                SP--;
                PC = _stack[SP];
            }
            else if (opCode == 0x1000 + nnn)
            {
                //Console.WriteLine("Jump to nnn=" + nnn);
                PC = nnn;
            }
            else if (opCode == 0x2000 + nnn)
            {
                //Console.WriteLine("Call subroutine at nnn=" + nnn);
                _stack[SP] = (ushort) (PC + 2);
                SP++;
                PC = nnn;
            }
            else if (opCode == 0x3000 + x * 256 + kk)
            {
                //Console.WriteLine("Skip instruction if kk=" + kk + " is V[x]=" + V[x]);
                if (V[x] == kk) PC += 2;
                PC += 2;
            }
            else if (opCode == 0x4000 + x * 256 + kk)
            {
                //Console.WriteLine("Skip instruction if kk=" + kk + " is not V[x]=" + V[x]);
                if (V[x] != kk) PC += 2;
                PC += 2;
            }
            else if (opCode == 0x5000 + x * 256 + y * 16)
            {
                //Console.WriteLine("Skip instruction if V[x]=" + V[x] + " is V[y]=" + V[y]);
                if (V[x] == V[y]) PC += 2;
                PC += 2;
            }
            else if (opCode == 0x6000 + x * 256 + kk)
            {
                //Console.WriteLine("Set V[x] to kk=" + kk);
                V[x] = kk;
                PC += 2;
            }
            else if (opCode == 0x7000 + x * 256 + kk)
            {
                V[x] = (byte) ((V[x] + kk) % 256);
                PC += 2;
            }
            else if (opCode == 0x8000 + x * 256 + y * 16)
            {
                V[x] = V[y];
                PC += 2;
            }
            else if (opCode == 0x8000 + x * 256 + y * 16 + 1)
            {
                V[x] = (byte) (V[x] | V[y]);
                PC += 2;
            }
            else if (opCode == 0x8000 + x * 256 + y * 16 + 2)
            {
                V[x] = (byte) (V[x] & V[y]);
                PC += 2;
            }
            else if (opCode == 0x8000 + x * 256 + y * 16 + 3)
            {
                V[x] = (byte) (V[x] ^ V[y]);
                PC += 2;
            }
            else if (opCode == 0x8000 + x * 256 + y * 16 + 4)
            {
                var sum = V[x] + V[y];
                V[0xF] = (byte) (sum > 255 ? 1 : 0);
                V[x] = (byte) (sum % 256);
                PC += 2;
            }
            else if (opCode == 0x8000 + x * 256 + y * 16 + 5)
            {
                V[0xF] = (byte) (V[x] > V[y] ? 1 : 0);
                V[x] = (byte) ((256 + V[x] - V[y]) % 256);
                PC += 2;
            }
            else if (opCode == 0x8000 + x * 256 + y * 16 + 6)
            {
                V[0xF] = (byte) (V[x] % 2);
                V[x] = (byte) (V[x] / 2);
                PC += 2;
            }
            else if (opCode == 0x8000 + x * 256 + y * 16 + 7)
            {
                V[0xF] = (byte)(V[y] > V[x] ? 1 : 0);
                V[x] = (byte)((256 + V[y] - V[x]) % 256);
                PC += 2;
            }
            else if (opCode == 0x8000 + x * 256 + y * 16 + 0xE)
            {
                V[0xF] = (byte) (V[x] >= 128 ? 1 : 0);
                V[x] = (byte) (V[x] * 2 % 256);
                PC += 2;
            }
            else if (opCode == 0x9000 + x * 256 + y * 16)
            {
                if (V[x] != V[y]) PC += 2;
                PC += 2;
            }
            else if (opCode == 0xA000 + nnn)
            {
                I = nnn;
                PC += 2;
            }
            else if (opCode == 0xB000 + nnn)
            {
                PC = (ushort) (nnn + V[0]);
                PC += 2;
            }
            else if (opCode == 0xC000 + x * 256 + kk)
            {
                var r = (byte)_random.Next(0, 256);
                V[x] = (byte) (r & kk);
                PC += 2;
            }
            else if (opCode == 0xD000 + x * 256 + y * 16 + n)
            {
                // Display n-byte sprite at memory location I.
                // Display it at Vx, Vy top left
                // Set VF = 1 if any pixel changes from 1 to 0. Otherwise, VF = 0
                // Display should wrap around the screen if necessary.
                // That is, n bytes vertically down, each xor-ed in order from top to bottom
                var bytes = new byte[n];
                V[0xF] = 0;
                for (var i = 0; i < n; i++) bytes[i] = _memory[I + i];
                for (var i = 0; i < bytes.Length; i++)
                {
                    var spriteByte = bytes[i];
                    for (var b = 0; b < 8; b++)
                    {
                        var spriteBit = spriteByte.GetBit(b);
                        var xLocation = (V[x] + b) % ScreenWidth;
                        var yLocation = (V[y] + i) % ScreenHeight;
                        if (spriteBit && _screen[xLocation, yLocation]) V[0xF] = 1;

                        _screen[xLocation, yLocation] ^= spriteBit;
                    }
                }
                PC += 2;
            }
            else if (opCode == 0xE000 + x * 256 + 0x9E)
            {
                if (keyboardState.IsKeyDown(V[x])) PC += 2;
                PC += 2;
            }
            else if (opCode == 0xE000 + x * 256 + 0xA1)
            {
                if (!keyboardState.IsKeyDown(V[x])) PC += 2;
                PC += 2;
            }
            else if (opCode == 0xF000 + x * 256 + 0x07)
            {
                V[x] = _delayRegister;
                PC += 2;
            }
            else if (opCode == 0xF000 + x * 256 + 0x0A)
            {
                var setKey = false;
                for (byte i = 0; i < 16; i++)
                {
                    if (!keyboardState.IsKeyDown(i)) continue;

                    V[x] = i;
                    setKey = true;
                    break;
                }

                if (!setKey)
                {
                    return;
                }
                PC += 2;
            }
            else if (opCode == 0xF000 + x * 256 + 0x15)
            {
                _delayRegister = V[x];
                PC += 2;
            }
            else if (opCode == 0xF000 + x * 256 + 0x18)
            {
                _soundRegister = V[x];
                PC += 2;
            }
            else if (opCode == 0xF000 + x * 256 + 0x1E)
            {
                I = (ushort)((I + V[x]) % ushort.MaxValue);
                PC += 2;
            }
            else if (opCode == 0xF000 + x * 256 + 0x29)
            {
                var digit = V[x] % 16;
                I = (ushort) (digit * 5);
                PC += 2;
            }
            else if (opCode == 0xF000 + x * 256 + 0x33)
            {
                _memory[I] = (byte) (V[x] / 100 % 10);
                _memory[I + 1] = (byte) (V[x] / 10 % 10);
                _memory[I + 2] = (byte) (V[x] % 10);
                PC += 2;
            }
            else if (opCode == 0xF000 + x * 256 + 0x55)
            {
                for (var i = 0; i <= x; i++)
                {
                    _memory[I + i] = V[i];
                }
                PC += 2;
            }
            else if (opCode == 0xF000 + x * 256 + 0x65)
            {
                for (var i = 0; i <= x; i++)
                {
                    V[i] = _memory[I + i];
                }
                PC += 2;
            }

            // These timers should not be decremented if waiting for a key press
            // Therefore, it is important they are below the op codes above.
            if (stopwatch.ElapsedMilliseconds > 1000 / 60)
            {
                if (_soundRegister > 0) _soundRegister--;
                if (_delayRegister > 0) _delayRegister--;
                stopwatch.Restart();
            }
        }

        private ushort GetOpCode()
        {
            return (ushort)(256 * _memory[PC] + _memory[PC + 1]);
        }

        private void ClearScreen()
        {
            for (var x = 0; x < ScreenWidth; x++)
            {
                for (var y = 0; y < ScreenHeight; y++)
                {
                    _screen[x, y] = false;
                }
            }
        }

        private void LoadCharacters()
        {
            // 0
            _memory[0] = 0xF0;
            _memory[1] = 0x90;
            _memory[2] = 0x90;
            _memory[3] = 0x90;
            _memory[4] = 0xF0;

            // 1
            _memory[5] = 0x20;
            _memory[6] = 0x60;
            _memory[7] = 0x20;
            _memory[8] = 0x20;
            _memory[9] = 0x70;

            // 2
            _memory[10] = 0xF0;
            _memory[11] = 0x10;
            _memory[12] = 0xF0;
            _memory[13] = 0x80;
            _memory[14] = 0xF0;

            // 3
            _memory[15] = 0xF0;
            _memory[16] = 0x10;
            _memory[17] = 0xF0;
            _memory[18] = 0x10;
            _memory[19] = 0xF0;

            // 4
            _memory[20] = 0x90;
            _memory[21] = 0x90;
            _memory[22] = 0xF0;
            _memory[23] = 0x10;
            _memory[24] = 0x10;

            // 5
            _memory[25] = 0xF0;
            _memory[26] = 0x80;
            _memory[27] = 0xF0;
            _memory[28] = 0x10;
            _memory[29] = 0xF0;

            // 6
            _memory[30] = 0xF0;
            _memory[31] = 0x80;
            _memory[32] = 0xF0;
            _memory[33] = 0x90;
            _memory[34] = 0xF0;

            // 7
            _memory[35] = 0xF0;
            _memory[36] = 0x10;
            _memory[37] = 0x20;
            _memory[38] = 0x40;
            _memory[39] = 0x40;

            // 8
            _memory[40] = 0xF0;
            _memory[41] = 0x90;
            _memory[42] = 0xF0;
            _memory[43] = 0x90;
            _memory[44] = 0xF0;

            // 9
            _memory[45] = 0xF0;
            _memory[46] = 0x90;
            _memory[47] = 0xF0;
            _memory[48] = 0x10;
            _memory[49] = 0xF0;

            // A
            _memory[50] = 0xF0;
            _memory[51] = 0x90;
            _memory[52] = 0xF0;
            _memory[53] = 0x90;
            _memory[54] = 0x90;

            // B
            _memory[55] = 0xE0;
            _memory[56] = 0x90;
            _memory[57] = 0xE0;
            _memory[58] = 0x90;
            _memory[59] = 0xE0;

            // C
            _memory[60] = 0xF0;
            _memory[61] = 0x80;
            _memory[62] = 0x80;
            _memory[63] = 0x80;
            _memory[64] = 0xF0;

            // D
            _memory[65] = 0xE0;
            _memory[66] = 0x90;
            _memory[67] = 0x90;
            _memory[68] = 0x90;
            _memory[69] = 0xE0;

            // E
            _memory[70] = 0xF0;
            _memory[71] = 0x80;
            _memory[72] = 0xF0;
            _memory[73] = 0x80;
            _memory[74] = 0xF0;

            // F
            _memory[75] = 0xF0;
            _memory[76] = 0x80;
            _memory[77] = 0xF0;
            _memory[78] = 0x80;
            _memory[79] = 0x80;
        }

        public bool IsPixelSet(int x, int y)
        {
            return _screen[x, y];
        }

        public bool ShouldPlaySound()
        {
            return _soundRegister > 0;
        }

        private void WriteState()
        {
            var str = "   |" + " V  |  S " + Environment.NewLine;
            for (var i = 0; i < 16; i++)
            {
                str += i.ToString("X1") + "  |" + " " + V[i].ToString("X2") + " | " + _stack[i].ToString("X3") + Environment.NewLine;
            }

            str += "PC | " + PC.ToString("X3") + Environment.NewLine;
            str += "SP | " + SP.ToString("X3") + Environment.NewLine;
            str += "I  | " + I.ToString("X2") + Environment.NewLine;

            str += "DT | " + _delayRegister.ToString("X2") + Environment.NewLine;
            str += "ST | " + _soundRegister.ToString("X2") + Environment.NewLine;

            str += GetOpCode().ToString("X4") + Environment.NewLine;
            Console.WriteLine(str);
        }
    }
}
