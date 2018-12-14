using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace HarryPotterGame
{
    /// <summary>
    /// Note that the definition of target has changed, targets are now relative instead of absolute.
    /// </summary>
    class RotatingCamera:Camera
    {
        protected float xRotation, yRotation, zRotation;
        public float XRotation { 
            get { return xRotation; } 
            set { xRotation = value; } 
        }
        public float YRotation { 
            get { return yRotation; } 
            set { yRotation = value; } 
        }
        public float ZRotation { 
            get { return zRotation; } 
            set { zRotation = value; } 
        }

        public RotatingCamera(Game game, Vector3 position, Vector3 target, Vector3 up)
            : base(game, position, target, up) { }

        public override Matrix GetViewMatrix()
        {
            Matrix camYRotation = Matrix.CreateRotationY(yRotation);

            Vector3 targetNormal = new Vector3(target.X, target.Y, target.Z);
            targetNormal.Normalize();
            Vector3 targetReference = Vector3.Transform(targetNormal, camYRotation);

            Vector3 finalTarget = targetReference + position;

            return Matrix.CreateLookAt(position, finalTarget, up);
        }
    }
}
