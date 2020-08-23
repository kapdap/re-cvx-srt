using System;
using System.Globalization;

namespace RECVXSRT
{
    public class GameMemory : IDisposable
    {
        private const string IGT_TIMESPAN_STRING_FORMAT = @"hh\:mm\:ss";

        public bool IsBigEndian { get; private set; }
        public ProcessMemory.ProcessMemory Memory { get; private set; }

        public GamePointers Pointers { get; private set; }
        public GameProduct Product { get; private set; }

        public GamePlayer Player { get; private set; }
        public EnemyEntry[] EnemyEntry { get; private set; }
        public int EnemyEntrySize { get; private set; }
        public bool IsRoomLoaded { get; private set; }

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
            IsBigEndian = game.IsBigEndian;
            Memory = game.MainMemory;
            Pointers = game.Pointers;
            Product = game.Product;

            Player = new GamePlayer();

            // Initialize variables to default values.
            Player.Room = 0;
            Player.Character = 0;
            Player.Health = 0;
            Player.MaxHealth = 0;
            Player.Status = 0x00;
            Player.Poison = false;
            Player.Gassed = false;
            Player.Difficulty = 0;
            Player.Slot = 0;
            Player.Equipment = new InventoryEntry();
            Player.Inventory = new InventoryEntry[11];

            EnemyEntry = new EnemyEntry[0];

            IGTRunningTimer = 0;
        }

        public void RefreshSlim()
        {
            int magic = Product.System == "PS2" ? 0x00002041 : 0x0003D650;

            IsRoomLoaded = ByteHelper.SwapBytes(Memory.GetIntAt(Pointers.RDXHeader.ToInt64())) == magic;
            IGTRunningTimer = ByteHelper.SwapBytes(Memory.GetIntAt(Pointers.Time.ToInt64()), IsBigEndian);
        }

        public void Refresh()
        {
            RefreshSlim();

            Player.Difficulty = Memory.GetByteAt(Pointers.Difficulty.ToInt64());
            Player.Character = Memory.GetByteAt(Pointers.Character.ToInt64());
            Player.Health = ByteHelper.SwapBytes(Memory.GetIntAt(Pointers.Health.ToInt64()), IsBigEndian);
            Player.Status = Memory.GetByteAt(Pointers.Status.ToInt64());
            Player.Poison = (Player.Status & 0x08) != 0;
            Player.Gassed = (Player.Status & 0x20) != 0;
            Player.Saves = ByteHelper.SwapBytes(Memory.GetIntAt(Pointers.Saves.ToInt64()), IsBigEndian);
            Player.Retry = ByteHelper.SwapBytes(Memory.GetShortAt(Pointers.Retry.ToInt64()), IsBigEndian);
            Player.Room = ByteHelper.SwapBytes(Memory.GetShortAt(Pointers.Room.ToInt64())); // Note: Room bytes are always swapped!

            if (Product.Country == "JP")
                Player.MaxHealth = Player.Difficulty == 2 ? 400 : 200;
            else
                Player.MaxHealth = 160;

            RefreshInventory();
            RefreshEnemies();
        }

        public void RefreshInventory()
        {
            int index = -1;

            IntPtr pointer = IntPtr.Add(Pointers.Inventory, Player.Character * 0x40);

            for (int i = 0; i < 12; ++i)
            {
                int item = ByteHelper.SwapBytes(Memory.GetIntAt(pointer.ToInt64()), IsBigEndian);
                pointer = IntPtr.Add(pointer, 0x4);

                if (i <= 0)
                {
                    Player.Slot = item;
                    continue;
                }

                Player.Inventory[++index] = new InventoryEntry(index, BitConverter.GetBytes(item));
            }

            if (Player.Slot > 0)
                Player.Equipment = Player.Inventory[Player.Slot + 1];
            else
                Player.Equipment = new InventoryEntry();
        }

        public void RefreshEnemies()
        {
            if (!IsRoomLoaded)
            {
                EnemyEntry = new EnemyEntry[0];
                return;
            }

            IntPtr pointer = new IntPtr(Pointers.Enemy.ToInt64());

            int count = ByteHelper.SwapBytes(Memory.GetIntAt(Pointers.EnemyCount.ToInt64()), IsBigEndian);
            int index = -1;

            EnemyEntry = new EnemyEntry[count];
            EnemyEntrySize = Product.System == "PS2" ? 0x580 : 0x578;

            for (int i = 0; i < count; ++i)
            {
                short type = ByteHelper.SwapBytes(Memory.GetShortAt(IntPtr.Add(pointer, 0x0004).ToInt64()), IsBigEndian);
                byte action = Memory.GetByteAt(IntPtr.Add(pointer, 0x000C).ToInt64());
                byte status = Memory.GetByteAt(IntPtr.Add(pointer, 0x000F).ToInt64());
                int health = ByteHelper.SwapBytes(Memory.GetIntAt(IntPtr.Add(pointer, 0x041C).ToInt64()), IsBigEndian);

                pointer = IntPtr.Add(pointer, EnemyEntrySize);

                if (!Enum.IsDefined(typeof(EnemyEnumeration), (EnemyEnumeration)type)
                    || action <= 0 || action >= 4)
                    continue;

                switch ((EnemyEnumeration)type)
                {
                    case EnemyEnumeration.GlupWorm:
                        if (status > 0)
                            EnemyEntry[++index] = new EnemyEntry(300, health);
                        break;

                    case EnemyEnumeration.AnatomistZombie:
                        if (status > 0)
                            EnemyEntry[++index] = new EnemyEntry(200, health);
                        break;

                    case EnemyEnumeration.Tyrant:
                        if (health >= 0 && Player.Room == 0x0501)
                            EnemyEntry[++index] = new EnemyEntry(700, health);
                        else if (health >= 0)
                            EnemyEntry[++index] = new EnemyEntry(500, health);
                        break;

                    case EnemyEnumeration.AlbinoidAdult:
                        if (status > 0)
                            EnemyEntry[++index] = new EnemyEntry(250, health);
                        break;

                    case EnemyEnumeration.GiantBlackWidow:
                        if (status > 0)
                            EnemyEntry[++index] = new EnemyEntry(250, health);
                        break;

                    case EnemyEnumeration.MutatedSteve:
                        if (status > 0)
                            EnemyEntry[++index] = new EnemyEntry(250, health);
                        break;

                    case EnemyEnumeration.Nosferatu:
                        EnemyEntry[++index] = new EnemyEntry(600, health);
                        break;

                    case EnemyEnumeration.AlexiaAshford:
                        if (Player.Room != 0x091E)
                            EnemyEntry[++index] = new EnemyEntry(300, health);
                        break;

                    case EnemyEnumeration.AlexiaAshfordB:
                        EnemyEntry[++index] = new EnemyEntry(700, health);
                        break;

                    case EnemyEnumeration.AlexiaAshfordC:
                        EnemyEntry[++index] = new EnemyEntry(400, health);
                        break;

                    case EnemyEnumeration.Tenticle:
                        if (Player.Room != 0x091E)
                            EnemyEntry[++index] = new EnemyEntry(health, health, false);
                        break;

                    default:
                        if (status > 0)
                            EnemyEntry[++index] = new EnemyEntry(health, health, false);
                        break;
                }
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