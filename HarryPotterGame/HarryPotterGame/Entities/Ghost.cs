using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HarryPotterGame.Entities
{
    class Ghost : Entity, IFlatDrawable
    {
        protected BlendState nbs;
        private float alpha = 0;
        private int direction;
        private bool dying = false;
        public bool Dying { set { dying = value; } }
        private bool dead = false;
        public bool Dead { get { return dead; } }

        public Ghost(Game game, Vector3 position, Camera camera)
            : base(game, position, camera, game.Content.Load<Model>("models/ghost"))
        {
            scale = Game1.random.Next(5, 20);
            direction = Math.Sign(Game1.random.Next(-1,2));
            direction = direction == 0 ? 1 : direction;
        }

        public override void Update(GameTime gameTime)
        {
            if (dying) {
                alpha -= (float)gameTime.ElapsedGameTime.TotalSeconds / 2f;
                alpha = MathHelper.Clamp(alpha, 0, 1);
                if (alpha <= 0) dead = true;
            }

            if (alpha < 1) alpha = alpha + (float)gameTime.ElapsedGameTime.TotalSeconds / 2f;
            if (alpha > 0.7) alpha = 0.7f;

            yrotation += (float)gameTime.ElapsedGameTime.TotalSeconds * direction;
            yrotation %= MathHelper.TwoPi;
        }

        public override void Draw(GameTime gameTime)
        {
            foreach (ModelMesh mesh in model.Meshes) foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.Alpha = alpha;
                    effect.DiffuseColor = Color.White.ToVector3();
                }

            base.Draw(gameTime);
        }

        public override void DrawFlatRender(GameTime gameTime, Color color)
        {
            foreach (ModelMesh mesh in model.Meshes) foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.Alpha = 1;
                }

            base.DrawFlatRender(gameTime, color);
        }
    }
}
