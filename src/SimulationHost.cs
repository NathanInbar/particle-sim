using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using particle_sim.Core.Graphics;
using particle_sim.Simulation.Boids;
using particle_sim.Simulation.Spatial;

namespace particle_sim
{
    public class SimulationHost : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Camera2D _camera;
        private Random _random;

        private List<Boid> _boids;
        private SpatialGrid _spatialGrid;
        private Texture2D _boidTexture;

        private const int MaxBoids = 200;
        private float _boidSpawnTimer = 0f;
        private const float BoidSpawnInterval = 0.05f;

        private Rectangle _simulationAreaBounds; // Overall area for grid and initial spawn
        private Vector2 _mouseWorldPosition;

        // Adjusted Default Boid Parameters
        private const float DefaultBoidSize = 8f;
        private const float DefaultMaxSpeed = 150f;
        private const float DefaultMinSpeed = 30f;
        private const float DefaultMaxForce = 15f;
        private const float DefaultPerceptionRadius = 70f;
        private const float DefaultSeparationRadiusFactor = 0.4f;
        private const float DefaultSeparationWeight = 2.0f;
        private const float DefaultAlignmentWeight = 1.2f;
        private const float DefaultCohesionWeight = 1.2f;
        private const float DefaultGoalWeight = 2.0f;
        private const float DefaultBoundaryAvoidanceWeight = 3.0f;
        private const float DefaultBoundaryMargin = 30f;


