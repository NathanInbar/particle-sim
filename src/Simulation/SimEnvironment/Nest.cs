using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using particle_sim.Simulation.Agents;
using static System.Math;

namespace particle_sim.Simulation.Environment
{

    public class Nest
    {
        public int NestId { get; private set; }
        public Vector2 Position { get; private set; }
        public Color NestBaseColor { get; private set; }
        private Color _nestDrawColor;
        public float Size { get; private set; } // radius for circle

        private static System.Random _random = new System.Random();

        public Nest(int id, Vector2 position, Color color, float size)
        {
            NestId = id;
            Position = position;
            NestBaseColor = color;
            Size = size;

            // calculate and store a darker version of the base color for the nest
            _nestDrawColor = Color.Lerp(NestBaseColor, Color.Black, 0.4f);
        }

        public Agent SpawnAgent()
        {
            // spawn agent with random velocity
            float angle = (float)(_random.NextDouble() * MathHelper.TwoPi);
            Vector2 initialVelocity = new Vector2((float)Cos(angle), (float)Sin(angle)) * 50f; // Example speed

            Agent agent = new Agent(this, initialVelocity);
            return agent;
        }

        public bool IsPositionInside(Vector2 worldPosition)
            => Vector2.DistanceSquared(Position, worldPosition) <= Size * Size;

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            int radius = (int)Size;
            int segments = 20; // circle segments

            for (int i = 0; i < segments; i++)
            {
                float angle1 = (i / (float)segments) * MathHelper.TwoPi;
                float angle2 = ((i + 1) / (float)segments) * MathHelper.TwoPi;

                Vector2 p1 = Position + new Vector2((float)Cos(angle1) * radius, (float)Sin(angle1) * radius);
                Vector2 p2 = Position + new Vector2((float)Cos(angle2) * radius, (float)Sin(angle2) * radius);
        
            }

            spriteBatch.Draw(pixelTexture, Position - new Vector2(Size/2, Size/2), null, _nestDrawColor, 0f, Vector2.Zero, Size, SpriteEffects.None, 0f);
            
        }
    }
}