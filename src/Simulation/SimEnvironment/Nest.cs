// src/Simulation/Environment/Nest.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics; // Required for Color, Texture2D
using particle_sim.Simulation.Agents;   // For Agent type
using particle_sim.Simulation.Core;     // For AgentState
using static System.Math;

namespace particle_sim.Simulation.Environment
{
    public enum NestShape // Example, can be expanded
    {
        Circle
        // Future: Square, Triangle etc.
    }

    public class Nest
    {
        public int NestId { get; private set; }
        public Vector2 Position { get; private set; }
        public Color NestBaseColor { get; private set; }
        private Color _nestDrawColor;
        public float Size { get; private set; } // e.g., radius for a circle
        public NestShape Shape { get; private set; }

        // Random number generator for spawning agents with slight variations
        private static System.Random _random = new System.Random();

        public Nest(int id, Vector2 position, Color color, float size, NestShape shape)
        {
            NestId = id;
            Position = position;
            NestBaseColor = color;
            Size = size;
            Shape = shape;

            // calculate and store darker draw color for the nest itself
            _nestDrawColor = Color.Lerp(NestBaseColor, Color.Black, 0.4f); // 40% darker
        }

        public Agent SpawnAgent()
        {
            // Spawn agent at the nest's position for now
            // Initial velocity can be a random direction
            float angle = (float)(_random.NextDouble() * MathHelper.TwoPi);
            Vector2 initialVelocity = new Vector2((float)Cos(angle), (float)Sin(angle)) * 50f; // Example speed

            Agent agent = new Agent(this, initialVelocity);
            return agent;
        }

        public bool IsPositionInside(Vector2 worldPosition)
        {
            if (Shape == NestShape.Circle)
            {
                return Vector2.DistanceSquared(Position, worldPosition) <= Size * Size;
            }
            // Implement other shapes if added
            return false;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (Shape == NestShape.Circle)
            {
                int radius = (int)Size;
                int segments = 20; // Number of segments to approximate a circle

                for (int i = 0; i < segments; i++)
                {
                    float angle1 = (i / (float)segments) * MathHelper.TwoPi;
                    float angle2 = ((i + 1) / (float)segments) * MathHelper.TwoPi;

                    Vector2 p1 = Position + new Vector2((float)Cos(angle1) * radius, (float)Sin(angle1) * radius);
                    Vector2 p2 = Position + new Vector2((float)Cos(angle2) * radius, (float)Sin(angle2) * radius);
                    
                    // Draw line segment (requires a DrawLine helper or use a more direct circle drawing method)
                    // For simplicity, drawing a filled circle by plotting points:
                }
                // A simpler way to draw a basic circle representation for now:
                // Draw a square marker for the nest center, actual circle drawing can be more refined.
                 spriteBatch.Draw(pixelTexture, Position - new Vector2(Size/2, Size/2), null, _nestDrawColor, 0f, Vector2.Zero, Size, SpriteEffects.None, 0f);

            }
            // Implement drawing for other shapes if added
        }
    }
}