using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Celeste.Mod.QuantumMechanics.Entities
{
    [CustomEntity("QuantumMechanics/WonkyCassetteBlock")]
    [Tracked]
    public class WonkyCassetteBlock : CassetteBlock
    {
        private static readonly Regex OnAtBeatsSplitRegex = new(@",\s*", RegexOptions.Compiled);

        private readonly int[] OnAtBeats;
        private readonly int ControllerIndex;

        private readonly int OverrideBoostFrames;
        private int boostFrames = 0;
        private bool boostActive = false;

        private string textureDir;

        private string Key;

        public static readonly Dictionary<string, bool[,]> Connections = new(StringComparer.Ordinal);

        private List<Image> _pressed, _solid; // we'll use these instead of pressed and solid, to make `UpdateVisualState` not enumerate through them for no reason.

        protected bool Lonely = false; // if true, won't connect to any wonky cassette blocks other than itself

        protected Vector2 blockOffset => Vector2.UnitY * (2 - blockHeight);

        protected Color pressedColor => color.Mult(Calc.HexToColor("667da5"));

        public WonkyCassetteBlock(Vector2 position, EntityID id, float width, float height, bool lonely, int index, string moveSpec, Color color, string textureDir, int overrideBoostFrames, int controllerIndex)
            : base(position, id, width, height, index, 1.0f)
        {
            Lonely = lonely;

            Tag = Tags.FrozenUpdate | Tags.TransitionUpdate;

            OnAtBeats = OnAtBeatsSplitRegex.Split(moveSpec).Select(s => int.Parse(s) - 1).ToArray();
            Array.Sort(OnAtBeats);

            base.color = color;

            this.textureDir = textureDir;

            OverrideBoostFrames = overrideBoostFrames;

            Key = Lonely ? $"{id.ID}|{Index}|{ControllerIndex}|{string.Join(",", OnAtBeats)}" : $"{Index}|{ControllerIndex}|{string.Join(",", OnAtBeats)}";

            if (controllerIndex < 0)
                throw new ArgumentException($"Controller Index must be 0 or greater, but is set to {controllerIndex}.");

            ControllerIndex = controllerIndex;

            _pressed = new();
            _solid = new();

            Add(new WonkyCassetteListener(ID, controllerIndex)
            {
                ShouldBeActive = currentBeatIndex => OnAtBeats.Contains(currentBeatIndex),
                OnStart = SetActivatedSilently,
                OnStop = Stop,
                OnWillActivate = WillToggle,
                OnWillDeactivate = WillToggle,
                OnActivated = () => Activated = true,
                OnDeactivated = () => Activated = false,
                FreezeUpdate = Update
            });
        }

        public WonkyCassetteBlock(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, id, data.Width, data.Height, false, data.Int("index"), data.Attr("onAtBeats"), data.HexColor("color"), data.Attr("textureDirectory", "objects/cassetteblock").TrimEnd('/'), data.Int("boostFrames", -1), data.Int("controllerIndex", 0)) { }

        protected void AddCenterSymbol(Image solid, Image pressed)
        {
            _solid.Add(solid);
            _pressed.Add(pressed);
            Vector2 origin = groupOrigin - Position;
            Vector2 size = new(Width, Height);

            Vector2 half = (size - new Vector2(solid.Width, solid.Height)) * 0.5f;
            solid.Origin = origin - half;
            solid.Position = origin;
            solid.Color = color;
            Add(solid);

            half = (size - new Vector2(pressed.Width, pressed.Height)) * 0.5f;
            pressed.Origin = origin - half;
            pressed.Position = origin;
            pressed.Color = color;
            Add(pressed);
        }

        // We need to reimplement some of our parent's methods because they refer directly to CassetteBlock when fetching entities

        private static void NewFindInGroup(On.Celeste.CassetteBlock.orig_FindInGroup orig, CassetteBlock self, CassetteBlock block)
        {
            if (self is not WonkyCassetteBlock)
            {
                orig(self, block);

                return;
            }

            WonkyCassetteBlock selfCast = (WonkyCassetteBlock)self;

            if (selfCast.Lonely)
                return;

            foreach (WonkyCassetteBlock entity in self.Scene.Tracker.GetEntities<WonkyCassetteBlock>())
            {
                if (entity != self && entity != block && entity.Index == self.Index &&
                    entity.ControllerIndex == selfCast.ControllerIndex &&
                    (entity.CollideRect(new Rectangle((int)block.X - 1, (int)block.Y, (int)block.Width + 2, (int)block.Height))
                        || entity.CollideRect(new Rectangle((int)block.X, (int)block.Y - 1, (int)block.Width, (int)block.Height + 2))) &&
                    !self.group.Contains(entity) && entity.OnAtBeats.SequenceEqual(selfCast.OnAtBeats) && !entity.Lonely)
                {
                    self.group.Add(entity);
                    NewFindInGroup(orig, self, entity);
                }
            }
        }

        public override void Awake(Scene scene)
        {
            if (Connections.Count == 0)
            {
                IndexConnections(SceneAs<Level>());
            }

            base.Awake(scene);
        }

        public override void Update()
        {
            bool activating = groupLeader && Activated && !Collidable;

            base.Update();

            if (Activated && Collidable)
            {
                if (activating)
                {
                    // Block has activated, Cassette boost is possible this frame
                    if (OverrideBoostFrames > 0)
                    {
                        boostFrames = OverrideBoostFrames - 1;
                        boostActive = true;
                    }
                    else if (OverrideBoostFrames < 0)
                    {
                        WonkyCassetteBlockController controller = this.Scene.Tracker.GetEntity<WonkyCassetteBlockController>();
                        if (controller != null)
                        {
                            boostFrames = controller.ExtraBoostFrames;
                            boostActive = true;
                        }
                    }

                    foreach (CassetteBlock cassetteBlock in group)
                    {
                        WonkyCassetteBlock wonkyBlock = (WonkyCassetteBlock)cassetteBlock;
                        wonkyBlock.boostFrames = boostFrames;
                        wonkyBlock.boostActive = boostActive;
                    }
                }

                if (boostActive)
                {
                    // Vanilla lift boost is active this frame, do nothing
                    boostActive = false;
                }
                else if (boostFrames > 0)
                {
                    // Provide an extra boost for the duration of the extra boost frames
                    this.LiftSpeed.Y = -1 / Engine.DeltaTime;

                    // Update lift of riders
                    MoveVExact(0);

                    boostFrames -= 1;
                }
            }
        }

        public override void Render()
        {
            if (Utilities.IsRectangleVisible(Position.X, Position.Y, Width, Height))
            {
                List<Image> images = Collidable ? _solid : _pressed;

                foreach (Image item in images)
                {
                    if (item.Visible)
                    {
                        item.Texture.Draw(item.Position + Position, item.Origin, item.Color, item.Scale, item.Rotation, item.Effects);
                    }
                }
            }
        }

        private void Stop()
        {
            // If fully activated, stopping is going to only move the block down by 1 pixel
            // We need one extra here.
            if ((Activated && this.blockHeight == 2) || (!Activated && this.blockHeight == 1))
            {
                ShiftSize(1);
            }

            Activated = false;
        }

        private static void IndexConnectionsForBlock(Rectangle bounds, Rectangle tileBounds, Entity entity, ref bool[,] connection)
        {
            for (float x = entity.Left; x < entity.Right; x += 8f)
            {
                for (float y = entity.Top; y < entity.Bottom; y += 8f)
                {
                    int ix = ((int)x - bounds.Left) / 8 + 1;
                    int iy = ((int)y - bounds.Top) / 8 + 1;

                    if (ix < 0) ix = 0;
                    else if (ix > tileBounds.Width) ix = tileBounds.Width + 1;
                    if (iy < 0) iy = 0;
                    else if (iy > tileBounds.Height) iy = tileBounds.Height + 1;

                    connection[ix, iy] = true;
                }
            }
        }

        private static void IndexConnections(Level level)
        {
            LevelData levelData = level.Session.LevelData;
            Rectangle bounds = levelData.Bounds;
            Rectangle tileBounds = levelData.TileBounds;

            foreach (WonkyCassetteBlock entity in level.Tracker.GetEntities<WonkyCassetteBlock>())
            {
                if (entity.Lonely)
                {
                    bool[,] blockConnections = new bool[tileBounds.Width + 2, tileBounds.Height + 2];
                    IndexConnectionsForBlock(bounds, tileBounds, entity, ref blockConnections);
                    Connections.Add(entity.Key, blockConnections);
                }
                else
                {
                    bool[,] connection;

                    if (!Connections.TryGetValue(entity.Key, out connection))
                    {
                        Connections.Add(entity.Key, connection = new bool[tileBounds.Width + 2, tileBounds.Height + 2]);
                    }

                    IndexConnectionsForBlock(bounds, tileBounds, entity, ref connection);
                }
            }
        }

        protected virtual void HandleUpdateVisualState()
        {
            foreach (StaticMover staticMover in staticMovers)
            {
                staticMover.Visible = Visible;
            }
        }

        private static void Platform_EnableStaticMovers(On.Celeste.Platform.orig_EnableStaticMovers orig, Platform self)
        {
            if (self is WonkyCassetteBlock && !self.Visible)
                return;
            orig(self);
        }

        private static bool NewCheckForSame(On.Celeste.CassetteBlock.orig_CheckForSame origCheckForSame, CassetteBlock self, float x, float y)
        {
            if (!(self is WonkyCassetteBlock))
                return origCheckForSame(self, x, y);

            WonkyCassetteBlock selfCast = (WonkyCassetteBlock)self;

            bool[,] connection;

            if (!Connections.TryGetValue(selfCast.Key, out connection))
            {
                // Fallback just in case
                foreach (WonkyCassetteBlock entity in self.Scene.Tracker.GetEntities<WonkyCassetteBlock>())
                {
                    if (selfCast.Lonely)
                        return self.Collider.Collide(new Rectangle((int)x, (int)y, 8, 8));
                    else if (entity.Index == self.Index && entity.ControllerIndex == selfCast.ControllerIndex &&
                        entity.Collider.Collide(new Rectangle((int)x, (int)y, 8, 8)) &&
                        entity.OnAtBeats.SequenceEqual(selfCast.OnAtBeats))
                    {
                        return true;
                    }
                }

                return false;
            }

            Level level = selfCast.SceneAs<Level>();
            LevelData levelData = level.Session.LevelData;
            Rectangle bounds = levelData.Bounds;
            Rectangle tileBounds = levelData.TileBounds;

            int ix = ((int)x - bounds.Left) / 8 + 1;
            int iy = ((int)y - bounds.Top) / 8 + 1;

            if (ix < 0) ix = 0;
            else if (ix > tileBounds.Width) ix = tileBounds.Width + 1;
            if (iy < 0) iy = 0;
            else if (iy > tileBounds.Height) iy = tileBounds.Height + 1;

            return connection[ix, iy];
        }

        private static void CassetteBlock_SetImage(On.Celeste.CassetteBlock.orig_SetImage orig, CassetteBlock self, float x, float y, int tx, int ty)
        {
            if (self is WonkyCassetteBlock block)
            {

                GFX.Game.PushFallback(GFX.Game["objects/cassetteblock/pressed00"]);
                Image img = block.CreateImage(x, y, tx, ty, GFX.Game[block.textureDir + "/pressed"]);
                // we don't want to have the image in the component list, because then the entity.Get<> function becomes much more expensive,
                // and some modded hooks call it each frame, for each entity...
                // this makes a huge difference in GMHS flag 1.
                img.RemoveSelf();
                block._pressed.Add(img);
                GFX.Game.PopFallback();

                GFX.Game.PushFallback(GFX.Game["objects/cassetteblock/solid"]);
                img = block.CreateImage(x, y, tx, ty, GFX.Game[block.textureDir + "/solid"]);
                img.RemoveSelf();
                block._solid.Add(img);
                GFX.Game.PopFallback();
            }
            else
                orig(self, x, y, tx, ty);
        }

        private static void CassetteBlock_Awake(ILContext il)
        {
            ILCursor cursor = new(il);
            // Don't add the BoxSide, as it breaks rendering due to transparency
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt<Scene>("Add")))
            {
                ILLabel afterAdd = cursor.DefineLabel();

                // skip the Add call if this is a wonky cassette
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitDelegate<Func<Scene, object, CassetteBlock, bool>>(IsWonky);
                cursor.Emit(OpCodes.Brtrue, afterAdd);

                // restore the args for the Add call
                cursor.Emit(OpCodes.Ldarg_1); // Scene
                cursor.Emit(OpCodes.Ldloc_2); // side
                // Scene.Add will be called here

                cursor.Index++;
                cursor.MarkLabel(afterAdd);
            }
        }

        private static void CassetteBlock_ShiftSize(ILContext il)
        {
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallOrCallvirt<Platform>("MoveV")))
            {
                cursor.GotoPrev(MoveType.After, instr => instr.MatchConvR4());

                ILLabel beforeMoveV = cursor.DefineLabel();
                ILLabel afterMoveV = cursor.DefineLabel();

                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitDelegate<Func<CassetteBlock, bool>>(IsWonkyWithoutBoost);
                cursor.Emit(OpCodes.Brfalse, beforeMoveV); // Only run if boostless

                cursor.EmitDelegate<Action<CassetteBlock, float>>(MoveVWithoutBoost);
                cursor.Emit(OpCodes.Br, afterMoveV);

                cursor.MarkLabel(beforeMoveV);
                cursor.Emit(OpCodes.Nop); // Placed as label target so that SJ patch comes after this

                cursor.GotoNext(MoveType.After, instr => instr.MatchCallOrCallvirt<Platform>("MoveV"));
                cursor.MarkLabel(afterMoveV);
            }
        }

        private static void CassetteBlock_UpdateVisualState(On.Celeste.CassetteBlock.orig_UpdateVisualState orig, CassetteBlock self)
        {
            orig(self);
            if (self is WonkyCassetteBlock block)
            {
                block?.HandleUpdateVisualState();
            }
        }

        private static bool IsWonky(Scene scene, object side, CassetteBlock self) => self is WonkyCassetteBlock;
        private static bool IsWonkyWithoutBoost(CassetteBlock self) => self is WonkyCassetteBlock block && block.OverrideBoostFrames == 0;

        private static void MoveVWithoutBoost(CassetteBlock self, float amount) => self.MoveV(amount, 0);

        public static void Load()
        {
            On.Celeste.CassetteBlock.FindInGroup += NewFindInGroup;
            On.Celeste.CassetteBlock.CheckForSame += NewCheckForSame;
            On.Celeste.CassetteBlock.SetImage += CassetteBlock_SetImage;
            On.Celeste.CassetteBlock.UpdateVisualState += CassetteBlock_UpdateVisualState;
            On.Celeste.Platform.EnableStaticMovers += Platform_EnableStaticMovers;
            IL.Celeste.CassetteBlock.Awake += CassetteBlock_Awake;
            IL.Celeste.CassetteBlock.ShiftSize += CassetteBlock_ShiftSize;
        }

        public static void Unload()
        {
            On.Celeste.CassetteBlock.FindInGroup -= NewFindInGroup;
            On.Celeste.CassetteBlock.CheckForSame -= NewCheckForSame;
            On.Celeste.CassetteBlock.SetImage -= CassetteBlock_SetImage;
            On.Celeste.CassetteBlock.UpdateVisualState -= CassetteBlock_UpdateVisualState;
            On.Celeste.Platform.EnableStaticMovers -= Platform_EnableStaticMovers;
            IL.Celeste.CassetteBlock.Awake -= CassetteBlock_Awake;
            IL.Celeste.CassetteBlock.ShiftSize -= CassetteBlock_ShiftSize;
        }
    }
}
