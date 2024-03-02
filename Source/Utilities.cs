using Monocle;

namespace Celeste.Mod.QuantumMechanics {
    public static class Utilities {
        public static bool IsRectangleVisible(float x, float y, float w, float h) {
            const float lenience = 4f;
            Camera camera = (Engine.Scene as Level)?.Camera;
            if (camera is null) {
                return true;
            }

            return x + w >= camera.Left - lenience
                && x <= camera.Right + lenience
                && y + h >= camera.Top - lenience
                && y <= camera.Bottom + 180f + lenience;
        }
    }
}
