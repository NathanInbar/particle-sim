using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using particle_sim.Simulation.Core;
using particle_sim.Simulation.Environment;
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

        // used for wandering when trail is lost
        private Vector2 _lastKnownGoodDirection;
        private float _lostTrailWanderTimer;
        private const float MaxLostTrailWanderTime = 8.3f;

        private static System.Random _random = new System.Random();

        public Agent(Nest homeNest, Vector2 initialVelocity)
        {
            HomeNest = homeNest;
            NestId = homeNest.NestId;
            Position = homeNest.Position;
            AgentColor = homeNest.NestBaseColor;

            Velocity = initialVelocity;
            CurrentHeadingAngle = (float)System.Math.Atan2(Velocity.Y, Velocity.X);
            CurrentState = AgentState.Exploring;
            MovementSpeed = initialVelocity.Length(); 
            _lastKnownGoodDirection = Vector2.Normalize(initialVelocity); // Initialize with starting direction
        }

        public void Update(GameTime gameTime, SimWorld simWorld)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (CurrentState)
            {
                case AgentState.Exploring:
                    UpdateExploring(deltaTime, simWorld);
                    break;
                case AgentState.ReturningToNest:
                    UpdateReturning(deltaTime, simWorld); 
                    break;
                case AgentState.RestingAtNest:
                    UpdateResting(deltaTime);
                    break;
            }

            Position += Velocity * deltaTime;

            // world boundary checks
            if (Position.X - AgentSize / 2 < 0)
            {
                Position.X = AgentSize / 2;
                Velocity.X *= -1;
            }
            else if (Position.X + AgentSize / 2 > simWorld.Width)
            {
                Position.X = simWorld.Width - AgentSize / 2;
                Velocity.X *= -1;
            }

            if (Position.Y - AgentSize / 2 < 0)
            {
                Position.Y = AgentSize / 2; 
                Velocity.Y *= -1;           
            }
            else if (Position.Y + AgentSize / 2 > simWorld.Height)
            {
                Position.Y = simWorld.Height - AgentSize / 2; 
                Velocity.Y *= -1;                             
            }
            
            if (Velocity.LengthSquared() > 0.001f) // Update heading if moving
            {
                CurrentHeadingAngle = (float)System.Math.Atan2(Velocity.Y, Velocity.X);
            }
        }

        private void UpdateExploring(float deltaTime, SimWorld simWorld)
        {
            float wanderStrength = 1.5f;
            CurrentHeadingAngle += (_random.NextSingle() * 2f - 1f) * wanderStrength * deltaTime;
            Velocity = new Vector2((float)Cos(CurrentHeadingAngle), (float)Sin(CurrentHeadingAngle)) * MovementSpeed;
            _lastKnownGoodDirection = Vector2.Normalize(Velocity); // Keep track of exploring direction

            simWorld.AddPheromone(Position, NestId, 1.0f);

            List<PheromoneSignal> foreignSignals = simWorld.QueryPheromonesInRadius(Position, DetectionRadius, NestId, true);
            
            // switching to nest return state, finds inital return direction
            if (foreignSignals.Count > 0)
            {
                CurrentState = AgentState.ReturningToNest;
                _lostTrailWanderTimer = 0;

                // Initial direction towards nest
                Vector2 directionToNest = this.HomeNest.Position - Position;
                if (directionToNest.LengthSquared() > 0)
                {
                    _lastKnownGoodDirection = Vector2.Normalize(directionToNest);
                    Velocity = _lastKnownGoodDirection * MovementSpeed;
                }
            }
        }

        private void UpdateReturning(float deltaTime, SimWorld simWorld)
        {
            List<PheromoneSignal> homeSignals = simWorld.QueryPheromonesInRadius(Position, DetectionRadius, this.NestId, false);

            // If home signal pheromone found try to follow it
            if (homeSignals.Count > 0)
            {
                Vector2 averagePheromonePosition = Vector2.Zero;
                float totalStrength = 0f;

                foreach (PheromoneSignal signal in homeSignals)
                {
                    averagePheromonePosition += signal.Position * signal.Strength;
                    totalStrength += signal.Strength;
                }

                if (totalStrength > 0)
                {
                    averagePheromonePosition /= totalStrength;
                }
                else if (homeSignals.Count > 0)
                {
                    foreach (PheromoneSignal signal in homeSignals)
                    {
                        averagePheromonePosition += signal.Position;
                    }
                    averagePheromonePosition /= homeSignals.Count;
                }

                Vector2 directionToAveragePheromone = averagePheromonePosition - Position;
                if (directionToAveragePheromone.LengthSquared() > 0.001f)
                {
                    directionToAveragePheromone.Normalize();
                    Velocity = directionToAveragePheromone * MovementSpeed;
                    _lastKnownGoodDirection = directionToAveragePheromone;
                    _lostTrailWanderTimer = 0; // Reset timer since we are on a trail
                }
            }
            else
            {
                // No home pheromones detected so wander in the last known good direction
                if (_lostTrailWanderTimer < MaxLostTrailWanderTime && _lastKnownGoodDirection.LengthSquared() > 0.001f)
                {
                    Velocity = _lastKnownGoodDirection * MovementSpeed;
                    _lostTrailWanderTimer += deltaTime;
                }
                else
                {
                    // Wander time expired or no last good direction, revert to direct to nest

                    Vector2 directionToNest = this.HomeNest.Position - Position;
                    if (directionToNest.LengthSquared() > 0.001f)
                    {
                        directionToNest.Normalize();
                        Velocity = directionToNest * MovementSpeed;
                    }
                    else Velocity = Vector2.Zero;
                }
            }

            if (this.HomeNest.IsPositionInside(Position))
            {
                CurrentState = AgentState.RestingAtNest;
                _restTimer = RestDuration;
                Velocity = Vector2.Zero;
            }
        }

        private void UpdateResting(float deltaTime)
        {
            _restTimer -= deltaTime;
            if (_restTimer <= 0)
            {
                CurrentState = AgentState.Exploring;
                CurrentHeadingAngle = _random.NextSingle() * MathHelper.TwoPi;
                Position = HomeNest.Position; // Spawn at nest center
                Velocity = new Vector2((float)Cos(CurrentHeadingAngle), (float)Sin(CurrentHeadingAngle)) * MovementSpeed;
                _lastKnownGoodDirection = Vector2.Normalize(Velocity);
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