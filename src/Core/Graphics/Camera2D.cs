using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace particle_sim.Core.Graphics
{
    public class Camera2D
    {
        private float _zoom;
        private Matrix _transform;
        private Vector2 _position;
        private float _rotation;
        private Viewport _viewport;
        private Vector2 _origin; // Screen center

        public Camera2D(Viewport viewport)
        {
            _viewport = viewport;
            _zoom = 1.0f;
            _rotation = 0.0f;
            _position = Vector2.Zero;
            _origin = new Vector2(viewport.Width / 2f, viewport.Height / 2f); // Calculate screen center
            UpdateMatrix();
        }

        private void UpdateMatrix()
        {
            _transform = Matrix.CreateTranslation(new Vector3(-_position.X, -_position.Y, 0)) * // Translate to camera position
                         Matrix.CreateRotationZ(_rotation) * // Rotate around origin
                         Matrix.CreateScale(new Vector3(_zoom, _zoom, 1)) * // Zoom
                         Matrix.CreateTranslation(new Vector3(_origin.X, _origin.Y, 0)); // Offset by screen center
        }

        public Matrix GetViewMatrix()
        {
            return _transform;
        }

        public Vector2 Position
        {
            get { return _position; }
            set
            {
                _position = value;
                UpdateMatrix();
            }
        }

        public float Zoom
        {
            get { return _zoom; }
            set
            {
                _zoom = value > 0 ? value : 0.1f; // Prevent zoom from being zero or negative
                UpdateMatrix();
            }
        }

        public float Rotation
        {
            get { return _rotation; }
            set
            {
                _rotation = value;
                UpdateMatrix();
            }
        }

        // Call this if the viewport size changes (e.g., window resize)
        public void UpdateViewport(Viewport newViewport)
        {
            _viewport = newViewport;
            _origin = new Vector2(_viewport.Width / 2f, _viewport.Height / 2f);
            UpdateMatrix(); // Recalculate matrix with new origin if it depends on viewport size
        }

        // Helper to convert screen coordinates to world coordinates
        public Vector2 ScreenToWorld(Vector2 screenPosition)
            => Vector2.Transform(screenPosition, Matrix.Invert(_transform));
        

        // Helper to convert world coordinates to screen coordinates
        public Vector2 WorldToScreen(Vector2 worldPosition)
            => Vector2.Transform(worldPosition, _transform);
        
    }
}