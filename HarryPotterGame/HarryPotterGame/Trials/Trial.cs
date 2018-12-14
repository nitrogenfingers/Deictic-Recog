using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HarryPotterGame.Entities;

namespace HarryPotterGame.Trials
{
    /// <summary>
    /// SELECT: The user needs to perform a section gesture on the screen at a selectable object.
    /// GRAB: The user needs to perform a grab gesture on a selectable object, and move them to a draggable object.
    /// DRAG: The user needs to perform a grab gesture on a selectable object, move them over a draggable object and perform a drop gesture.
    /// </summary>
    public enum TrialType
    {
        SELECT, GRAB, DRAGDROP
    }

    public class Trial {
        /// <summary>
        /// To keep track of the game state
        /// PRE: 
        /// </summary>
        protected enum TrialState {
            PRE, INT, POST
        }

        protected struct TrialWaypoint {
            Vector3 position;
            float facing;
            Door doorToOpen;

            public TrialWaypoint(Vector3 position, float facing, Door doorToOpen) {
                this.position = position;
                this.facing = facing;
                this.doorToOpen = doorToOpen;
            }
        }

        //This is the game object.
        protected Game game;
        //This is the camera object
        protected Camera camera;
        //The position the player will need to begin in order to start the challenge
        protected Vector3 position;
        public Vector3 Position { get { return position; } }
        //The yrotation on the camera
        protected float yrotation;
        public float YRotation { get { return yrotation; } }
        //What kind of trial is being performed
        protected TrialType type;
        //A copy of the flat colour screen render, to be set by the Game object on request (flag)
        protected Texture2D flatRender;
        public Texture2D FlatRender { set { flatRender = value; renderRequested = false; } }
        //A flag to indicate a flat render is needed. At the earliest opportunity, a flat render should be set-
        //this will deactivate the flag.
        protected bool renderRequested = false;
        public bool RenderRequested { get { return renderRequested; } }
        //Whether or not the trial is currently complete
        protected bool complete = false;
        public bool Complete { get { return complete; } }

        //Our flat render constants
        protected Color selectionColor = Color.Red;
        protected Color dragColor = Color.Blue;
        protected Color sceneryColor = Color.Lime;
        protected Color otherColor = Color.Black;

        //A debug effect
        BasicEffect debugEffect;

        //The list of selectable objects
        protected List<Entity> selectableList = new List<Entity>();
        //The list of objects onto which selectable objects can be dragged
        protected List<Entity> draggableList = new List<Entity>();
        //A list of scenery objects that are part of the scenario but unimportant
        protected List<Entity> sceneryList = new List<Entity>();

        //The cursor pos. This is updated every call by extracting it from the Game
        private Vector2 cursorPos;
        protected Vector2 CursorPos{ get { return cursorPos; } }
        //The current gesture. Updated each call, extracted from game
        private ManipMode gesture, lastGesture;
        protected ManipMode Gesture { get { return gesture; } }
        protected ManipMode LastGesture { get { return lastGesture; } }
        
        //The instructions for this trial. They are drawn before the trial begins
        protected string preTrialInstructions;
        //The in-trial instructions. These are drawn while the trial is being run (at all times)
        protected string inTrialInstructions;
        //The state of the trial
        protected TrialState trialState = TrialState.PRE;
        //The text alpha- it fades in and out
        protected float trialTextAlpha;
        //The sprite font. Shared between trials
        protected static SpriteFont displayFont;
        public static void InitializeFont(Game game) { displayFont = game.Content.Load<SpriteFont>("display"); }

        //The current waypoint the player is moving towards

        /// <summary>
        /// Constructs the trial. Necessary parameters should be overridden here
        /// </summary>
        /// <param name="game">The game object</param>
        public Trial(Game game, Camera camera) {
            this.game = game;
            this.camera = camera;

            debugEffect = new BasicEffect(game.GraphicsDevice);
        }

        /// <summary>
        /// Performs updates on the trial to determine whether or not it has been successfully completed or not.
        /// </summary>
        /// <param name="gameTime">The elapsed time since the previous call</param>
        public virtual void Update(GameTime gameTime)
        {
            cursorPos = ((Game1)game).CursorPos;
            lastGesture = gesture;
            gesture = ((Game1)game).Gesture;

            foreach (Entity entity in selectableList) entity.Update(gameTime);
            foreach (Entity entity in draggableList) entity.Update(gameTime);
            foreach (Entity entity in sceneryList) entity.Update(gameTime);
        }

