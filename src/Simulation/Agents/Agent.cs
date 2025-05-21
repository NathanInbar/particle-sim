// src/Simulation/Agents/Agent.cs
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using particle_sim.Simulation.Core;
using particle_sim.Simulation.Environment; // Assuming Nest and SimWorld are in here or sub-namespaces
using static System.Math;

namespace particle_sim.Simulation.Agents
{
    public class Agent
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float CurrentHeadingAngle;

        public AgentState CurrentState { get; private set; }
        public int NestId { get; private set; }
        public Color AgentColor { get; private set; }
        public Nest HomeNest { get; private set; }

        public float DetectionRadius { get; set; } = 20f;
        public float MovementSpeed { get; set; } = 50f;
        public float AgentSize { get; set; } = 5f;

        private float _restTimer;
        private const float RestDuration = 2f;

        private static System.Random _random = new System.Random();

        // Constructor accepts the Nest object
        public Agent(Nest homeNest, Vector2 initialVelocity)
        {
            HomeNest = homeNest; // <<< INITIALIZE HomeNest HERE
            NestId = homeNest.NestId;
            Position = homeNest.Position; // Spawn at nest position
            AgentColor = homeNest.NestBaseColor;

            Velocity = initialVelocity;
            CurrentHeadingAngle = (float)System.Math.Atan2(Velocity.Y, Velocity.X);
            CurrentState = AgentState.Exploring;
            MovementSpeed = initialVelocity.Length(); // Or use a default if initialVelocity is just direction
        }

        // Agent.Update now uses its internal this.HomeNest
        public void Update(GameTime gameTime, SimWorld simWorld) // Notice homeNest is NOT a parameter here
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (CurrentState)
            {
                case AgentState.Exploring:
                    UpdateExploring(deltaTime, simWorld);
                    break;
                case AgentState.ReturningToNest:
                    UpdateReturning(deltaTime); // Uses this.HomeNest
                    break;
                case AgentState.RestingAtNest:
                    UpdateResting(deltaTime);
                    break;
            }

            Position += Velocity * deltaTime;

            bool bounced = false;
            // Check horizontal boundaries
            if (Position.X - AgentSize / 2 < 0)
            {
                Position.X = AgentSize / 2; // Clamp position
                Velocity.X *= -1;           // Invert X velocity
                bounced = true;
            }
            else if (Position.X + AgentSize / 2 > simWorld.Width)
            {
                Position.X = simWorld.Width - AgentSize / 2; // Clamp position
                Velocity.X *= -1;                            // Invert X velocity
                bounced = true;
            }

            // Check vertical boundaries
            if (Position.Y - AgentSize / 2 < 0)
            {
                Position.Y = AgentSize / 2; // Clamp position
                Velocity.Y *= -1;           // Invert Y velocity
                bounced = true;
            }
            else if (Position.Y + AgentSize / 2 > simWorld.Height)
            {
                Position.Y = simWorld.Height - AgentSize / 2; // Clamp position
                Velocity.Y *= -1;                             // Invert Y velocity
                bounced = true;
            }

            // If bounced, update heading angle to match new velocity
            if (bounced && Velocity.LengthSquared() > 0.001f)
            {
                CurrentHeadingAngle = (float)System.Math.Atan2(Velocity.Y, Velocity.X);
            }
            // --- END OF BOUNDARY CHECKS ---
            
            // Keep heading angle synchronized with velocity if not bounced (or if velocity became zero)
            // This was the old logic, now only needed if no bounce occurred or to recalc if velocity is non-zero
            if (!bounced && Velocity.LengthSquared() > 0.001f)
            {
                CurrentHeadingAngle = (float)System.Math.Atan2(Velocity.Y, Velocity.X);
            }

            if (Velocity.LengthSquared() > 0.001f)
            {
                CurrentHeadingAngle = (float)System.Math.Atan2(Velocity.Y, Velocity.X);
            }
        }

        private void UpdateExploring(float deltaTime, SimWorld simWorld)
        {
            float wanderStrength = 1.5f;
            CurrentHeadingAngle += (_random.NextSingle() * 2f - 1f) * wanderStrength * deltaTime;
            Velocity = new Vector2((float)Cos(CurrentHeadingAngle), (float)Sin(CurrentHeadingAngle)) * MovementSpeed;

            simWorld.AddPheromone(Position, NestId, 1.0f);

            // Example: Query foreign pheromones
            List<PheromoneSignal> foreignSignals = simWorld.QueryPheromonesInRadius(Position, DetectionRadius, NestId, true);
            if (foreignSignals.Count > 0)
            {
                CurrentState = AgentState.ReturningToNest;
                // Basic logic to head towards nest, could be more sophisticated (e.g. away from avg foreign signal)
                Vector2 directionToNest = this.HomeNest.Position - Position;
                if (directionToNest.LengthSquared() > 0)
                {
                    Velocity = Vector2.Normalize(directionToNest) * MovementSpeed;
                }
            }
        }

        private void UpdateReturning(float deltaTime) // Uses this.HomeNest
        {
            Vector2 directionToNest = this.HomeNest.Position - Position;
            if (directionToNest.LengthSquared() > 0.001f)
            {
                directionToNest.Normalize();
                Velocity = directionToNest * MovementSpeed;
            }
            else
            {
                Velocity = Vector2.Zero; // Arrived or very close
            }

            if (this.HomeNest.IsPositionInside(Position))
            {
                CurrentState = AgentState.RestingAtNest;
                _restTimer = RestDuration;
                Velocity = Vector2.Zero;
            }
        }

        private void UpdateResting(float deltaTime) // Uses this.HomeNest
        {
            _restTimer -= deltaTime;
            if (_restTimer <= 0)
            {
                CurrentState = AgentState.Exploring;
                CurrentHeadingAngle = _random.NextSingle() * MathHelper.TwoPi;
                Velocity = new Vector2((float)Cos(CurrentHeadingAngle), (float)Sin(CurrentHeadingAngle)) * MovementSpeed;
                Position = this.HomeNest.Position; // Ensure starting from nest center
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            Rectangle destRect = new Rectangle(
                (int)(Position.X - AgentSize / 2),
                (int)(Position.Y - AgentSize / 2),
                (int)AgentSize,
                (int)AgentSize
            );
            spriteBatch.Draw(pixelTexture, destRect, AgentColor);
        }
    }
}