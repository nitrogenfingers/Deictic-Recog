using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HarryPotterGame.Entities
{
    class Cage : GEntity, ISphereCollidable
    {
        BoundingSphere sphere;
        Model spheremodel;
        RasterizerState wireframe;

        private readonly Vector3 sphereOffset = new Vector3(0, 7.2f, 0);
        private const float sphereRadius = 7.5f;

        public Cage(Game game, Vector3 position, Camera camera)
            : base(game, position, camera, "models/cage", null)
        {
            sphere = model.Meshes[0].BoundingSphere;
            sphere.Center = position + sphereOffset;
            sphere.Radius = sphereRadius;
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

        public override void Update(GameTime gameTime)
        {
            sphere.Center = position + sphereOffset;
            
            base.Update(gameTime);
        }

        public bool Intersects(Ray ray)
        {
            return sphere.Intersects(ray) != null;
        }
        public bool Intersects(BoundingSphere oSphere)
        {
            return sphere.Intersects(oSphere);
        }
        public bool Contains(Vector3 point)
        {
            return sphere.Contains(point) != ContainmentType.Disjoint;
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
            base.DrawFlatRender(gameTime, color);
        }
    }
}
