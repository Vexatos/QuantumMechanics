using Monocle;
using System;
using System.Linq;

namespace Celeste.Mod.QuantumMechanics.Entities {
    [Tracked]
    public class WonkyCassetteListener : Component {

        /// <summary>
        /// The parameter indicates whether this component should be active.
        /// </summary>
        /// <param name="currentBeatIndex">The current beat index.</param>
        public Predicate<int> ShouldBeActive;

        /// <summary>
        /// Called when loading a room.
        /// The parameter indicates whether this component will be the active one.
        /// </summary>
        public Action<bool> OnStart;

        /// <summary>
        /// Called when disabling the controller.
        /// </summary>
        public Action OnStop;

        /// <summary>
        /// Called shortly before the component is is about to become active.
        /// </summary>
        public Action OnWillActivate;

        /// <summary>
        /// Called shortly before the component is is about to become inactive.
        /// </summary>
        public Action OnWillDeactivate;

        /// <summary>
        /// Called if <see cref="Activated"/> was changed from false to true.
        /// </summary>
        public Action OnActivated;

        /// <summary>
        /// Called if <see cref="Activated"/> was changed from true to false.
        /// </summary>
        public Action OnDeactivated;

        /// <summary>
        /// Called to update the component during freeze frames.
        /// </summary>
        public Action FreezeUpdate;

        /// <summary>
        /// Called every beat with the current beat index.
        /// </summary>
        public Action<int> OnBeat;

        /// <summary>
        /// The index of the controller the component should listen to.
        /// </summary>
        public readonly int ControllerIndex;

        /// <summary>
        /// Matches the functionality of <see cref="CassetteBlock.Activated"/>.
        /// </summary>
        public bool Activated;

        /// <summary>
        /// Matches the functionality of <see cref="CassetteBlock.Mode"/>.
        /// </summary>
        public Modes Mode;

        /// <summary>
        /// Matches the functionality of <see cref="CassetteBlock.ID"/>.
        /// </summary>
        public EntityID ID;

        public WonkyCassetteListener(EntityID id, int controllerIndex) : base(false, false) {
            ControllerIndex = controllerIndex;
            ID = id;
        }

        public void SetActivated(bool activated) {
            if (activated == Activated) {
                return;
            }

            Activated = activated;

            if (activated) {
                Mode = Modes.Enabled;
                OnActivated?.Invoke();
            } else {
                Mode = Modes.Disabled;
                OnDeactivated?.Invoke();
            }
        }

        public void Start(bool activated) {
            Activated = activated;
            Mode = Activated ? Modes.Enabled : Modes.Disabled;
            OnStart?.Invoke(activated);
        }

        public void Stop() {
            Activated = false;
            Mode = Modes.Disabled;
            OnStop?.Invoke();
        }

        public void WillToggle() {
            if (Activated) {
                Mode = Modes.WillDisable;
                OnWillDeactivate?.Invoke();
            } else {
                Mode = Modes.WillEnable;
                OnWillActivate?.Invoke();
            }
        }

        public override void EntityAdded(Scene scene) {
            base.EntityAdded(scene);
        }

        public enum Modes {
            Enabled,
            WillDisable,
            Disabled,
            WillEnable,
        }

        public readonly record struct BeatInfo(
            int barLength, // The top number in the time signature
            int beatLength // The bottom number in the time signature
        );
    }
}