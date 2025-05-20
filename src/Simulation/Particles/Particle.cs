using Microsoft.Xna.Framework;

namespace particle_sim.Simulation.Particles
{
    public class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Size;
        public float Lifetime;
        public float Age; // Current age in seconds
        public bool IsAlive;

        public Particle()
        {
            IsAlive = false;
        }

        // Call this to initialize / re-initialize a particle
        public void Spawn(Vector2 position, Vector2 velocity, Color color, float size, float lifetime)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Size = size;
            Lifetime = lifetime;
            Age = 0f;
            IsAlive = true;
        }

        public void Update(float deltaTime)
        {
            if (!IsAlive)
                return;

            Position += Velocity * deltaTime;
            Age += deltaTime;

            if (Age >= Lifetime)
            {
                IsAlive = false;
            }
        }
    }
}