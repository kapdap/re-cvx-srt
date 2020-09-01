using DoubleBuffered;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RECVXSRT
{
    public partial class MainUI : Form
    {
        // How often to perform more expensive operations.
        // 2000 milliseconds for updating pointers.
        // 333 milliseconds for a full scan.
        // 16 milliseconds for a slim scan.
        public const long OVER_UPDATE_TICKS = TimeSpan.TicksPerMillisecond * 3000L;
        public const long LONG_UPDATE_TICKS = TimeSpan.TicksPerMillisecond * 2000L;
        public const long FULL_UI_DRAW_TICKS = TimeSpan.TicksPerMillisecond * 333L;
        public const double SLIM_UI_DRAW_MS = 16d;

        private System.Timers.Timer memoryPollingTimer;
        private long lastOverUpdate;
        private long lastLongUpdate;
        private long lastFullUIDraw;

        // Quality settings (high performance).
        private CompositingMode compositingMode = CompositingMode.SourceOver;

        private CompositingQuality compositingQuality = CompositingQuality.HighSpeed;
        private SmoothingMode smoothingMode = SmoothingMode.None;
        private PixelOffsetMode pixelOffsetMode = PixelOffsetMode.Half;
        private InterpolationMode interpolationMode = InterpolationMode.NearestNeighbor;
        private TextRenderingHint textRenderingHint = TextRenderingHint.AntiAliasGridFit;

        // Text alignment and formatting.
        private StringFormat invStringFormat = new StringFormat(StringFormat.GenericDefault) { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far };
        private StringFormat stdStringFormat = new StringFormat(StringFormat.GenericDefault) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };

        private JSONServer jsonServer;
        private Task jsonServerTask;
        private OverlayDrawer overlayDrawer;
        private Task overlayDrawerTask;

        private Bitmap inventoryError; // An error image.
        private Bitmap inventoryImage;

        public MainUI()
        {
            InitializeComponent();

            // Set titlebar.
            this.Text += string.Format(" {0}", Program.srtVersion);

            this.ContextMenu = Program.contextMenu;
            this.playerHealthStatus.ContextMenu = Program.contextMenu;
            this.statisticsPanel.ContextMenu = Program.contextMenu;
            this.inventoryPanel.ContextMenu = Program.contextMenu;

            // JSON http endpoint.
            jsonServer = new JSONServer();
            jsonServerTask = jsonServer.Start(CancellationToken.None);

            //GDI+
            this.playerHealthStatus.Paint += this.playerHealthStatus_Paint;
            this.statisticsPanel.Paint += this.statisticsPanel_Paint;
            this.inventoryPanel.Paint += this.inventoryPanel_Paint;

            if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoTitleBar))
                this.FormBorderStyle = FormBorderStyle.None;

            if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.Transparent))
                this.TransparencyKey = Color.Black;

            // Only run the following code if we're rendering inventory.
            if (!Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoInventory))
            {
                GenerateImages();

                // Set the width and height of the inventory display so it matches the maximum items and the scaling size of those items.
                this.inventoryPanel.Width = Program.INV_SLOT_WIDTH * 2;
                this.inventoryPanel.Height = Program.INV_SLOT_HEIGHT * 6;

                // Adjust main form width as well.
                this.Width = this.inventoryPanel.Width + this.statisticsPanel.Width + 24;

                // Only adjust form height if its greater than 545. We don't want it to go below this size.
                if (41 + this.inventoryPanel.Height > 545)
                    this.Height = 41 + this.inventoryPanel.Height;
            }
            else
            {
                // Disable rendering of the inventory panel.
                this.inventoryPanel.Visible = false;

                // Adjust main form width as well.
                this.Width = this.statisticsPanel.Width + 2;
            }

            lastOverUpdate = DateTime.UtcNow.Ticks;
            lastLongUpdate = DateTime.UtcNow.Ticks;
            lastFullUIDraw = DateTime.UtcNow.Ticks;
        }

        public void DrawOverlay()
        {
            // DirectX
            if (Program.gameWindowHandle != IntPtr.Zero)
                overlayDrawer = new OverlayDrawer(Program.gameWindowHandle, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT, Program.programSpecialOptions.ScalingFactor);

            if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.DirectXOverlay) && overlayDrawer != null)
                overlayDrawerTask = overlayDrawer.Run(CancellationToken.None);
            else
                overlayDrawerTask = Task.CompletedTask;
        }

        public void GenerateImages()
        {
            // Create a black slot image for when side-pack is not equipped.
            inventoryError = new Bitmap(Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT, PixelFormat.Format32bppPArgb);
            using (Graphics grp = Graphics.FromImage(inventoryError))
            {
                grp.FillRectangle(new SolidBrush(Color.FromArgb(255, 0, 0, 0)), 0, 0, inventoryError.Width, inventoryError.Height);
                grp.DrawLine(new Pen(Color.FromArgb(150, 255, 0, 0), 3), 0, 0, inventoryError.Width, inventoryError.Height);
                grp.DrawLine(new Pen(Color.FromArgb(150, 255, 0, 0), 3), inventoryError.Width, 0, 0, inventoryError.Height);
            }

            // Transform the image into a 32-bit PARGB Bitmap.
            try
            {
                inventoryImage = Properties.Resources.ICONS.Clone(new Rectangle(0, 0, Properties.Resources.ICONS.Width, Properties.Resources.ICONS.Height), PixelFormat.Format32bppPArgb);
            }
            catch (Exception ex)
            {
                Program.FailFast(string.Format("[{0}] An unhandled exception has occurred. Please see below for details.\r\n\r\n[{1}] {2}\r\n{3}.\r\n\r\nPARGB Transform.", Program.srtVersion, ex.GetType().ToString(), ex.Message, ex.StackTrace), ex);
            }

            // Rescales the image down if the scaling factor is not 1.
            if (Program.programSpecialOptions.ScalingFactor != 1d)
            {
                try
                {
                    inventoryImage = new Bitmap(inventoryImage, (int)Math.Round(inventoryImage.Width * Program.programSpecialOptions.ScalingFactor, MidpointRounding.AwayFromZero), (int)Math.Round(inventoryImage.Height * Program.programSpecialOptions.ScalingFactor, MidpointRounding.AwayFromZero));
                }
                catch (Exception ex)
                {
                    Program.FailFast(string.Format(@"[{0}] An unhandled exception has occurred. Please see below for details.
---
[{1}] {2}
{3}", Program.srtVersion, ex.GetType().ToString(), ex.Message, ex.StackTrace), ex);
                }
            }
        }

        private void MemoryPollingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            bool exitLoop = false;

            try
            {
                bool procRun = Program.gameProcess.ProcessRunning;
                int procExitCode = Program.gameProcess.ProcessExitCode;
                if (!procRun)
                {
                    Program.mainProcess = null;
                    exitLoop = true;
                    return;
                }

                if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.AlwaysOnTop))
                {
                    bool hasFocus;
                    if (this.InvokeRequired)
                        hasFocus = PInvoke.HasActiveFocus((IntPtr)this.Invoke(new Func<IntPtr>(() => this.Handle)));
                    else
                        hasFocus = PInvoke.HasActiveFocus(this.Handle);

                    if (!hasFocus)
                    {
                        if (this.InvokeRequired)
                            this.Invoke(new Action(() => this.TopMost = true));
                        else
                            this.TopMost = true;
                    }
                }

                // Only perform a product update occasionally.
                if (DateTime.UtcNow.Ticks - lastLongUpdate >= LONG_UPDATE_TICKS)
                {
                    // Update the last drawn time.
                    lastLongUpdate = DateTime.UtcNow.Ticks;
                    Program.gameProcess.UpdateProduct();
                }

                // Only update if the game is supported.
                if (Program.gameProcess.Product.Supported)
                {
                    if (DateTime.UtcNow.Ticks - lastOverUpdate >= OVER_UPDATE_TICKS && overlayDrawer == null)
                    {
                        lastOverUpdate = DateTime.UtcNow.Ticks;

                        IntPtr gameWindowHandle = Program.gameProcess.FindGameWindowHandle();

                        if (gameWindowHandle != IntPtr.Zero)
                        {
                            Program.gameWindowHandle = gameWindowHandle;
                            DrawOverlay();
                        }
                    }

                    // Only draw occasionally, not as often as the stats panel.
                    if (DateTime.UtcNow.Ticks - lastFullUIDraw >= FULL_UI_DRAW_TICKS)
                    {
                        // Update the last drawn time.
                        lastFullUIDraw = DateTime.UtcNow.Ticks;

                        // Get the full amount of updated information from memory.
                        Program.gameMemory.Refresh();

                        // Only draw these periodically to reduce CPU usage.
                        this.playerHealthStatus.Invalidate();
                        if (!Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoInventory))
                            this.inventoryPanel.Invalidate();
                    }
                    else
                    {
                        // Get a slimmed-down amount of updated information from memory.
                        Program.gameMemory.RefreshSlim();
                    }
                }

                // Always draw this as these are simple text draws and contains the IGT/frame count.
                this.statisticsPanel.Invalidate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[{0}] {1}\r\n{2}", ex.GetType().ToString(), ex.Message, ex.StackTrace);
            }
            finally
            {
                // Trigger the timer to start once again. if we're not in fatal error.
                if (!exitLoop)
                    ((System.Timers.Timer)sender).Start();
                else
                    CloseForm();
            }
        }

        private void playerHealthStatus_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = smoothingMode;
            e.Graphics.CompositingQuality = compositingQuality;
            e.Graphics.CompositingMode = compositingMode;
            e.Graphics.InterpolationMode = interpolationMode;
            e.Graphics.PixelOffsetMode = pixelOffsetMode;
            e.Graphics.TextRenderingHint = textRenderingHint;

            // Draw health.
            Font healthFont = new Font("Consolas", 14, FontStyle.Bold);

            if (Program.gameMemory.Player.Health < 0) // Dead
            {
                e.Graphics.DrawString("DEAD", healthFont, Brushes.Red, 15, 37, stdStringFormat);
                playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.EMPTY, "EMPTY");

                return;
            }

            Brush brush = null;

            if (Program.gameMemory.Player.Gassed)
                brush = Brushes.Purple;
            else if (Program.gameMemory.Player.Poison)
                brush = Brushes.Violet;

            if (Program.gameMemory.Player.Health < 30) // Danger (Red)
            {
                if (brush == null) brush = Brushes.Red;

                if (Program.gameMemory.Player.Poison || Program.gameMemory.Player.Gassed)
                    playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.POISON, "POISON");
                else
                    playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.DANGER, "DANGER");
            }
            else if (Program.gameMemory.Player.Health < 60) // Caution (Orange)
            {
                if (brush == null) brush = Brushes.Gold;

                if (Program.gameMemory.Player.Poison || Program.gameMemory.Player.Gassed)
                    playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.POISON, "POISON");
                else
                    playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.CAUTION_YELLOW, "CAUTION_YELLOW"); // TODO: Orange image
            }
            else if (Program.gameMemory.Player.Health < 120) // Caution (Yellow)
            {
                if (brush == null) brush = Brushes.Goldenrod;

                if (Program.gameMemory.Player.Poison || Program.gameMemory.Player.Gassed)
                    playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.POISON, "POISON");
                else
                    playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.CAUTION_YELLOW, "CAUTION_YELLOW");
            }
            else // Fine (Green)
            {
                if (brush == null) brush = Brushes.LawnGreen;

                if (Program.gameMemory.Player.Poison || Program.gameMemory.Player.Gassed)
                    playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.POISON, "POISON");
                else
                    playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.FINE, "FINE");
            }

            e.Graphics.DrawString(Program.gameMemory.Player.Health.ToString(), healthFont, brush, 15, 37, stdStringFormat);
        }

        private void inventoryPanel_Paint(object sender, PaintEventArgs e)
        {
            int currentSlot = 0;

            if (!Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoInventory))
            {
                e.Graphics.SmoothingMode = smoothingMode;
                e.Graphics.CompositingQuality = compositingQuality;
                e.Graphics.CompositingMode = compositingMode;
                e.Graphics.InterpolationMode = interpolationMode;
                e.Graphics.PixelOffsetMode = pixelOffsetMode;
                e.Graphics.TextRenderingHint = textRenderingHint;

                foreach (InventoryEntry inv in Program.gameMemory.Player.Inventory)
                {
                    if (inv != default && inv.SlotPosition == 0 && inv.IsEmptySlot)
                        currentSlot++;

                    if (inv == default || inv.SlotPosition < 0 || inv.SlotPosition > 11 || inv.IsEmptySlot)
                        continue;

                    currentSlot++;

                    int slotColumn = currentSlot % 2;
                    int slotRow = currentSlot / 2;
                    int imageX = slotColumn * Program.INV_SLOT_WIDTH;
                    int imageY = slotRow * Program.INV_SLOT_HEIGHT;
                    int textX = imageX + Program.INV_SLOT_WIDTH;
                    int textY = imageY + Program.INV_SLOT_HEIGHT;
                    bool evenSlotColumn = slotColumn % 2 == 0;
                    Brush textBrush = Brushes.White;

                    if (inv.Quantity == 0)
                        textBrush = Brushes.DarkRed;

                    TextureBrush imageBrush;
                    if (Program.ItemToImageTranslation.ContainsKey(inv.ItemID))
                        imageBrush = new TextureBrush(inventoryImage, Program.ItemToImageTranslation[inv.ItemID]);
                    else
                        imageBrush = new TextureBrush(inventoryError, new Rectangle(0, 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT));

                    // Double-slot item.
                    if (imageBrush.Image.Width == Program.INV_SLOT_WIDTH * 2)
                    {
                        // Shift the quantity text over into the 2nd slot's area.
                        textX += Program.INV_SLOT_WIDTH;
                        currentSlot++;
                    }

                    e.Graphics.FillRectangle(imageBrush, imageX, imageY, imageBrush.Image.Width, imageBrush.Image.Height);

                    // TODO: Colors for different ammo types (Red = Flame, Green = B.O.W., Yellow = Acid, Blue = Normal).
                    e.Graphics.DrawString(!inv.Infinite ? inv.Quantity.ToString() : "∞", new Font("Consolas", 14, FontStyle.Bold), textBrush, textX, textY, invStringFormat);
                }
            }
        }

        private void statisticsPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = smoothingMode;
            e.Graphics.CompositingQuality = compositingQuality;
            e.Graphics.CompositingMode = compositingMode;
            e.Graphics.InterpolationMode = interpolationMode;
            e.Graphics.PixelOffsetMode = pixelOffsetMode;
            e.Graphics.TextRenderingHint = textRenderingHint;

            // Additional information and stats.
            // Adjustments for displaying text properly.
            int heightGap = 15;
            int heightOffset = 0;
            int i = 1;

            // IGT Display.
            e.Graphics.DrawString(string.Format("{0}", Program.gameMemory.IGTFormattedString), new Font("Consolas", 16, FontStyle.Bold), Brushes.White, 0, 0, stdStringFormat);

            if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.Debug))
            {
                e.Graphics.DrawString("T:" + Program.gameMemory.IGTRunningTimer.ToString("0000000000"), new Font("Consolas", 9, FontStyle.Bold), Brushes.Gray, 0, 25, stdStringFormat);
                e.Graphics.DrawString("C:" + Program.gameProcess.Product.Code, new Font("Consolas", 9, FontStyle.Bold), Brushes.Gray, 0, 38, stdStringFormat);
                heightOffset = 30; // Adding an additional offset to accomdate Raw IGT.
            }

            string status = "Normal";

            if (Program.gameMemory.Player.Gassed)
                status = "Gassed";
            else if (Program.gameMemory.Player.Poison)
                status = "Poison";

            e.Graphics.DrawString("Status: " + status, new Font("Consolas", 10, FontStyle.Bold), Brushes.White, 0, heightOffset + 25, stdStringFormat);
            e.Graphics.DrawString("Saves: " + Program.gameMemory.Player.Saves, new Font("Consolas", 10, FontStyle.Bold), Brushes.White, 0, heightOffset + 38, stdStringFormat);
            e.Graphics.DrawString("Retry: " + Program.gameMemory.Player.Retry, new Font("Consolas", 10, FontStyle.Bold), Brushes.White, 0, heightOffset + 51, stdStringFormat);
            heightOffset += 39;

            e.Graphics.DrawString("Enemy HP", new Font("Consolas", 10, FontStyle.Bold), Brushes.Red, 0, heightOffset + (heightGap * ++i), stdStringFormat);

            List<EnemyEntry> enemyList = Program.gameMemory.EnemyEntry;

            if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.DebugEnemy))
                enemyList = enemyList.OrderBy(a => a.Slot).ToList();
            else
                enemyList = enemyList.Where(a => a.IsAlive).OrderBy(a => a.Percentage).ThenByDescending(a => a.CurrentHP).ToList();

            foreach (EnemyEntry enemy in enemyList)
            {
                int x = 0;
                int y = heightOffset + (heightGap * ++i);

                DrawProgressBarGDI(e, backBrushGDI, foreBrushGDI, x, y, 146, heightGap, enemy.Percentage * 100f, 100f);

                if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.DebugEnemy))
                    e.Graphics.DrawString(enemy.DebugMessage, new Font("Consolas", 8, FontStyle.Regular), Brushes.Red, x, y + 1, stdStringFormat);
                else
                    e.Graphics.DrawString(enemy.HealthMessage, new Font("Consolas", 10, FontStyle.Bold), Brushes.Red, x, y, stdStringFormat);
            }
        }

        // Customisation in future?
        private Brush backBrushGDI = new SolidBrush(Color.FromArgb(255, 60, 60, 60));
        private Brush foreBrushGDI = new SolidBrush(Color.FromArgb(255, 100, 0, 0));

        private void DrawProgressBarGDI(PaintEventArgs e, Brush bgBrush, Brush foreBrush, float x, float y, float width, float height, float value, float maximum = 100)
        {
            // Draw BG.
            e.Graphics.DrawRectangles(new Pen(bgBrush, 2f), new RectangleF[1] { new RectangleF(x, y, width, height) });

            // Draw FG.
            RectangleF foreRect = new RectangleF(
                x + 1f,
                y + 1f,
                (width * value / maximum) - 2f,
                height - 2f
                );
            e.Graphics.FillRectangle(foreBrush, foreRect);
        }

        private void inventoryPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (!Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoInventory))
                if (e.Button == MouseButtons.Left)
                    PInvoke.DragControl(((DoubleBufferedPanel)sender).Parent.Handle);
        }

        private void statisticsPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PInvoke.DragControl(((DoubleBufferedPanel)sender).Parent.Handle);
        }

        private void playerHealthStatus_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PInvoke.DragControl(((PictureBox)sender).Parent.Handle);
        }

        private void MainUI_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PInvoke.DragControl(((Form)sender).Handle);
        }

        private void MainUI_Load(object sender, EventArgs e)
        {
            memoryPollingTimer = new System.Timers.Timer() { AutoReset = false, Interval = SLIM_UI_DRAW_MS };
            memoryPollingTimer.Elapsed += MemoryPollingTimer_Elapsed;
            memoryPollingTimer.Start();
        }

        private void MainUI_FormClosed(object sender, FormClosedEventArgs e)
        {
            memoryPollingTimer.Stop();
            memoryPollingTimer.Dispose();
        }

        private void CloseForm()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    this.Close();
                }));
            }
            else
                this.Close();
        }
    }
}