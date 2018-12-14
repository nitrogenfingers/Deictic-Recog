using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HarryPotterGame.Entities
{
    class Warlock:Entity
    {
        public Warlock(Game game, Vector3 position, Camera camera):
            base(game, position, camera, game.Content.Load<Model>("models/warlock"))
        {
            scale = 12;
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.TextureEnabled = true;
                    effect.Texture = game.Content.Load<Texture2D>("models/warlocktex");
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.NumPad8))
            {
                position.Z -= 0.5f;
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.NumPad2))
            {
                position.Z += 0.5f;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.NumPad4))
            {
                position.X -= 0.5f;
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.NumPad6))
            {
                position.X += 0.5f;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            foreach (ModelMesh mesh in model.Meshes)
                foreach (BasicEffect effect in mesh.Effects){
                    effect.AmbientLightColor = Color.White.ToVector3();
                    effect.DiffuseColor = Color.White.ToVector3();
                    effect.TextureEnabled = true;
                }
            
            base.Draw(gameTime);
        }

        public override void DrawFlatRender(GameTime gameTime, Color color)
        {
            foreach(ModelMesh mesh in model.Meshes)
                foreach (BasicEffect effect in mesh.Effects)
                    effect.TextureEnabled = false;

            base.DrawFlatRender(gameTime, color);
        } 
    }
}
