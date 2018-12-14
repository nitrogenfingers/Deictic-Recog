using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HarryPotterGame.Entities
{
    class Firefly:GEntity {
        protected static Random r = new Random();
        private float glowDuration;
        private float alphaState = 1;
        private const float alphaMin = 0.25f, alphaMax = 0.75f;
        private float alpha = 0;
        private float stdYpos;
        private float flutter = 0;
        private float flutterState = 1;
        private const float flutterMin = -MathHelper.PiOver2, flutterMax = MathHelper.PiOver2;
        private bool glowGone = false;

        public Firefly(Game game, Vector3 position, Camera camera) :
            base(game, position, camera, "models/firefly", "textures/fft")
        {
            alpha = (float)r.NextDouble() * (alphaMax - alphaMin) + alphaMin;
            flutter = (float)r.NextDouble() * (flutterMax - flutterMin) + flutterMin;
            if (r.Next(1, 3) == 1) alphaState = -1;
            if (r.Next(1, 3) == 1) flutterState = -1;
            glowDuration = (float)r.NextDouble() * 4 + 1;
            stdYpos = position.Y;
        }

        public override void Update(GameTime gameTime) {
            alpha += (float)(gameTime.ElapsedGameTime.TotalSeconds / 2f) * alphaState;
            flutter += (float)(gameTime.ElapsedGameTime.TotalSeconds * 18f) * flutterState;
            if (alpha > alphaMax) {
                alpha = alphaMax;
                alphaState *= -1;
            } else if (alpha < alphaMin) {
                alpha = alphaMin;
                alphaState *= -1;
                glowDuration = (float)Game1.random.NextDouble() * 4 + 1;
            }

            if (flutter > flutterMax) {
                flutter = flutterMax;
                flutterState *= -1;
            } else if (flutter < flutterMin) {
                flutter = flutterMin;
                flutterState *= -1;
            }
        }

        public void RemoveGlow() { glowGone = true; }

        public override void Draw(GameTime gameTime) {
            float absa = alpha / (alphaMax - alphaMin) - alphaMin;

            Matrix[] boneTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            foreach (ModelMesh mesh in model.Meshes) {
                if (mesh.Name == "glow" && glowGone) continue;

                foreach (BasicEffect effect in mesh.Effects) {

                    if (mesh.Name == "leftwings" || mesh.Name == "rightwings") {
                        effect.AmbientLightColor = effect.DiffuseColor = Color.White.ToVector3();
                    } else if (selected) {
                        effect.AmbientLightColor = (selectedColor ?? (standardColor ?? Color.White)).ToVector3();
                        effect.DiffuseColor = (selectedColor ?? (standardColor ?? Color.White)).ToVector3();
                    } else {
                        effect.AmbientLightColor = (standardColor ?? Color.White).ToVector3();
                        effect.DiffuseColor = (standardColor ?? Color.White).ToVector3();
                    }
                    effect.TextureEnabled = true;

                    Vector3 additionalTranslation = Vector3.Zero;
                    Vector3 additionalRotation = new Vector3(0, MathHelper.Pi/4, 0);
                    if (mesh.Name == "glow") {
                        effect.Alpha = alpha;
                    } else {
                        additionalTranslation.Y += absa - 0.5f;
                        effect.Alpha = 1;
                    }
                    if (mesh.Name == "leftwings") {
                        additionalRotation.Z = flutter;
                    } else if (mesh.Name == "rightwings") {
                        additionalRotation.Z = -flutter;
                    }


                    effect.World = world = boneTransforms[mesh.ParentBone.Index] * Matrix.CreateScale(scale) * Matrix.CreateRotationX(xrotation + additionalRotation.X)
                        * Matrix.CreateRotationY(yrotation + additionalRotation.Y) * Matrix.CreateRotationZ(zrotation + additionalRotation.Z) 
                        * Matrix.CreateTranslation(position + additionalTranslation);
                    effect.View = camera.GetViewMatrix();
                    effect.Projection = camera.GetProjectionMatrix();
                    effect.EnableDefaultLighting();
                }
                mesh.Draw();
            }
        }

        public override void DrawFlatRender(GameTime gameTime, Color color)
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.Alpha = 1;
                }
            }

            base.DrawFlatRender(gameTime, color);
        }
    }
}
