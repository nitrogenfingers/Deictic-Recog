using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using WiimoteLib;

using HarryPotterGame.Entities;
using HarryPotterGame.Trials;
using HarryPotterGame.Input;

namespace HarryPotterGame
{
    /// <summary>
    /// The kind of interface being used:
    /// - MOUSE: This allows the use of any standard mouse input. It uses the screen cursor position within the window, 
    /// and is the only mode where the cursor's position is visible to the user.
    /// - KINECT: Uses the Kinect input combined with the mouse to determine gestures and positions. 
    /// </summary>
    public enum InputMode { KINECT, MOUSE, WAND }
    public enum ManipMode { NONE, SELECT, GRAB, SELECTGRAB }
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        //The static random number generator. USE ONLY THIS!!!!
        public static Random random = new Random(1);

        GraphicsDeviceManager graphics;
        public SpriteBatch spriteBatch;
        Cloister cloister;
        RotatingCamera camera;
        private float camrotation = 0;
        private float speed = 2f;
        private float rSpeed = 0.035f;
        private float baseY = 50;
        public SpriteFont debug;
        KeyboardState lastState;
        MouseState lastMouseState;
        WiimoteState lastWMState;
        RasterizerState fillRaster, wireRaster;

        bool inSeq = true;
        int seqIt = -1;
        string[] trialSequences;

        public KinectAccess kAccess;
        public WiimoteAccess wAccess;
        Vector2 cursorPos;
        public static WandCursor wandCursor;
        ManipMode gesture;
        public Vector2 CursorPos { get { return cursorPos; } }
        public ManipMode Gesture { get { return gesture; } }
        public string accessDirectory = null;

        /** EDIT HERE **/

        private static InputMode inputMode = InputMode.MOUSE;
        public static InputMode InputMode { get { return inputMode; } }
 
        public string username = "DefUser";
        public bool collectingData = true;

        //Smoothing to help improve the "guessing" position of the cursor
        public const float cursorArea = 75;
        //A list of points kept. A constantly updating moving average is used
        private List<Vector2> averagePoints = new List<Vector2>();
        //The number of points the averagePoint list can carry
        private int pointCapacity = 8;

        String lastTarget = "none";
        Texture2D cursor;

        #region Render Targeting
        RenderTarget2D renderTarget;
        private List<Texture2D> fcrList = new List<Texture2D>();
        private int fcrIndex = 0;
        private bool displayingFCR = false;
        private int texNo = 1;
        #endregion

        List<Door> doorList = new List<Door>();
        List<Ghost> ghostList = new List<Ghost>();
        int selDoor = 0;

        private float camBobDuration = 0;
        
        //Trials
        Trial trial;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            graphics.IsFullScreen = false;

            var inc = 0;
            if (inputMode == InputMode.MOUSE) {
                trialSequences = new string[3];
            } else {
                trialSequences = new string[4];
                trialSequences[inc++] = "KCalibration";
            }
            trialSequences[inc++] = "P1";
            trialSequences[inc++] = "G1";
            trialSequences[inc++] = "D1";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();

            string inputm = "";
            do {
                Dialog.InputBox("Enter input mode", "Input mode:", ref inputm);
            } while (!Enum.TryParse<InputMode>(inputm, out inputMode));

            if (collectingData) {
                if (Dialog.InputBox("Enter user name", "User name:", ref username) == DialogResult.OK) {
                    try {
                        DateTime dt = DateTime.Now;
                        accessDirectory = username + inputMode.ToString() + " " + dt.Day + "-" + dt.Month + " " + dt.Hour + ";" + dt.Minute;
                        if (Directory.Exists(accessDirectory)) {
                            Console.Out.WriteLine("Directory exists");
                            this.Exit();
                            return;
                        }
                        DirectoryInfo di = Directory.CreateDirectory(accessDirectory);
                    } catch (Exception e) {
                        Console.Out.WriteLine("Failed! " + e.StackTrace);
                        this.Exit();
                        return;
                    }
                } else {
                    this.Exit();
                    return;
                }
            }

