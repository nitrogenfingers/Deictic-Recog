using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using HarryPotterGame.Entities;
using HarryPotterGame;

namespace HarryPotterGame.Trials
{
    class D3 : Trial
    {
        //The number of fireballs to be blocked
        private int fireballsLeft = 6;
        //The ratio of distance between the key and the player
        private float ratio;

        private MouseState lms;
        public D3(Game game, Camera camera)
            : base(game, camera)
        {
            position = new Vector3(348, 245, 245);

            Entity entity = new Warlock(game, new Vector3(348, 222, 600), camera);
            entity.yrotation = MathHelper.PiOver2;
            sceneryList.Add(entity);

            Entity shield = new Shield(game, new Vector3(348, 250, 310), camera);
            shield.yrotation = -MathHelper.PiOver2;
            shield.scale = 2.5f;
            selectableList.Add(shield);

            renderRequested = true;
        }

        private void AddDragPos()
        {
            Vector3 dpos = new Vector3(Game1.random.Next(330, 370), Game1.random.Next(220, 280), 310);
            AddDragPos(dpos);
        }

        private void AddDragPos(Vector3 dppos)
        {
            Entity dp = new GEntity(game, dppos, camera, "models/fireflies", null);
            draggableList.Add(dp);
        }

        /// <summary>
        /// The update checks for successful ghost selection against the selection render. Successful ones are hidden, and a new one is displayed.
        /// New renders are made each time a new ghost appears on the screen.
        /// </summary>
        /// <param name="gameTime">The game time</param>
        public override void Update(GameTime gameTime)
        {
            MouseState ms = Mouse.GetState();

            if (selectableList[0].selected)
            {
                Ray ray = XNAHelper.CalculateCursorRay(new Vector2(ms.X, ms.Y), camera.GetProjectionMatrix(), camera.GetViewMatrix(), game.GraphicsDevice);
                selectableList[0].position = Vector3.Lerp(selectableList[0].position, ray.Position + (ray.Direction * ratio), (float)gameTime.ElapsedGameTime.TotalSeconds * 8);
                selectableList[0].position.Y = MathHelper.Clamp(selectableList[0].position.Y, 220, 280);
                selectableList[0].position.X = MathHelper.Clamp(selectableList[0].position.X, 330, 370);
                selectableList[0].position.Z = 310;

                Ray camRay = new Ray(camera.Position, Vector3.Normalize(draggableList[0].position - camera.Position));
                /*
                if ((selectableList[0]).Intersects(camRay))
                {
                    selectableList[0].selected = false;
                    selectableList[0].position = new Vector3(selectableList[0].position.X < midp ? Game1.random.Next(rightp, midp - 1) : Game1.random.Next(midp + 1, leftp), 25, 65);
                    selectableList[0].yrotation = 0;
                    ((GEntityaggableList[0]).standardColor = null;
                    Entity e = draggableList[1];
                    draggableList.RemoveAt(1);
                    draggableList.Insert(0, e);
                    ((GEntity)draggableList[0]).standardColor = Color.Red;
                }*/
            }

            if (ms.LeftButton == ButtonState.Pressed && lms.LeftButton == ButtonState.Released)
            {
                Rectangle source = new Rectangle(Mouse.GetState().X, Mouse.GetState().Y, 1, 1);
                Color[] retrieved = new Color[1];
                try
                {
                    flatRender.GetData<Color>(0, source, retrieved, 0, 1);
                    Console.Out.WriteLine(retrieved[0].R + "," + retrieved[0].G + "," + retrieved[0].B);
                    if (retrieved[0] == this.selectionColor)
                    {
                        selectableList[0].selected = true;
                        Ray ray = XNAHelper.CalculateCursorRay(new Vector2(ms.X, ms.Y), camera.GetProjectionMatrix(), camera.GetViewMatrix(), game.GraphicsDevice);
                        ratio = Vector3.Distance(ray.Position, selectableList[0].position)
                            / Vector3.Distance(ray.Position, ray.Position + new Vector3(0, 0, ray.Direction.Z));
                        //((GEntity)selectableList[0]).invisibleNextRender = true;
                        renderRequested = true;
                        ((GEntity)selectableList[0]).waiting = true;
                    }
                }
                catch (ArgumentException)
                {
                    Console.Error.WriteLine("Mouse Click outside Bounds!");
                }
            }
            else if (ms.LeftButton == ButtonState.Released && lms.LeftButton == ButtonState.Pressed)
            {
                selectableList[0].selected = false;
                renderRequested = true;
            }

            lms = ms;
            base.Update(gameTime);
        }
    }
}
