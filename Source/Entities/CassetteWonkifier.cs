using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Celeste.Mod.QuantumMechanics.Entities {
    [CustomEntity("QuantumMechanics/CassetteWonkifier")]
    [Tracked]
    public class CassetteWonkifier : Entity {
        private static readonly Regex OnAtBeatsSplitRegex = new(@",\s*", RegexOptions.Compiled);

        private readonly int[] OnAtBeats;
        private readonly int CassetteIndex;

        private readonly int ControllerIndex;

        public CassetteWonkifier(Vector2 position, EntityID id, string moveSpec, int cassetteIndex, int controllerIndex)
            : base(position) {

            OnAtBeats = OnAtBeatsSplitRegex.Split(moveSpec).Select(s => int.Parse(s) - 1).ToArray();
            Array.Sort(OnAtBeats);

            if (controllerIndex < 0)
                throw new ArgumentException($"Controller Index must be 0 or greater, but is set to {controllerIndex}.");

            ControllerIndex = controllerIndex;
            CassetteIndex = cassetteIndex;

            Add(new WonkyCassetteListener(id, controllerIndex) {
                ShouldBeActive = currentBeatIndex => OnAtBeats.Contains(currentBeatIndex),
                OnStart = activated => ForAllCassetteBlocks(block => block.SetActivatedSilently(activated)),
                OnStop = () => ForAllCassetteBlocks(block => block.Finish()),
                OnWillActivate = () => ForAllCassetteBlocks(block => block.WillToggle()),
                OnWillDeactivate = () => ForAllCassetteBlocks(block => block.WillToggle()),
                OnActivated = () => ForAllCassetteBlocks(block => block.Activated = true),
                OnDeactivated = () => ForAllCassetteBlocks(block => block.Activated = false),
                FreezeUpdate = Update
            });
        }

        public CassetteWonkifier(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, id, data.Attr("onAtBeats"), data.Int("cassetteIndex", 0), data.Int("controllerIndex", 0)) { }

        private void ForAllCassetteBlocks(Action<CassetteBlock> action) {
            foreach (CassetteBlock block in base.Scene.Tracker.GetEntities<CassetteBlock>()) {
                if (block.Index == this.CassetteIndex) {
                    action.Invoke(block);
                }
            }
        }
    }
}