            kAccess = new KinectAccess(Content, GraphicsDevice);
            if (inputMode == InputMode.WAND) {
                wAccess = new WiimoteAccess();
                lastWMState = wAccess.GetState();
            }

            Console.Out.WriteLine("We got here");
            wandCursor = new WandCursor(this, spriteBatch);

            camera = new RotatingCamera(this, new Vector3(0, 50, 0), Vector3.Backward, Vector3.Up);
            camera.YRotation = 0;

            cloister = new Cloister(this, camera, Vector3.Zero);

            fillRaster = new RasterizerState();
            fillRaster.FillMode = FillMode.Solid;
            fillRaster.CullMode = CullMode.CullClockwiseFace;
            wireRaster = new RasterizerState();
            wireRaster.FillMode = FillMode.WireFrame;
            fillRaster.CullMode = CullMode.None;

            Door door = new Door(this, camera, new Vector3(32, 5.75f, -100), 0);
            door.scale = 10;
            doorList.Add(door); 
            door = new Door(this, camera, new Vector3(-47.5f, 5.75f, 185.5f), 0);
            door.scale = 10;
            doorList.Add(door);
            door = new Door(this, camera, new Vector3(31f, 5.75f, 185.5f), 0);
            door.scale = 10;
            doorList.Add(door);
            door = new Door(this, camera, new Vector3(-245, 101.5f, 349), -MathHelper.PiOver2);
            door.scale = 10;
            doorList.Add(door);
            door = new Door(this, camera, new Vector3(-374, 101.5f, 301.5f), MathHelper.Pi);
            door.scale = 10;
            doorList.Add(door);
            door = new Door(this, camera, new Vector3(-374, 101.5f, 195), MathHelper.Pi);
            door.scale = 10;
            doorList.Add(door);
            door = new Door(this, camera, new Vector3(-374, 101.5f, 88.5f), MathHelper.Pi);
            door.scale = 10;
            doorList.Add(door);
            door = new Door(this, camera, new Vector3(-374, 101.5f, -120), MathHelper.Pi);
            door.scale = 10;
            doorList.Add(door);
            door = new Door(this, camera, new Vector3(-374, 101.5f, -327), MathHelper.Pi);
            door.scale = 10;
            doorList.Add(door);
            door = new Door(this, camera, new Vector3(-320.25f, 101.5f, -380.5f), MathHelper.PiOver2);
            door.scale = 10;
            doorList.Add(door);
            door = new Door(this, camera, new Vector3(-12, 101.5f, -380.5f), MathHelper.PiOver2);
            door.scale = 10;
            doorList.Add(door);
            door = new Door(this, camera, new Vector3(296, 101.5f, -380.5f), MathHelper.PiOver2);
            door.scale = 10;
            doorList.Add(door);
            door = new Door(this, camera, new Vector3(350, 101.5f, -428.5f), MathHelper.Pi);
            door.scale = 10;
            doorList.Add(door);
            door = new Door(this, camera, new Vector3(348.75f, 101.5f, -327), 0);
            door.scale = 10;
            doorList.Add(door);
            door = new Door(this, camera, new Vector3(349.5f, 101.5f, 153.5f), MathHelper.Pi);
            door.scale = 10;
            doorList.Add(door);

            selDoor = doorList.Count - 1;
            Trial.InitializeFont(this);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            debug = Content.Load<SpriteFont>("debug");
            cursor = Content.Load<Texture2D>("textures/cursor");

            PresentationParameters pp = GraphicsDevice.PresentationParameters;
            renderTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, true, GraphicsDevice.DisplayMode.Format, DepthFormat.Depth24);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

