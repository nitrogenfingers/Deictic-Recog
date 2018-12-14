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
    class P1 : Trial
    {
        //The number of ghosts that need to be selected
        private int ghostsLeft = 18;

        public P1(Game game, Camera camera)
            : base(game, camera)
        {
            position = new Vector3(32, 50, -120);
            yrotation = MathHelper.Pi;
            type = TrialType.SELECT;

            this.camera = camera;

            switch (Game1.InputMode) {
                case InputMode.KINECT:
                    preTrialInstructions = "Perform selections by pointing your hand at the ghost and tapping your index finger.\n" + 
                        "Try pointing at different parts of the screen and tapping your finger to practice selecting.";
                    break;
                case InputMode.WAND:
                    preTrialInstructions = "Perform selections by pointing the techno-wand at the screen and pressing the \'A\' button.\n" + 
                        "Try pointing at different parts of the screen and pressing 'A' to practice selecting.";
                    break;
                case InputMode.MOUSE:
                    preTrialInstructions = "By tilting or moving the mouse, you can change the position of your cursor.\n" +
                        "To select, move your cursor over an object and click the left mouse button.\n" +
                        "Try moving your cursor and pressing the mouse button to practice selecting.";
                    break;
            }
            inTrialInstructions = "Select all the ghosts in this scene.";
            Game1.wandCursor.DisplayingWithoutGesture = true;
        }

        /// <summary>
        /// Constructs a new ghost in a random position, from range:
        /// X = [-40, -100]
        /// Y = [20, 70]
        /// Z = [-140, -130]
        /// Constructed ghosts start with alpha = 0 for a fade-in view.
        /// </summary>
        /// <returns>True if no more ghosts can be added, false otherwise</returns>
        private bool addGhost()
        {
            ghostsLeft--;
            if (ghostsLeft < 0) return true;

            Vector3 ghostPos = new Vector3(Game1.random.Next(-75, 120), Game1.random.Next(20, 90), Game1.random.Next(-300, -275));
            Ghost ghost = new Ghost(game, ghostPos, camera);

            selectableList.Insert(0, ghost);
            return false;
        }

        /// <summary>
        /// Constructs a new ghost in a chosen position.
        /// Constructed ghosts start with alpha = 0 for a fade-in view.
        /// </summary>
        /// <param name="position">The position for the ghost to be displayed</param>
        /// <returns>True if no more ghosts can be added, false otherwise</returns>
        private bool addGhost(Vector3 ghostPos)
        {
            ghostsLeft--;
            if (ghostsLeft < 0) return true;

            Ghost ghost = new Ghost(game, ghostPos, camera);

            selectableList.Insert(0, ghost);
            return false;
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
                    addGhost();
                    renderRequested = true;
                }
                return;
            }

            if ((Gesture == ManipMode.SELECT || Gesture == ManipMode.SELECTGRAB) && LastGesture == ManipMode.NONE)
            {

                Rectangle source = new Rectangle((int)(CursorPos.X - Game1.cursorArea / 2), (int)(CursorPos.Y - Game1.cursorArea / 2), (int)Game1.cursorArea, (int)Game1.cursorArea);
                Color[] retrieved = new Color[(int)(Game1.cursorArea * Game1.cursorArea)];

                try
                {
                    bool selectSuccessful = false;
                    flatRender.GetData<Color>(0, source, retrieved, 0, (int)(Game1.cursorArea * Game1.cursorArea));
                    //We increment by 2 as an optimization measure, as those values are going to be pretty consistent.
                    for (int i = 0; i < retrieved.Length; i+=2) {
                        if (retrieved[i] == this.selectionColor) {
                            selectableList.RemoveAt(0);
                            this.complete = addGhost();
                            renderRequested = true;
                            selectSuccessful = true;
                            break;
                        }
                    }
                    if (selectSuccessful) {
                        DataLogger.Environment = LoggingEnvironment.YESSELECT;
                    } else {
                        DataLogger.Environment = LoggingEnvironment.NOSELECT;
                    }
                }
                catch (ArgumentException)
                {
                    Console.Error.WriteLine("Mouse Click outside Bounds!");
                }
            }
            base.Update(gameTime);
        }
    }
}
