using System;
using System.Diagnostics;

namespace RECVXSRT
{
    [DebuggerDisplay("{_DebuggerDisplay,nq}")]
    public struct InventoryEntry : IEquatable<InventoryEntry>
    {
        /// <summary>
        /// Debugger display message.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string _DebuggerDisplay
        {
            get
            {
                if (!IsEmptySlot)
                    return string.Format("[#{0}] Item {1} Quantity {2} Infinite {3}", SlotPosition, ItemID, Quantity, Infinite);
                else
                    return string.Format("[#{0}] Empty Slot", SlotPosition);
            }
        }

        // Storage variable.
        public int SlotPosition { get; private set; }
        private byte[] Data { get; set; }

        // Accessor properties.
        public ItemEnumeration ItemID { get; private set; }
        public int Quantity { get; private set; }
        public bool Infinite { get; private set; }
        public bool IsFlame { get; private set; }
        public bool IsGas { get; private set; }
        public bool IsBOW { get; private set; }
        public bool IsEmptySlot => ItemID == ItemEnumeration.None;

        public InventoryEntry(int slotPosition, byte[] data)
        {
            SlotPosition = slotPosition;
            Data = data;

            Quantity = BitConverter.ToInt16(Data, 0);
            ItemID = (ItemEnumeration)Data[2];

            Infinite = (Data[3] & (byte)ItemStatusEnumeration.Infinate) != 0;
            IsFlame = (Data[3] & (byte)ItemStatusEnumeration.Flame) != 0;
            IsGas = (Data[3] & (byte)ItemStatusEnumeration.Acid) != 0;
            IsBOW = (Data[3] & (byte)ItemStatusEnumeration.BOW) != 0;
        }

        public bool Equals(InventoryEntry other)
        {
            return Data.ByteArrayEquals(other.Data);
        }

        public override bool Equals(object obj)
        {
            if (obj is InventoryEntry)
                return Equals((InventoryEntry)obj);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public static bool operator ==(InventoryEntry obj1, InventoryEntry obj2)
        {
            if (ReferenceEquals(obj1, obj2))
                return true;

            if (ReferenceEquals(obj1, null))
                return false;

            if (ReferenceEquals(obj2, null))
                return false;

            return obj1.Data.ByteArrayEquals(obj2.Data);
        }

        public static bool operator !=(InventoryEntry obj1, InventoryEntry obj2)
        {
            return !(obj1 == obj2);
        }
    }
}