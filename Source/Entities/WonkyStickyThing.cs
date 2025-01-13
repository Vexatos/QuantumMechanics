using System;
using System.Linq;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.QuantumMechanics.Entities {
    [CustomEntity("QuantumMechanics/WonkyStickyThing")]
    [Tracked]
    public class WonkyStickyThing : Solid {
        public int[] OnAtBeats;
        public readonly int ControllerIndex;

        public readonly Color color;

        private readonly WonkyCassetteListener cassette;

        private int blockHeight = 2;

        public WonkyStickyThing(Vector2 position, EntityID id, float width, float height, string moveSpec, int controllerIndex, Color color)
            : base(position, width, height, false) {

            Collidable = false;
            Visible = false;

            OnAtBeats = Utilities.OnAtBeats(moveSpec);

            if (controllerIndex < 0)
                throw new ArgumentException($"Controller Index must be 0 or greater, but is set to {controllerIndex}.");

            ControllerIndex = controllerIndex;

            this.color = color;

            Add(cassette = new WonkyCassetteListener(id, controllerIndex) {
                ShouldBeActive = currentBeatIndex => OnAtBeats.Contains(currentBeatIndex),
                OnStart = Start,
                OnStop = Stop,
                OnWillActivate = () => ShiftSize(-1),
                OnWillDeactivate = () => ShiftSize(1),
                OnActivated = () => SetActive(true),
                OnDeactivated = () => SetActive(false)
            });
        }

        public WonkyStickyThing(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, id, data.Width, data.Height, data.Attr("onAtBeats"), data.Int("controllerIndex"), data.HexColor("color")) { }
        
        public override void Awake(Scene scene) {
            base.Awake(scene);

            Color color = Calc.HexToColor("667da5");
            Color disabledColor = new Color((float)(int)color.R / 255f * ((float)(int)this.color.R / 255f), (float)(int)color.G / 255f * ((float)(int)this.color.G / 255f), (float)(int)color.B / 255f * ((float)(int)this.color.B / 255f), 1f);
            foreach (StaticMover staticMover in staticMovers) {
                if (staticMover.Entity is Spikes spikes) {
                    spikes.EnabledColor = this.color;
                    spikes.DisabledColor = disabledColor;
                    spikes.VisibleWhenDisabled = true;
                    spikes.SetSpikeColor(this.color);
                }
                if (staticMover.Entity is Spring spring) {
                    spring.DisabledColor = disabledColor;
                    spring.VisibleWhenDisabled = true;
                }
            }
        }

        private void UpdateStaticMovers(bool active) {
            if (active) {
                EnableStaticMovers();
            } else {
                DisableStaticMovers();
            }
        }

        private void ShiftSize(int amount) {
            MoveV(amount, 0);
            blockHeight -= amount;
        }

        private void SetActive(bool active) {
            ShiftSize(active ? -1 : 1);
            UpdateStaticMovers(active);
        }

        private void Start(bool active) {
            if (!active) {
                ShiftSize(2);
            }
            UpdateStaticMovers(active);
        }

        private void Stop() {
            if (blockHeight > 0) {
                ShiftSize(blockHeight);
            }
            DisableStaticMovers();
        }
    }
}