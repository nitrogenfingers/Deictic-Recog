using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HarryPotterGame.Entities
{
    public interface IFlatDrawable
    {
        void DrawFlatRender(GameTime gameTime, Color color);
    }

    public abstract class Entity : IFlatDrawable
    {
        public bool invisibleNextRender = false;

        public Vector3 position;
        protected Camera camera;
        protected Game game;

        public float xrotation = 0;
        public float yrotation = 0;
        public float zrotation = 0;

        //For ray intersects
        public Matrix world;

        public float scale = 1f;
        public Model model;
        //This is something trials can toggle, to indicate the user has "grabbed" it
        public bool selected = false;

        public Entity(Game game, Vector3 position, Camera camera, Model model)
        {
            this.game = game;
            this.position = position;
            this.camera = camera;
            this.model = model;
        }

        public abstract void Update(GameTime gameTime);

        public virtual void Draw(GameTime gameTime)
        {
            Matrix[] boneTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
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

        public virtual void DrawFlatRender(GameTime gameTime, Color color)
        {
            if (invisibleNextRender)
            {
                invisibleNextRender = false;
                return;
            }

            Matrix[] boneTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
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
