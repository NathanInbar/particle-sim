// src/SimulationHost.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq; // Keep for potential future use, though not strictly needed in this version

// New using statements for our simulation classes
using particle_sim.Simulation.Core;
using particle_sim.Simulation.Environment;
using particle_sim.Simulation.Agents;
using particle_sim.Core.Graphics; // Assuming Camera2D is here

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

        // Helper Textures
        private Texture2D _pixelTexture; // For drawing simple shapes

        // Simulation Parameters
        private const int EnvironmentWidth = 1200;
        private const int EnvironmentHeight = 800;
        private const int NumNests = 2; // Example: 2 nests
        private const int AgentsPerNest = 50;

        private Random _random; // General purpose random

        // New color for the SimWorld background
        private Color _simWorldBackgroundColor = new Color(220, 220, 220); 
        // New color for the area outside the SimWorld (camera background)
        private Color _cameraBackgroundColor = Color.Black;

        public SimulationHost()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Adjust window size if desired
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

            // Initialize Camera
            _camera = new Camera2D(GraphicsDevice.Viewport);
            _camera.Position = new Vector2(EnvironmentWidth / 2f, EnvironmentHeight / 2f);
            _camera.Zoom = 0.75f; // Adjusted zoom for potentially larger area

            // Create 1x1 white pixel texture for drawing
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            // Initialize Environment
            _environment = new SimWorld(GraphicsDevice, EnvironmentWidth, EnvironmentHeight);

            // Initialize Nests and Agents
            _nests = new List<Nest>();
            _agents = new List<Agent>();

            // Define some base colors for nests
            Color[] baseNestColors = new Color[] { Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Purple };

            for (int i = 0; i < NumNests; i++)
            {
                Vector2 nestPosition = new Vector2(
                    _random.Next(100, EnvironmentWidth - 100),
                    _random.Next(100, EnvironmentHeight - 100)
                );
                Color agentAndBaseNestColor = baseNestColors[i % baseNestColors.Length]; // This color will be for agents
                float nestSize = 30f;
                
                // Nests will store their primary color (used by agents)
                Nest newNest = new Nest(i, nestPosition, agentAndBaseNestColor, nestSize, NestShape.Circle);
                _nests.Add(newNest);
                _nestColorsById[i] = agentAndBaseNestColor; 

                for (int j = 0; j < AgentsPerNest; j++)
                {
                    Agent agent = newNest.SpawnAgent(); // Agent gets the primary (brighter) nest color
                    _agents.Add(agent);
                }
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Update Environment Logic (Pheromone Decay, etc.)
            _environment.UpdateLogic(gameTime);

            // Update Agents
            foreach (Agent agent in _agents)
            {
                // The agent's Update method now uses its internal HomeNest reference
                agent.Update(gameTime, _environment); // Pass HomeNest for context like IsPositionInside
            }

            // Camera Controls (example)
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
            GraphicsDevice.Clear(_cameraBackgroundColor); // A different background color

            // Update the pheromone render layer (texture)
            // This should happen before drawing it to the screen.
            _environment.UpdatePheromoneRenderLayer(GraphicsDevice, _spriteBatch, _nestColorsById, _pixelTexture);

            // Begin SpriteBatch with camera transform
            _spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix());

            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, EnvironmentWidth, EnvironmentHeight), _simWorldBackgroundColor);
            // 1. Draw Environment (Pheromone Layer)
            _environment.Draw(_spriteBatch);

            // 2. Draw Nests
            foreach (Nest nest in _nests)
            {
                nest.Draw(_spriteBatch, _pixelTexture);
            }

            // 3. Draw Agents
            foreach (Agent agent in _agents)
            {
                agent.Draw(_spriteBatch, _pixelTexture);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}