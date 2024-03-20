namespace Celeste.Mod.QuantumMechanics {
    public class QuantumMechanicsModuleSession : EverestModuleSession {
        public int MusicWonkyBeatIndex;
        public int CassetteWonkyBeatIndex;
        public bool MusicLoopStarted = false;
        public float MusicBeatTimer;
        public float CassetteBeatTimer;
        public bool CassetteBlocksDisabled = true;
        public string CassetteBlocksLastParameter = "";
    }
}
