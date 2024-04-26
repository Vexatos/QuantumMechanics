using Monocle;
using Microsoft.Xna.Framework;
using System;

namespace Celeste.Mod.QuantumMechanics
{
    public static class Utilities
    {
        public static bool IsRectangleVisible(float x, float y, float w, float h)
        {
            const float lenience = 4f;
            Camera camera = (Engine.Scene as Level)?.Camera;
            if (camera is null)
            {
                return true;
            }

            return x + w >= camera.Left - lenience
                && x <= camera.Right + lenience
                && y + h >= camera.Top - lenience
                && y <= camera.Bottom + 180f + lenience;
        }

        public static Vector2 Min(Vector2 a, Vector2 b)
        {
            return new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
        }

        public static Vector2 Max(Vector2 a, Vector2 b)
        {
            return new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
        }

        public static Rectangle Rectangle(Vector2 a, Vector2 b)
        {
            Vector2 min = Min(a, b);
            Vector2 size = Max(a, b) - min;
            return new((int)min.X, (int)min.Y, (int)size.X, (int)size.Y);
        }
    }
}
