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
                    return String.Format("{0} / {1} ({2:P1})", CurrentHP, MaximumHP, Percentage);
                else
                    return "DEAD / DEAD (0%)";
            }
        }

        public string HealthMessage
        {
            get
            {
                return HasMaxHP ? $"{DisplayHP} {Percentage:P1}" : $"{DisplayHP}";
            }
        }


        public string DebugMessage
        {
            get
            {
                return $"{Slot}:{Damage}:{CurrentHP}:{MaximumHP}:{Convert.ToInt32(HasMaxHP)}:{Convert.ToInt32(IsAlive)}:{Action}:{Status}:{Model}:{Convert.ToInt32(Type)}";
            }
        }

        public int Damage { get; private set; }
        public int MaximumHP { get; private set; }
        public int CurrentHP { get; private set; }
        public int DisplayHP => Math.Max(CurrentHP, 0);
        public bool HasMaxHP { get; private set; }
        public bool IsAlive { get; private set; }
        public float Percentage => (IsAlive && DisplayHP > 0) ? (float)DisplayHP / (float)MaximumHP : 0f;

        public int Slot { get; private set; }
        public EnemyEnumeration Type { get; private set; }
        public byte Action { get; private set; }
        public byte Status { get; private set; }
        public byte Model { get; private set; }

        public EnemyEntry(int slot, EnemyEnumeration type, byte action, byte status, byte model, int currentHP, int damage)
        {
            Slot = slot;
            Type = type;
            Action = action;
            Status = status;
            Model = model;

            MaximumHP = currentHP;
            CurrentHP = currentHP;
            Damage = damage;

            HasMaxHP = false;
            IsAlive = false;
        }

        public void SetLife(int maximumHP, bool isAlive = true)
        {
            MaximumHP = maximumHP;
            HasMaxHP = true;
            IsAlive = isAlive;
        }

        public void SetLife(bool isAlive = true)
        {
            IsAlive = isAlive;
        }
    }
}