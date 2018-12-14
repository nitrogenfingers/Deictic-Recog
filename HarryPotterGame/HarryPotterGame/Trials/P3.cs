using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using HarryPotterGame.Entities;

namespace HarryPotterGame.Trials
{
    class P3 : Trial
    {
        //The number of ghosts that need to be selected
        private int warlocksLeft = 9;
        //The last mouse state
        private MouseState lms;

        public P3(Game game, Camera camera)
            : base(game, camera)
        {
            position = new Vector3(329, 150, -23);
            yrotation = MathHelper.Pi/5f;
            type = TrialType.SELECT;

            this.camera = camera;
            addWarlock(new Vector3(500, 120, 120));
            renderRequested = true;
        }

        /// <summary>
        /// Constructs a new ghost in a random position, from range:
        /// X = [-40, -100]
        /// Y = [20, 70]
        /// Z = [-140, -130]
        /// Constructed ghosts start with alpha = 0 for a fade-in view.
        /// </summary>
        /// <returns>True if no more ghosts can be added, false otherwise</returns>
        private bool addWarlock()
        {
            Vector3 warlockPos = Vector3.Zero;
            var area = Game1.random.Next(1, 5);
            switch (area)
            {
                case 1:
                case 2:
                    warlockPos = new Vector3(Game1.random.Next(329, 455), 120, Game1.random.Next(10, 144));
                    break;
                case 3:
                    warlockPos = new Vector3(Game1.random.Next(315, 515), 120, Game1.random.Next(84, 145));
                    break;
                case 4:
                    warlockPos = new Vector3(Game1.random.Next(315, 515), 120, Game1.random.Next(5, 85));
                    break;
            }
            return addWarlock(warlockPos);
        }

        /// <summary>
        /// Constructs a new ghost in a chosen position.
        /// Constructed ghosts start with alpha = 0 for a fade-in view.
        /// </summary>
        /// <param name="position">The position for the ghost to be displayed</param>
        /// <returns>True if no more ghosts can be added, false otherwise</returns>
        private bool addWarlock(Vector3 warlockPos)
        {
            warlocksLeft--;
            if (warlocksLeft < 0) return true;

            Warlock warlock = new Warlock(game, warlockPos, camera);

            Vector2 toCamera = Vector2.Subtract(new Vector2(camera.Position.X, camera.Position.Z), new Vector2(warlock.position.X, warlock.position.Z));
            Vector2 facing = new Vector2(warlock.position.X + MathHelper.Pi, warlock.position.Z);

            toCamera.Normalize();
            facing.Normalize();

            warlock.yrotation = (float)Math.Acos(Vector2.Dot(toCamera, facing));

            selectableList.Insert(0, warlock);
            return false;
        }

        /// <summary>
        /// The update checks for successful ghost selection against the selection render. Successful ones are hidden, and a new one is displayed.
        /// New renders are made each time a new ghost appears on the screen.
        /// </summary>
        /// <param name="gameTime">The game time</param>
        public override void Update(GameTime gameTime)
        {
            MouseState ms = Mouse.GetState();

            if (ms.LeftButton == ButtonState.Pressed && lms.LeftButton == ButtonState.Released)
            {
                Rectangle source = new Rectangle((int)CursorPos.X, (int)CursorPos.Y, 1, 1);
                Color[] retrieved = new Color[1];
                try
                {
                    flatRender.GetData<Color>(0, source, retrieved, 0, 1);
                    Console.Out.WriteLine(retrieved[0].R + "," + retrieved[0].G + "," + retrieved[0].B);
                    if (retrieved[0] == this.selectionColor)
                    {
                        selectableList.RemoveAt(0);
                        this.complete = addWarlock();
                        renderRequested = true;
                    }
                }
                catch (ArgumentException)
                {
                    Console.Error.WriteLine("Mouse Click outside Bounds!");
                }
            }

            lms = ms;
            base.Update(gameTime);
        }
    }
}
