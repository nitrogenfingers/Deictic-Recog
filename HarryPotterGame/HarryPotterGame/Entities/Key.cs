using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HarryPotterGame.Entities
{
    class Key : GEntity, ISphereCollidable
    {
        //Debuggy stuff
        RasterizerState wireframe;
        Model spheremodel;
        BoundingSphere sphere;

        public Key(Game game, Vector3 position, Camera camera) :
            base(game, position, camera, "models/tavern/key", null)
        {
            this.sphere = model.Meshes[0].BoundingSphere;
            spheremodel = game.Content.Load<Model>("models/fireflies");

            Texture2D baseTexture = game.Content.Load<Texture2D>("textures/brass_dark");
            foreach (ModelMesh mesh in model.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.TextureEnabled = true;
                    effect.Texture = baseTexture;
                }
            }

            wireframe = new RasterizerState();
            wireframe.FillMode = FillMode.WireFrame;
        }

        public bool Intersects(Ray ray){
            return ray.Intersects(sphere) != null;
        }
        public bool Intersects(BoundingSphere oSphere){
            return sphere.Intersects(oSphere);
        }
        public bool Contains(Vector3 point)
        {
            return sphere.Contains(point) != ContainmentType.Disjoint;
        }

        public override void Update(GameTime gameTime)
        {
            sphere.Center = position;
            if (selected && yrotation > -MathHelper.PiOver2)
            {
                yrotation -= (float)gameTime.ElapsedGameTime.TotalSeconds * 1.5f;
            }
            
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            
            #region Debuggy Stuff
            /*
            RasterizerState temp = game.GraphicsDevice.RasterizerState;
            game.GraphicsDevice.RasterizerState = wireframe;

            Matrix[] baseTransform = new Matrix[spheremodel.Bones.Count];
            spheremodel.CopyAbsoluteBoneTransformsTo(baseTransform);

            foreach (ModelMesh mesh in spheremodel.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.View = camera.GetViewMatrix();
                    effect.Projection = camera.GetProjectionMatrix();
                    effect.World = Matrix.CreateScale(sphere.Radius) * Matrix.CreateTranslation(sphere.Center);
                }
                mesh.Draw();
            }

            game.GraphicsDevice.RasterizerState = temp;
             */
            #endregion

            base.Draw(gameTime);
        }

        public override void DrawFlatRender(GameTime gameTime, Color color)
        {
            if (selected)
            {
                Matrix[] baseTransform = new Matrix[spheremodel.Bones.Count];
                spheremodel.CopyAbsoluteBoneTransformsTo(baseTransform);

                foreach (ModelMesh mesh in spheremodel.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.View = camera.GetViewMatrix();
                        effect.Projection = camera.GetProjectionMatrix();
                        effect.World = Matrix.CreateScale(sphere.Radius) * Matrix.CreateTranslation(sphere.Center);

                        effect.DiffuseColor = (selectedColor ?? (standardColor ?? Color.White)).ToVector3();
                        effect.AmbientLightColor = (selectedColor ?? (standardColor ?? Color.White)).ToVector3();
                    }
                    mesh.Draw();
                }
            }
            else
            {
                base.DrawFlatRender(gameTime, color);
            }
        }
    }
}
