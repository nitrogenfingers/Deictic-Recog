using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HarryPotterGame.Entities
{
    public interface ISphereCollidable
    {
        bool Intersects(Ray ray);
        bool Intersects(BoundingSphere sphere);
        bool Contains(Vector3 point);
    }

    class GEntity : Entity
    {
        public bool waiting = false;
        public Color? standardColor, selectedColor;
        public GEntity(Game game, Vector3 position, Camera camera, String modelPath, String texturePath)
            : base(game, position, camera, game.Content.Load<Model>(modelPath))
        {
            Texture2D tex = game.Content.Load<Texture2D>(texturePath ?? "models/texture");
            foreach (ModelMesh mesh in model.Meshes) foreach (BasicEffect effect in mesh.Effects)
                    effect.Texture = tex;
        }

        public override void Update(GameTime gameTime)
        {

        }

        public override void Draw(GameTime gameTime)
        {
            foreach (ModelMesh mesh in model.Meshes) foreach (BasicEffect effect in mesh.Effects)
                {
                    if (selected)
                    {
                        effect.AmbientLightColor = (selectedColor ?? (standardColor ?? Color.White)).ToVector3();
                        effect.DiffuseColor = (selectedColor ?? (standardColor ?? Color.White)).ToVector3();
                    }
                    else
                    {
                        effect.AmbientLightColor = (standardColor ?? Color.White).ToVector3();
                        effect.DiffuseColor = (standardColor ?? Color.White).ToVector3();
                    }
                    effect.TextureEnabled = true;
                }
            
            base.Draw(gameTime);
        }

        public override void DrawFlatRender(GameTime gameTime, Color color)
        {
            foreach (ModelMesh mesh in model.Meshes) foreach (BasicEffect effect in mesh.Effects)
                    effect.TextureEnabled = false;

            base.DrawFlatRender(gameTime, color);
        }
    }
}