        public SimulationHost()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
        }

        protected override void Initialize()
        {
            _random = new Random();
            _boids = new List<Boid>(MaxBoids);

            _simulationAreaBounds = new Rectangle(0, 0, 1920, 1080);

            float cellSize = DefaultPerceptionRadius * 1.1f;
            _spatialGrid = new SpatialGrid(_simulationAreaBounds, cellSize);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _camera = new Camera2D(GraphicsDevice.Viewport);
            _camera.Position = new Vector2(_simulationAreaBounds.Width / 2f, _simulationAreaBounds.Height / 2f);
            _camera.Zoom = 1.0f;

            _boidTexture = CreateTriangleTexture(10, 16);

            for (int i = 0; i < 75; i++) // Initial population
            {
                Vector2 randomPosition = new Vector2(
                    _random.Next(_simulationAreaBounds.X, _simulationAreaBounds.X + _simulationAreaBounds.Width),
                    _random.Next(_simulationAreaBounds.Y, _simulationAreaBounds.Y + _simulationAreaBounds.Height)
                );
                SpawnBoid(randomPosition);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            MouseState currentMouseState = Mouse.GetState();
            _mouseWorldPosition = _camera.ScreenToWorld(new Vector2(currentMouseState.X, currentMouseState.Y));

            _boidSpawnTimer += deltaTime;
            if (_boidSpawnTimer >= BoidSpawnInterval && _boids.Count(b => b.IsAlive) < MaxBoids)
            {
                SpawnBoid(_mouseWorldPosition);
                _boidSpawnTimer = 0f;
            }

            // --- Calculate visible world bounds based on camera
            Vector2 viewTopLeft = _camera.ScreenToWorld(Vector2.Zero);
            Vector2 viewBottomRight = _camera.ScreenToWorld(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
            Rectangle visibleWorldBounds = new Rectangle(
                (int)viewTopLeft.X,
                (int)viewTopLeft.Y,
                (int)(viewBottomRight.X - viewTopLeft.X),
                (int)(viewBottomRight.Y - viewTopLeft.Y)
            );

            _spatialGrid.Clear();
            foreach (Boid boid in _boids)
            {
                if (boid.IsAlive)
                {
                    _spatialGrid.Add(boid);
                }
            }

            foreach (Boid boid in _boids)
            {
                if (boid.IsAlive)
                {
                    List<Boid> neighbors = _spatialGrid.GetNeighbors(boid, boid.PerceptionRadius);

                    boid.ApplySteeringForces(neighbors, _mouseWorldPosition, visibleWorldBounds);
                    boid.UpdatePhysics(deltaTime);
                }
            }

            HandleCameraInput(deltaTime);
            base.Update(gameTime);
        }

        private void HandleCameraInput(float deltaTime)
        {
            KeyboardState kbState = Keyboard.GetState();

            // Adjust camera speed based on zoom
            float cameraSpeed = 300f * deltaTime / Math.Max(0.1f, _camera.Zoom);


            if (kbState.IsKeyDown(Keys.A)) _camera.Position -= new Vector2(cameraSpeed, 0);
            if (kbState.IsKeyDown(Keys.D)) _camera.Position += new Vector2(cameraSpeed, 0);
            if (kbState.IsKeyDown(Keys.W)) _camera.Position -= new Vector2(0, cameraSpeed);
            if (kbState.IsKeyDown(Keys.S)) _camera.Position += new Vector2(0, cameraSpeed);

            if (kbState.IsKeyDown(Keys.OemPlus) || kbState.IsKeyDown(Keys.PageUp)) _camera.Zoom += 0.8f * deltaTime;
            if (kbState.IsKeyDown(Keys.OemMinus) || kbState.IsKeyDown(Keys.PageDown)) _camera.Zoom -= 0.8f * deltaTime;

            _camera.Zoom = MathHelper.Clamp(_camera.Zoom, 0.2f, 3f);
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                null,
                _camera.GetViewMatrix()
            );

            foreach (Boid boid in _boids)
            {
                if (boid.IsAlive)
                {
                    boid.Draw(_spriteBatch, _boidTexture);
                }
            }

            if (_boidTexture != null)
            {
                _spriteBatch.Draw(
                    _boidTexture,
                    _mouseWorldPosition,
                    null,
                    Color.Red,
                    0f,
                    new Vector2(_boidTexture.Width / 2f, _boidTexture.Height / 2f),
                    0.7f,
                    SpriteEffects.None,
                    0f);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void SpawnBoid(Vector2 spawnPosition)
        {
            Boid boid = _boids.FirstOrDefault(p => !p.IsAlive);

            if (boid == null)
            {
                if (_boids.Count < MaxBoids)
                {
                    boid = new Boid();
                    _boids.Add(boid);
                }
                else
                {
                    return;
                }
            }

            float angle = (float)(_random.NextDouble() * MathHelper.TwoPi);
            float initialSpeedFactor = (float)(_random.NextDouble() * 0.4f + 0.3f); // 30% to 70% of MaxSpeed
            Vector2 velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (DefaultMaxSpeed * initialSpeedFactor);

            Color color = new Color(
                (float)_random.NextDouble() * 0.5f + 0.5f,
                (float)_random.NextDouble() * 0.5f + 0.5f,
                (float)_random.NextDouble() * 0.5f + 0.5f
            );

            boid.MaxSpeed = DefaultMaxSpeed + (float)(_random.NextDouble() * 60f - 30f); // e.g., 120-180
            boid.MinSpeed = DefaultMinSpeed + (float)(_random.NextDouble() * 20f - 10f); // e.g., 20-40
            boid.MaxForce = DefaultMaxForce + (float)(_random.NextDouble() * 10f - 5f);   // e.g., 10-20
            boid.PerceptionRadius = DefaultPerceptionRadius + (float)(_random.NextDouble() * 40f - 20f); // e.g., 50-90
            boid.SeparationRadius = boid.PerceptionRadius * (DefaultSeparationRadiusFactor + (float)(_random.NextDouble() * 0.2f - 0.1f)); // 30-50% of perception

            boid.SeparationWeight = DefaultSeparationWeight + (float)(_random.NextDouble() * 0.5f - 0.25f);
            boid.AlignmentWeight = DefaultAlignmentWeight + (float)(_random.NextDouble() * 0.5f - 0.25f);
            boid.CohesionWeight = DefaultCohesionWeight + (float)(_random.NextDouble() * 0.5f - 0.25f);
            boid.GoalWeight = DefaultGoalWeight + (float)(_random.NextDouble() * 0.4f - 0.2f);
            boid.BoundaryAvoidanceWeight = DefaultBoundaryAvoidanceWeight;
            boid.BoundaryMargin = DefaultBoundaryMargin;

            boid.Spawn(spawnPosition, velocity, color, DefaultBoidSize, _random);
        }

        private Texture2D CreateTriangleTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(GraphicsDevice, width, height);
            Color[] data = new Color[width * height];
            float halfHeight = (height - 1) / 2f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float yDistFromCenter = Math.Abs(y - halfHeight);

                    float xEdge = (width - 1) * (1 - (yDistFromCenter / halfHeight));

                    if (x <= xEdge)
                    {
                        data[y * width + x] = Color.LightCyan; // Changed color for visibility
                    }
                    else
                    {
                        data[y * width + x] = Color.Transparent;
                    }
                }
            }
            texture.SetData(data);
            return texture;
        }
    }
}