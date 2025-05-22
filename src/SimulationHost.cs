using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using particle_sim.Simulation.Environment;
using particle_sim.Simulation.Agents;
using particle_sim.Core.Graphics;

namespace particle_sim
{
    public class SimulationHost : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Camera2D _camera;

        // Simulation Components
        private SimWorld _environment;
        private List<Nest> _nests;
        private List<Agent> _agents;
        private Dictionary<int, Color> _nestColorsById; // For passing colors to environment rendering

        private Texture2D _pixelTexture; // For drawing simple square

        // Simulation Parameters
        private const int EnvironmentWidth = 1200;
        private const int EnvironmentHeight = 800;
        private const int NumNests = 4;
        private const int AgentsPerNest = 100;
        // - - -
        private readonly Color _simWorldBackgroundColor = new Color(33, 33, 33);
        private readonly Color _cameraBackgroundColor = Color.Black;

        private Random _random;

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
            _nestColorsById = new Dictionary<int, Color>();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Init camera
            _camera = new Camera2D(GraphicsDevice.Viewport);
            _camera.Position = new Vector2(EnvironmentWidth / 2f, EnvironmentHeight / 2f);
            _camera.Zoom = 0.75f; // Adjusted zoom for potentially larger area

            // 1x1 white pixel texture for drawing little squares
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            // Init the simulation world 
            _environment = new SimWorld(GraphicsDevice, EnvironmentWidth, EnvironmentHeight);

            // Init Nests / Agents
            _nests = new List<Nest>();
            _agents = new List<Agent>();

            // base colors for nests
            Color[] baseNestColors = { Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Purple };

            for (int i = 0; i < NumNests; i++)
            {
                Vector2 nestPosition = new Vector2(
                    _random.Next(100, EnvironmentWidth - 100),
                    _random.Next(100, EnvironmentHeight - 100)
                );
                Color agentAndBaseNestColor = baseNestColors[i % baseNestColors.Length]; // color for agents
                float nestSize = 30f;
                
                // Create nests and agents
                Nest newNest = new Nest(i, nestPosition, agentAndBaseNestColor, nestSize);
                _nests.Add(newNest);
                _nestColorsById[i] = agentAndBaseNestColor; 

                for (int j = 0; j < AgentsPerNest; j++)
                {
                    Agent agent = newNest.SpawnAgent();
                    _agents.Add(agent);
                }
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Update environment
            _environment.UpdateLogic(gameTime);

            // Update agents
            foreach (Agent agent in _agents)
                agent.Update(gameTime, _environment);

            // Camera controls
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float cameraSpeed = 300f;
            if (Keyboard.GetState().IsKeyDown(Keys.Left)) _camera.Position += new Vector2(-cameraSpeed * deltaTime, 0);
            if (Keyboard.GetState().IsKeyDown(Keys.Right)) _camera.Position += new Vector2(cameraSpeed * deltaTime, 0);
            if (Keyboard.GetState().IsKeyDown(Keys.Up)) _camera.Position += new Vector2(0, -cameraSpeed * deltaTime);
            if (Keyboard.GetState().IsKeyDown(Keys.Down)) _camera.Position += new Vector2(0, cameraSpeed * deltaTime);
            if (Keyboard.GetState().IsKeyDown(Keys.OemPlus) || Keyboard.GetState().IsKeyDown(Keys.Add)) _camera.Zoom += 0.5f * deltaTime;
            if (Keyboard.GetState().IsKeyDown(Keys.OemMinus) || Keyboard.GetState().IsKeyDown(Keys.Subtract)) _camera.Zoom -= 0.5f * deltaTime;
            _camera.Zoom = MathHelper.Clamp(_camera.Zoom, 0.1f, 5f); // Prevent extreme zoom

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_cameraBackgroundColor);

            // Update the pheromone render layer before drawing it to the screen
            _environment.UpdatePheromoneRenderLayer(GraphicsDevice, _spriteBatch, _nestColorsById, _pixelTexture);

            // Begin the sprite batch 
            _spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix());

            // Draw environment background
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, EnvironmentWidth, EnvironmentHeight), _simWorldBackgroundColor);

            // Draw pheromone layer
            _environment.Draw(_spriteBatch);

            // Draw nests
            foreach (Nest nest in _nests)
                nest.Draw(_spriteBatch, _pixelTexture);

            // Draw agents
            foreach (Agent agent in _agents)
                agent.Draw(_spriteBatch, _pixelTexture);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}