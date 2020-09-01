using System;
using System.Collections.Generic;
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
        public List<EnemyEntry> EnemyEntry { get; private set; }
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

            EnemyEntry = new List<EnemyEntry>();

            IGTRunningTimer = 0;
        }

        public void RefreshSlim()
        {
            int magic = 0x00002041;

            if (Product.Console == "PS3")
            {
                switch (Player.Character)
                {
                    case CharacterEnumeration.Chris:
                        magic = 0x0003C4FC;
                        break;

                    case CharacterEnumeration.Steve:
                        magic = 0x0003A4D0;
                        break;

                    case CharacterEnumeration.Wesker:
                        magic = 0x0003E814;
                        break;

                    default:
                        magic = 0x0003D650;
                        break;
                }
            }

            IsRoomLoaded = ByteHelper.SwapBytes(Memory.GetIntAt(Pointers.RDXHeader.ToInt64())) == magic;
            IGTRunningTimer = ByteHelper.SwapBytes(Memory.GetIntAt(Pointers.Time.ToInt64()), IsBigEndian);
        }

        public void Refresh()
        {
            RefreshSlim();

            Player.Character = (CharacterEnumeration)Memory.GetByteAt(Pointers.Character.ToInt64());
            Player.Difficulty = Memory.GetByteAt(Pointers.Difficulty.ToInt64());
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
            Player.Inventory = new InventoryEntry[12];

            IntPtr pointer = IntPtr.Add(Pointers.Inventory, (int)Player.Character * 0x40);
            int index = 0;

            for (int i = 0; i < 12; ++i)
            {
                int item = ByteHelper.SwapBytes(Memory.GetIntAt(pointer.ToInt64()), IsBigEndian);
                pointer = IntPtr.Add(pointer, 0x4);

                if (i <= 0)
                    Player.Slot = item;
                else
                {
                    Player.Inventory[++index] = new InventoryEntry(index, BitConverter.GetBytes(item), Player.Slot == index);

                    if (Player.Slot == index)
                        Player.Inventory[0] = new InventoryEntry(0, BitConverter.GetBytes(item));
                }
            }
        }

        public void RefreshEnemies()
        {
            EnemyEntry = new List<EnemyEntry>();

            if (!IsRoomLoaded)
                return;

            IntPtr pointer = new IntPtr(Pointers.Enemy.ToInt64());
            int count = ByteHelper.SwapBytes(Memory.GetIntAt(Pointers.EnemyCount.ToInt64()), IsBigEndian);

            int entryOffset = Product.Console == "PS2" ? 0x0580 : 0x0578;
            int modelOffset = Product.Console == "PS2" ? 0x008B : 0x0088;

            for (int i = 0; i < count; ++i)
            {
                short type = ByteHelper.SwapBytes(Memory.GetShortAt(IntPtr.Add(pointer, 0x0004).ToInt64()), IsBigEndian);
                int slot = ByteHelper.SwapBytes(Memory.GetIntAt(IntPtr.Add(pointer, 0x039C).ToInt64()), IsBigEndian);
                int health = ByteHelper.SwapBytes(Memory.GetIntAt(IntPtr.Add(pointer, 0x041C).ToInt64()), IsBigEndian);
                int damage = ByteHelper.SwapBytes(Memory.GetIntAt(IntPtr.Add(pointer, 0x0574).ToInt64()), IsBigEndian);

                // Not sure what to call these values. They are useful to help determine enemy life status.
                byte action = Memory.GetByteAt(IntPtr.Add(pointer, 0x000C).ToInt64());
                byte status = Memory.GetByteAt(IntPtr.Add(pointer, 0x000F).ToInt64());
                byte model = Memory.GetByteAt(IntPtr.Add(pointer, modelOffset).ToInt64());

                pointer = IntPtr.Add(pointer, entryOffset);

                if (!Enum.IsDefined(typeof(EnemyEnumeration), (EnemyEnumeration)type))
                    continue;

                EnemyEntry enemy = new EnemyEntry(slot, (EnemyEnumeration)type, action, status, model, health, damage);
                bool active = action > 0 && action < 4;

                switch (enemy.Type)
                {
                    case EnemyEnumeration.Tenticle:
                        enemy.SetLife(160, active && health >= 0 && model == 0 && Player.Room != 0x091E);
                        break;

                    case EnemyEnumeration.GlupWorm:
                        enemy.SetLife(300, active && status > 0);
                        break;

                    case EnemyEnumeration.AnatomistZombie:
                        enemy.SetLife(200, active && status > 0);
                        break;

                    case EnemyEnumeration.Tyrant:
                        if (Player.Room == 0x0501)
                            enemy.SetLife(700, active && health >= 0);
                        else
                            enemy.SetLife(500, active && health >= 0);
                        break;

                    case EnemyEnumeration.Nosferatu:
                        enemy.SetLife(600, active && model == 0);
                        break;

                    case EnemyEnumeration.AlbinoidAdult:
                    case EnemyEnumeration.MutatedSteve:
                        enemy.SetLife(250, active && model == 0);
                        break;

                    case EnemyEnumeration.GiantBlackWidow:
                        enemy.SetLife(250, active && status > 0);
                        break;

                    case EnemyEnumeration.AlexiaAshford:
                        enemy.SetLife(300, active && Player.Room != 0x091E);
                        break;

                    case EnemyEnumeration.AlexiaAshfordB:
                        enemy.SetLife(700, active);
                        break;

                    case EnemyEnumeration.AlexiaAshfordC:
                        enemy.SetLife(400, !EnemyEntry[0].IsAlive);
                        break;

                    case EnemyEnumeration.AlexiaBaby:
                        enemy.SetLife(EnemyEntry[0].IsAlive);
                        break;
                        
                    case EnemyEnumeration.Hunter:
                        enemy.SetLife(active && model == 0);
                        break;

                    default:
                        enemy.SetLife(active && status > 0);
                        break;
                }

                EnemyEntry.Add(enemy);
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