using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.QuantumMechanics;

public static class Extensions
{
    public static Color Mult(this Color color, Color other)
    {
        color.R = (byte)(color.R * other.R / 256f);
        color.G = (byte)(color.G * other.G / 256f);
        color.B = (byte)(color.B * other.B / 256f);
        color.A = (byte)(color.A * other.A / 256f);
        return color;
    }

    public static Rectangle GetBounds(this Camera camera)
    {
        int top = (int)camera.Top;
        int bottom = (int)camera.Bottom;
        int left = (int)camera.Left;
        int right = (int)camera.Right;

        return new(left, top, right - left, bottom - top);
    }

    public static MoveBlock.Directions Direction(this Vector2 vec)
    {
        vec = vec.SafeNormalize();

        if (vec.X > 0)
        {
            return MoveBlock.Directions.Right;
        }
        else if (vec.X < 0)
        {
            return MoveBlock.Directions.Left;
        }
        else if (vec.Y < 0)
        {
            return MoveBlock.Directions.Up;
        }
        else
        {
            return MoveBlock.Directions.Down;
        }
    }

    public static float Angle(this MoveBlock.Directions dir)
    {
        return dir switch
        {
            MoveBlock.Directions.Left => (float)Math.PI,
            MoveBlock.Directions.Up => -(float)Math.PI / 2f,
            MoveBlock.Directions.Down => (float)Math.PI / 2f,
            _ => 0f
        };
    }
}
