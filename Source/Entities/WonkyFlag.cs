using System;
using System.Linq;
using System.Text.RegularExpressions;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuantumMechanics.Entities {

    [CustomEntity("QuantumMechanics/WonkyFlag")]
    [Tracked]
    public class WonkyFlag : Entity {
        private static readonly Regex OnAtBeatsSplitRegex = new(@",\s*", RegexOptions.Compiled);

        private readonly int[] OnAtBeats;
        private readonly string Flag;
        private readonly bool Invert;

        private readonly int ControllerIndex;

        public WonkyFlag(EntityData data, Vector2 offset)
            : this(data.Position + offset, new EntityID(data.Level.Name, data.ID), data.Attr("flag"), data.Attr("onAtBeats"), data.Int("controllerIndex", 0), data.Bool("invert", false)) { }

        public WonkyFlag(Vector2 position, EntityID id, string flag, string moveSpec, int controllerIndex, bool invert) 
            : base(position) {

            OnAtBeats = OnAtBeatsSplitRegex.Split(moveSpec).Select(s => int.Parse(s) - 1).ToArray();
            Array.Sort(OnAtBeats);

            Flag = flag;
            Invert = invert;

            if (controllerIndex < 0)
                throw new ArgumentException($"Controller Index must be 0 or greater, but is set to {controllerIndex}.");

            ControllerIndex = controllerIndex;

            Add(new WonkyCassetteListener(id, controllerIndex) {
                ShouldBeActive = currentBeatIndex => OnAtBeats.Contains(currentBeatIndex) ^ Invert,
                OnStart = SetFlag,
                OnStop = () => SetFlag(false),
                OnActivated = () => SetFlag(true),
                OnDeactivated = () => SetFlag(false)
            });
        }

        private void SetFlag(bool value) {
            SceneAs<Level>().Session.SetFlag(Flag, value);
        }
    }
}