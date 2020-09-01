namespace RECVXSRT
{
    public class GamePlayer
    {
        public CharacterEnumeration Character { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public bool Poison { get; set; }
        public bool Gassed { get; set; }
        public int Retry { get; set; }
        public int Saves { get; set; }
        public int Room { get; set; }
        public int Slot { get; set; }
        public InventoryEntry Equipment { get; set; }
        public InventoryEntry[] Inventory { get; set; }

        public int Difficulty { get; set; }
        public byte Status { get; set; }
    }
}