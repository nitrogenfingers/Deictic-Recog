using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using HarryPotterGame.Entities;

namespace HarryPotterGame.Trials
{
    class P2 : Trial
    {
        MouseState lms;
        int trialCount = 0;
        string[] statueTypes = { "man", "snake", "tree" };

        public P2(Game game, Camera camera)
            : base(game, camera)
        {
            position = new Vector3(330, 150, -525);
            yrotation = MathHelper.PiOver2;

            Pedestal pedestal = new Pedestal(game, camera, new Vector3(440, 100, -525));
            pedestal.scale = 10;
            pedestal.Selectable = true;
            pedestal.yrotation = MathHelper.Pi;
            selectableList.Insert(0, pedestal);

            GEntity table = new GEntity(game, new Vector3(400, 101, -525), camera, "models/tavern/round_table", "textures/tavern/round_table_color");
            table.scale = 5;
            sceneryList.Add(table);
            table = new GEntity(game, new Vector3(420, 101, -460), camera, "models/tavern/round_table", "textures/tavern/round_table_color");
            table.scale = 5;
            sceneryList.Add(table);
            table = new GEntity(game, new Vector3(420, 101, -590), camera, "models/tavern/round_table", "textures/tavern/round_table_color");
            table.scale = 5;
            sceneryList.Add(table);

            GEntity statue = new GEntity(game, new Vector3(400, 121, -525), camera, "models/statues/snake", null);
            statue.scale = 8;
            statue.yrotation = MathHelper.Pi;
            statue.selectedColor = Color.Yellow;
            selectableList.Insert(0, statue);
            statue = new GEntity(game, new Vector3(420, 121, -460), camera, "models/statues/man", null);
            statue.scale = 8;
            statue.yrotation = MathHelper.Pi;
            statue.selectedColor = Color.Yellow;
            selectableList.Insert(0, statue);
            statue = new GEntity(game, new Vector3(420, 121, -590), camera, "models/statues/tree", null);
            statue.scale = 8;
            statue.yrotation = MathHelper.Pi;
            statue.selectedColor = Color.Yellow;
            selectableList.Insert(0, statue);

            renderRequested = true;
        }

        public override void Update(GameTime gameTime)
        {
            MouseState ms = Mouse.GetState();

            if (ms.LeftButton == ButtonState.Pressed && lms.LeftButton == ButtonState.Released)
            {
                Rectangle source = new Rectangle(Mouse.GetState().X, Mouse.GetState().Y, 1, 1);
                Color[] retrieved = new Color[1];
                try
                {
                    flatRender.GetData<Color>(0, source, retrieved, 0, 1);
                    Console.Out.WriteLine(retrieved[0].R + "," + retrieved[0].G + "," + retrieved[0].B);
                    if (retrieved[0] == this.selectionColor)
                    {
                        if (selectableList[0] is Pedestal)
                        {
                            var cup = selectableList[1];
                            selectableList.RemoveAt(1);
                            selectableList.Add(cup);
                            cup = selectableList[0];
                            ((Pedestal)cup).selection++;
                            selectableList.RemoveAt(0);
                            selectableList.Add(cup);

                            trialCount++;
                            if (trialCount == 9)
                            {
                                complete = true;
                                return;
                            }
                            else if (trialCount % 3 == 0)
                            {
                                //Wipe out the list
                                for (int i = 0; i <= 2; i++)
                                {
                                    selectableList.RemoveAt(0);
                                }
                                ((Pedestal)selectableList[0]).selection = 0;
                                //And repopulate it
                                GEntity statue = new GEntity(game, new Vector3(400, 121, -525), camera, "models/statues/snake", null);
                                statue.scale = 8;
                                statue.yrotation = MathHelper.Pi;
                                statue.selectedColor = Color.Yellow;
                                selectableList.Insert(0, statue);
                                statue = new GEntity(game, new Vector3(420, 121, -460), camera, "models/statues/man", null);
                                statue.scale = 8;
                                statue.yrotation = MathHelper.Pi;
                                statue.selectedColor = Color.Yellow;
                                selectableList.Insert(0, statue);
                                statue = new GEntity(game, new Vector3(420, 121, -590), camera, "models/statues/tree", null);
                                statue.scale = 8;
                                statue.yrotation = MathHelper.Pi;
                                statue.selectedColor = Color.Yellow;
                                selectableList.Insert(0, statue);
                            }
                        }
                        else
                        {
                            var cup = selectableList[selectableList.Count-1];
                            selectableList.RemoveAt(selectableList.Count-1);
                            selectableList.Insert(0, cup);
                        }
                        renderRequested = true;
                    }
                }
                catch (ArgumentException)
                {
                    Console.Error.WriteLine("Mouse Click outside Bounds!");
                }
            }

            lms = ms;
            base.Update(gameTime);
        }
    }
}