        /// <summary>
        /// Draws necessary components on the screen. As many components may use transparency and other effects, it is recommended this
        /// is the LAST performed draw call. Dynamic lighting will not be used.
        /// </summary>
        /// <param name="gameTime">The elapsed time since the previous call</param>
        public virtual void Draw(GameTime gameTime)
        {
            foreach (Entity e in selectableList) e.Draw(gameTime);
            foreach (Entity e in draggableList) e.Draw(gameTime);
            foreach (Entity e in sceneryList) e.Draw(gameTime);

            SpriteBatch sb = ((Game1)game).spriteBatch;
            sb.Begin();
            if (trialState == TrialState.PRE) {
                sb.DrawString(displayFont, preTrialInstructions, new Vector2((int)(sb.GraphicsDevice.Viewport.Width / 2 - displayFont.MeasureString(preTrialInstructions).X / 2), 50), Color.White);
            } else if (trialState == TrialState.INT) {
                sb.DrawString(displayFont, inTrialInstructions, new Vector2(sb.GraphicsDevice.Viewport.Width / 2 - displayFont.MeasureString(inTrialInstructions).X / 2, 50), Color.White);
            }
            sb.End();
        }

        /// <summary>
        /// Draws to a flat colour render, for detection of appropriate pixels
        /// </summary>
        /// <param name="gameTime">The elapsed time since the previous call</param>
        public virtual void DrawFlatRender(GameTime gameTime)
        {
            for (int i = 0; i < selectableList.Count; i++)
            {
                if (i == 0)
                    selectableList[i].DrawFlatRender(gameTime, selectionColor);
                else
                    selectableList[i].DrawFlatRender(gameTime, otherColor);
            }

            for (int i = 0; i < draggableList.Count; i++)
            {
                if (i == 0)
                    draggableList[i].DrawFlatRender(gameTime, dragColor);
                else
                    draggableList[i].DrawFlatRender(gameTime, otherColor);
            }

            foreach (Entity e in sceneryList)
                e.DrawFlatRender(gameTime, sceneryColor);
        }

        /// <summary>
        /// Just displays some debug info
        /// </summary>
        /// <param name="gameTime">The game time</param>
        /// <param name="batch">The sprite batch</param>
        public void DrawDebug(GameTime gameTime, SpriteBatch batch, SpriteFont font)
        {
            if(draggableList.Count > 0){
                VertexPositionColor[] ray = new VertexPositionColor[2];

                Ray camRay = new Ray(camera.Position, Vector3.Normalize(camera.Position - draggableList[0].position));

                ray[0] = new VertexPositionColor(position, Color.Red);
                ray[1] = new VertexPositionColor(draggableList[0].position, Color.Red);

                debugEffect.VertexColorEnabled = true;
                debugEffect.View = camera.GetViewMatrix();
                debugEffect.Projection = camera.GetProjectionMatrix();

                foreach(EffectPass pass in debugEffect.CurrentTechnique.Passes){
                    pass.Apply();
                }

                game.GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, ray, 0, 1);
            }

            for (int i = 0; i < selectableList.Count + draggableList.Count; i++)
            {
                Entity e = i < selectableList.Count ? selectableList[i] : draggableList[i - selectableList.Count];
                Color stringcol = e.selected ? Color.Green : Color.Red;
                batch.DrawString(font, e.GetType().ToString() /*+ ": " + e.position + " R= " + e.yrotation*/, new Vector2(0, 40 + 20 * i), stringcol);

                Vector3 sspos = game.GraphicsDevice.Viewport.Project(e.position, camera.GetProjectionMatrix(), camera.GetViewMatrix(), e.world);

                batch.DrawString(font, "O", new Vector2(sspos.X, sspos.Y), Color.Green);
                batch.DrawString(font, sspos.ToString(), new Vector2(400, 40 + 20 * i), stringcol);
            }

            batch.DrawString(font, "Gesture : " + gesture.ToString(), new Vector2(0, 200), Color.White);
        }
    }
}
