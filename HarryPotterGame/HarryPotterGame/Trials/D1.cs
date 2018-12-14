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
    class D1 : Trial
    {
        //The number of fireflies to catch
        private const int fireflyLimit = 6;
        //The ratio of distance between the key and the player
        private float ratio;

        //The limits on the Y and Z placement of fireflies (the X is of course fixed)
        private const int ymin = 120, ymax = 180;
        private const int zmin = 320, zmax = 375;
        private const int xFixed = -476;

        public D1(Game game, Camera camera)
            : base(game, camera)
        {
            position = new Vector3(-391, 140, 345);
            yrotation = -MathHelper.PiOver2;
            type = TrialType.DRAGDROP;

            GEntity cage = new Cage(game, new Vector3(-476, 116, 326.5f), camera);
            cage.yrotation = MathHelper.Pi;
            cage.scale = 8;
            draggableList.Add(cage);

            for (int i = 0; i < fireflyLimit; i++)
            {
                GEntity firefly = new Firefly(game, new Vector3(-476, Game1.random.Next(ymin, ymax), Game1.random.Next(zmin, zmax)), camera);
                firefly.scale = 4;
                firefly.standardColor = i == 0 ? Color.Orange : Color.Yellow;
                firefly.selectedColor = Color.Red;
                selectableList.Add(firefly);
            }

            trialState = TrialState.INT;
            inTrialInstructions = "Select the orange firefly, and drag it into the cage.";
            Game1.wandCursor.DisplayingWithoutGesture = false;
            renderRequested = true;
        }

        /// <summary>
        /// The update checks for successful ghost selection against the selection render. Successful ones are hidden, and a new one is displayed.
        /// New renders are made each time a new ghost appears on the screen.
        /// </summary>
        /// <param name="gameTime">The game time</param>
        public override void Update(GameTime gameTime)
        {
            MouseState ms = Mouse.GetState();

            if (selectableList[0].selected && (Gesture == ManipMode.GRAB || Gesture == ManipMode.SELECTGRAB))
            {
                Ray ray = XNAHelper.CalculateCursorRay(CursorPos, camera.GetProjectionMatrix(), camera.GetViewMatrix(), game.GraphicsDevice);
                selectableList[0].position = Vector3.Lerp(selectableList[0].position, ray.Position + (ray.Direction * ratio), (float)gameTime.ElapsedGameTime.TotalSeconds * 8);
                selectableList[0].position.Y = MathHelper.Clamp(selectableList[0].position.Y, ymin, ymax);
                selectableList[0].position.Z = MathHelper.Clamp(selectableList[0].position.Z, zmin, zmax);
                selectableList[0].position.X = xFixed;
            }

            if ((Gesture == ManipMode.SELECT || Gesture == ManipMode.SELECTGRAB) && LastGesture == ManipMode.NONE)
            {
                Rectangle source = new Rectangle((int)(CursorPos.X - Game1.cursorArea / 2), (int)(CursorPos.Y - Game1.cursorArea / 2), (int)Game1.cursorArea, (int)Game1.cursorArea);
                Color[] retrieved = new Color[(int)(Game1.cursorArea * Game1.cursorArea)];
                bool selectionSuccessful = false;

                try {
                    flatRender.GetData<Color>(0, source, retrieved, 0, (int)(Game1.cursorArea * Game1.cursorArea));
                    //We increment by 2 as an optimization measure, as those values are going to be pretty consistent.
                    for (int i = 0; i < retrieved.Length; i += 2) {
                        if (retrieved[i] == this.selectionColor) {
                            selectableList[0].selected = true;
                            selectionSuccessful = true;
                            Ray ray = XNAHelper.CalculateCursorRay(new Vector2(ms.X, ms.Y), camera.GetProjectionMatrix(), camera.GetViewMatrix(), game.GraphicsDevice);
                            ratio = Vector3.Distance(ray.Position, selectableList[0].position)
                                / Vector3.Distance(ray.Position, ray.Position + new Vector3(ray.Direction.X, 0, 0));
                            ((GEntity)selectableList[0]).waiting = true;
                        }
                    }
                    if (selectionSuccessful) {
                        DataLogger.Environment = LoggingEnvironment.YESSELECT;
                    } else {
                        DataLogger.Environment = LoggingEnvironment.NOSELECT;
                    }
                } catch (ArgumentException) {
                    Console.Error.WriteLine("Mouse Click outside Bounds!");
                }
            }
            
            if (Gesture == ManipMode.NONE && LastGesture != ManipMode.NONE)
            {
                if (((Cage)draggableList[0]).Contains(selectableList[0].position)) {
                    selectableList[0].selected = false;
                    sceneryList.Insert(0, selectableList[0]);
                    selectableList.RemoveAt(0);
                    ((Firefly)sceneryList[0]).standardColor = Color.Yellow;
                    ((Firefly)sceneryList[0]).RemoveGlow();
                    DataLogger.Environment = LoggingEnvironment.YESDROP;
                    if (selectableList.Count == 0) {
                        complete = true;
                    } else
                        ((Firefly)selectableList[0]).standardColor = Color.Orange;
                } else {
                    DataLogger.Environment = LoggingEnvironment.NODROP;
                }
                renderRequested = true;
            }

            base.Update(gameTime);
        }
    }
}
