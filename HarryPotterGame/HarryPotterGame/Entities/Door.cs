using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HarryPotterGame.Entities
{
    public class Door : Entity, IFlatDrawable
    {
        public enum Opening { NONE, OFORWARD, OBACK, CLOSING }

        public float initRotation = 0;
        Opening opening;
        Model left, right;

        public Door(Game game, Camera camera, Vector3 position, float initRotation)
            :base(game, position, camera, null)
        {
            left = game.Content.Load<Model>("models/leftdoor");
            right = game.Content.Load<Model>("models/rightdoor");

            foreach (ModelMesh mesh in left.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.Texture = game.Content.Load<Texture2D>("textures/cloister/2_Doors_Diffuse");
                }
            }
            foreach (ModelMesh mesh in right.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.Texture = game.Content.Load<Texture2D>("textures/cloister/2_Doors_Diffuse");
                }
            }

            this.initRotation = initRotation;
        }

        public override void Update(GameTime gameTime)
        {
            if (opening == Opening.OFORWARD)
            {
                if (yrotation < MathHelper.PiOver4)
                {
                    yrotation += MathHelper.PiOver4 / (float)(60 / gameTime.ElapsedGameTime.TotalSeconds);
                }
                else
                {
                    opening = Opening.NONE;
                }
            }
            else if (opening == Opening.OBACK)
            {

                if (yrotation > -MathHelper.PiOver4)
                {
                    yrotation -= MathHelper.PiOver4 / (float)(60 / gameTime.ElapsedGameTime.TotalSeconds);
                }
                else
                {
                    opening = Opening.NONE;
                }
            }
            else if (opening == Opening.CLOSING)
            {
                if (yrotation != 0)
                {
                    yrotation += -Math.Sign(yrotation) * (MathHelper.PiOver4 / (float)(60 / gameTime.ElapsedGameTime.TotalSeconds));
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Vector3 offset = new Vector3(2.427f, 0, 0);

            Matrix[] bones = new Matrix[left.Bones.Count];
            left.CopyAbsoluteBoneTransformsTo(bones);

            foreach (ModelMesh mesh in left.Meshes)
            {
                Matrix world = bones[mesh.ParentBone.Index] * Matrix.CreateScale(scale) * Matrix.CreateRotationY(MathHelper.Pi + yrotation);
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.TextureEnabled = true;
                    effect.View = camera.GetViewMatrix();
                    effect.Projection = camera.GetProjectionMatrix();
                    effect.World = world * Matrix.CreateTranslation(offset*scale) * Matrix.CreateRotationY(initRotation) * Matrix.CreateTranslation(position);

                    effect.EnableDefaultLighting();
                    effect.AmbientLightColor = Color.Gray.ToVector3();
                    effect.DiffuseColor = Color.Gray.ToVector3();
                    effect.SpecularPower = 0;
                }
                mesh.Draw();
            }

            bones = new Matrix[right.Bones.Count];
            right.CopyAbsoluteBoneTransformsTo(bones);

            foreach (ModelMesh mesh in right.Meshes)
            {
                Matrix world = bones[mesh.ParentBone.Index] * Matrix.CreateScale(scale) * Matrix.CreateRotationY(MathHelper.Pi + -yrotation);
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.TextureEnabled = true;
                    effect.View = camera.GetViewMatrix();
                    effect.Projection = camera.GetProjectionMatrix();
                    effect.World = world * Matrix.CreateTranslation(-offset*scale) * Matrix.CreateRotationY(initRotation) * Matrix.CreateTranslation(position);

                    effect.EnableDefaultLighting();
                    effect.AmbientLightColor = Color.Gray.ToVector3();
                    effect.DiffuseColor = Color.Gray.ToVector3();
                    effect.SpecularPower = 0;
                }
                mesh.Draw();
            }
        }

        public override void DrawFlatRender(GameTime gameTime, Color color)
        {
            Vector3 offset = new Vector3(2.427f, 0, 0);

            Matrix[] bones = new Matrix[left.Bones.Count];
            left.CopyAbsoluteBoneTransformsTo(bones);

            foreach (ModelMesh mesh in left.Meshes)
            {
                Matrix world = bones[mesh.ParentBone.Index] * Matrix.CreateScale(scale) * Matrix.CreateRotationY(MathHelper.Pi + yrotation);
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.TextureEnabled = false;
                    effect.View = camera.GetViewMatrix();
                    effect.Projection = camera.GetProjectionMatrix();
                    effect.World = world * Matrix.CreateTranslation(offset * scale) * Matrix.CreateRotationY(initRotation) * Matrix.CreateTranslation(position);

                    effect.AmbientLightColor = color.ToVector3();
                    effect.DiffuseColor = color.ToVector3();
                    effect.SpecularPower = 0;
                    effect.DirectionalLight0.Enabled = false;
                    effect.DirectionalLight1.Enabled = false;
                    effect.DirectionalLight2.Enabled = false;
                }
                mesh.Draw();
            }

            bones = new Matrix[right.Bones.Count];
            right.CopyAbsoluteBoneTransformsTo(bones);

            foreach (ModelMesh mesh in right.Meshes)
            {
                Matrix world = bones[mesh.ParentBone.Index] * Matrix.CreateScale(scale) * Matrix.CreateRotationY(MathHelper.Pi - yrotation);
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.TextureEnabled = false;
                    effect.View = camera.GetViewMatrix();
                    effect.Projection = camera.GetProjectionMatrix();
                    effect.World = world * Matrix.CreateTranslation(-offset * scale) * Matrix.CreateRotationY(initRotation) * Matrix.CreateTranslation(position);

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
