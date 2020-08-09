using System;
using System.Globalization;

namespace RECVXSRT
{
    public class GameMemory : IDisposable
    {
        private const string IGT_TIMESPAN_STRING_FORMAT = @"hh\:mm\:ss";

        public GameProcess Game { get; private set; }
        public GamePointers Pointers { get; private set; }
        public ProcessMemory.ProcessMemory Memory { get; private set; }

        public int PlayerRoom { get; private set; }
        public int PlayerCharacter { get; private set; }
        public int PlayerHealth { get; private set; }
        public int PlayerMaxHealth { get; private set; }
        public bool PlayerPoisoned { get; private set; }
        public bool PlayerSerum { get; private set; }
        public int PlayerDifficulty { get; private set; }
        public int PlayerSlot { get; private set; }
        public InventoryEntry PlayerEquipped { get; private set; }
        public InventoryEntry[] PlayerInventory { get; private set; }
        public EnemyEntry[] EnemyEntry { get; private set; }

        public int IGTRunningTimer { get; private set; }
        public int IGTCalculated => IGTRunningTimer / 60;

        public TimeSpan IGTTimeSpan
        {
            get
            {
                TimeSpan timespanIGT;

                if (IGTCalculated <= TimeSpan.MaxValue.Ticks)
                    timespanIGT = new TimeSpan(0, 0, IGTCalculated);
                else
                    timespanIGT = new TimeSpan();

                return timespanIGT;
            }
        }

        public string IGTFormattedString => IGTTimeSpan.ToString(IGT_TIMESPAN_STRING_FORMAT, CultureInfo.InvariantCulture);

        public GameMemory(GameProcess game)
        {
            Game = game;
            Pointers = game.Pointers;
            Memory = game.MainMemory;

            // Initialize variables to default values.
            PlayerRoom = 0;
            PlayerCharacter = 0;
            PlayerHealth = 0;
            PlayerMaxHealth = 0;
            PlayerPoisoned = false;
            PlayerSerum = false;
            PlayerDifficulty = 0;
            PlayerSlot = 0;
            PlayerEquipped = new InventoryEntry();
            PlayerInventory = new InventoryEntry[11];
            EnemyEntry = new EnemyEntry[32];
            IGTRunningTimer = 0;
        }

        /// <summary>
        /// This call refreshes important variables such as IGT.
        /// </summary>
        /// <param name="cToken"></param>
        public void RefreshSlim()
        {
            IGTRunningTimer = ByteHelper.SwapBytes(Memory.GetIntAt(Pointers.Time.ToInt64()), Game.IsBigEndian);
        }

        /// <summary>
        /// This call refreshes everything. This should be used less often. Inventory rendering can be more expensive and doesn't change as often.
        /// </summary>
        /// <param name="cToken"></param>
        public void Refresh()
        {
            RefreshSlim();

            PlayerDifficulty = Memory.GetByteAt(Pointers.Difficulty.ToInt64());
            PlayerCharacter = Memory.GetByteAt(Pointers.Character.ToInt64());
            PlayerRoom = ByteHelper.SwapBytes(Memory.GetShortAt(Pointers.Room.ToInt64()), Game.IsBigEndian);
            //PlayerPoisoned = Memory.GetByteAt(Pointers.Poison.ToInt64()) == 0x01;
            PlayerHealth = ByteHelper.SwapBytes(Memory.GetIntAt(Pointers.Health.ToInt64()), Game.IsBigEndian);

            if (Game.Product.Country == "JP")
                PlayerMaxHealth = PlayerDifficulty == 2 ? 400 : 200;
            else
                PlayerMaxHealth = 160;

            RefreshInventory();
            //RefreshEnemies();
        }

        public void RefreshInventory()
        {
            int index = -1;

            IntPtr pointer = IntPtr.Add(Pointers.Inventory, PlayerCharacter * 0x40);

            for (int i = 0; i < 12; ++i)
            {
                int item = ByteHelper.SwapBytes(Memory.GetIntAt(pointer.ToInt64()), Game.IsBigEndian);
                pointer = IntPtr.Add(pointer, 0x4);

                if (i <= 0)
                {
                    PlayerSlot = item;
                    continue;
                }

                PlayerInventory[++index] = new InventoryEntry(index, BitConverter.GetBytes(item));

                if (PlayerSlot == (index + 1))
                    PlayerEquipped = PlayerInventory[index];
            }
        }

        public void RefreshEnemies()
        {
            IntPtr pointer = new IntPtr(Pointers.Enemies.ToInt64());

            for (int i = 0; i < 6; ++i)
            {
                //int item = Memory.GetIntAt(pointer.ToInt64());
                pointer = IntPtr.Add(pointer, 0x580);

                EnemyEntry[i] = new EnemyEntry(0, 0);
            }
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~REmake1Memory() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}