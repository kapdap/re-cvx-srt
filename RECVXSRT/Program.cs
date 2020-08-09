using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace RECVXSRT
{
    public static class Program
    {
        public static ContextMenu contextMenu;
        public static Options programSpecialOptions;

        public static Process mainProcess;

        public static GameProcess gameProcess;
        public static GameMemory gameMemory;

        public static IntPtr gameWindowHandle;

        public static readonly string srtVersion = string.Format("v{0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
        public static readonly string srtTitle = string.Format("RE: CVX SRT - {0}", srtVersion);

        public static int INV_SLOT_WIDTH;
        public static int INV_SLOT_HEIGHT;

        public static IReadOnlyDictionary<ItemEnumeration, System.Drawing.Rectangle> ItemToImageTranslation;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // Handle command-line parameters.
                programSpecialOptions = new Options();
                programSpecialOptions.GetOptions();

                foreach (string arg in args)
                {
                    if (arg.Equals("--Help", StringComparison.InvariantCultureIgnoreCase))
                    {
                        StringBuilder message = new StringBuilder("Command-line arguments:\r\n\r\n");
                        message.AppendFormat("{0}\r\n\t{1}\r\n\r\n", "--No-Titlebar", "Hide the titlebar and window frame.");
                        message.AppendFormat("{0}\r\n\t{1}\r\n\r\n", "--Always-On-Top", "Always appear on top of other windows.");
                        message.AppendFormat("{0}\r\n\t{1}\r\n\r\n", "--Transparent", "Make the background transparent.");
                        message.AppendFormat("{0}\r\n\t{1}\r\n\r\n", "--ScalingFactor=n", "Set the inventory slot scaling factor on a scale of 0.0 to 1.0. Default: 0.75 (75%)");
                        message.AppendFormat("{0}\r\n\t{1}\r\n\r\n", "--NoInventory", "Disables the inventory display.");
                        message.AppendFormat("{0}\r\n\t{1}\r\n\r\n", "--NoEnemyHealth", "Disables the enemy health display.");
                        message.AppendFormat("{0}\r\n\t{1}\r\n\r\n", "--DirectX", "Enables the DirectX overlay.");
                        message.AppendFormat("{0}\r\n\t{1}\r\n\r\n", "--Debug", "Debug mode.");

                        MessageBox.Show(null, message.ToString().Trim(), string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Environment.Exit(0);
                    }

                    if (arg.Equals("--No-Titlebar", StringComparison.InvariantCultureIgnoreCase))
                        programSpecialOptions.Flags |= ProgramFlags.NoTitleBar;

                    if (arg.Equals("--Always-On-Top", StringComparison.InvariantCultureIgnoreCase))
                        programSpecialOptions.Flags |= ProgramFlags.AlwaysOnTop;

                    if (arg.Equals("--Transparent", StringComparison.InvariantCultureIgnoreCase))
                        programSpecialOptions.Flags |= ProgramFlags.Transparent;

                    if (arg.Equals("--NoInventory", StringComparison.InvariantCultureIgnoreCase))
                        programSpecialOptions.Flags |= ProgramFlags.NoInventory;

                    if (arg.Equals("--NoEnemyHealth", StringComparison.InvariantCultureIgnoreCase))
                        programSpecialOptions.Flags |= ProgramFlags.NoEnemyHealth;

                    if (arg.Equals("--DirectX", StringComparison.InvariantCultureIgnoreCase))
                        programSpecialOptions.Flags |= ProgramFlags.DirectXOverlay;

                    if (arg.StartsWith("--ScalingFactor=", StringComparison.InvariantCultureIgnoreCase))
                        if (!double.TryParse(arg.Split(new char[1] { '=' }, 2, StringSplitOptions.None)[1], out programSpecialOptions.ScalingFactor))
                            programSpecialOptions.ScalingFactor = 0.75d; // Default scaling factor for the inventory images. If we fail to process the user input, ensure this gets set to the default value just in case.

                    if (arg.Equals("--Debug", StringComparison.InvariantCultureIgnoreCase))
                        programSpecialOptions.Flags |= ProgramFlags.Debug;
                }

                // Context menu.
                contextMenu = new ContextMenu();
                contextMenu.MenuItems.Add("Options", (object sender, EventArgs e) =>
                {
                    using (OptionsUI optionsForm = new OptionsUI())
                        optionsForm.ShowDialog();
                });
                contextMenu.MenuItems.Add("-", (object sender, EventArgs e) => { });
                contextMenu.MenuItems.Add("Exit", (object sender, EventArgs e) =>
                {
                    Environment.Exit(0);
                });

                // Set item slot sizes after scaling is determined.
                INV_SLOT_WIDTH = (int)Math.Round(112d * programSpecialOptions.ScalingFactor, MidpointRounding.AwayFromZero); // Individual inventory slot width.
                INV_SLOT_HEIGHT = (int)Math.Round(112d * programSpecialOptions.ScalingFactor, MidpointRounding.AwayFromZero); // Individual inventory slot height.

                GenerateClipping();

                // Standard WinForms stuff.
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                AttachAndShowUI();
            }
            catch (Exception ex)
            {
                FailFast(string.Format("[{0}] An unhandled exception has occurred. Please see below for details.\r\n\r\n[{1}] {2}\r\n{3}.", srtVersion, ex.GetType().ToString(), ex.Message, ex.StackTrace), ex);
            }
        }

        public static void AttachAndShowUI()
        {
            // This form finds the process for pcsx2.exe (assigned to gameProc) or waits until it is found.
            using (AttachUI attachUI = new AttachUI())
            using (ApplicationContext mainContext = new ApplicationContext(attachUI))
            {
                Application.Run(mainContext);
            }

            // If we exited the attach UI without finding a process, bail out completely.
            Debug.WriteLine("Checking for emulator process...");
            if (mainProcess == null)
                return;

            // Attach to the pcsx2.exe process now that we've found it and show the UI.
            Debug.WriteLine("Showing MainUI...");
            using (gameProcess = new GameProcess(mainProcess))
            using (gameMemory = new GameMemory(gameProcess))
            using (MainUI mainUI = new MainUI())
            using (ApplicationContext mainContext = new ApplicationContext(mainUI))
            {
                Application.Run(mainContext);
            }
        }

        public static void GetProcessInfo()
        {
            mainProcess = null;

            // Get list of supported emulator process names
            List<string> list = Emulators.GetList();

            // Try to find a running process from the list of supported emulators
            foreach (string name in list)
            {
                Process[] processes = Process.GetProcessesByName(name);
                Debug.WriteLine("{0} processes found: {1}", processes.Length, name);

                if (processes.Length <= 0)
                    continue;

                foreach (Process p in processes)
                    Debug.WriteLine("PID: {0}", p.Id);

                mainProcess = processes[0];
            }
        }

        public static void FailFast(string message, Exception ex)
        {
            ShowError(message);
            Environment.FailFast(message, ex);
        }

        public static void ShowError(string message)
        {
            MessageBox.Show(message, srtTitle, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
        }

        public static string GetExceptionMessage(Exception ex) => string.Format("[{0}] An unhandled exception has occurred. Please see below for details.\r\n\r\n[{1}] {2}\r\n{3}.", srtVersion, ex.GetType().ToString(), ex.Message, ex.StackTrace);

        public static void GenerateClipping()
        {
            int itemColumnInc = -1;
            int itemRowInc = -1;
            ItemToImageTranslation = new Dictionary<ItemEnumeration, System.Drawing.Rectangle>()
            {
                { ItemEnumeration.None, new System.Drawing.Rectangle(0, 0, 0, 0) },

                // 01. Healing Items
                { ItemEnumeration.FAidSpray, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GreenHerb, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RedHerb, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BlueHerb, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerb2Green, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerbRedGreen, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerbBlueGreen, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerb2GreenBlue, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerb3Green, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerbGreenBlueRed, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Hemostatic, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 11, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Serum, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 12, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 02. Weapon Ammo
                { ItemEnumeration.HandgunBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ShotgunShells, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ARifleBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BowGunArrows, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AcidRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 6), Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.FlameRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GrenadeRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BOWGasRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.InkRibbon, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BowGunPowder, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 14), Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GunPowderArrow, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MagnumBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 3, Program.INV_SLOT_HEIGHT * (itemRowInc + 1), Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MagnumBulletsInsideCase, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 3, Program.INV_SLOT_HEIGHT * (itemRowInc + 1), Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.M93RPart, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT * (itemRowInc + 1), Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 03. Keys
                { ItemEnumeration.PadlockKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 1, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SilverKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 3, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AirportKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 13, Program.INV_SLOT_HEIGHT * 5, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ChemStorageKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 4, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.TurnTableKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 9, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MiningRoomKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 8, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MachineRoomKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 8, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.CraneKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 8, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SterileRoomKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 8, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.KeyWithTag, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 8, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.HawkEmblem, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 15, Program.INV_SLOT_HEIGHT * 5, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SpAlloyEmblem, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 8, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 04.
                { ItemEnumeration.BiohazardCard, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 10, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.EmblemCard, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 10, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SecurityCard, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 10, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.IDCard, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 10, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.QueenAntObject, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 13, Program.INV_SLOT_HEIGHT * 1, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.KingAntObject, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 12, Program.INV_SLOT_HEIGHT * 1, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AirForceProof, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 8, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ArmyProof, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 11, Program.INV_SLOT_HEIGHT * 5, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.NavyProof, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 15, Program.INV_SLOT_HEIGHT * 5, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BlueJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 12, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RedJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 5, Program.INV_SLOT_HEIGHT * 8, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.FamilyPicture, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 10, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PlayingManual, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 9, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 05.
                { ItemEnumeration.Extinguisher, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 8, Program.INV_SLOT_HEIGHT * 8, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SteeringWheel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 10, Program.INV_SLOT_HEIGHT * 8, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SkeletonPicture, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GlassEye, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 1, Program.INV_SLOT_HEIGHT * 2, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RustedSword, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 9, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PianoRoll, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 4, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ControlLever, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 6, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.EaglePlate, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 14, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MusicBoxPlate, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 4, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Halberd, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 7, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PaperWeight, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 4, Program.INV_SLOT_HEIGHT * 5, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Detonator, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BatteryPack, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BarCodeSticker, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 11, Program.INV_SLOT_HEIGHT * 1, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DoorKnob, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 13, Program.INV_SLOT_HEIGHT * 7, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.TankObject, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 12, Program.INV_SLOT_HEIGHT * 7, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 06.
                { ItemEnumeration.Lighter, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 14, Program.INV_SLOT_HEIGHT * 2, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Lockpick, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 12, Program.INV_SLOT_HEIGHT * 2, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GasMask, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 7, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.TG01, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 14, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ClementAlpha, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 7, Program.INV_SLOT_HEIGHT * 7, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ClementSigma, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 8, Program.INV_SLOT_HEIGHT * 7, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ClementMixture, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 6, Program.INV_SLOT_HEIGHT * 7, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ValveHandle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 13, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.OctaValveHandle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 13, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SqValveHandle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 13, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH , Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Socket, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 5, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 07. Jewelry
                { ItemEnumeration.SilverDragonfly, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 15, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SilverDragonflyNoWings, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 15, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonflyNoWings, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 15, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonfly1Wing, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 15, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonfly2Wings, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 15, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonfly3Wings, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 15, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonfly, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 15, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.WingObject, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 13, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlexandersPierce, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT * 7, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlfredsRing, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 3, Program.INV_SLOT_HEIGHT * 7, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlexiasChoker, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 6, Program.INV_SLOT_HEIGHT * 8, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlexandersJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 16, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlfredsJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 16, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlexiasJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 16, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 08. Containers
                { ItemEnumeration.Briefcase, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 15, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.EarthenwareVase, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 9, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PlantPot, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 10, Program.INV_SLOT_HEIGHT * 6, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Crystal, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 6, Program.INV_SLOT_HEIGHT * 5, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SecurityFile, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 11, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DuraluminCaseBowGunPowder, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 9, Program.INV_SLOT_HEIGHT * 14, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DuraluminCaseM93RParts, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 9, Program.INV_SLOT_HEIGHT * 14, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DuraluminCaseMagnumRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 9, Program.INV_SLOT_HEIGHT * 1, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 09. Weapons
                { ItemEnumeration.CombatKnife, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 13, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Handgun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 12, Program.INV_SLOT_HEIGHT * 9, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.CustomHandgun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 14, Program.INV_SLOT_HEIGHT * 9, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.HandgunGlock17, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 1, Program.INV_SLOT_HEIGHT * 11, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.EnhancedHandgun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 4, Program.INV_SLOT_HEIGHT * 11, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GrenadeLauncher, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 7, Program.INV_SLOT_HEIGHT * 11, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BowGun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 11, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Shotgun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 6, Program.INV_SLOT_HEIGHT * 10, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Magnum, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 4, Program.INV_SLOT_HEIGHT * 10, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 10. Weapons (2 Slot Icon)
                { ItemEnumeration.M1P, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 13, Program.INV_SLOT_HEIGHT * 10, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldLugers, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 16, Program.INV_SLOT_HEIGHT * 11, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SubMachineGun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 15, Program.INV_SLOT_HEIGHT * 10, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AssaultRifle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 9, Program.INV_SLOT_HEIGHT * 12, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SniperRifle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 5, Program.INV_SLOT_HEIGHT * 11, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.LinearLauncher, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT * 12, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RocketLauncher, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 12, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },

                // 11. (Unused Content)
                { ItemEnumeration.Album, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Map, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Instructions, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.LugerReplica, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.CalicoBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RifleBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SidePack, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.HemostaticWire, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 11. (Unfinished Icon)
                { ItemEnumeration.MapRoll, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MGunBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PictureB, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.QueenAntRelief, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.KingAntRelief, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Shares Icon (Unused Content)
                { ItemEnumeration.AlfredsMemo, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BoardClip, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BowGunPowderUnused, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 14, Program.INV_SLOT_HEIGHT * 1, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DirectorsMemo, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DuraluminCaseUnused, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 9, Program.INV_SLOT_HEIGHT * 15, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.File, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PrisonersDiary, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 3, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SpAlloyEmblemUnused, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 8, Program.INV_SLOT_HEIGHT * 4, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // No Icon (Unused Content)
                { ItemEnumeration.Card, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.CrestKeyS, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.CrestKeyG, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.EmptyExtinguisher, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.FileFolders, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.Memo, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.NewspaperClip, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.SquareSocket, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.RemoteController, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.QueenAntReliefComplete, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.QuestionA, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.QuestionB, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.QuestionC, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.QuestionD, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.QuestionE, new System.Drawing.Rectangle(0, 0, 0, 0) },
            };
        }

        /*
        public static void GenerateClipping()
        {
            int itemColumnInc = -1;
            int itemRowInc = -1;
            ItemToImageTranslation = new Dictionary<ItemEnumeration, System.Drawing.Rectangle>()
            {
                { ItemEnumeration.None, new System.Drawing.Rectangle(0, 0, 0, 0) },

                // 01. Healing Items
                { ItemEnumeration.FAidSpray, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GreenHerb, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RedHerb, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BlueHerb, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerb2Green, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerbRedGreen, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerbBlueGreen, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerb2GreenBlue, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerb3Green, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerbGreenBlueRed, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Hemostatic, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Serum, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 02. Weapon Ammo
                { ItemEnumeration.HandgunBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ShotgunShells, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ARifleBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BowGunArrows, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AcidRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.FlameRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GrenadeRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BOWGasRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.InkRibbon, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BowGunPowder, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GunPowderArrow, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MagnumBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MagnumBulletsInsideCase, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.M93RPart, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 03. Keys
                { ItemEnumeration.PadlockKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SilverKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AirportKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ChemStorageKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.TurnTableKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MiningRoomKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MachineRoomKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.CraneKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SterileRoomKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.KeyWithTag, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.HawkEmblem, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SpAlloyEmblem, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 04.
                { ItemEnumeration.BiohazardCard, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.EmblemCard, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SecurityCard, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.IDCard, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.QueenAntObject, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.KingAntObject, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AirForceProof, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ArmyProof, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.NavyProof, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BlueJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RedJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.FamilyPicture, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PlayingManual, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 05.
                { ItemEnumeration.Extinguisher, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SteeringWheel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SkeletonPicture, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GlassEye, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RustedSword, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PianoRoll, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ControlLever, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.EaglePlate, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MusicBoxPlate, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Halberd, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PaperWeight, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Detonator, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BatteryPack, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BarCodeSticker, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DoorKnob, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.TankObject, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 06.
                { ItemEnumeration.Lighter, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Lockpick, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GasMask, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.TG01, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ClementAlpha, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ClementSigma, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ClementMixture, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ValveHandle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.OctaValveHandle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SqValveHandle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Socket, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 07. Jewelry
                { ItemEnumeration.SilverDragonfly, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SilverDragonflyNoWings, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonflyNoWings, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonfly1Wing, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonfly2Wings, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonfly3Wings, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonfly, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.WingObject, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlexandersPierce, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlfredsRing, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlexiasChoker, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlexandersJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlfredsJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlexiasJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 08. Containers
                { ItemEnumeration.Briefcase, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.EarthenwareVase, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PlantPot, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Crystal, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SecurityFile, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DuraluminCaseBowGunPowder, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DuraluminCaseM93RParts, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DuraluminCaseMagnumRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },

                // 09. Weapons
                { ItemEnumeration.CombatKnife, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Handgun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.EnhancedHandgun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.HandgunGlock17, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.CustomHandgun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GrenadeLauncher, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BowGun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Shotgun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Magnum, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 10. Weapons (2 Slot Icon)
                { ItemEnumeration.M1P, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldLugers, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SubMachineGun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AssaultRifle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SniperRifle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.LinearLauncher, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RocketLauncher, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },

                // 11. (Unused Content)
                { ItemEnumeration.Album, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Map, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Instructions, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.LugerReplica, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.CalicoBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RifleBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SidePack, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // 11. (Unfinished Icon)
                { ItemEnumeration.MapRoll, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MGunBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PictureB, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.QueenAntRelief, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.KingAntRelief, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Shares Icon (Unused Content)
                { ItemEnumeration.AlfredsMemo, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BoardClip, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BowGunPowderUnused, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DirectorsMemo, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DuraluminCaseUnused, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.File, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.HemostaticWire, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PrisonersDiary, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SpAlloyEmblemUnused, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // No Icon (Unused Content)
                { ItemEnumeration.Card, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.CrestKeyS, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.CrestKeyG, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.EmptyExtinguisher, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.FileFolders, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.Memo, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.NewspaperClip, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.SquareSocket, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.RemoteController, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.QueenAntReliefComplete, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.QuestionA, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.QuestionB, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.QuestionC, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.QuestionD, new System.Drawing.Rectangle(0, 0, 0, 0) },
                { ItemEnumeration.QuestionE, new System.Drawing.Rectangle(0, 0, 0, 0) },
            };
        }
        */
    }
}