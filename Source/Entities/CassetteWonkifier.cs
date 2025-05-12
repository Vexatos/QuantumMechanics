using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;

namespace Celeste.Mod.QuantumMechanics.Entities {
    [CustomEntity("QuantumMechanics/CassetteWonkifier")]
    [Tracked]
    public class CassetteWonkifier : Entity {
        public int[] OnAtBeats;
        public readonly int ControllerIndex;

        private readonly int CassetteIndex;
        private readonly bool DoFreezeUpdate;

        public CassetteWonkifier(Vector2 position, EntityID id, string moveSpec, int cassetteIndex, int controllerIndex, bool doFreezeUpdate)
            : base(position) {

            OnAtBeats = Utilities.OnAtBeats(moveSpec);

            if (controllerIndex < 0)
                throw new ArgumentException($"Controller Index must be 0 or greater, but is set to {controllerIndex}.");

            ControllerIndex = controllerIndex;
            CassetteIndex = cassetteIndex;

            DoFreezeUpdate = doFreezeUpdate;
        }

        public CassetteWonkifier(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, id, data.Attr("onAtBeats"), data.Int("cassetteIndex", 0), data.Int("controllerIndex", 0), data.Bool("freezeUpdate", true)) { }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            foreach (CassetteBlock block in base.Scene.Tracker.GetEntities<CassetteBlock>()) {
                if (block.Index == this.CassetteIndex && block.Components.Get<WonkyCassetteListener>() == null) {
                    block.Add(new WonkyCassetteListener(block.ID, this.ControllerIndex) {
                        ShouldBeActive = currentBeatIndex => OnAtBeats.Contains(currentBeatIndex),
                        OnStart = activated => block.SetActivatedSilently(activated),
                        OnStop = () => block.Finish(),
                        OnWillActivate = () => block.WillToggle(),
                        OnWillDeactivate = () => block.WillToggle(),
                        OnActivated = () => block.Activated = true,
                        OnDeactivated = () => block.Activated = false,
                        FreezeUpdate = this.DoFreezeUpdate ? () => block.Update() : null
                    });
                }
            }

            foreach (CassetteListener listener in base.Scene.Tracker.GetComponents<CassetteListener>()) {
                if (listener.Index == this.CassetteIndex && listener.Entity?.Components.Get<WonkyCassetteListener>() == null) {
                    listener.Entity?.Add(new WonkyCassetteListener(listener.ID, this.ControllerIndex) {
                        ShouldBeActive = currentBeatIndex => OnAtBeats.Contains(currentBeatIndex),
                        OnStart = activated => listener.Start(activated),
                        OnStop = () => listener.Finish(),
                        OnWillActivate = () => listener.WillToggle(),
                        OnWillDeactivate = () => listener.WillToggle(),
                        OnActivated = () => listener.Activated = true,
                        OnDeactivated = () => listener.Activated = false,
                        FreezeUpdate = this.DoFreezeUpdate ? () => listener.Entity?.Update() : null
                    });
                }
            }
        }
    }
}