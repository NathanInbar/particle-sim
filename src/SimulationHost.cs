using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using particle_sim.Core.Graphics;
using particle_sim.Simulation.Particles;


namespace particle_sim;

public class SimulationHost : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Camera2D _camera;

    // particles
    private List<Particle> _particles;
    private Texture2D _pixelTexture;
    private Random _random;

    private const int MaxParticles = 500;
    private float _particleSpawnTimer = 0f;
    private const float ParticleSpawnInterval = 0.01f;
    // - - -

    public SimulationHost()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // initialize camera
        _camera = new Camera2D(GraphicsDevice.Viewport);
        _camera.Position = new Vector2(100, 100);
        _camera.Zoom = 1.5f;

        _random = new Random();

        // Create a 1x1 white pixel texture
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        _particles = new List<Particle>(MaxParticles);

        // Spawn some initial particles for testing
        for (int i = 0; i < 50; i++)
        {
            SpawnParticle(Vector2.Zero); // Spawn near the world origin
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // particles
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Update particle spawn timer
        _particleSpawnTimer += deltaTime;
        if (_particleSpawnTimer >= ParticleSpawnInterval)
        {
            Vector2 spawnPos = _camera.ScreenToWorld(new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f)); // Center of screen
            SpawnParticle(spawnPos);
            _particleSpawnTimer = 0f;
        }


        // Update all active particles
        foreach (var particle in _particles)
        {
            if (particle.IsAlive)
            {
                particle.Update(deltaTime);
            }
        }

        // - - -

        // Camera Controls
        if (Keyboard.GetState().IsKeyDown(Keys.Left)) _camera.Position += new Vector2(-100f * deltaTime, 0);
        if (Keyboard.GetState().IsKeyDown(Keys.Right)) _camera.Position += new Vector2(100f * deltaTime, 0);
        if (Keyboard.GetState().IsKeyDown(Keys.Up)) _camera.Position += new Vector2(0, -100f * deltaTime);
        if (Keyboard.GetState().IsKeyDown(Keys.Down)) _camera.Position += new Vector2(0, 100f * deltaTime);
        if (Keyboard.GetState().IsKeyDown(Keys.OemPlus)) _camera.Zoom += 0.5f * deltaTime;
        if (Keyboard.GetState().IsKeyDown(Keys.OemMinus)) _camera.Zoom -= 0.5f * deltaTime;


        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix());
        
        // Draw particles
        foreach (var particle in _particles)
        {
            if (particle.IsAlive)
            {
                Rectangle destinationRectangle = new Rectangle(
                    (int)particle.Position.X,
                    (int)particle.Position.Y,
                    (int)particle.Size,
                    (int)particle.Size
                );
                _spriteBatch.Draw(_pixelTexture, destinationRectangle, particle.Color);
            }
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }



    // HELPERS
    private void SpawnParticle(Vector2 spawnPosition)
    {
        // Simple object pooling
        Particle particle = _particles.FirstOrDefault(p => !p.IsAlive);

        if (particle == null)
        {
            if (_particles.Count < MaxParticles)
            {
                particle = new Particle();
                _particles.Add(particle);
            }
            else
            {
                return; // Max particles reached, cannot spawn more
            }
        }

        // particle properties
        float angle = (float)(_random.NextDouble() * MathHelper.TwoPi); // Random direction
        float speed = (float)(_random.NextDouble() * 100f + 50f);      // Random speed (pixels/sec)
        Vector2 velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;

        // Random color
        Color color = new Color(
            (float)_random.NextDouble() * 0.5f + 0.5f, // R
            (float)_random.NextDouble() * 0.5f + 0.5f, // G
            (float)_random.NextDouble() * 0.5f + 0.5f  // B
        );

        float size = (float)(_random.NextDouble() * 4f + 2f);          // Random size (2 to 6 pixels)
        float lifetime = (float)(_random.NextDouble() * 2f + 1f);       // Random lifetime (1 to 3 seconds)

        particle.Spawn(spawnPosition, velocity, color, size, lifetime);
    }
}
