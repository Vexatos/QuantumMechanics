using System.Linq;
using System.Text.RegularExpressions;
using Monocle;

namespace Celeste.Mod.QuantumMechanics {
    public static class Utilities {
        public static readonly Regex OnAtBeatsSplitRegex = new(@",\s*", RegexOptions.Compiled);

        public static int[] OnAtBeats(string moveSpec) => OnAtBeatsSplitRegex.Split(moveSpec).Select(s => int.Parse(s) - 1).Order().ToArray();

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
