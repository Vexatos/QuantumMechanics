using Microsoft.Xna.Framework;
using Monocle;

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
}
