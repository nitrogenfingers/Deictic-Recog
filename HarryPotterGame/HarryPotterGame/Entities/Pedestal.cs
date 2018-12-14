using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HarryPotterGame.Entities
{
    class Pedestal : GEntity
    {
        //This cycles from 0-2 representing the tree, man and snake respectively
        public int selection { get; set; }
        public void UpdateSelection() { selection = (selection + 1) % 3; }
        //This toggles whether or not the pedestal can actually *be* selected.
        public bool Selectable = false;

        public Pedestal(Game game, Camera camera, Vector3 position)
            :base(game, position, camera, "models/statues/pedestal", null)
        {

        }

        public override void Update(GameTime gameTime)
        {
            
        }

        public override void DrawFlatRender(GameTime gameTime, Color color)
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
                Color toDraw = Color.Lime;
                if (Selectable && (
                        (mesh.Name == "tree" && selection == 0) ||
                        (mesh.Name == "man" && selection == 1) ||
                        (mesh.Name == "snake" && selection == 2)))
                    toDraw = color;

                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = world = boneTransforms[mesh.ParentBone.Index] * Matrix.CreateScale(scale) * Matrix.CreateRotationX(xrotation)
                        * Matrix.CreateRotationY(yrotation) * Matrix.CreateRotationZ(zrotation) * Matrix.CreateTranslation(position);
                    effect.View = camera.GetViewMatrix();
                    effect.Projection = camera.GetProjectionMatrix();

                    effect.AmbientLightColor = toDraw.ToVector3();
                    effect.DiffuseColor = toDraw.ToVector3();
                    effect.SpecularPower = 0;
                    effect.DirectionalLight0.Enabled = false;
                    effect.DirectionalLight1.Enabled = false;
                    effect.DirectionalLight2.Enabled = false;
                    effect.TextureEnabled = false;
                }
                mesh.Draw();
            }
            
            //base.DrawFlatRender(gameTime, color);
        }
    }
}
