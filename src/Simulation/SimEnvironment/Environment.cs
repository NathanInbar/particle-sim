// src/Simulation/Environment/Environment.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using particle_sim.Simulation.Core; // For PheromoneSignal

namespace particle_sim.Simulation.Environment
{
    public class SimWorld
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        // Spatial Partitioning for Pheromone Detection
        private List<PheromoneSignal>[,] _spatialPheromoneGrid;
        private int _gridCellSize = 20; // Size of each cell in the spatial grid
        private int _gridWidth;
        private int _gridHeight;

        // Pheromone Rendering
        private RenderTarget2D _pheromoneRenderLayer;
        private bool _pheromoneLayerDirty = true; // Flag to redraw render target if needed

        // Pheromone properties
        private const float MaxPheromoneStrength = 10.0f;
        private const float PheromoneDecayRate = 0.5f; // Strength per second

        public SimWorld(GraphicsDevice graphicsDevice, int width, int height)
        {
            Width = width;
            Height = height;

            // Initialize Spatial Grid
            _gridWidth = (Width + _gridCellSize - 1) / _gridCellSize;
            _gridHeight = (Height + _gridCellSize - 1) / _gridCellSize;
            _spatialPheromoneGrid = new List<PheromoneSignal>[_gridWidth, _gridHeight];
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _spatialPheromoneGrid[x, y] = new List<PheromoneSignal>();
                }
            }

            // Initialize Render Target for pheromones
            _pheromoneRenderLayer = new RenderTarget2D(
                graphicsDevice,
                width,
                height,
                false, // mipmap
                SurfaceFormat.Color, // format
                DepthFormat.None,
                0, // preferredMultiSampleCount
                RenderTargetUsage.PreserveContents // Important for incremental drawing/decay
            );
        }

        public void AddPheromone(Vector2 position, int nestId, float strength)
        {
            int gridX = (int)(position.X / _gridCellSize);
            int gridY = (int)(position.Y / _gridCellSize);

            if (gridX >= 0 && gridX < _gridWidth && gridY >= 0 && gridY < _gridHeight)
            {
                // For simplicity in this initial step, let's assume one signal per nest per cell, updating strength.
                // A more robust system might allow multiple distinct signals or average them.
                // Here, we'll just add a new signal each time for now. More complex merging/updating later.
                PheromoneSignal signal = new PheromoneSignal(position, nestId, strength);
                _spatialPheromoneGrid[gridX, gridY].Add(signal);
                _pheromoneLayerDirty = true; // Mark for redraw
            }
        }

        public List<PheromoneSignal> QueryPheromonesInRadius(Vector2 center, float radius, int agentNestId, bool queryForeign)
        {
            List<PheromoneSignal> foundSignals = new List<PheromoneSignal>();
            // Determine the grid cells that overlap with the query radius
            int minGridX = (int)((center.X - radius) / _gridCellSize);
            int maxGridX = (int)((center.X + radius) / _gridCellSize);
            int minGridY = (int)((center.Y - radius) / _gridCellSize);
            int maxGridY = (int)((center.Y + radius) / _gridCellSize);

            for (int x = System.Math.Max(0, minGridX); x <= System.Math.Min(_gridWidth - 1, maxGridX); x++)
            {
                for (int y = System.Math.Max(0, minGridY); y <= System.Math.Min(_gridHeight - 1, maxGridY); y++)
                {
                    foreach (PheromoneSignal signal in _spatialPheromoneGrid[x, y])
                    {
                        if (Vector2.DistanceSquared(center, signal.Position) <= radius * radius)
                        {
                            if (queryForeign && signal.NestId != agentNestId)
                            {
                                foundSignals.Add(signal);
                            }
                            else if (!queryForeign && signal.NestId == agentNestId)
                            {
                                foundSignals.Add(signal);
                            }
                        }
                    }
                }
            }
            return foundSignals;
        }

        public void UpdateLogic(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            bool changed = false;

            // Decay pheromones in the spatial grid
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    List<PheromoneSignal> cellSignals = _spatialPheromoneGrid[x, y];
                    for (int i = cellSignals.Count - 1; i >= 0; i--)
                    {
                        PheromoneSignal signal = cellSignals[i];
                        signal.Age += deltaTime;
                        signal.Strength -= PheromoneDecayRate * deltaTime; // Simple linear decay based on time

                        if (signal.Strength <= 0)
                        {
                            cellSignals.RemoveAt(i);
                            changed = true;
                        }
                        else
                        {
                            cellSignals[i] = signal; // Update struct in list
                        }
                    }
                }
            }
            if (changed) _pheromoneLayerDirty = true;

            // TODO: Implement diffusion if desired
        }
        
        // This method will draw the current state of _spatialPheromoneGrid to _pheromoneRenderLayer
        public void UpdatePheromoneRenderLayer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Dictionary<int, Color> nestColors, Texture2D pixelTexture)
        {
            if (!_pheromoneLayerDirty) return;

            graphicsDevice.SetRenderTarget(_pheromoneRenderLayer);
            graphicsDevice.Clear(Color.Transparent); // Clear with transparent

            spriteBatch.Begin();
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    foreach (PheromoneSignal signal in _spatialPheromoneGrid[x,y])
                    {
                        if (signal.Strength > 0.01f) // Only draw if reasonably strong
                        {
                            Color pheromoneColor = nestColors.GetValueOrDefault(signal.NestId, Color.Gray); // Get color from NestId
                            float alpha = System.Math.Clamp(signal.Strength / MaxPheromoneStrength, 0.1f, 1.0f);
                            
                            // Draw each pheromone signal as a small dot/square
                            // For simplicity, drawing a 1x1 pixel at the exact position.
                            // A small brush texture could be used for softer pheromones.
                            spriteBatch.Draw(pixelTexture, signal.Position, null, pheromoneColor * alpha, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                        }
                    }
                }
            }
            spriteBatch.End();

            graphicsDevice.SetRenderTarget(null); // Reset render target
            _pheromoneLayerDirty = false;
        }


        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw the entire pre-rendered pheromone layer
            spriteBatch.Draw(_pheromoneRenderLayer, Vector2.Zero, Color.White);
        }
    }
}