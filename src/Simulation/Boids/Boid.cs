using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace particle_sim.Simulation.Boids
{
    public class Boid
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Acceleration;
        public Color Color;
        public float Size;
        public bool IsAlive;
        public float Orientation => (Velocity.LengthSquared() > Epsilon * Epsilon) ? (float)Math.Atan2(Velocity.Y, Velocity.X) : 0f; // Avoid Atan2(0,0)

        public float MaxSpeed { get; set; }
        public float MinSpeed { get; set; }
        public float MaxForce { get; set; }
        public float PerceptionRadius { get; set; }
        public float SeparationRadius { get; set; }

        public float SeparationWeight { get; set; }
        public float AlignmentWeight { get; set; }
        public float CohesionWeight { get; set; }
        public float GoalWeight { get; set; }
        public float BoundaryAvoidanceWeight { get; set; }
        public float BoundaryMargin { get; set; }

        private const float Epsilon = 0.0001f;

        public Boid()
        {
            IsAlive = false;
            MaxSpeed = 150f;
            MinSpeed = 20f;
            MaxForce = 10f;
            PerceptionRadius = 75f;
            SeparationRadius = 30f;
            SeparationWeight = 1.5f;
            AlignmentWeight = 1.0f;
            CohesionWeight = 1.0f;
            GoalWeight = 0.5f;
            BoundaryAvoidanceWeight = 2.0f;
            BoundaryMargin = 50f;
        }

        public void Spawn(Vector2 position, Vector2 initialVelocity, Color color, float size, Random random)
        {
            Position = position;
            Velocity = initialVelocity;
            Acceleration = Vector2.Zero;
            Color = color;
            Size = size;
            IsAlive = true;

            // Ensure initial velocity stays within speed limit
            if (Velocity.LengthSquared() > MaxSpeed * MaxSpeed)
            {
                Velocity.Normalize();
                Velocity *= MaxSpeed;
            }
            else if (Velocity.LengthSquared() < MinSpeed * MinSpeed && Velocity.LengthSquared() > Epsilon)
            {
                Velocity.Normalize();
                Velocity *= MinSpeed;
            }
        }

        public void ApplySteeringForces(List<Boid> neighbors, Vector2 goalPosition, Rectangle currentVisibleBounds)
        {
            if (!IsAlive) return;
            Acceleration = Vector2.Zero;

            Vector2 separationForce = CalculateSeparation(neighbors);
            Vector2 alignmentForce = CalculateAlignment(neighbors);
            Vector2 cohesionForce = CalculateCohesion(neighbors);
            Vector2 goalForce = CalculateGoalSeeking(goalPosition);
            Vector2 boundaryForce = CalculateBoundaryAvoidance(currentVisibleBounds);

            Acceleration += separationForce * SeparationWeight;
            Acceleration += alignmentForce * AlignmentWeight;
            Acceleration += cohesionForce * CohesionWeight;
            Acceleration += goalForce * GoalWeight;
            Acceleration += boundaryForce * BoundaryAvoidanceWeight;
        }

        public void UpdatePhysics(float deltaTime)
        {
            if (!IsAlive) return;

            if (Acceleration.LengthSquared() > MaxForce * MaxForce)
            {
                Acceleration.Normalize();
                Acceleration *= MaxForce;
            }

            Velocity += Acceleration * deltaTime;

            float speedSq = Velocity.LengthSquared();
            if (speedSq > MaxSpeed * MaxSpeed)
            {
                Velocity.Normalize();
                Velocity *= MaxSpeed;
            }
            else if (speedSq < MinSpeed * MinSpeed && speedSq > Epsilon) // Apply MinSpeed
            {
                Velocity.Normalize();
                Velocity *= MinSpeed;
            }
            else if (speedSq <= Epsilon && Acceleration.LengthSquared() <= Epsilon)
            {
                // If almost stationary and no acceleration, give a tiny nudge to prevent getting stuck
                // (Can happen if forces cancel out.)
                Velocity = new Vector2((float)(new Random().NextDouble() * 2 - 1), (float)(new Random().NextDouble() * 2 - 1)) * MinSpeed * 0.1f;
            }


            Position += Velocity * deltaTime;
        }

        private Vector2 CalculateSeparation(List<Boid> neighbors)
        {
            Vector2 steer = Vector2.Zero;
            int count = 0;
            foreach (Boid other in neighbors)
            {
                if (!other.IsAlive || other == this) continue;
                float distanceSq = Vector2.DistanceSquared(Position, other.Position); // Use DistanceSquared for efficiency
                if (distanceSq > Epsilon * Epsilon && distanceSq < SeparationRadius * SeparationRadius)
                {
                    Vector2 diff = Position - other.Position;
                    // Weight by inverse distance (stronger repulsion for closer boids)
                    if (diff.LengthSquared() > Epsilon)
                    {
                         diff.Normalize();
                         steer += diff;
                         count++;
                    }
                }
            }
            if (count > 0)
            {
                steer /= count;
            }
            if (steer.LengthSquared() > Epsilon)
            {
                steer.Normalize();
                steer *= MaxSpeed;
                steer -= Velocity;
            }
            return steer;
        }

        private Vector2 CalculateAlignment(List<Boid> neighbors)
        {
            Vector2 sumVelocity = Vector2.Zero;
            int count = 0;
            foreach (Boid other in neighbors)
            {
                if (!other.IsAlive || other == this) continue;

                // Assuming neighbors are already within PerceptionRadius due to SpatialGrid
                sumVelocity += other.Velocity;
                count++;
            }
            if (count > 0)
            {
                sumVelocity /= count;
                if (sumVelocity.LengthSquared() > Epsilon)
                {
                    sumVelocity.Normalize();
                    sumVelocity *= MaxSpeed;
                    Vector2 steer = sumVelocity - Velocity;
                    return steer;
                }
            }
            return Vector2.Zero;
        }

        private Vector2 CalculateCohesion(List<Boid> neighbors)
        {
            Vector2 sumPosition = Vector2.Zero;
            int count = 0;
            foreach (Boid other in neighbors)
            {
                if (!other.IsAlive || other == this) continue;

                // Assuming neighbors are already within PerceptionRadius
                sumPosition += other.Position;
                count++;
            }
            if (count > 0)
            {
                sumPosition /= count;
                return Seek(sumPosition);
            }
            return Vector2.Zero;
        }

        private Vector2 CalculateGoalSeeking(Vector2 targetPosition)
        {
            return Seek(targetPosition);
        }

        private Vector2 Seek(Vector2 target)
        {
            Vector2 desiredVelocity = target - Position;
            if (desiredVelocity.LengthSquared() > Epsilon) // Check before normalize
            {
                desiredVelocity.Normalize();
                desiredVelocity *= MaxSpeed;
                Vector2 steer = desiredVelocity - Velocity;
                return steer;
            }
            return Vector2.Zero;
        }

        /// <summary>
        /// Calculates steering force to stay within the given dynamic bounds.
        /// </summary>
        private Vector2 CalculateBoundaryAvoidance(Rectangle dynamicVisibleBounds)
        {
            Vector2 desiredVelocity = Vector2.Zero;
            bool applyForce = false;

            // Using the boid's current world position directly with the dynamic world bounds
            if (Position.X < dynamicVisibleBounds.Left + BoundaryMargin)
            {
                desiredVelocity.X = MaxSpeed;
                applyForce = true;
            }
            else if (Position.X > dynamicVisibleBounds.Right - BoundaryMargin)
            {
                desiredVelocity.X = -MaxSpeed;
                applyForce = true;
            }

            if (Position.Y < dynamicVisibleBounds.Top + BoundaryMargin)
            {
                desiredVelocity.Y = MaxSpeed;
                applyForce = true;
            }
            else if (Position.Y > dynamicVisibleBounds.Bottom - BoundaryMargin)
            {
                desiredVelocity.Y = -MaxSpeed;
                applyForce = true;
            }

            if (applyForce && desiredVelocity.LengthSquared() > Epsilon)
            {
                desiredVelocity.Normalize();
                desiredVelocity *= MaxSpeed;
                Vector2 steer = desiredVelocity - Velocity;
                return steer;
            }
            return Vector2.Zero;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            if (!IsAlive || texture == null) return;
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            float scale = Size / Math.Max(texture.Width, texture.Height);
            if (scale < 0.1f) scale = 0.1f; // Prevent texture from becoming too small/invisible

            spriteBatch.Draw(
                texture, Position, null, Color,
                Orientation, origin, scale, SpriteEffects.None, 0f
            );
        }
    }
}