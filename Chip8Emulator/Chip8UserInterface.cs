using System;
using System.Linq;
using Chip8Emulator.Models;
using Chip8Emulator.Properties;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

namespace Chip8Emulator
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Chip8UserInterface : Game
    {
        private const int FrameRate = 500;
        private static readonly Rectangle Resolution = new Rectangle(0, 0, 64, 32);

        private SoundEffectInstance _soundEffectInstance;
        private bool _playingSound;

        private SpriteBatch _spriteBatch;
        private Texture2D _whiteSquare;
        private Texture2D _blackSquare;
        private SoundEffect _soundEffect;

        private IChip8 _chip8;
        
        public Chip8UserInterface()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = Resolution.Width * 32,
                PreferredBackBufferHeight = Resolution.Height * 32
            };
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1 / (double)FrameRate);
            IsMouseVisible = false;
            Window.AllowUserResizing = true;
            Window.Title = "Chip 8 Emulator";

            _chip8 = new Chip8();
            _chip8.LoadGame(Resources.INVADERS);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _blackSquare = Content.Load<Texture2D>("BlackSprite");
            _whiteSquare = Content.Load<Texture2D>("WhiteSprite");
            _soundEffect = Content.Load<SoundEffect>("audiocheck.net_sin_450Hz_-3dBFS_10s");

        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            var keyboardState = GetChip8KeyboardState();
            _chip8.ProcessOneStep(keyboardState);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();
            for (var x = 0; x < Resolution.Width; x++)
            {
                for (var y = 0; y < Resolution.Height; y++)
                {
                    DrawSquare(x, y, _chip8.IsPixelSet(x, y) ? _whiteSquare : _blackSquare);
                }
            }
            _spriteBatch.End();

            if (_chip8.ShouldPlaySound())
            {
                PlaySound();
            }
            else
            {
                StopSound();
            }

            base.Draw(gameTime);
        }

        private void DrawSquare(int x, int y, Texture2D square)
        {
            var viewPort = GraphicsDevice.Viewport.Bounds;
            var doubleWidth = viewPort.Width / (double) Resolution.Width;
            var doubleHeight = viewPort.Height / (double) Resolution.Height;
            var xPosition = Convert.ToInt32(x * doubleWidth);
            var yPosition = Convert.ToInt32(y * doubleHeight);

            int width;
            if (xPosition == Resolution.Width - 1)
            {
                width = viewPort.Width + 1 - xPosition;
            }
            else
            {
                width = Convert.ToInt32((x + 1) * doubleWidth) - xPosition;
            }

            int height;
            if (yPosition == Resolution.Height - 1)
            {
                height = viewPort.Height + 1 - yPosition;
            }
            else
            {
                height = Convert.ToInt32((y + 1) * doubleHeight) - yPosition;
            }

            _spriteBatch.Draw(square, new Rectangle(xPosition, yPosition, width, height), Color.White);
        }

        private void PlaySound()
        {
            if (_playingSound) return;

            _soundEffectInstance?.Stop(true);

            _soundEffectInstance = _soundEffect.CreateInstance();
            _soundEffectInstance.IsLooped = true;
            _soundEffectInstance.Play();

            _playingSound = true;
        }

        private void StopSound()
        {
            if (!_playingSound) return;

            _soundEffectInstance.Stop(true);

            _playingSound = false;
        }

        private bool[] _previousKeyStates = new bool[16];

        private Models.KeyboardState GetChip8KeyboardState()
        {
            var keyboardState = Keyboard.GetState();
            /*
             *+-+-+-+-+                +-+-+-+-+
               |1|2|3|C|                |1|2|3|4|
               +-+-+-+-+                +-+-+-+-+
               |4|5|6|D|                |Q|W|E|R|
               +-+-+-+-+       =>       +-+-+-+-+
               |7|8|9|E|                |A|S|D|F|
               +-+-+-+-+                +-+-+-+-+
               |A|0|B|F|                |Z|X|C|V|
               +-+-+-+-+                +-+-+-+-+
             */
            var newKeyStates = new bool[16];
            newKeyStates[0x0] = keyboardState.IsKeyDown(Keys.X);
            newKeyStates[0x1] = keyboardState.IsKeyDown(Keys.D1);
            newKeyStates[0x2] = keyboardState.IsKeyDown(Keys.D2);
            newKeyStates[0x3] = keyboardState.IsKeyDown(Keys.D3);
            newKeyStates[0x4] = keyboardState.IsKeyDown(Keys.Q);
            newKeyStates[0x5] = keyboardState.IsKeyDown(Keys.W);
            newKeyStates[0x6] = keyboardState.IsKeyDown(Keys.E);
            newKeyStates[0x7] = keyboardState.IsKeyDown(Keys.A);
            newKeyStates[0x8] = keyboardState.IsKeyDown(Keys.S);
            newKeyStates[0x9] = keyboardState.IsKeyDown(Keys.D);
            newKeyStates[0xA] = keyboardState.IsKeyDown(Keys.Z);
            newKeyStates[0xB] = keyboardState.IsKeyDown(Keys.C);
            newKeyStates[0xC] = keyboardState.IsKeyDown(Keys.D4);
            newKeyStates[0xD] = keyboardState.IsKeyDown(Keys.R);
            newKeyStates[0xE] = keyboardState.IsKeyDown(Keys.F);
            newKeyStates[0xF] = keyboardState.IsKeyDown(Keys.V);

            var keyStates = new bool[16];

            for (var i = 0; i < keyStates.Length; i++)
            {
                keyStates[i] = _previousKeyStates[i] && !newKeyStates[i];
            }

            _previousKeyStates = newKeyStates;

            // Some games play better when reacting only to released keys!
            //return new Models.KeyboardState(keyStates);
            return new Models.KeyboardState(newKeyStates);
        }
    }
}
