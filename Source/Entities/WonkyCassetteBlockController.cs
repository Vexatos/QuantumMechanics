using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.QuantumMechanics.Entities {
    [CustomEntity("QuantumMechanics/WonkyCassetteBlockController")]
    [Tracked]
    public class WonkyCassetteBlockController : Entity {

        // Music stuff
        public readonly int bpm;
        public readonly int bars;
        public readonly int introBars;
        public readonly int barLength; // The top number in the time signature
        public readonly int beatLength; // The bottom number in the time signature
        public readonly float cassetteOffset;
        private readonly string param;

        public readonly int ExtraBoostFrames;

        public readonly EntityID ID;

        private readonly string DisableFlag;

        public float beatIncrement;
        private int maxBeats;
        private int introBeats;

        private bool isLevelMusic;
        private EventInstance sfx;
        private EventInstance snapshot;

        private bool transitioningIn = false;

        public WonkyCassetteBlockController(EntityData data, Vector2 offset)
            : this(data.Position + offset, new EntityID(data.Level.Name, data.ID), data.Int("bpm"), data.Int("bars"), data.Int("introBars"), data.Attr("timeSignature"), data.Attr("sixteenthNoteParam", "sixteenth_note"), data.Float("cassetteOffset"), data.Int("boostFrames", 1), data.Attr("disableFlag")) { }

        public WonkyCassetteBlockController(Vector2 position, EntityID id, int bpm, int bars, int introBars, string timeSignature, string param, float cassetteOffset, int boostFrames, string disableFlag)
            : base(position) {
            Tag = Tags.FrozenUpdate | Tags.TransitionUpdate;

            Add(new TransitionListener() {
                OnInBegin = () => transitioningIn = true,
                OnInEnd = () => transitioningIn = false
            });

            ID = id;

            this.bpm = bpm;
            this.bars = bars;
            this.introBars = introBars;
            this.param = param;
            this.cassetteOffset = cassetteOffset;

            GroupCollection timeSignatureParsed = new Regex(@"^(\d+)/(\d+)$").Match(timeSignature).Groups;
            if (timeSignatureParsed.Count == 0)
                throw new ArgumentException($"\"{timeSignature}\" is not a valid time signature.");

            barLength = int.Parse(timeSignatureParsed[1].Value);
            beatLength = int.Parse(timeSignatureParsed[2].Value);

            if (boostFrames < 1)
                throw new ArgumentException($"Boost Frames must be 1 or greater, but is set to {boostFrames}.");

            ExtraBoostFrames = boostFrames - 1;

            this.DisableFlag = disableFlag;
        }

        public void DisableAndReset(Scene scene, QuantumMechanicsModuleSession session) {
            session.MusicBeatTimer = 0;
            session.MusicWonkyBeatIndex = 0;
            session.MusicLoopStarted = false;

            session.CassetteBeatTimer = session.MusicBeatTimer - cassetteOffset;
            session.CassetteWonkyBeatIndex = 0;

            var wonkyListeners = scene.Tracker.GetComponents<WonkyCassetteListener>().Cast<WonkyCassetteListener>();

            foreach (WonkyCassetteListener wonkyListener in wonkyListeners) {
                wonkyListener.Stop();
            }

            var minorControllers = scene.Tracker.GetEntities<WonkyMinorCassetteBlockController>();

            foreach (WonkyMinorCassetteBlockController minorController in minorControllers) {
                minorController.Reset(session);
            }

            session.CassetteBlocksDisabled = true;
        }

        public void PrepareEnable(Scene scene, QuantumMechanicsModuleSession session) {
            var wonkyListeners = scene.Tracker.GetComponents<WonkyCassetteListener>().Cast<WonkyCassetteListener>();

            foreach (WonkyCassetteListener wonkyListener in wonkyListeners) {
                if (wonkyListener.ControllerIndex == 0) {
                    if (wonkyListener.ShouldBeActive(session.CassetteWonkyBeatIndex / (16 / beatLength) % barLength) != wonkyListener.Activated) {
                        wonkyListener.WillToggle();
                    }
                } else {
                    foreach (WonkyMinorCassetteBlockController minorController in Scene.Tracker.GetEntities<WonkyMinorCassetteBlockController>()) {
                        if (wonkyListener.ControllerIndex == minorController.ControllerIndex && wonkyListener.ShouldBeActive(minorController.CassetteWonkyBeatIndex / (16 / minorController.beatLength) % minorController.barLength) != wonkyListener.Activated) {
                            wonkyListener.WillToggle();
                            break;
                        }
                    }
                }
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            if (Scene.Tracker.GetEntity<CassetteBlockManager>() is not null)
                throw new Exception("WonkyCassetteBlockController detected in same room as CassetteBlockManager!");

            isLevelMusic = AreaData.Areas[SceneAs<Level>().Session.Area.ID].CassetteSong == "-";

            if (isLevelMusic)
                sfx = Audio.CurrentMusicEventInstance;
            else
                snapshot = Audio.CreateSnapshot("snapshot:/music_mains_mute");

            QuantumMechanicsModuleSession session = QuantumMechanicsModule.Session;

            // We always want sixteenth notes here, regardless of time signature
            beatIncrement = (float) (60.0 / bpm * beatLength / 16.0);
            maxBeats = 16 * bars * barLength / beatLength;
            introBeats = 16 * introBars * barLength / beatLength;

            session.MusicWonkyBeatIndex = session.MusicWonkyBeatIndex % maxBeats;

            // Synchronize the beat indices.
            // This may leave cassette blocks activated or deactivated for up to
            // the duration of an offset longer than normal at the start, but
            // that will fix itself within one beatIncrement duration
            session.CassetteWonkyBeatIndex = session.MusicWonkyBeatIndex;

            // Re-synchronize the beat timers
            // Positive offsets will make the cassette blocks lag behind the music progress
            session.CassetteBeatTimer = session.MusicBeatTimer - cassetteOffset;

            // Make sure minor controllers are set up after the main one
            foreach (WonkyMinorCassetteBlockController minorController in Scene.Tracker.GetEntities<WonkyMinorCassetteBlockController>()) {
                if (minorController.ID.Level == this.ID.Level) {
                    minorController.MinorAwake(scene, session, this);
                }
            }
        }

        public void CheckDisableAndReset() {
            QuantumMechanicsModuleSession session = QuantumMechanicsModule.Session;

            Level level = SceneAs<Level>();

            if (DisableFlag.Length == 0) {
                if (session.CassetteBlocksDisabled) {
                    session.CassetteBlocksDisabled = false;

                    PrepareEnable(level, session);
                }

                return;
            }

            bool shouldDisable = level.Session.GetFlag(DisableFlag);

            if (!session.CassetteBlocksDisabled && shouldDisable) {
                DisableAndReset(level, session);

            } else if (session.CassetteBlocksDisabled && !shouldDisable) {
                session.CassetteBlocksDisabled = false;

                PrepareEnable(level, session);
            }
        }

        public int NextBeatIndex(QuantumMechanicsModuleSession session, int currentBeatIndex) {
            int nextBeatIndex = (currentBeatIndex + 1) % maxBeats;

            if (session.MusicLoopStarted) {
                nextBeatIndex = Math.Max(nextBeatIndex, introBeats);
            }

            return nextBeatIndex;
        }

        private void AdvanceMusic(float time, Scene scene, QuantumMechanicsModuleSession session) {
            CheckDisableAndReset();

            if (session.CassetteBlocksDisabled)
                return;

            session.CassetteBeatTimer += time;

            bool synchronizeMinorControllers = false;

            if (session.CassetteBeatTimer >= beatIncrement) {

                session.CassetteBeatTimer -= beatIncrement;

                // beatIndex is always in sixteenth notes
                var wonkyListeners = scene.Tracker.GetComponents<WonkyCassetteListener>().Cast<WonkyCassetteListener>();
                int nextBeatIndex = NextBeatIndex(session, session.CassetteWonkyBeatIndex);
                int beatInBar = session.CassetteWonkyBeatIndex / (16 / beatLength) % barLength; // current beat

                int nextBeatInBar = nextBeatIndex / (16 / beatLength) % barLength; // next beat
                bool beatIncrementsNext = (nextBeatIndex / (float) (16 / beatLength)) % 1 == 0; // will the next beatIndex be the start of a new beat

                foreach (WonkyCassetteListener wonkyListener in wonkyListeners) {
                    if (wonkyListener.ControllerIndex != 0)
                        continue;

                    wonkyListener.OnBeat?.Invoke(beatInBar);

                    wonkyListener.SetActivated(wonkyListener.ShouldBeActive(beatInBar));

                    if (beatIncrementsNext && wonkyListener.ShouldBeActive(nextBeatInBar) != wonkyListener.Activated) {
                        wonkyListener.WillToggle();
                    }
                }

                // Doing this here because it would go to the next beat with a sixteenth note offset at start
                session.CassetteWonkyBeatIndex = NextBeatIndex(session, session.CassetteWonkyBeatIndex);

                // Synchronize minor controllers right before the start of a bar
                if (nextBeatInBar == 0 && beatInBar != 0) {
                    synchronizeMinorControllers = true;
                }
            }

            session.MusicBeatTimer += time;

            if (session.MusicBeatTimer >= beatIncrement) {

                session.MusicBeatTimer -= beatIncrement;

                sfx?.setParameterValue(param, (session.MusicWonkyBeatIndex * beatLength / 16) + 1);

                // Doing this here because it would go to the next beat with a sixteenth note offset at start
                session.MusicWonkyBeatIndex = NextBeatIndex(session, session.MusicWonkyBeatIndex);

                if (!session.MusicLoopStarted && session.MusicWonkyBeatIndex == introBeats) {
                    session.MusicLoopStarted = true;
                }
            }

            // Make sure minor controllers are set up after the main one
            foreach (WonkyMinorCassetteBlockController minorController in Scene.Tracker.GetEntities<WonkyMinorCassetteBlockController>()) {
                if (minorController.ID.Level != (scene as Level)?.Session?.Level) {
                    continue;
                }

                minorController.AdvanceMusic(time, scene);

                if (synchronizeMinorControllers) {
                    minorController.Synchronize(time, session);
                }
            }
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            if (!isLevelMusic) {
                Audio.Stop(snapshot);
                Audio.Stop(sfx);
            }
        }

        public override void Update() {
            base.Update();

            if (transitioningIn)
                return;

            if (isLevelMusic)
                sfx = Audio.CurrentMusicEventInstance;

            if (!isLevelMusic && sfx == null) {
                sfx = Audio.CreateInstance(AreaData.Areas[SceneAs<Level>().Session.Area.ID].CassetteSong);
                //Audio.Play("event:/game/general/cassette_block_switch_2");
                sfx.start();
            } else {
                AdvanceMusic(Engine.DeltaTime, Scene, QuantumMechanicsModule.Session);
            }
        }

        private static Hook hook_Level_get_ShouldCreateCassetteManager;

        public static void Load() {
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            IL.Monocle.Engine.Update += Engine_Update;

            hook_Level_get_ShouldCreateCassetteManager = new Hook(
                typeof(Level).GetProperty("ShouldCreateCassetteManager", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(nonPublic: true),
                Level_get_ShouldCreateCassetteManager
            );
        }

        public static void Unload() {
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            IL.Monocle.Engine.Update -= Engine_Update;

            hook_Level_get_ShouldCreateCassetteManager.Dispose();
        }

        private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            // Various cache clears
            WonkyCassetteBlock.Connections.Clear();
            CheckedForWonkyController = false;

            orig(self, playerIntro, isFromLoader);

            QuantumMechanicsModuleSession session = QuantumMechanicsModule.Session;
            WonkyCassetteBlockController mainController = self.Tracker.GetEntities<WonkyCassetteBlockController>().Cast<WonkyCassetteBlockController>().FirstOrDefault(controller => controller.ID.Level == self.Session.Level);

            if (mainController == null) {
                return;
            }

            var minorControllers = self.Tracker.GetEntities<WonkyMinorCassetteBlockController>();

            foreach (WonkyCassetteListener wonkyListener in self.Tracker.GetComponents<WonkyCassetteListener>()) {
                if (wonkyListener.ID.Level != self.Session.Level) {
                    continue;
                }

                if (wonkyListener.ControllerIndex == 0) {
                    var currentBeatIndex = session.CassetteWonkyBeatIndex / (16 / mainController.beatLength) % mainController.barLength;
                    wonkyListener.Start(!session.CassetteBlocksDisabled && wonkyListener.ShouldBeActive(currentBeatIndex));
                } else {
                    foreach (WonkyMinorCassetteBlockController minorController in minorControllers) {
                        if (minorController.ID.Level == self.Session.Level && wonkyListener.ControllerIndex == minorController.ControllerIndex) {
                            var currentBeatIndex = minorController.CassetteWonkyBeatIndex / (16 / minorController.beatLength) % minorController.barLength;
                            wonkyListener.Start(!session.CassetteBlocksDisabled && wonkyListener.ShouldBeActive(currentBeatIndex));
                            break;
                        }
                    }
                }
            }

            // Reset timers on song change
            // Has to happen after controllers and listeners are fully initialized
            if (session.CassetteBlocksLastParameter != mainController.param) {
                mainController.DisableAndReset(self, session);
                session.CassetteBlocksLastParameter = mainController.param;
            }
        }

        private static void Engine_Update(ILContext context) {
            ILCursor cursor = new ILCursor(context);

            if (cursor.TryGotoNext(instr => instr.MatchLdsfld<Engine>("FreezeTimer"),
                    instr => instr.MatchCall<Engine>("get_RawDeltaTime"))) {
                cursor.EmitDelegate<Action>(FreezeUpdate);
            }
        }

        private static void FreezeUpdate() {
            Engine.Scene.Tracker.GetEntity<WonkyCassetteBlockController>()?.AdvanceMusic(Engine.DeltaTime, Engine.Scene, QuantumMechanicsModule.Session);
            var components = Engine.Scene.Tracker.GetComponents<WonkyCassetteListener>();
            foreach (WonkyCassetteListener wonkyListener in components) {
                wonkyListener.FreezeUpdate?.Invoke();
            }
        }

        private static bool CheckedForWonkyController = false;
        private static bool hasWonkyController = false;

        private delegate bool orig_Level_get_ShouldCreateCassetteManager(Level self);
        private static bool Level_get_ShouldCreateCassetteManager(orig_Level_get_ShouldCreateCassetteManager orig, Level self) {
            if (!CheckedForWonkyController) {
                hasWonkyController = false;

                foreach (EntityData entityData in self.Session.LevelData.Entities) {
                    if (entityData.Name == "QuantumMechanics/WonkyCassetteBlockController") {
                        hasWonkyController = true;
                        break;
                    }
                }

                CheckedForWonkyController = true;
            }

            if (hasWonkyController) {
                return false;
            }

            return orig(self);
        }
    }
}
