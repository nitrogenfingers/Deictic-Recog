using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HarryPotterGame
{
    class Cloister:DrawableGameComponent
    {
        private Model model;
        private Vector3 position;
        private Camera camera;

        private Dictionary<string, string> texID = new Dictionary<string, string>();

        public Cloister(Game game, Camera camera, Vector3 position)
            : base(game)
        {
            texID.Add("Straight_Hall", "Castle_Wall_Tiles_X");
            texID.Add("Cap_Wall", "Castle_Wall_Tiles_X");
            texID.Add("Window_Wall", "Castle_Wall_Tiles_X");
            texID.Add("4_Way_Vault", "Castle_Wall_Tiles_X");
            texID.Add("8_Way_Vault", "4_bay_diffuse");
            texID.Add("Room_Fireplace", "4_bay_diffuse");
            texID.Add("Hall_Arch", "Arch");
            texID.Add("Solid_Arch", "Arch");
            texID.Add("Capped_Arch", "Arch");
            texID.Add("Window__Arch", "Window_Arch");
            texID.Add("Window_Seat", "Stone_Tiles");
            texID.Add("Window_Dark", "Window_Dark");
            texID.Add("Stair_Hall", "ashlar-wall");
            texID.Add("Stairs", "ashlar-wall");
            texID.Add("Fireplace", "Fireplace_Diffuse");
            texID.Add("Ceiling__Joists", "old_wood_dark");
            texID.Add("Bracket", "Bracket_Diffuse");
            texID.Add("Plank_Ceiling", "Plank_floor");
            texID.Add("Left_Door", "2_Doors_Diffuse");
            texID.Add("Right_Door", "2_Doors_Diffuse");
            texID.Add("Ring_Pull", "Ring_Pull_Diffuse");
            texID.Add("Floor_Upstairs", "floor_pavers");
            texID.Add("Torch", "pitted_metal_dark256");

            this.position = position;
            this.camera = camera;
            model = game.Content.Load<Model>("models/Halls_TexTest");

            Console.Out.WriteLine(model.Meshes.Count);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    string name = mesh.Name;
                    int divider = name.LastIndexOf('_');
                    name = name.Substring(0, divider);

                    if (texID.ContainsKey(name)) effect.Texture = game.Content.Load<Texture2D>("textures/cloister/" + texID[name]);
                    else effect.Texture = game.Content.Load<Texture2D>("textures/cloister/DEFtexture");
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.TextureEnabled = true;
                    effect.View = camera.GetViewMatrix();
                    effect.Projection = camera.GetProjectionMatrix();
                    effect.World = transforms[mesh.ParentBone.Index] * Matrix.CreateScale(10f);
                    effect.EnableDefaultLighting();
                }
                mesh.Draw();
            }
            
            base.Draw(gameTime);
        }

        public void DrawFlatRender(GameTime gameTime, Color color)
        {
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.TextureEnabled = false;
                    effect.View = camera.GetViewMatrix();
                    effect.Projection = camera.GetProjectionMatrix();
                    effect.World = transforms[mesh.ParentBone.Index] * Matrix.CreateScale(10f);

                    effect.DiffuseColor = color.ToVector3();
                    effect.AmbientLightColor = color.ToVector3();
                    effect.SpecularPower = 0;
                }
                mesh.Draw();
            }
        }
    }
}
