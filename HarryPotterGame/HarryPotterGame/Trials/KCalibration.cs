using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using HarryPotterGame.Entities;

namespace HarryPotterGame.Trials {
    class KCalibration:Trial {
        public struct State {
            public string message;

            public State(string message) {
                this.message = message;
            }
        }

        public List<State> states = new List<State>();
        public List<Vector2> results = new List<Vector2>();
        int currState = 0;
        private MouseState lms;

        public KCalibration(Game game, Camera camera)
            : base(game, camera) {
                position = new Vector3(0, 50, 0);

                switch (Game1.InputMode) {
                    case InputMode.KINECT:
                        states.Add(new State("Point to the top left corner of the screen and bend your index finger"));
                        states.Add(new State("Point to the top right corner of the screen and bend your index finger"));
                        states.Add(new State("Point to the bottom right corner of the screen and bend your index finger"));
                        states.Add(new State("Point to the bottom left corner of the screen and bend your index finger"));
                        break;
                    case InputMode.WAND:
                        states.Add(new State("Point to the top left corner of the screen and press the A button"));
                        states.Add(new State("Point to the top right corner of the screen and press the A button"));
                        states.Add(new State("Point to the bottom right corner of the screen and press the A button"));
                        states.Add(new State("Point to the bottom left corner of the screen and press the A button"));
                        break;
                }
        }

        public override void Update(GameTime gameTime) {

            if (Gesture == ManipMode.SELECT && LastGesture == ManipMode.NONE) {
                    Vector3 bray = ((Game1)game).kAccess.BoneRay;
                    results.Add(new Vector2(bray.X, bray.Y));
                    currState++;
            }

            if (currState >= states.Count) {
                ((Game1)game).kAccess.SetWinLimits(results[0], results[1], results[2], results[3]);

                complete = true;
            }

            base.Update(gameTime);
        }
        
        public override void Draw(GameTime gameTime) {
            SpriteBatch sb = ((Game1)game).spriteBatch;

            sb.Begin();
            sb.DrawString(((Game1)game).debug, states[currState].message, new Vector2(100, 100), Color.White);
            sb.End();

            GraphicsDevice gd = game.GraphicsDevice;
            DepthStencilState dss = new DepthStencilState();
            dss.DepthBufferEnable = true;
            gd.DepthStencilState = dss;
        }
    }
}
