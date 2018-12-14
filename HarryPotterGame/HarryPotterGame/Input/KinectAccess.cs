using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Microsoft.Kinect;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace HarryPotterGame.Input {
    /// <summary>
    /// This is a simple accessor class that encapsulates a few functions for messing around the sensor,
    /// to make accessing and changing the state of the Kinect more hassle-free. It's not strictly necessary,
    /// but it is a little bit of a time saver.
    /// 
    /// It also translates points from kinect space into world space, for convenient drawing.
    /// </summary>
    public class KinectAccess {
        /// <summary>
        /// Enum stating the gesture state of the participant's hand. A small number of gestures are supported in this study.
        /// </summary>
        public enum Gesture {
            OPEN, POINT, SELECT, FIST
        }
        //Models for each hand gestures
        private Model openmodel, pointmodel, selectmodel, fistmodel;
        //The position and facing of the camera (for debugging)
        private Vector3 campos = new Vector3(0, 0.75f, 0.1125f), camFacing = new Vector3(0, 0.75f, -1f) + Vector3.Forward;

        //Access to the kinect sensor itself
        private KinectSensor sensor;
        //Whether or not the kinect is running (I can't seem to find this in its state information)
        private bool sensorStarted;
        //Whether or not the kinect can and is tracking the user
        public bool Tracking {
            get {
                return sensorStarted && skeleton != null &&
                    skeleton[0].Joints[JointType.HandLeft].TrackingState != JointTrackingState.NotTracked;
            }
        }
        //The current frame of Skeleton Data
        private Skeleton[] skeleton;
        //Access for each joint in the skeleton
        public JointCollection Joints {
            get { return skeleton[0].Joints; }
        }
        //A normal vector from the right elbow to the right hand
        public Vector3 PointingNormal {
            get { return ToWorldSpace(Joints[JointType.HandRight].Position) - ToWorldSpace(Joints[JointType.ElbowRight].Position); }
        }

        //Time since the sensor was activated
        private float activationTime = 0f;
        //The time spent before the sensor is de and reactivated
        private readonly float untrackedLimit = 2f;
        //The bounds of the inner and outer walls (the position on the Z axis, in real space)
        private static float innerWallBounds = 0f;
        private static float outerWallBounds = -10f;
        //This converts them to game space
        public float InnerWallBounds { get { return innerWallBounds; } }
        public float OuterWallBounds { get { return outerWallBounds; } }

        //The window corners- our translation vectors to get from world space to screen space.
        private Vector2 winTL, winTR, winBL, winBR;
        //What essentially are our "world space" limits for the rectangle, in a perfect rectangle (which the win values are unlikely to be).
        //These are set when the win limits are set.
        private Vector2 realTL, realTR, realBL, realBR;
        //The list of translate vectors from winspace to realspace. Ordered topleft, topright, bottomleft, bottomright
        private Vector2[] winToReal = new Vector2[4];
        public void SetWinLimits(Vector2 tl, Vector2 tr, Vector2 br, Vector2 bl) {
            Console.Out.WriteLine("HERE");
            winTL = tl;
            winTR = tr;
            winBL = bl;
            winBR = br;

            float yMax = MathHelper.Max(MathHelper.Max(MathHelper.Max(tl.X, tr.X), br.X), bl.X);
            float yMin = MathHelper.Min(MathHelper.Min(MathHelper.Min(tl.X, tr.X), br.X), bl.X);
            float xMax = MathHelper.Max(MathHelper.Max(MathHelper.Max(tl.Y, tr.Y), br.Y), bl.Y);
            float xMin = MathHelper.Min(MathHelper.Min(MathHelper.Min(tl.Y, tr.Y), br.Y), bl.Y);

            realTL = new Vector2(xMin, yMin);
            realTR = new Vector2(xMax, yMin);
            realBR = new Vector2(xMax, yMax);
            realBL = new Vector2(xMin, yMax);

            winToReal[0] = realTL - winTL;
            winToReal[1] = realTR - winTR;
            winToReal[2] = realBR - winBR;
            winToReal[3] = realBL - winBL;
        }

        //Vectors indicating the edges and distances of each of the three world spaces
        private bool spaceInitialized = false;
        private static Vector2 xSpace; //= new Vector2(-0.12081f, 0.236322f);
        private static Vector2 ySpace; //= new Vector2(-0.998297f, -1.564171f);
        //Need to be replaced with dynamic values- can be done later
        private const float worldWidth = 1600;
        private const float worldHeight = 900;

        //The mouse state (we use this to "estimate" gestures, for the present)
        private MouseState lastMouseState;

        #region Window Bounds

        /*Vector2 topLeft = new Vector2(-0.1244499f, 1.502441f),
            topRight = new Vector2(0.208466f, 1.502441f),
            botLeft = new Vector2(-0.1244499f, 1.082321f),
            botRight = new Vector2(0.208466f, 1.082321f);*/
        Vector2 topLeft, topRight, botLeft, botRight;

        //Some debug stuff for the main viewing area
        private VertexPositionColor[] windowVertices;
        private short[] windowIndices;
        private BasicEffect windowEffect;
        private Model joint;
        private RasterizerState wireframe;
        private SpriteFont debug;

        private Vector3 boneRay;
        public Vector3 BoneRay { get { return boneRay; } }

        private VertexPositionColor[] boneverts;
        private short[] boneInds;

        private VertexPositionColor[] crosshairVerts;
        private short[] crosshairInds;

        private Vector2 fitzTopLeft;
        private Vector2 fitzBottomRight;
        private VertexPositionColor[] fitzRectVerts;
        private short[] fitzRectInds;
        private float fitzLinger = 0;

        private GraphicsDevice device;

        private VertexPositionColor constructVert (JointType type) {
            JointTrackingState state = skeleton[0].Joints[type].TrackingState;
            Color c;

            switch (state) {
                case JointTrackingState.Inferred: c = Color.Orange; break;
                case JointTrackingState.Tracked: c = Color.White; break;
                default: c = Color.Teal; break;
            }

            return new VertexPositionColor(ToWorldSpace(skeleton[0].Joints[type].Position), c);
        }

        private void setBoneConstructs() {
            boneverts = new VertexPositionColor[skeleton[0].Joints.Count + 1];
            JointCollection jlist = skeleton[0].Joints;

            boneverts[0] = constructVert(JointType.Head);
            boneverts[1] = constructVert(JointType.ShoulderCenter);
            boneverts[2] = constructVert(JointType.ShoulderRight);
            boneverts[3] = constructVert(JointType.ElbowRight);
            boneverts[4] = constructVert(JointType.WristRight);
            boneverts[5] = constructVert(JointType.HandRight);
            boneverts[6] = constructVert(JointType.ShoulderLeft);
            boneverts[7] = constructVert(JointType.ElbowLeft);
            boneverts[8] = constructVert(JointType.WristLeft);
            boneverts[9] = constructVert(JointType.HandLeft);

            boneInds = new short[18 + 2];

            for (short i = 0; i < 5; i++) {
                boneInds[i * 2] = i;
                boneInds[i * 2 + 1] = (short)(i + 1);
            }
            boneInds[10] = 1;
            boneInds[11] = 6;
            for (short i = 6; i < 9; i++) {
                boneInds[i * 2] = i;
                boneInds[i * 2 + 1] = (short)(i + 1);
            }
            //The right hand ray
            //boneRay = ToWorldSpace(skeleton[0].Joints[JointType.HandRight].Position);
            //Vector3 sh = ToWorldSpace(skeleton[0].Joints[JointType.ElbowRight].Position) - ToWorldSpace(skeleton[0].Joints[JointType.HandRight].Position);
            //sh.Normalize();

            //float distance = innerWallBounds - ToWorldSpace(skeleton[0].Joints[JointType.HandRight].Position).Z;
            //boneRay -= (boneNormal * distance);
            
            /** New Line Position Calculation performed here! **/
            Vector3 planeOrigin = new Vector3(0, 0, innerWallBounds);
            Vector3 planeNormal = new Vector3(0, 0, outerWallBounds - innerWallBounds);
            planeNormal.Normalize();

            Vector3 sElbow = ToWorldSpace(skeleton[0].Joints[JointType.ElbowRight].Position);
            Vector3 sDirection = ToWorldSpace(skeleton[0].Joints[JointType.HandRight].Position) - sElbow;
            sDirection.Normalize();

            float distance = Vector3.Dot(planeNormal, (planeOrigin - sElbow)) / Vector3.Dot(planeNormal, sDirection);

            boneRay = distance * sDirection + sElbow; 

            boneverts[10] = new VertexPositionColor(boneRay, Color.Red);
            boneInds[18] = 3;
            boneInds[19] = 10;
        }

        /*private void createFitzRectangle() {
            Random random = new Random();
            float widthlim = topRight.X - topLeft.X;
            float heightlim = topLeft.Y - botLeft.Y;

            float width = widthlim * (float)(0.1 + random.NextDouble() * 0.4);
            float height = heightlim * (float)(0.1 + random.NextDouble() * 0.4);

            fitzTopLeft = new Vector2(topLeft.X + width, botLeft.Y + height);
            fitzBottomRight = new Vector2(fitzTopLeft.X + width, fitzTopLeft.Y + height);
            fitzLinger = 0;

            fitzRectVerts = new VertexPositionColor[4];
            fitzRectVerts[0] = new VertexPositionColor(new Vector3(fitzTopLeft.X, fitzTopLeft.Y, innerWallBounds), Color.Red);
            fitzRectVerts[1] = new VertexPositionColor(new Vector3(fitzBottomRight.X, fitzTopLeft.Y, innerWallBounds), Color.Red);
            fitzRectVerts[2] = new VertexPositionColor(new Vector3(fitzBottomRight.X, fitzBottomRight.Y, innerWallBounds), Color.Red);
            fitzRectVerts[3] = new VertexPositionColor(new Vector3(fitzTopLeft.X, fitzBottomRight.Y, innerWallBounds), Color.Red);

            fitzRectInds = new short[8];
            fitzRectInds[0] = 0;
            fitzRectInds[1] = 1;
            fitzRectInds[2] = 1;
            fitzRectInds[3] = 2;
            fitzRectInds[4] = 2;
            fitzRectInds[5] = 3;
            fitzRectInds[6] = 3;
            fitzRectInds[7] = 0;
        }*/

        /*private void updateFitzRectangle() {
            for (int i = 0; i < fitzRectVerts.Length; i++) {
                fitzRectVerts[i].Color = new Color(1 - fitzLinger, fitzLinger, 0);
            }
        }*/

        private void setupCrosshairs() {
            crosshairVerts = new VertexPositionColor[12];

            crosshairVerts[0] = new VertexPositionColor(new Vector3(topLeft.X, boneRay.Y, innerWallBounds), Color.Red);
            crosshairVerts[1] = new VertexPositionColor(new Vector3(topRight.X, boneRay.Y, innerWallBounds), Color.Red);
            crosshairVerts[2] = new VertexPositionColor(new Vector3(boneRay.X, topLeft.Y, innerWallBounds), Color.Red);
            crosshairVerts[3] = new VertexPositionColor(new Vector3(boneRay.X, botLeft.Y, innerWallBounds), Color.Red);

            Vector3 hand = ToWorldSpace(skeleton[0].Joints[JointType.HandRight].Position);

            crosshairVerts[4] = new VertexPositionColor(new Vector3(topLeft.X, topLeft.Y, hand.Z), Color.Green);
            crosshairVerts[5] = new VertexPositionColor(new Vector3(topLeft.X, botLeft.Y, hand.Z), Color.Green);
            crosshairVerts[6] = new VertexPositionColor(new Vector3(topLeft.X, hand.Y, innerWallBounds), Color.Green);
            crosshairVerts[7] = new VertexPositionColor(new Vector3(topLeft.X, hand.Y, outerWallBounds), Color.Green);

            crosshairVerts[8] = new VertexPositionColor(new Vector3(topRight.X, topLeft.Y, hand.Z), Color.Green);
            crosshairVerts[9] = new VertexPositionColor(new Vector3(topRight.X, botLeft.Y, hand.Z), Color.Green);
            crosshairVerts[10] = new VertexPositionColor(new Vector3(topRight.X, hand.Y, innerWallBounds), Color.Green);
            crosshairVerts[11] = new VertexPositionColor(new Vector3(topRight.X, hand.Y, outerWallBounds), Color.Green);

            crosshairInds = new short[12];

            crosshairInds[0] = 0;
            crosshairInds[1] = 1;
            crosshairInds[2] = 2;
            crosshairInds[3] = 3;
            crosshairInds[4] = 4;
            crosshairInds[5] = 5;
            crosshairInds[6] = 6;
            crosshairInds[7] = 7;
            crosshairInds[8] = 8;
            crosshairInds[9] = 9;
            crosshairInds[10] = 10;
            crosshairInds[11] = 11;
        }

        private void initializeWindow() {
            //TL: -1,1 TR: 1,1 BL: -1,-1 BR: 1,-1

            windowVertices = new VertexPositionColor[8];
            windowVertices[0] = new VertexPositionColor(new Vector3(topRight.X, topRight.Y, innerWallBounds), Color.White);
            windowVertices[1] = new VertexPositionColor(new Vector3(botRight.X, botRight.Y, innerWallBounds), Color.White);
            windowVertices[2] = new VertexPositionColor(new Vector3(botLeft.X, botLeft.Y, innerWallBounds), Color.White);
            windowVertices[3] = new VertexPositionColor(new Vector3(topLeft.X, topLeft.Y, innerWallBounds), Color.White);
            windowVertices[4] = new VertexPositionColor(new Vector3(topRight.X, topRight.Y, outerWallBounds), Color.Teal);
            windowVertices[5] = new VertexPositionColor(new Vector3(botRight.X, botRight.Y, outerWallBounds), Color.Teal);
            windowVertices[6] = new VertexPositionColor(new Vector3(botLeft.X, botLeft.Y, outerWallBounds), Color.Teal);
            windowVertices[7] = new VertexPositionColor(new Vector3(topLeft.X, topLeft.Y, outerWallBounds), Color.Teal);


            windowIndices = new short[24];
            //The front face- outlines the Inner Wall
            for (short i = 0; i < 8; i++) {
                windowIndices[i] = (short)(Math.Ceiling(((float)i) / 2f) % 4);
            }
            //The rear face- outlines the Outer Wall
            for (short i = 8; i < 16; i++) {
                windowIndices[i] = (short)(Math.Ceiling(((float)i) / 2f) % 4 + 4);
            }
            //Brings the two planes into a cube (this can be omitted, really)
            windowIndices[16] = 0;
            windowIndices[17] = 4;
            windowIndices[18] = 1;
            windowIndices[19] = 5;
            windowIndices[20] = 2;
            windowIndices[21] = 6;
            windowIndices[22] = 3;
            windowIndices[23] = 7;
        }
        #endregion

        /// <summary>
        /// Attemps to connect the sensor. This will eject the user from the application if a kinect was not found.
        /// </summary>
        public KinectAccess(ContentManager content, GraphicsDevice device) {
            this.device = device;
            try {
                sensor = KinectSensor.KinectSensors.FirstOrDefault<KinectSensor>();
                sensor.SkeletonStream.Enable();
                sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                sensorStarted = false;
            } catch (Exception) {
                DialogResult result = MessageBox.Show("A kinect sensor was not found. Make sure the Kinect is detect in a USB slot, has been plugged " +
                    "into a power source, and all appropriate drivers have been installed. If issues continue, contact the manufacturer.");
            }

            fistmodel = content.Load<Model>("files/fist");
            openmodel = content.Load<Model>("files/open");
            pointmodel = content.Load<Model>("files/point");
            selectmodel = content.Load<Model>("files/select");
#if DEBUG
            Matrix view = Matrix.CreateLookAt(campos, camFacing, Vector3.Up);
            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.1f, Math.Abs(outerWallBounds));

            Vector3 tl3d = device.Viewport.Unproject(new Vector3(0, 0, 0), projection, view, Matrix.Identity);
            Vector3 tr3d = device.Viewport.Unproject(new Vector3(device.Viewport.Width, 0, 0), projection, view, Matrix.Identity);
            Vector3 bl3d = device.Viewport.Unproject(new Vector3(0, device.Viewport.Height, 0), projection, view, Matrix.Identity);
            Vector3 br3d = device.Viewport.Unproject(new Vector3(device.Viewport.Width, device.Viewport.Height, 0), projection, view, Matrix.Identity);

            topLeft = new Vector2(tl3d.X, tl3d.Y);
            topRight = new Vector2(tr3d.X, tr3d.Y);
            botLeft = new Vector2(bl3d.X, bl3d.Y);
            botRight = new Vector2(br3d.X, br3d.Y);

            Console.Write(topLeft + "," + topRight + "," + botRight + "," + botLeft);

            joint = content.Load<Model>("files/joint");
            initializeWindow();
            //createFitzRectangle();
            wireframe = new RasterizerState();
            wireframe.FillMode = FillMode.WireFrame;
            wireframe.CullMode = CullMode.None;
            debug = content.Load<SpriteFont>("files/debug");
#endif
        }

        /// <summary>
        /// Translates according to the vector space. This process is described in the thesis section 3.3.2.
        /// </summary>
        public Vector2 CursorPos {
            get {
                if (winTL == null)
                {
                    Vector2 rlPos = new Vector2(boneRay.X, boneRay.Y);
                    float[] dQ = { Vector2.Distance(rlPos, winTL), Vector2.Distance(rlPos, winTR), Vector2.Distance(rlPos, winBR), Vector2.Distance(rlPos, winBL) };
                    float ave = dQ[0] + dQ[1] + dQ[2] + dQ[3];

                    Vector2 QTranslate = Vector2.Zero;
                    for (int i = 0; i < dQ.Length; i++)
                        QTranslate += winToReal[i] * (1 - (dQ[i] / ave));

                    rlPos += QTranslate;
                    
                    return new Vector2((rlPos.X - topLeft.X) / (topRight.X - topLeft.X) * device.Viewport.Width,
                        (rlPos.Y - botLeft.Y) / (botLeft.Y - topLeft.Y) * device.Viewport.Height + device.Viewport.Height);
                    //Once again, fingers are crossed this work...
                    //return new Vector2((boneRay.X - topLeft.X) / (topRight.X - topLeft.X) * device.Viewport.Width,
                    //    (boneRay.Y - botLeft.Y) / (botLeft.Y - topLeft.Y) * device.Viewport.Height + device.Viewport.Height);
                } else {
                    Console.Out.WriteLine("The easy way");
                    return new Vector2((boneRay.X - winTL.X) / (winTR.X - winTL.X) * device.Viewport.Width,
                        (boneRay.Y - winBL.Y) / (winBL.Y - winTL.Y) * device.Viewport.Height + device.Viewport.Height);
                }
            }
        }

        /// <summary>
        /// Updates the sensor state, performing tracks and retracks when necessary
        /// </summary>
        /// <param name="gameTime">The game time</param>
        public void Update(GameTime gameTime) {

            if (sensor == null) return;

            if (!sensor.IsRunning) {
                activationTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (activationTime > untrackedLimit) sensor.Start();
                return;
            }

            using (SkeletonFrame sframe = sensor.SkeletonStream.OpenNextFrame((int)gameTime.ElapsedGameTime.TotalMilliseconds)) {
                if (sframe == null) return;

                lastMouseState = Mouse.GetState();
                skeleton = new Skeleton[sframe.SkeletonArrayLength];
                sframe.CopySkeletonDataTo(skeleton);

                //A lazy test- 0 isn't valid for Z so that tells us if the point is "useful" yet.
                if (!spaceInitialized && skeleton[0].Joints[JointType.ShoulderRight].Position.Z != 0) {

                    float xdistance = topRight.X - topLeft.X;
                    float ydistance = botLeft.Y - topLeft.Y;

                    SkeletonPoint shoulder = skeleton[0].Joints[JointType.ShoulderRight].Position;

                    xSpace = new Vector2(shoulder.X - xdistance / 2f, shoulder.X + xdistance / 2f);
                    ySpace = new Vector2(shoulder.Y - ydistance / 2f, shoulder.Y + ydistance / 2f);

                    spaceInitialized = true;
                }

                /*
                campos = ToWorldSpace(skeleton[0].Joints[JointType.HandRight].Position);
                camFacing = new Vector3(boneRay.X, boneRay.Y, boneRay.Z);
                camFacing.Normalize();
                campos = new Vector3(topLeft.X + (topRight.X - topLeft.X)/2, botLeft.Y + (topLeft.Y - botLeft.Y)/2, (3 *innerWallBounds) / 4);
                camFacing = campos + Vector3.Forward;
                */

                setupCrosshairs();

                /*if (skeleton[0].Joints[JointType.HandRight].TrackingState == JointTrackingState.NotTracked &&
                        skeleton[0].Joints[JointType.HandLeft].TrackingState == JointTrackingState.NotTracked) {
                    activationTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (activationTime > untrackedLimit) {
                        sensor.Stop();
                    }
                }*/

                //Fitz law

                /*
                if (GetGesture() == Gesture.POINT && boneRay.X >= fitzTopLeft.X && boneRay.X <= fitzBottomRight.X && boneRay.Y >= fitzTopLeft.Y && boneRay.Y <= fitzBottomRight.Y) {
                    fitzLinger += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (fitzLinger > 1) {
                        fitzLinger = 0;
                        //createFitzRectangle();
                    }
                } else {
                    fitzLinger -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (fitzLinger < 0) fitzLinger = 0;
                }*/
                //updateFitzRectangle();
            }
        }

        /// <summary>
        /// Attempts to restart the sensor.
        /// </summary>
        public void StartSensor() {
            sensor.Start();
            sensorStarted = true;
            activationTime = 0f;
        }

        /// <summary>
        /// Stops the sensor. It will attempt to reconnect after the
        /// untracked limit has been reached (default 1 second)
        /// </summary>
        public void StopSensor() {
            sensor.Stop();
            sensorStarted = false;
            activationTime = 0f;
        }

        /// <summary>
        /// Converts any skeleton point from Real Space into World Space.
        /// At present does nothing.
        /// </summary>
        /// <param name="point">The joint to be converted</param>
        /// <returns>A Vector3 in the new world space</returns>
        public static Vector3 ToWorldSpace(SkeletonPoint point) {
            //float x = ((point.X - xSpace.X) / (xSpace.Y - xSpace.X)) * 2 - 1;
            //float y = ((point.Y - ySpace.Y) / (ySpace.X - ySpace.Y)) * 2 - 1;
            float x = point.X;
            float y = point.Y;
            float z = -point.Z;
            return new Vector3(x, y, z);
        }

        public static Vector3 ToWorldSpace(Vector3 point) {
            //float x = ((point.X - xSpace.X) / (xSpace.Y - xSpace.X)) * 2 - 1;
            //float y = ((point.Y - ySpace.Y) / (ySpace.X - ySpace.Y)) * 2 - 1;
            float x = point.X;
            float y = point.Y;
            float z = -point.Z;
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Determines the gesture being performed based on the Gesture enum,
        /// which is a glorified mouse state deteminer at present.
        /// </summary>
        /// <returns>The gesture being performed</returns>
        public Gesture GetGesture() {
            if (lastMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                    lastMouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed) {
                return Gesture.FIST;
            } else if (lastMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed) {
                return Gesture.SELECT;
            } else if (lastMouseState.RightButton== Microsoft.Xna.Framework.Input.ButtonState.Pressed) {
                return Gesture.POINT;
            } else {
                return Gesture.OPEN;
            }
        }

        public Model GetGestureModel() {
            switch (GetGesture()) {
                case Gesture.FIST: return fistmodel;
                case Gesture.SELECT: return selectmodel;
                case Gesture.POINT: return pointmodel;
                default: return openmodel;
            }
        }

        /// <summary>
        /// Draws a simple representation of the world. The camera, for interest sake, is at the absolute origin,
        /// facing on the Forward vector
        /// </summary>
        /// <param name="device">The device to do the 3D drawing with</param>
        /// <param name="drawStyle">How much detail is to be drawn in the visualisation</param>
        public void Draw(GraphicsDevice device, SpriteBatch spriteBatch) {
//#if DEBUG
            if (windowEffect == null) windowEffect = new BasicEffect(device);

            windowEffect.View = Matrix.CreateLookAt(campos, camFacing, Vector3.Up);
            windowEffect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.1f, Math.Abs(outerWallBounds * 4));
            windowEffect.World = Matrix.Identity;
            windowEffect.VertexColorEnabled = true;

            foreach (EffectPass pass in windowEffect.CurrentTechnique.Passes) {
                pass.Apply();
                //device.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.LineList, windowVertices, 0, 8, windowIndices, 0, 12);
                //device.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.LineList, fitzRectVerts, 0, 4, fitzRectInds, 0, 4);
                if (skeleton != null) {
                    if (true) {//(GetGesture() == Gesture.POINT) {
                        device.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.LineList, crosshairVerts, 0, 12, crosshairInds, 0, 6);
                    }
                }
            }

            if (skeleton != null) {
                DrawJoint(device, skeleton[0].Joints[JointType.HandRight]);
                /*foreach (Joint j in skeleton[0].Joints) {
                    DrawJoint(device, j);
                }*/
                setBoneConstructs();
                foreach (EffectPass pass in windowEffect.CurrentTechnique.Passes) {
                    pass.Apply();
                    device.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.LineList, boneverts, 0, 11, boneInds, 0, 9);
                    if (true) {//(GetGesture() == Gesture.POINT) {
                        device.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.LineList, boneverts, 0, 11, boneInds, 18, 1);
                    }
                }
            }
            //DrawDebugText(spriteBatch);
