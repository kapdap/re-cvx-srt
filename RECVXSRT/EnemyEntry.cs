using System;
using System.Diagnostics;

namespace RECVXSRT
{
    [DebuggerDisplay("{_DebuggerDisplay,nq}")]
    public struct EnemyEntry
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string _DebuggerDisplay
        {
            get
            {
                if (IsAlive)
                    return string.Format("{0} / {1} ({2:P1})", CurrentHP, MaximumHP, Percentage);
                else
                    return "DEAD / DEAD (0%)";
            }
        }

        public int MaximumHP { get; private set; }
        public int CurrentHP { get; private set; }
        public int DisplayHP => Math.Max(CurrentHP, 0);
        public bool HasMaxHP { get; private set; }
        public bool IsAlive { get; private set; }
        public float Percentage => (IsAlive && DisplayHP != 0) ? (float)DisplayHP / (float)MaximumHP : 0f;

        public EnemyEntry(int maximumHP, int currentHP, bool hasMaxHP = true)
        {
            MaximumHP = maximumHP;
            CurrentHP = (currentHP <= maximumHP) ? currentHP : 0;
            HasMaxHP = hasMaxHP;
            IsAlive = true;
        }
    }
}