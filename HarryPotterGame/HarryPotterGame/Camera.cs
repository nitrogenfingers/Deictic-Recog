using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace HarryPotterGame
{
    /// <summary>
    /// On testing in the 2nd floor "Wedge" room, deduced that the screen there will be suitable for this project. The parameters as testing with a fake render in Blender are as follows:
    /// 1920 x 1080, full screen
    /// TrueColour LED, so bright contrasts and detailed shadows are necessary
    /// Shorter focal lens to increase perspective- for effective use in Blender lens was reduced from 35mm to 24mm
    /// </summary>

    public class Camera
    {
        protected Game game;
        protected Vector3 position, target, up;
        public Vector3 Position { 
            get { return position; }
            set { position = value; }
        }
        public Vector3 Target { 
            get { return target; }
            set { target = value; }
        }
        public Vector3 Up { 
            get { return up; }
            set { up = value; }
        }

        public Camera(Game game, Vector3 position, Vector3 target, Vector3 up)
        {
            this.game = game;
            this.position = position;
            this.target = target;
            this.up = up;
        }

        public virtual Matrix GetViewMatrix()
        {
            return Matrix.CreateLookAt(position, position+target, up);
        }

        public virtual Matrix GetProjectionMatrix()
        {
            return Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi/3f, game.GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000f);
        }
    }
}
