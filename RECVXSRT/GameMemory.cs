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

        public GamePlayer Player { get; private set; }
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

            Player = new GamePlayer();

            // Initialize variables to default values.
            Player.Room = 0;
            Player.Character = 0;
            Player.Health = 0;
            Player.MaxHealth = 0;
            Player.Status = 0x00;
            Player.Poisoned = false;
            Player.Gassed = false;
            Player.Difficulty = 0;
            Player.Slot = 0;
            Player.Equipment = new InventoryEntry();
            Player.Inventory = new InventoryEntry[11];

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

            Player.Difficulty = Memory.GetByteAt(Pointers.Difficulty.ToInt64());
            Player.Character = Memory.GetByteAt(Pointers.Character.ToInt64());
            Player.Room = ByteHelper.SwapBytes(Memory.GetShortAt(Pointers.Room.ToInt64()), Game.IsBigEndian);
            Player.Health = ByteHelper.SwapBytes(Memory.GetIntAt(Pointers.Health.ToInt64()), Game.IsBigEndian);
            Player.Status = Memory.GetByteAt(Pointers.Status.ToInt64());
            Player.Poisoned = (Player.Status & 0x08) != 0;
            Player.Gassed = (Player.Status & 0x20) != 0;
            Player.Saves = ByteHelper.SwapBytes(Memory.GetIntAt(Pointers.Saves.ToInt64()), Game.IsBigEndian);
            Player.Retries = ByteHelper.SwapBytes(Memory.GetShortAt(Pointers.Retries.ToInt64()), Game.IsBigEndian);

            if (Game.Product.Country == "JP")
                Player.MaxHealth = Player.Difficulty == 2 ? 400 : 200;
            else
                Player.MaxHealth = 160;

            RefreshInventory();
            //RefreshEnemies();
        }

        public void RefreshInventory()
        {
            int index = -1;

            IntPtr pointer = IntPtr.Add(Pointers.Inventory, Player.Character * 0x40);

            for (int i = 0; i < 12; ++i)
            {
                int item = ByteHelper.SwapBytes(Memory.GetIntAt(pointer.ToInt64()), Game.IsBigEndian);
                pointer = IntPtr.Add(pointer, 0x4);

                if (i <= 0)
                {
                    Player.Slot = item;
                    continue;
                }

                Player.Inventory[++index] = new InventoryEntry(index, BitConverter.GetBytes(item));

                if (Player.Slot == (index + 1))
                    Player.Equipment = Player.Inventory[index];
            }
        }

        public void RefreshEnemies()
        {
            IntPtr pointer = new IntPtr(Pointers.Enemies.ToInt64());

            for (int i = 0; i < 6; ++i)
            {
                int parent = Memory.GetIntAt(IntPtr.Add(pointer, 0x02EC).ToInt64());
                int linked = Memory.GetIntAt(IntPtr.Add(pointer, 0x02F4).ToInt64());
                int type = Memory.GetIntAt(IntPtr.Add(pointer, 0x040C).ToInt64());

                int start = Memory.GetIntAt(pointer.ToInt64());
                int slot = Memory.GetIntAt(IntPtr.Add(pointer, 0x039C).ToInt64());
                int status = Memory.GetIntAt(IntPtr.Add(pointer, 0x000C).ToInt64());
                int spawned = Memory.GetIntAt(IntPtr.Add(pointer, 0x008B).ToInt64());
                int health = Memory.GetIntAt(IntPtr.Add(pointer, 0x041C).ToInt64());
                int damage = Memory.GetIntAt(IntPtr.Add(pointer, 0x0475).ToInt64());

                EnemyEntry[i] = new EnemyEntry(0, 0);

                pointer = IntPtr.Add(pointer, 0x580);
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