//#endif
        }

        private void DrawDebugText(SpriteBatch batch) {
            batch.Begin();
            var height = batch.GraphicsDevice.Viewport.Height;

            if (skeleton != null) {
                batch.DrawString(debug, "Right Hand: " + skeleton[0].Joints[JointType.HandRight].TrackingState, new Vector2(0, height-200), Color.White);
                batch.DrawString(debug, "Right Elbow: " + skeleton[0].Joints[JointType.ElbowRight].TrackingState, new Vector2(0, height-175), Color.White);
                bool valid = skeleton[0].Joints[JointType.HandRight].TrackingState == JointTrackingState.Tracked && skeleton[0].Joints[JointType.ElbowRight].TrackingState == JointTrackingState.Tracked;
                batch.DrawString(debug, valid ? "Accurate Tracking" : "Inferred Tracking", new Vector2(0, 660), valid ? Color.Green : Color.Red);

                batch.DrawString(debug, "Ray At (" + boneRay.X + "," + boneRay.Y + "," + boneRay.Z + ")", new Vector2(0, height-125), Color.Yellow);
                batch.DrawString(debug, "Mouse At (" + this.CursorPos.X + "," + this.CursorPos.Y + ")", new Vector2(0, height-100), Color.Yellow);
                batch.DrawString(debug, "Absolute Hand: (" + skeleton[0].Joints[JointType.HandRight].Position.X + ", " + skeleton[0].Joints[JointType.HandRight].Position.Y + ", " +
                    skeleton[0].Joints[JointType.HandRight].Position.Z + ")", new Vector2(0, height-75), Color.Orange);
                Vector3 relHand = ToWorldSpace(skeleton[0].Joints[JointType.HandRight].Position);
                batch.DrawString(debug, "Absolute Hand: (" + relHand.X + ", " + relHand.Y + ", " + relHand.Z + ")", new Vector2(0, height-50), Color.Orange);
                batch.DrawString(debug, "Wall bounds : (" + innerWallBounds + ", " + outerWallBounds + ")", new Vector2(0, height-25), Color.Orange);
            }
            batch.End();
        }

        /// <summary>
        /// A simple debug method that draws joints in the world space
        /// </summary>
        /// <param name="device">The device to draw with</param>
        /// <param name="j">The joint to draw</param>
        private void DrawJoint(GraphicsDevice device, Joint j) {
            Model m = joint;
            if (j.JointType == JointType.HandLeft || j.JointType == JointType.HandRight)
                m = GetGestureModel();

            Matrix[] boneTransforms = new Matrix[m.Bones.Count];
            m.CopyAbsoluteBoneTransformsTo(boneTransforms);

            RasterizerState oldRaster = device.RasterizerState;
            device.RasterizerState = wireframe;

            foreach (ModelMesh mesh in m.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.View = Matrix.CreateLookAt(campos, camFacing, Vector3.Up);
                    effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.1f, Math.Abs(outerWallBounds * 4));
                    effect.World = boneTransforms[mesh.ParentBone.Index] * Matrix.CreateScale(0.01f) * Matrix.CreateTranslation(ToWorldSpace(j.Position));
                }
                mesh.Draw();
            }

            device.RasterizerState = oldRaster;
        }
    }
}