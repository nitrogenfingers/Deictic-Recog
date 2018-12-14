using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HarryPotterGame.Input {
    /// <summary>
    /// This nifty little class allows you to transform your cursor into a sparkly wand trail. It can:
    /// - Draw a glowing wand icon where the cursor is
    /// - Have a trail of glowing particles that float and slowly fall from where the cursor is
    /// - Create a burst of particles when a gesture is made
    /// A gauge from 0 to any real number can be used to determine how many particles are created and displayed
    /// </summary>
    public class WandCursor {
        SpriteBatch batch;
        Game game;
        Texture2D arrowTexture;
        Random sparkleRandom = new Random();
        ParticleSystem sparkleSystem;
        private Color[] grabColours = { Color.Red, Color.Orange, Color.Yellow };
        private ManipMode lastGesture;

        bool displaying = true;
        public bool DisplayingWithoutGesture {
            get { return displaying; }
            set { displaying = value; }
        }

        float arrowAlpha = 0;

        Vector2 lastOKArrowPos = Vector2.Zero;
        float lastOKArrowangle = 0;

        SpriteFont db;

        public float sparkleIntv = 0.1f;
        public float nextSparkle = 0;

        public WandCursor(Game game, SpriteBatch batch) {
            sparkleSystem = new ParticleSystem(game);
            sparkleSystem.Gravity = new Vector2(0, 10);
            sparkleSystem.Texture = game.Content.Load<Texture2D>("textures/particle");
            arrowTexture = game.Content.Load<Texture2D>("textures/arrow");
            db = game.Content.Load<SpriteFont>("debug");
            this.game = game;
            this.batch = batch;
        }

        public void Update(GameTime gameTime) {
            Game1 g1 = (Game1)game;

            if (displaying || g1.Gesture == ManipMode.GRAB || g1.Gesture == ManipMode.SELECTGRAB) {
                nextSparkle -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (nextSparkle < 0) {
                    sparkleSystem.DefaultColor = g1.Gesture != ManipMode.GRAB ? Color.Lime : grabColours[sparkleRandom.Next(0, grabColours.Length)];
                    var ranX = (float)(sparkleRandom.NextDouble() * 0.5 - 0.25);
                    sparkleSystem.AddInstance(((Game1)game).CursorPos, new Vector2(ranX, ranX + 1) * 20);
                    nextSparkle = (float)sparkleRandom.NextDouble() * sparkleIntv;
                }
            }
            sparkleSystem.Update(gameTime);

            if ((g1.Gesture == ManipMode.SELECT || g1.Gesture == ManipMode.SELECTGRAB) && lastGesture == ManipMode.NONE) {
                sparkleSystem.DefaultColor = Color.White;
                int pCount = sparkleRandom.Next(20, 50);
                for (int i = 0; i < pCount; i++) {
                    var ranX = (float)(sparkleRandom.NextDouble() * 0.5 - 0.25);
                    sparkleSystem.AddInstance(((Game1)game).CursorPos, 50);
                }
            }

            lastGesture = g1.Gesture;
        }

        public void Draw(GameTime gameTime) {

            batch.Begin();
            sparkleSystem.Draw(gameTime, batch);
            Vector2 cursorPos = ((Game1)game).CursorPos;

            if (cursorPos.X < 0 || cursorPos.X > batch.GraphicsDevice.Viewport.Width || cursorPos.Y < 0 || cursorPos.Y > batch.GraphicsDevice.Viewport.Height) {
                arrowAlpha = MathHelper.Clamp(arrowAlpha + (float)gameTime.ElapsedGameTime.TotalSeconds, 0, 0.5f);
            } else {
                arrowAlpha = MathHelper.Clamp(arrowAlpha - (float)gameTime.ElapsedGameTime.TotalSeconds, 0, 0.5f);
            }
            Vector2 arrowPos = new Vector2(MathHelper.Clamp(cursorPos.X, arrowTexture.Height / 2, batch.GraphicsDevice.Viewport.Width - arrowTexture.Height / 2),
                MathHelper.Clamp(cursorPos.Y, arrowTexture.Height / 2, batch.GraphicsDevice.Viewport.Height - arrowTexture.Height / 2));
            Vector2 line = Vector2.Normalize(cursorPos - arrowPos);
            float angle = line.X != 0 && line.Y <= 0 ? -(float)Math.Acos(line.X) + MathHelper.PiOver2 : -(float)Math.Asin(line.Y) - MathHelper.PiOver2;
            if (line.X > 0 && line.Y > 0) angle = (float)Math.Acos(line.X) + MathHelper.PiOver2;

            if (arrowPos == cursorPos) {
                arrowPos = lastOKArrowPos;
                angle = lastOKArrowangle;
            } else {
                lastOKArrowPos = arrowPos;
                lastOKArrowangle = angle;
            }

            batch.Draw(arrowTexture, new Rectangle((int)arrowPos.X, (int)arrowPos.Y, arrowTexture.Width, arrowTexture.Height),
                arrowTexture.Bounds, new Color(arrowAlpha, arrowAlpha, arrowAlpha, arrowAlpha), angle, new Vector2(arrowTexture.Width / 2, arrowTexture.Height / 2), SpriteEffects.None, 1);

            batch.End();
        }
    }
}
