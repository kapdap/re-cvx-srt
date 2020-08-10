namespace RECVXSRT
{
    public class GamePlayer
    {
        public int Character;
        public int Health;
        public int MaxHealth;
        public bool Poisoned;
        public bool Gassed;
        public int Retries;
        public int Saves;
        public int Room;
        public int Slot;
        public InventoryEntry Equipment;
        public InventoryEntry[] Inventory;

        public int Difficulty;
        public byte Status;
    }
}