using System.Diagnostics;

namespace BlitzSniffer.Game.Tracker.Versus.VLift
{
    public class VLiftCheckpoint
    {
        public uint BaseHp
        {
            get;
            private set;
        }

        public uint _Hp;

        public uint Hp
        {
            get
            {
                return _Hp;
            }
            set
            {
                Debug.Assert(BaseHp >= value, "Checkpoint HP cannot exceed limit");

                _Hp = value;

                if (BestHp > _Hp)
                {
                    BestHp = _Hp;
                }
            }
        }

        public uint BestHp
        {
            get;
            private set;
        }

        public VLiftCheckpoint(uint hp)
        {
            BaseHp = hp;
            BestHp = BaseHp;
            Hp = BaseHp;
        }

    }
}
