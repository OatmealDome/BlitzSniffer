using System.Text.Json.Serialization;

namespace BlitzSniffer.Game.Event.Player
{
    public class PlayerDeathEvent : PlayerEvent
    {
        public override string Name => "PlayerDeath";

        public int AttackerIdx
        {
            get;
            set;
        } = -1;

        public int AssisterIdx
        {
            get;
            set;
        } = -1;

        public string Cause
        {
            get;
            set;
        }

        [JsonIgnore]
        public bool IsComplete
        {
            get;
            set;
        }

        [JsonIgnore]
        public uint SendDeadline
        {
            get;
            set;
        }

    }
}
