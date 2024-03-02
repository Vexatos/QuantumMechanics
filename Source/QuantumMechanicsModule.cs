using System;
using Celeste.Mod.QuantumMechanics.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.QuantumMechanics {
    public class QuantumMechanicsModule : EverestModule {
        public static QuantumMechanicsModule Instance { get; private set; }

        public override Type SettingsType => typeof(QuantumMechanicsModuleSettings);
        public static QuantumMechanicsModuleSettings Settings => (QuantumMechanicsModuleSettings) Instance._Settings;

        public override Type SessionType => typeof(QuantumMechanicsModuleSession);
        public static QuantumMechanicsModuleSession Session => (QuantumMechanicsModuleSession) Instance._Session;

        public override Type SaveDataType => typeof(QuantumMechanicsModuleSaveData);
        public static QuantumMechanicsModuleSaveData SaveData => (QuantumMechanicsModuleSaveData) Instance._SaveData;

        public QuantumMechanicsModule() {
            Instance = this;
#if DEBUG
            // debug builds use verbose logging
            Logger.SetLogLevel(nameof(QuantumMechanicsModule), LogLevel.Verbose);
#else
            // release builds use info logging to reduce spam in log files
            Logger.SetLogLevel(nameof(QuantumMechanicsModule), LogLevel.Info);
#endif
        }

        public override void Load() {
            WonkyCassetteBlock.Load();
            WonkyCassetteBlockController.Load();
        }

        public override void Unload() {
            WonkyCassetteBlock.Unload();
            WonkyCassetteBlockController.Unload();
        }
    }
}
