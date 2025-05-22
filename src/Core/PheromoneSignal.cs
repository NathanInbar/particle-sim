
using Microsoft.Xna.Framework;

namespace particle_sim.Simulation.Core
{
    public struct PheromoneSignal
    {
        public Vector2 Position;
        public int NestId;
        public float Strength;
        public float Age; // To handle decay

        public PheromoneSignal(Vector2 position, int nestId, float strength)
        {
            Position = position;
            NestId = nestId;
            Strength = strength;
            Age = 0f;
        }
    }
}