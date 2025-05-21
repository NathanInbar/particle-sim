using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using particle_sim.Simulation.Boids;

namespace particle_sim.Simulation.Spatial
{
    public class SpatialGrid
    {
        private List<Boid>[,] _cells; // hold boids in each cell
        private int _columns;
        private int _rows;
        private float _cellSize;
        private Rectangle _worldBounds; // The total area this grid covers

        /// <summary>
        /// Initializes a new spatial grid.
        /// </summary>
        /// <param name="worldBounds">The total rectangular area the grid should cover.</param>
        /// <param name="cellSize">The size of each square cell in the grid. Should ideally be >= max perception radius.</param>
        public SpatialGrid(Rectangle worldBounds, float cellSize)
        {
            _worldBounds = worldBounds;
            _cellSize = cellSize;
            _columns = (int)Math.Ceiling(worldBounds.Width / _cellSize);
            _rows = (int)Math.Ceiling(worldBounds.Height / _cellSize);

            _cells = new List<Boid>[_columns, _rows];
            for (int x = 0; x < _columns; x++)
            {
                for (int y = 0; y < _rows; y++)
                {
                    _cells[x, y] = new List<Boid>();
                }
            }
        }

        /// <summary>
        /// Clears all boids from all cells in the grid. Call this each frame before adding boids.
        /// </summary>
        public void Clear()
        {
            for (int x = 0; x < _columns; x++)
            {
                for (int y = 0; y < _rows; y++)
                {
                    _cells[x, y].Clear();
                }
            }
        }

        /// <summary>
        /// Adds a boid to the appropriate cell in the grid.
        /// </summary>
        public void Add(Boid boid)
        {
            Point cellIndex = GetCellIndex(boid.Position);

            // Clamp cell indices to be within grid bounds
            int x = Math.Max(0, Math.Min(cellIndex.X, _columns - 1));
            int y = Math.Max(0, Math.Min(cellIndex.Y, _rows - 1));

            _cells[x, y].Add(boid);
        }

        /// <summary>
        /// Gets a list of potential neighbors for a given boid within a specified perception radius.
        /// </summary>
        public List<Boid> GetNeighbors(Boid boid, float perceptionRadius)
        {
            List<Boid> potentialNeighbors = new List<Boid>();
            Point centerCellIndex = GetCellIndex(boid.Position);

            // Determine the search range of cells
            int searchRadiusInCells = (int)Math.Ceiling(perceptionRadius / _cellSize);

            int minX = Math.Max(0, centerCellIndex.X - searchRadiusInCells);
            int maxX = Math.Min(_columns - 1, centerCellIndex.X + searchRadiusInCells);
            int minY = Math.Max(0, centerCellIndex.Y - searchRadiusInCells);
            int maxY = Math.Min(_rows - 1, centerCellIndex.Y + searchRadiusInCells);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    // Add all boids from these cells to the potential neighbors list
                    potentialNeighbors.AddRange(_cells[x, y]);
                }
            }
            return potentialNeighbors;
        }

        /// <summary>
        /// Converts a world position to a grid cell index.
        /// </summary>
        private Point GetCellIndex(Vector2 position)
        {
            // Adjust position relative to the grid's origin
            float relativeX = position.X - _worldBounds.X;
            float relativeY = position.Y - _worldBounds.Y;

            int x = (int)(relativeX / _cellSize);
            int y = (int)(relativeY / _cellSize);
            return new Point(x, y);
        }

    }
}