#region TRIALS

        public void StartTrial(Type trialType)
        {
            Object[] param = {this, camera};
            try
            {
                trial = (Trial)Activator.CreateInstance(trialType, param);
                camera.Position = trial.Position;
                baseY = trial.Position.Y;
                camera.YRotation = camrotation = trial.YRotation;
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                var inner = tie.InnerException;
                Console.Error.WriteLine("Could not construct trial: " + inner.GetType().ToString() + "\n\"" + inner.Message + "\"");
            }
        }

        public void UpdateTrial(GameTime gameTime)
        {
            //Initial Data Logger State Stuff
            switch (Gesture) {
                case ManipMode.NONE: DataLogger.Environment = LoggingEnvironment.NOGESTURE; break;
                case ManipMode.GRAB: 
                case ManipMode.SELECTGRAB:
                    DataLogger.Environment = LoggingEnvironment.GRAB; break;
            }

            trial.Update(gameTime);
            if (trial.RenderRequested) {
                CreateFlatRender(gameTime);
                trial.FlatRender = fcrList[fcrIndex];
            }
            if (trial.Complete)
            {
                if (inSeq) {
                    seqIt++;
                    if (seqIt >= trialSequences.Length) {
                        this.Exit();
                        return;
                    }
                    StartTrial(Type.GetType("HarryPotterGame.Trials." +
                        trialSequences[seqIt]));
                    if (trial.RenderRequested) {
                        CreateFlatRender(gameTime);
                        trial.FlatRender = fcrList[fcrIndex];
                    }
                } else {
                    camera.Position = new Vector3(0, 50, 0);
                    camera.YRotation = 0;
                }
            }
        }

