using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HarryPotterGame;

namespace Microsoft.Xna.Framework {
    public class ParticleSystem : DrawableGameComponent {

        public enum FadeModes { Both, Start, End, None }

        public struct ParticleRef {
            private ParticleSystem system;
            private int index;
            public ParticleRef(ParticleSystem _system, int _index) {
                system = _system;
                index = _index;
            }
            public Vector2 Position {
                get { return system.GetParticle(index).Position; }
                set { system.SetParticlePosition(index, value); }
            }
            public bool IsValid { get { return system != null; } }

            public Color Colour {
                get { return system.GetParticle(index).Colour; }
                set { system.SetParticleColour(index, value); }
            }
        }

        public const int PARTICLECOUNT = 1024;

        public struct Particle {
            public Vector2 Position;
            public Color Colour;
            public Vector2 Velocity;
            public float TimeCreated;
            public int Rot;
            public float RotSpeed;
        }

        protected Particle[] particles = new Particle[PARTICLECOUNT];
        protected int firstParticle = 0, lastParticle = 0;
        protected Texture2D texture;
        protected float currentTime = 0.0f;
        protected BasicEffect effect;

        protected Color defaultColor = Color.White;
        public Color DefaultColor { set { defaultColor = value; } }

        public float StartSize;
        public float EndSize;
        public float Size {
            get { return (StartSize + EndSize) / 2; }
            set { StartSize = EndSize = value; }
        }
        public float Duration;
        public Vector2 Gravity;
        public FadeModes FadeMode;
        public bool Additive;

        public delegate Vector2 PositionWarperDel(int id, Vector2 pos);
        public PositionWarperDel PositionWarper;

        public Texture2D Texture {
            set {
                this.texture = value;
            }
        }

        public ParticleSystem(Game game)
            : base(game) {
            DrawOrder = 10;
            Size = 0.2f;
            Duration = 1f;
            Gravity = new Vector2(0, -1);
            Random rand = new Random();
            for (short p = 0; p < PARTICLECOUNT; ++p) {
                particles[p].Rot = p;
                particles[p].RotSpeed = (float)rand.NextDouble() * 20.0f - 10.0f;
            }
            Initialize();
            effect = new BasicEffect(GraphicsDevice);
            effect.VertexColorEnabled = true;
        }

        public override void Update(GameTime gameTime) {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            bool isInfiniteDuration = float.IsNaN(Duration);
            if (!isInfiniteDuration) {
                for (int p = firstParticle; p != lastParticle; p = (p < PARTICLECOUNT - 1 ? p + 1 : 0)) {
                    float partTimeAlive = currentTime - particles[p].TimeCreated;
                    if (partTimeAlive > Duration) {
                        firstParticle = p + 1;
                        if (firstParticle >= PARTICLECOUNT) firstParticle -= PARTICLECOUNT;
                    } else break;
                }
            }
            currentTime += dt;
            base.Update(gameTime);
        }

        static Random rand = new Random();
        public static Vector2 RandomVector2 {
            get {
                return new Vector2((float)rand.NextDouble() - 0.5f,
                    (float)rand.NextDouble() - 0.5f);
            }
        }
        public ParticleRef AddInstance(Vector2 position) {
            return AddInstance(position, Vector2.Zero);
        }
        public ParticleRef AddInstance(Vector2 position, float spread) {
            return AddInstance(position, RandomVector2 * spread);
        }
        public ParticleRef AddInstance(Vector2 position, Vector2 velocity) {
            return AddInstance(position, velocity, particles[lastParticle].Rot, particles[lastParticle].RotSpeed);
        }
        public ParticleRef AddInstance(Vector2 position, Vector2 velocity, int rot, float rotSpeed) {
            int part = lastParticle;
            ++lastParticle;
            if (lastParticle >= PARTICLECOUNT) lastParticle -= PARTICLECOUNT;
            if (firstParticle == lastParticle) {
                firstParticle++;
                if (firstParticle >= PARTICLECOUNT) firstParticle -= PARTICLECOUNT;
            }
            particles[part].Position = position;
            particles[part].Velocity = velocity;
            particles[part].TimeCreated = currentTime;
            particles[part].Colour = defaultColor;
            particles[part].Rot = rot % 16;
            particles[part].RotSpeed = rotSpeed;
            return new ParticleRef(this, part);
        }

        public void SetParticlePosition(int part, Vector2 pos) {
            particles[part].Position = pos;
        }
        public void SetParticleColour(int part, Color col) {
            particles[part].Colour = col;
        }
        public Particle GetParticle(int index) {
            return particles[index];
        }
        public void GetParticle(int index, ref Particle part) {
            part = particles[index];
        }
        public void SetParticle(int index, ref Particle part) {
            particles[index] = part;
        }

        public void Draw(GameTime gameTime, SpriteBatch batch) {
            int lINC = 1;
            int pcCount = firstParticle > lastParticle ? lastParticle + (PARTICLECOUNT - firstParticle) : lastParticle - firstParticle;

            for (int i = firstParticle; i != lastParticle; i = i == PARTICLECOUNT - 1 ? 0 : i + 1) {
                Particle p = particles[i];

                float partTimeAlive = currentTime - p.TimeCreated;
                Vector2 position = p.Position + p.Velocity * partTimeAlive + Gravity * partTimeAlive;
                if (PositionWarper != null) position = PositionWarper(i, position);
                int r = (p.Rot + (int)(partTimeAlive * p.RotSpeed));
                while (r < 0) r += 16 * 16;
                r = (r % 16) * 4;
                float size = StartSize;
                Color colour = p.Colour;
                if (!float.IsNaN(Duration)) {
                    float timeAliveN = partTimeAlive / Duration;
                    //float alpha = timeAliveN * (1.0f - timeAliveN) / 0.5f / 0.5f;
                    switch (FadeMode) {
                        case FadeModes.Both: colour = p.Colour * (timeAliveN * (1 - timeAliveN) / 0.5f / 0.5f); break;
                        case FadeModes.Start: colour = p.Colour * timeAliveN; break;
                        case FadeModes.End: colour = p.Colour * (1 - timeAliveN); break;
                        default: colour = p.Colour; break;
                    }
                    size = StartSize + (EndSize - StartSize) * timeAliveN;
                }

                batch.Draw(texture, position, texture.Bounds, colour, r, new Vector2(100, 100), size, SpriteEffects.None, 1 - (++lINC / pcCount));
            }

            Draw(gameTime);
        }

    }
}
