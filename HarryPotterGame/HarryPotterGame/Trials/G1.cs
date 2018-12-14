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
    class G1 : Trial
    {
        //The number of ghosts that need to be selected
        private int keysLeft = 6;
        //The ratio of distance between the key and the player
        private float ratio;

        //The positions on the X axis that equate to the far left, middle and far right of the table.
        //For key placement.
        private int leftp = 15, midp = -10, rightp = -30;

        public G1(Game game, Camera camera)
            : base(game, camera)
        {
            position = new Vector3(-8, 37.5f, 30);
            yrotation = 0;
            type = TrialType.GRAB;

            GEntity loc = new GEntity(game, new Vector3(-62.5f, 55.75f, 185.5f), camera, "models/tavern/lock", "textures/brass_dark");
            loc.scale = 10;
            this.draggableList.Add(loc);
            loc = new GEntity(game, new Vector3(46f, 55.75f, 185.5f), camera, "models/tavern/lock", "textures/brass_dark");
            loc.scale = 10;
            this.draggableList.Add(loc);

            Door door = new Door(game, camera, new Vector3(-47.5f, 5.75f, 185.5f), 0);
            door.scale = 10;
            this.sceneryList.Add(door);
            door = new Door(game, camera, new Vector3(31f, 5.75f, 185.5f), 0);
            door.scale = 10;
            this.sceneryList.Add(door);
            sceneryList.Insert(0, new GEntity(game, new Vector3(-20, 5, 75), camera, "models/tavern/table", "textures/tavern/table_color"));
            sceneryList[0].scale = 3;


            switch (Game1.InputMode) {
                case InputMode.KINECT:
                    preTrialInstructions = "Once an object has been selected, it can be grabbed by\n" + "      forming a fist with your hand, and dragged.\n" +
                        "Practice peforming the drag gesture.";
                    break;
                case InputMode.WAND:
                    preTrialInstructions = "Once an object has been selected, it can be grabbed by\n" + "      holding the B button, and dragged.\n" +
                        "Practice peforming the drag gesture.";
                    break;
                case InputMode.MOUSE:
                    preTrialInstructions = "Once an object has been selected, it can be grabbed by\n" + "      clicking the right mouse button, and dragged.\n" +
                        "Practice peforming the drag gesture.";
                    break;
            }
            inTrialInstructions = "Select each key that appears, and drag it to the coloured lock.";
            Game1.wandCursor.DisplayingWithoutGesture = true;
            this.camera = camera;
        }

        /// <summary>
        /// The update checks for successful ghost selection against the selection render. Successful ones are hidden, and a new one is displayed.
        /// New renders are made each time a new ghost appears on the screen.
        /// </summary>
        /// <param name="gameTime">The game time</param>
        public override void Update(GameTime gameTime)
        {
            if (trialState == TrialState.PRE) {
                if (Keyboard.GetState().IsKeyDown(Keys.Enter)) {
                    Game1.wandCursor.DisplayingWithoutGesture = false;
                    trialState = TrialState.INT;
                    renderRequested = true;

                    selectableList.Clear();
                    Key key = new Key(game, new Vector3(leftp - 5, 25, 65), camera);
                    key.scale = 2f;
                    key.xrotation = -MathHelper.PiOver2;
                    key.selectedColor = Color.Yellow;
                    selectableList.Add(key);
                    ((GEntity)draggableList[0]).standardColor = Color.Red;
                }
                return;
            }

            MouseState ms = Mouse.GetState();

            if ((Gesture == ManipMode.SELECT || Gesture == ManipMode.SELECTGRAB) && LastGesture == ManipMode.NONE){
                Rectangle source = new Rectangle((int)(CursorPos.X - Game1.cursorArea / 2), (int)(CursorPos.Y - Game1.cursorArea / 2), (int)Game1.cursorArea, (int)Game1.cursorArea);
                Color[] retrieved = new Color[(int)(Game1.cursorArea * Game1.cursorArea)];

                try
                {
                    flatRender.GetData<Color>(0, source, retrieved, 0, (int)(Game1.cursorArea * Game1.cursorArea));
                    bool selectSuccessful = false;
                    //We increment by 2 as an optimization measure, as those values are going to be pretty consistent.
                    for (int i = 0; i < retrieved.Length; i += 2) {
                        if (retrieved[i] == this.selectionColor) {
                            selectableList[0].selected = true;
                            Ray ray = XNAHelper.CalculateCursorRay(CursorPos, camera.GetProjectionMatrix(), camera.GetViewMatrix(), game.GraphicsDevice);
                            ratio = Vector3.Distance(ray.Position, selectableList[0].position)
                                / Vector3.Distance(ray.Position, ray.Position + new Vector3(0, 0, ray.Direction.Z));
                            //((GEntity)selectableList[0]).invisibleNextRender = true;
                            renderRequested = true;
                            selectSuccessful = true;
                            ((GEntity)selectableList[0]).waiting = true;
                        }
                    }
                    if (selectSuccessful) {
                        DataLogger.Environment = LoggingEnvironment.YESSELECT;
                    } else {
                        DataLogger.Environment = LoggingEnvironment.NOSELECT;
                    }
                } catch (ArgumentException) {
                    Console.Error.WriteLine("Mouse Click outside Bounds!");
                }
            }
            
            if ((Gesture == ManipMode.GRAB || Gesture == ManipMode.SELECTGRAB) && selectableList[0].selected){
                Ray ray = XNAHelper.CalculateCursorRay(CursorPos, camera.GetProjectionMatrix(), camera.GetViewMatrix(), game.GraphicsDevice);
                float zFixed = selectableList[0].position.Z;
                selectableList[0].position = Vector3.Lerp(selectableList[0].position, ray.Position + (ray.Direction * ratio), (float)gameTime.ElapsedGameTime.TotalSeconds * 8);
                selectableList[0].position.Y = MathHelper.Clamp(selectableList[0].position.Y, 23, 80);
                selectableList[0].position.Z = zFixed;

                Ray camRay = new Ray(camera.Position, Vector3.Normalize(draggableList[0].position - camera.Position));

                if (((Key)selectableList[0]).Intersects(camRay)) {
                    selectableList[0].selected = false;
                    selectableList[0].position = new Vector3(selectableList[0].position.X < midp ? Game1.random.Next(rightp, midp - 1) : Game1.random.Next(midp + 1, leftp), 25, 65);
                    selectableList[0].yrotation = 0;
                    ((GEntity)draggableList[0]).standardColor = null;
                    Entity e = draggableList[1];
                    draggableList.RemoveAt(1);
                    draggableList.Insert(0, e);
                    ((GEntity)draggableList[0]).standardColor = Color.Red;
                    DataLogger.Environment = LoggingEnvironment.YESDRAG;

                    keysLeft--;
                    if (keysLeft == 0) complete = true;
                }
            }
            
            if (Gesture == ManipMode.NONE && (LastGesture == ManipMode.GRAB || LastGesture == ManipMode.SELECTGRAB))
            {
                renderRequested = true;
                if (selectableList[0].selected) {
                    DataLogger.Environment = LoggingEnvironment.NODROP;
                }
            }

            base.Update(gameTime);
        }
    }
}