#endregion

        protected override void OnExiting(object sender, EventArgs args)
        {
            if (collectingData) {
                DataLogger.Close();
                for (int i = 0; i < fcrList.Count; i++) {
                    Console.Out.WriteLine("Saving as srender" + i);
                    fcrList[i].SaveAsPng(File.OpenWrite(accessDirectory + "/srender" + i + ".png"), fcrList[i].Width, fcrList[i].Height);
                }
            }
            
            base.OnExiting(sender, args);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime) {


            if (!DataLogger.Initialized && collectingData)
                DataLogger.Initialize(username, accessDirectory, this, gameTime);
                
            if (kAccess != null) kAccess.Update(gameTime);
            if (wAccess != null) wAccess.Update(gameTime);


            #region Damper
            /*
            float damper = 1f; 
            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
            {
                damper = 0.5f;
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
            {
                damper = 0.25f;
            }*/
            #endregion

            #region Door stuff
            /**
            if (Keyboard.GetState().IsKeyDown(Keys.Enter) && lastState.IsKeyUp(Keys.Enter))
            {
                Ghost ghost = new Ghost(this, new Vector3(camera.Position.X + (3 * (float)Math.Sin(camrotation)),
                    baseY, camera.Position.Z + (3 * (float)Math.Cos(camrotation))), camera);

                ghost.scale = 10;
                ghostList.Add(ghost);
            }
            if (Keyboard.GetState().IsKeyDown(Keys.R) && doorList.Count > 0)
            {
                Door d = doorList[selDoor];
                d.rotation += 0.015f * damper;
                d.rotation %= MathHelper.TwoPi;
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.T) && doorList.Count > 0)
            {
                Door d = doorList[selDoor];
                d.rotation -= 0.015f * damper;
                d.rotation %= MathHelper.TwoPi;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.W) && doorList.Count > 0)
            {
                Door d = doorList[selDoor];
                d.position = new Vector3(d.position.X, d.position.Y, d.position.Z + speed/2 * damper);
                
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.S) && doorList.Count > 0)
            {
                Door d = doorList[selDoor];
                d.position = new Vector3(d.position.X, d.position.Y, d.position.Z - speed/2 * damper);

            }
            if (Keyboard.GetState().IsKeyDown(Keys.A) && doorList.Count > 0)
            {
                Door d = doorList[selDoor];
                d.position = new Vector3(d.position.X + speed/2 * damper, d.position.Y, d.position.Z);

            }
            else if (Keyboard.GetState().IsKeyDown(Keys.D) && doorList.Count > 0)
            {
                Door d = doorList[selDoor];
                d.position = new Vector3(d.position.X - speed/2 * damper, d.position.Y, d.position.Z);

            }
            if (Keyboard.GetState().IsKeyDown(Keys.Z) && doorList.Count > 0)
            {
                Door d = doorList[selDoor];
                d.position = new Vector3(d.position.X, d.position.Y - speed/2 * damper, d.position.Z);

            }
            else if (Keyboard.GetState().IsKeyDown(Keys.X) && doorList.Count > 0)
            {
                Door d = doorList[selDoor];
                d.position = new Vector3(d.position.X, d.position.Y + speed/2 * damper, d.position.Z);

            }
            if (Keyboard.GetState().IsKeyDown(Keys.F) && doorList.Count > 0)
            {
                Door d = doorList[selDoor];
                d.scale += 0.01f * damper;

            }
            else if (Keyboard.GetState().IsKeyDown(Keys.G) && doorList.Count > 0)
            {
                Door d = doorList[selDoor];
                d.scale -= 0.01f * damper;

            }
            if (Keyboard.GetState().IsKeyDown(Keys.Q) && lastState.IsKeyUp(Keys.Q) && doorList.Count > 0)
            {
                Door d = doorList[selDoor];
                d.initRotation += MathHelper.PiOver2;

            }
            else if (Keyboard.GetState().IsKeyDown(Keys.E) && lastState.IsKeyUp(Keys.E) && doorList.Count > 0)
            {
                Door d = doorList[doorList.Count - 1];
                d.initRotation -= MathHelper.PiOver2;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.OemPeriod) && lastState.IsKeyUp(Keys.OemPeriod) && selDoor < doorList.Count-1)
            {
                selDoor++;
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.OemComma) && lastState.IsKeyUp(Keys.OemComma) && selDoor > 0)
            {
                selDoor--;
            }**/
            #endregion

            #region Movement Stuff (commented)
            /*
            if (Keyboard.GetState().IsKeyDown(Keys.Left))
            {
                camrotation += 0.035f * damper;
                camrotation %= MathHelper.TwoPi;
                camera.YRotation = camrotation;
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.Right))
            {
                camrotation -= 0.035f * damper;
                camrotation %= MathHelper.TwoPi;
                camera.YRotation = camrotation;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Up))
            {
                camBobDuration += (float)gameTime.ElapsedGameTime.TotalSeconds*10;

                camera.Position = new Vector3(camera.Position.X + (speed * damper * (float)Math.Sin(camrotation)), 
                    baseY + (float)Math.Sin(camBobDuration)/2f, 
                    camera.Position.Z + (speed * damper * (float)Math.Cos(camrotation)));
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.Down))
            {
                camBobDuration += (float)gameTime.ElapsedGameTime.TotalSeconds * 10;
                camera.Position = new Vector3(camera.Position.X - (speed * damper * (float)Math.Sin(camrotation)),
                    baseY + (float)Math.Sin(camBobDuration)/2f, 
                    camera.Position.Z - (speed * damper * (float)Math.Cos(camrotation)));
            }

            if (Keyboard.GetState().IsKeyDown(Keys.P))
            {
                baseY += speed * damper / 2f;
                camera.Position = new Vector3(camera.Position.X, baseY + (float)Math.Sin(camBobDuration), camera.Position.Z);
            } 
            else if (Keyboard.GetState().IsKeyDown(Keys.L))
            {
                baseY -= speed * damper / 2f;
                camera.Position = new Vector3(camera.Position.X, baseY + (float)Math.Sin(camBobDuration), camera.Position.Z);
            }*/
            #endregion

            #region RenderDebug
            /*
            if (Keyboard.GetState().IsKeyDown(Keys.M) && lastState.IsKeyUp(Keys.M) && fcrList.Count > 0)
            {
                displayingFCR = !displayingFCR;
            }*/
            #endregion

            if (Mouse.GetState().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed && inSeq && seqIt == -1) {
                seqIt = 0;
                StartTrial(Type.GetType("HarryPotterGame.Trials." + trialSequences[seqIt]));
            }

            if (Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D0) && lastState.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.D0))
                StartTrial(Type.GetType("HarryPotterGame.Trials.KCalibration"));
            if (Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D1) && lastState.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.D1))
                StartTrial(Type.GetType("HarryPotterGame.Trials.P1"));
            if (Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D2) && lastState.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.D2))
                StartTrial(Type.GetType("HarryPotterGame.Trials.G1"));
            if (Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D3) && lastState.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.D3))
                StartTrial(Type.GetType("HarryPotterGame.Trials.D1"));
            if (Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D4) && lastState.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.D4))
                StartTrial(Type.GetType("HarryPotterGame.Trials.P2"));
            if (Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D7) && lastState.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.D7))
                StartTrial(Type.GetType("HarryPotterGame.Trials.P3"));
            if (Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D8) && lastState.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.D8))
                StartTrial(Type.GetType("HarryPotterGame.Trials.D3"));
            if (Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                this.Exit();

            if (Mouse.GetState().X < 20)

                Mouse.SetPosition(20, Mouse.GetState().Y);

            if (Mouse.GetState().X > GraphicsDevice.Viewport.Width - 20)

                Mouse.SetPosition(GraphicsDevice.Viewport.Width - 20, Mouse.GetState().Y);

            if (Mouse.GetState().Y < 20)

                Mouse.SetPosition(Mouse.GetState().X, 20);

            if (Mouse.GetState().Y > GraphicsDevice.Viewport.Height-20)

                Mouse.SetPosition(Mouse.GetState().X, GraphicsDevice.Viewport.Height - 20);

            if (trial != null)
                UpdateTrial(gameTime);
            if(collectingData) DataLogger.Update(gameTime);

            if (wandCursor != null) wandCursor.Update(gameTime);

            /*
             * Input modes and gesture styles are defined and specified within this portion of the code
             */
            switch (inputMode) {
                case InputMode.MOUSE:
                    averagePoints.Insert(0, new Vector2(Mouse.GetState().X, Mouse.GetState().Y));
                    if (averagePoints.Count > pointCapacity) {
                        averagePoints.RemoveAt(averagePoints.Count - 1);
                    }
                    cursorPos = Vector2.Zero;
                    foreach (Vector2 pastPoint in averagePoints) cursorPos += pastPoint;
                    cursorPos /= averagePoints.Count;
                    if (Mouse.GetState().LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed) {
                        if (Mouse.GetState().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed) {
                            gesture = ManipMode.SELECTGRAB;
                        } else {
                            gesture = ManipMode.SELECT;
                        }
                    } else if (Mouse.GetState().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed) {
                        gesture = ManipMode.GRAB;
                    } else {
                        gesture = ManipMode.NONE;
                    }

                    if (Mouse.GetState().MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed) {
                        gesture = ManipMode.SELECT;
                        cursorPos = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
                        Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
                    }

                    break;
                case InputMode.KINECT:
                    averagePoints.Insert(0, kAccess.CursorPos);
                    if (averagePoints.Count > pointCapacity) {
                        averagePoints.RemoveAt(averagePoints.Count - 1);
                    }
                    cursorPos = Vector2.Zero;
                    foreach (Vector2 pastPoint in averagePoints) cursorPos += pastPoint;
                    cursorPos /= averagePoints.Count;
                    if (Mouse.GetState().LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed) {
                        if (Mouse.GetState().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed) {
                            gesture = ManipMode.GRAB;
                        } else {
                            gesture = ManipMode.SELECT;
                        }
                    } else {
                        gesture = ManipMode.NONE;
                    }
                    break;
                case InputMode.WAND:
                    averagePoints.Insert(0, kAccess.CursorPos);
                    if (averagePoints.Count > pointCapacity) {
                        averagePoints.RemoveAt(averagePoints.Count - 1);
                    }
                    cursorPos = Vector2.Zero;
                    foreach (Vector2 pastPoint in averagePoints) cursorPos += pastPoint;
                    cursorPos /= averagePoints.Count;

                    if (wAccess.SelectDetected) {
                        gesture = ManipMode.SELECT;
                    } else if (wAccess.GrabDetected) {
                        gesture = ManipMode.GRAB;
                    } else {
                        gesture = ManipMode.NONE;
                    }

                    break;
            }

            lastState = Keyboard.GetState();
            lastMouseState = Mouse.GetState();
            if (wAccess != null) lastWMState = wAccess.GetState();
            base.Update(gameTime);
        }

        private void CreateFlatRender(GameTime gameTime)
        {
            renderTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.Depth16);
            DepthStencilState dss = new DepthStencilState();
            dss.DepthBufferEnable = true;
            GraphicsDevice.DepthStencilState = dss;
            GraphicsDevice.SetRenderTarget(renderTarget);
            GraphicsDevice.Clear(Color.DarkSlateBlue);

            cloister.DrawFlatRender(gameTime, Color.White);
            //They just get in the way now.
            /*foreach (Door door in doorList)
            {
                door.DrawFlatRender(gameTime, Color.Orange);
            }*/
            if (trial != null) trial.DrawFlatRender(gameTime);

            GraphicsDevice.SetRenderTarget(null);
            fcrList.Add((Texture2D)renderTarget);

            fcrIndex = fcrList.Count - 1;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {

            //if (!graphics.IsFullScreen) graphics.ToggleFullScreen();
            GraphicsDevice.Clear(Color.Black);

            if (displayingFCR)
            {
                drawRender(gameTime);
                DrawDebug(gameTime);
                base.Draw(gameTime);
                return;
            }

            GraphicsDevice.RasterizerState = fillRaster;
            cloister.Draw(gameTime);
            foreach (Door door in doorList)
            {
                door.Draw(gameTime);
            }
            if (trial != null) trial.Draw(gameTime);
            //DrawDebug(gameTime);

            kAccess.Draw(GraphicsDevice, spriteBatch);
            wandCursor.Draw(gameTime);

            DepthStencilState dss = new DepthStencilState();
            dss.DepthBufferEnable = true;
            GraphicsDevice.DepthStencilState = dss; 

            base.Draw(gameTime);
        }

        private void drawRender(GameTime gameTime)
        {
            spriteBatch.Begin();

            spriteBatch.Draw(fcrList[fcrIndex], new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.White);

            spriteBatch.End();
        }

        private void DrawDebug(GameTime gameTime)
        {
            spriteBatch.Begin();

            spriteBatch.DrawString(debug, "(" + camera.Position.X + ", " + camera.Position.Y + ", " + camera.Position.Z + ")", Vector2.Zero, Color.White);
            spriteBatch.DrawString(debug, "R = " + camera.YRotation/MathHelper.Pi + "pi", new Vector2(0, 20), Color.White);

            /*for (int i = 0; i < doorList.Count; i++)
            {
                Door door = doorList[i];
                Color stringcol = selDoor == i ? Color.Green : Color.Red;
                spriteBatch.DrawString(debug, "[" + door.position.X + "," + door.position.Y + "," + door.position.Z 
                    + "] R=" + door.yrotation + " S=" + door.scale, new Vector2(0, 40 + 20 * i), stringcol);
            }*/
            //spriteBatch.DrawString(debug, "Last Click: " + lastTarget, new Vector2(0, 400), Color.Blue);

            if (trial != null) trial.DrawDebug(gameTime, spriteBatch, debug);
            

            //spriteBatch.Draw(cursor, new Rectangle((int)cursorPos.X - cursor.Width / 2, (int)cursorPos.Y - cursor.Height / 2, cursor.Width, cursor.Height), Color.White);

            spriteBatch.End();
        }
    }
}
