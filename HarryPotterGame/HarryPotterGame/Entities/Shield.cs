using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HarryPotterGame.Entities
{
    class Shield : GEntity, ISphereCollidable
    {
        private BoundingSphere sphere;
        public Shield(Game game, Vector3 position, Camera camera) :
            base(game, position, camera, "models/shield", null)
        {
            sphere = new BoundingSphere(position, 2 * scale);
        }

        public override void Update(GameTime gameTime)
        {
            sphere.Center = position;
            base.Update(gameTime);
        }

        
        public bool Contains(Vector3 oVector)
        {
            return sphere.Contains(oVector) == ContainmentType.Contains;
        }

        public bool Intersects(Ray ray)
        {
            return sphere.Intersects(ray) != null;
        }

        public bool Intersects(BoundingSphere osphere)
        {
            return sphere.Intersects(osphere);
        }
        
        public override void Draw(GameTime gameTime)
        {
            Matrix[] boneTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(boneTransforms);
            foreach (ModelMesh mesh in model.Meshes)
            {
                if (mesh.Name == "Select") continue;
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = world = boneTransforms[mesh.ParentBone.Index] * Matrix.CreateScale(scale) * Matrix.CreateRotationX(xrotation)
                        * Matrix.CreateRotationY(yrotation) * Matrix.CreateRotationZ(zrotation) * Matrix.CreateTranslation(position);
                    effect.View = camera.GetViewMatrix();
                    effect.Projection = camera.GetProjectionMatrix();
                    effect.EnableDefaultLighting();
                }
                mesh.Draw();
            }
        }

        public override void DrawFlatRender(GameTime gameTime, Color color)
        {
            Matrix[] boneTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(boneTransforms);
            foreach (ModelMesh mesh in model.Meshes)
            {
                if (mesh.Name != "Select") continue;
                foreach (BasicEffect effect in mesh.Effects)
                {

                    effect.World = world = boneTransforms[mesh.ParentBone.Index] * Matrix.CreateScale(scale) * Matrix.CreateRotationX(xrotation)
                        * Matrix.CreateRotationY(yrotation) * Matrix.CreateRotationZ(zrotation) * Matrix.CreateTranslation(position);
                    effect.View = camera.GetViewMatrix();
                    effect.Projection = camera.GetProjectionMatrix();

                    effect.AmbientLightColor = color.ToVector3();
                    effect.DiffuseColor = color.ToVector3();
                    effect.SpecularPower = 0;
                    effect.DirectionalLight0.Enabled = false;
                    effect.DirectionalLight1.Enabled = false;
                    effect.DirectionalLight2.Enabled = false;
                }
                mesh.Draw();
            }
        }
    }
}
