using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RECVXSRT
{
    public class GameProcess : IDisposable
    {
        public bool ProcessRunning => MainMemory.ProcessRunning;
        public int ProcessExitCode => MainMemory.ProcessExitCode;

        public int PID { get; private set; }
        public string Name { get; private set; }
        public bool IsBigEndian { get; private set; }

        public Process MainProcess { get; private set; }
        public ProcessMemory.ProcessMemory MainMemory { get; private set; }

        public IntPtr GamePointer { get; private set; }
        public IntPtr ProductPointer { get; private set; }

        public GamePointers Pointers { get; private set; }
        public GameProduct Product { get; private set; }

        public GameProcess(Process process)
        {
            PID = process.Id;

            MainProcess = process;
            MainMemory = new ProcessMemory.ProcessMemory(process.Id);

            if (MainProcess.ProcessName == Emulators.PCSX2)
            {
                Name = Emulators.PCSX2;
                IsBigEndian = false;

                GamePointer = new IntPtr(0x20000000);
                ProductPointer = IntPtr.Add(GamePointer, 0x00015B90);
            }
            else
            {
                Name = Emulators.RPCS3;
                IsBigEndian = true;

                GamePointer = new IntPtr(0x300000000);
                ProductPointer = IntPtr.Add(GamePointer, 0x20010251);
            }

            Pointers = new GamePointers();
            Product = new GameProduct();

            UpdateProduct();
        }

        public void UpdateProduct()
        {
            int length = 0;

            if (Name == Emulators.PCSX2)
                length = 11;
            else if (Name == Emulators.RPCS3)
                length = 9;

            byte[] buffer = MainMemory.GetByteArrayAt(ProductPointer.ToInt64(), length, true);
            string code = Encoding.UTF8.GetString(buffer);

            if (Product.Code != code)
            {
                Product.SetCode(code);
                UpdatePointers();
            }
        }

        public void UpdatePointers()
        {
            switch (Product.Code)
            {
                case GameProduct.SLPM_65022:
                    Pointers.Time = IntPtr.Add(GamePointer, 0x004314A0);
                    Pointers.Room = IntPtr.Add(GamePointer, 0x004314B4);
                    Pointers.Serum = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Poison = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Health = IntPtr.Add(GamePointer, 0x004301FC);
                    Pointers.Character = IntPtr.Add(GamePointer, 0x00430C84);
                    Pointers.Inventory = IntPtr.Add(GamePointer, 0x00430E70);
                    Pointers.Enemies = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Difficulty = IntPtr.Add(GamePointer, 0x00430C8C);
                    break;

                case GameProduct.SLUS_20184:
                    Pointers.Time = IntPtr.Add(GamePointer, 0x004339A0);
                    Pointers.Room = IntPtr.Add(GamePointer, 0x004339B4);
                    Pointers.Serum = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Poison = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Health = IntPtr.Add(GamePointer, 0x004326FC);
                    Pointers.Character = IntPtr.Add(GamePointer, 0x00433184);
                    Pointers.Inventory = IntPtr.Add(GamePointer, 0x00433370);
                    Pointers.Enemies = IntPtr.Add(GamePointer, 0x004062E0);
                    Pointers.Difficulty = IntPtr.Add(GamePointer, 0x0043318C);
                    break;

                case GameProduct.SLES_50306:
                    Pointers.Time = IntPtr.Add(GamePointer, 0x0044A1D0);
                    Pointers.Room = IntPtr.Add(GamePointer, 0x0044A1E4);
                    Pointers.Serum = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Poison = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Health = IntPtr.Add(GamePointer, 0x00448F2C);
                    Pointers.Character = IntPtr.Add(GamePointer, 0x004499B4);
                    Pointers.Inventory = IntPtr.Add(GamePointer, 0x00449BA0);
                    Pointers.Enemies = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Difficulty = IntPtr.Add(GamePointer, 0x004499BC);
                    break;

                case GameProduct.NPUB30467:
                    Pointers.Time = IntPtr.Add(GamePointer, 0x00BB3DB8);
                    Pointers.Room = IntPtr.Add(GamePointer, 0x00BB3DCC);
                    Pointers.Serum = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Poison = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Health = IntPtr.Add(GamePointer, 0x00BDEA1C);
                    Pointers.Character = IntPtr.Add(GamePointer, 0x00BB359C);
                    Pointers.Inventory = IntPtr.Add(GamePointer, 0x00BB3788);
                    Pointers.Enemies = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Difficulty = IntPtr.Add(GamePointer, 0x00BB36A4); // TODO
                    break;

                case GameProduct.NPEB00553:
                    Pointers.Time = IntPtr.Add(GamePointer, 0x00BC40B8);
                    Pointers.Room = IntPtr.Add(GamePointer, 0x00BC40CC);
                    Pointers.Serum = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Poison = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Health = IntPtr.Add(GamePointer, 0x00BEED1C);
                    Pointers.Character = IntPtr.Add(GamePointer, 0x00BC389C);
                    Pointers.Inventory = IntPtr.Add(GamePointer, 0x00BC3A88);
                    Pointers.Enemies = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Difficulty = IntPtr.Add(GamePointer, 0x00BC38A4); // TODO
                    break;

                default: // GameProduct.NPJB00135
                    Pointers.Time = IntPtr.Add(GamePointer, 0x00BB3E38);
                    Pointers.Room = IntPtr.Add(GamePointer, 0x00BB3E4C);
                    Pointers.Serum = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Poison = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Health = IntPtr.Add(GamePointer, 0x00BDEA9C);
                    Pointers.Character = IntPtr.Add(GamePointer, 0x00BB361C);
                    Pointers.Inventory = IntPtr.Add(GamePointer, 0x00BB3808);
                    Pointers.Enemies = IntPtr.Add(GamePointer, 0x00000000); // TODO
                    Pointers.Difficulty = IntPtr.Add(GamePointer, 0x00BB3624); // TODO
                    break;
            }
        }

        public IntPtr FindGameWindowHandle()
        {
            List<IntPtr> windowHandles = WindowHelper.EnumerateProcessWindowHandles(PID);
            foreach (IntPtr windowHandle in windowHandles)
            {
                // https://forums.pcsx2.net/Thread-can-someone-help-PCSX2-s-ClassName
                // How to return the PCSX2 game window handle (Post #4)
                // 1. Find all parent window handles having the "wxWindowClassNR" class name.
                // 2. Compare the leftmost window text of them with a string "GSdx".
                if (Name == Emulators.PCSX2)
                {
                    string windowTitle = WindowHelper.GetTitle(windowHandle);

                    if (windowTitle.Contains("GSdx"))
                        return windowHandle;
                }
                else // Emulators.RPCS3
                {
                    if (WindowHelper.GetClassName(windowHandle) != "Qt5QWindowIcon")
                        continue;

                    string windowTitle = WindowHelper.GetTitle(windowHandle);

                    if (windowTitle.StartsWith("FPS") || windowTitle.EndsWith("| SRT"))
                        return windowHandle;
                }
            }

            return IntPtr.Zero;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (MainMemory != null)
                        MainMemory.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~GameProcess()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            //GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}