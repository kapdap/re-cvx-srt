using GameOverlay.Drawing;
using GameOverlay.Windows;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RECVXSRT
{
    public class OverlayDrawer : DXOverlay, IDisposable
    {
        private SharpDX.Direct2D1.Bitmap inventoryError;
        private SharpDX.Direct2D1.Bitmap inventoryImage;

        private static Font consolasBold = null;
        private static SolidBrush darkRedBrush = null;
        private static SolidBrush redBrush = null;
        private static SolidBrush whiteBrush = null;
        private static SolidBrush greyBrush = null;
        private static SolidBrush blackBrush = null;
        private static SolidBrush greenBrush = null;
        private static SolidBrush yellowBrush = null;
        private static SolidBrush purpleBrush = null;
        private static SolidBrush violetBrush = null;
        private static SolidBrush goldBrush = null;
        private static SolidBrush goldenRodBrush = null;

        private static SolidBrush backBrush = null;
        private static SolidBrush foreBrush = null;

        public OverlayDrawer(IntPtr windowHook, int invSlotWidth, int invSlotHeight, double invSlotScaling = 0.5d, double desiredDrawRate = 60d) : base(windowHook, desiredDrawRate)
        {
            base.Initialize((OverlayWindow w, Graphics g) =>
            {
                consolasBold = g.CreateFont("Consolas", 8f, true);

                darkRedBrush = g.CreateSolidBrush(139, 0, 0);
                redBrush = g.CreateSolidBrush(255, 0, 0);
                yellowBrush = g.CreateSolidBrush(218, 165, 32); // Goldenrod
                greenBrush = g.CreateSolidBrush(124, 252, 0); // LawnGreen
                whiteBrush = g.CreateSolidBrush(255, 255, 255);
                greyBrush = g.CreateSolidBrush(150, 150, 150);
                blackBrush = g.CreateSolidBrush(0, 0, 0);
                purpleBrush = g.CreateSolidBrush(128, 0, 128);
                violetBrush = g.CreateSolidBrush(238, 130, 238);
                goldBrush = g.CreateSolidBrush(255, 215, 0);
                goldenRodBrush = g.CreateSolidBrush(218, 165, 32);

                backBrush = g.CreateSolidBrush(60, 60, 60);
                foreBrush = g.CreateSolidBrush(100, 0, 0);

                // Loads the inventory images into memory and scales them if required.
                GenerateImages(g, invSlotWidth, invSlotHeight, invSlotScaling);
            });
        }

        public Task Run(CancellationToken cToken) => base.Run(DirectXOverlay_Paint, cToken);

        private void DirectXOverlay_Paint(OverlayWindow w, Graphics g)
        {
            StatisticsDraw(w, g, 15, 60);
            HealthDraw(w, g, 15, 35);
            InventoryDraw(w, g, 135, 35);
        }

        private void DrawProgressBarDirectX(OverlayWindow w, Graphics g, SolidBrush bgBrush, SolidBrush foreBrush, float x, float y, float width, float height, float value, float maximum = 100)
        {
            // Draw BG.
            g.DrawRectangle(bgBrush, x, y, x + width, y + height, 3f);

            // Draw FG.
            Rectangle foreRect = new Rectangle(
                x,
                y,
                x + ((width * value / maximum)),
                y + (height)
                );
            g.FillRectangle(foreBrush, foreRect);
        }

        private void HealthDraw(OverlayWindow w, Graphics g, int xOffset, int yOffset)
        {
            float fontSize = 26f;

            // Draw health.
            if (Program.gameMemory.Player.Health < 0) // Dead
                g.DrawText(consolasBold, fontSize, redBrush, xOffset, yOffset, "DEAD");
            else if (Program.gameMemory.Player.Health < 30) // Caution (Orange)
            {
                if (Program.gameMemory.Player.Gassed)
                    g.DrawText(consolasBold, fontSize, purpleBrush, xOffset, yOffset, Program.gameMemory.Player.Health.ToString());
                else if (Program.gameMemory.Player.Poison)
                    g.DrawText(consolasBold, fontSize, violetBrush, xOffset, yOffset, Program.gameMemory.Player.Health.ToString());
                else
                    g.DrawText(consolasBold, fontSize, redBrush, xOffset, yOffset, Program.gameMemory.Player.Health.ToString());
            }
            else if (Program.gameMemory.Player.Health < 60) // Caution (Yellow)
            {
                if (Program.gameMemory.Player.Gassed)
                    g.DrawText(consolasBold, fontSize, purpleBrush, xOffset, yOffset, Program.gameMemory.Player.Health.ToString());
                else if (Program.gameMemory.Player.Poison)
                    g.DrawText(consolasBold, fontSize, violetBrush, xOffset, yOffset, Program.gameMemory.Player.Health.ToString());
                else
                    g.DrawText(consolasBold, fontSize, goldBrush, xOffset, yOffset, Program.gameMemory.Player.Health.ToString());
            }
            else if (Program.gameMemory.Player.Health < 120) // Danger (Red)
            {
                if (Program.gameMemory.Player.Gassed)
                    g.DrawText(consolasBold, fontSize, purpleBrush, xOffset, yOffset, Program.gameMemory.Player.Health.ToString());
                else if (Program.gameMemory.Player.Poison)
                    g.DrawText(consolasBold, fontSize, violetBrush, xOffset, yOffset, Program.gameMemory.Player.Health.ToString());
                else
                    g.DrawText(consolasBold, fontSize, goldenRodBrush, xOffset, yOffset, Program.gameMemory.Player.Health.ToString());
            }
            else // Fine (Green)
            {
                if (Program.gameMemory.Player.Gassed)
                    g.DrawText(consolasBold, fontSize, purpleBrush, xOffset, yOffset, Program.gameMemory.Player.Health.ToString());
                else if (Program.gameMemory.Player.Poison)
                    g.DrawText(consolasBold, fontSize, violetBrush, xOffset, yOffset, Program.gameMemory.Player.Health.ToString());
                else
                    g.DrawText(consolasBold, fontSize, greenBrush, xOffset, yOffset, Program.gameMemory.Player.Health.ToString());
            }
        }

        private void StatisticsDraw(OverlayWindow w, Graphics g, int xOffset, int yOffset)
        {
            // Additional information and stats.
            // Adjustments for displaying text properly.
            int heightGap = 15;
            int i = -1;

            // IGT Display.
            g.DrawText(consolasBold, 20f, whiteBrush, xOffset + 0, yOffset + (heightGap * ++i), string.Format("{0}", Program.gameMemory.IGTFormattedString));
            yOffset += 5;

            if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.Debug))
            {
                ++i;
                g.DrawText(consolasBold, 16f, greyBrush, xOffset + 0, yOffset + (heightGap * ++i), "T:" + Program.gameMemory.IGTRunningTimer.ToString("0000000000"));
                g.DrawText(consolasBold, 16f, greyBrush, xOffset + 0, yOffset + (heightGap * ++i), "C:" + Program.gameProcess.Product.Code);
                ++i;
            }

            string status = "Normal";

            if (Program.gameMemory.Player.Gassed)
                status = "Gassed";
            else if (Program.gameMemory.Player.Poison)
                status = "Poison";

            g.DrawText(consolasBold, 16f, whiteBrush, xOffset + 0, yOffset + (heightGap * ++i), "Status: " + status);
            g.DrawText(consolasBold, 16f, whiteBrush, xOffset + 0, yOffset + (heightGap * ++i), "Retries: " + Program.gameMemory.Player.Retries);
            g.DrawText(consolasBold, 16f, whiteBrush, xOffset + 0, yOffset + (heightGap * ++i), "Saves: " + Program.gameMemory.Player.Saves);
            ++i;

            if (!Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoEnemyHealth))
            {
                g.DrawText(consolasBold, 16f, redBrush, xOffset + 0, yOffset + (heightGap * ++i), "Enemy HP");
                yOffset += 6;
                foreach (EnemyEntry enemyHP in Program.gameMemory.EnemyEntry.Where(a => a.IsAlive).OrderBy(a => a.Percentage).ThenByDescending(a => a.CurrentHP))
                {
                    int x = xOffset + 0;
                    int y = yOffset + (heightGap * ++i);

                    DrawProgressBarDirectX(w, g, backBrush, foreBrush, x, y, 158, heightGap, enemyHP.Percentage * 100f, 100f);
                    g.DrawText(consolasBold, 12f, redBrush, x + 5, y, string.Format("{0} {1:P1}", enemyHP.CurrentHP, enemyHP.Percentage));
                }
            }
        }

        private void InventoryDraw(OverlayWindow w, Graphics g, int xOffset, int yOffset)
        {
            int currentSlot = 0;

            foreach (InventoryEntry inv in Program.gameMemory.Player.Inventory)
            {
                if (inv != default && inv.SlotPosition == 0 && inv.IsEmptySlot)
                    currentSlot++;

                if (inv == default || inv.SlotPosition < 0 || inv.SlotPosition > 11 || inv.IsEmptySlot)
                    continue;

                currentSlot++;

                int slotColumn = currentSlot % 2;
                int slotRow = currentSlot / 2;
                int imageX = xOffset + (slotColumn * Program.INV_SLOT_WIDTH);
                int imageY = yOffset + (slotRow * Program.INV_SLOT_HEIGHT);
                int textX = imageX + (int)(Program.INV_SLOT_HEIGHT * 0.7);
                int textY = imageY + (int)(Program.INV_SLOT_HEIGHT * 0.7);
                bool evenSlotColumn = slotColumn % 2 == 0;
                SolidBrush textBrush = whiteBrush;

                if (inv.Quantity == 0)
                    textBrush = darkRedBrush;

                System.Drawing.Rectangle r;
                SharpDX.Direct2D1.Bitmap b;

                if (!inv.IsEmptySlot && Program.ItemToImageTranslation.ContainsKey(inv.ItemID))
                {
                    r = Program.ItemToImageTranslation[inv.ItemID];
                    b = inventoryImage;
                }
                else
                {
                    r = new System.Drawing.Rectangle(0, 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT);
                    b = inventoryError;
                }

                // Double-slot item.
                if (r.Width == Program.INV_SLOT_WIDTH * 2)
                {
                    // Shift the quantity text over into the 2nd slot's area.
                    textX += Program.INV_SLOT_WIDTH;
                    currentSlot++;
                }

                SharpDX.Mathematics.Interop.RawRectangleF drrf = new SharpDX.Mathematics.Interop.RawRectangleF(imageX, imageY, imageX + r.Width, imageY + r.Height);
                using (SharpDX.Direct2D1.Bitmap croppedBitmap = new SharpDX.Direct2D1.Bitmap(g.GetRenderTarget(), new SharpDX.Size2(r.Width, r.Height), new SharpDX.Direct2D1.BitmapProperties()
                {
                    PixelFormat = new SharpDX.Direct2D1.PixelFormat()
                    {
                        AlphaMode = SharpDX.Direct2D1.AlphaMode.Premultiplied,
                        Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm
                    }
                }))
                {
                    croppedBitmap.CopyFromBitmap(b, new SharpDX.Mathematics.Interop.RawPoint(0, 0), RectangleToRawRectangle(r));
                    g.GetRenderTarget().DrawBitmap(croppedBitmap, drrf, 1f, SharpDX.Direct2D1.BitmapInterpolationMode.Linear);
                }
                g.DrawText(consolasBold, 18f, textBrush, textX, textY, !inv.Infinite ? inv.Quantity.ToString() : "∞");
            }
        }

        public SharpDX.Mathematics.Interop.RawRectangle RectangleToRawRectangle(System.Drawing.Rectangle r) => new SharpDX.Mathematics.Interop.RawRectangle(r.Left, r.Top, r.Right, r.Bottom);

        public void GenerateImages(Graphics g, int invSlotWidth, int invSlotHeight, double invSlotScaling = 0.5d)
        {
            try
            {
                // Create error inventory image.
                System.Drawing.Bitmap tempInventoryError = null;
                try
                {
                    // Create a blank bitmap to draw on.
                    tempInventoryError = new System.Drawing.Bitmap(invSlotWidth, invSlotHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    using (System.Drawing.Graphics grp = System.Drawing.Graphics.FromImage(tempInventoryError))
                    {
                        // Draw the bitmap.
                        grp.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 0, 0, 0)), 0, 0, tempInventoryError.Width, tempInventoryError.Height);
                        grp.DrawLine(new System.Drawing.Pen(System.Drawing.Color.FromArgb(150, 255, 0, 0), 3), 0, 0, tempInventoryError.Width, tempInventoryError.Height);
                        grp.DrawLine(new System.Drawing.Pen(System.Drawing.Color.FromArgb(150, 255, 0, 0), 3), tempInventoryError.Width, 0, 0, tempInventoryError.Height);
                    }

                    // Convert the bitmap from GDI Format32bppPArgb to DXGI R8G8B8A8_UNorm Premultiplied.
                    inventoryError = GDIBitmapToSharpDXBitmap(tempInventoryError, g.GetRenderTarget());
                }
                finally
                {
                    // Dispose of the GDI bitmaps.
                    tempInventoryError?.Dispose();
                }

                // Scale and convert the inventory images.
                System.Drawing.Bitmap ICONS = null;
                try
                {
                    // Create the bitmap from the byte array.
                    ICONS = Properties.Resources.ICONS;

                    // Scale the bitmap.
                    if (Program.programSpecialOptions.ScalingFactor != 1d)
                    {
                        ICONS = new System.Drawing.Bitmap(ICONS, (int)Math.Round(ICONS.Width * Program.programSpecialOptions.ScalingFactor, MidpointRounding.AwayFromZero), (int)Math.Round(ICONS.Height * Program.programSpecialOptions.ScalingFactor, MidpointRounding.AwayFromZero));
                    }

                    // Transform the bitmap into a pre-multiplied alpha.
                    ICONS = ICONS.Clone(new System.Drawing.Rectangle(0, 0, ICONS.Width, ICONS.Height), System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                    // Convert the bitmap from GDI Format32bppPArgb to DXGI R8G8B8A8_UNorm Premultiplied.
                    inventoryImage = GDIBitmapToSharpDXBitmap(ICONS, g.GetRenderTarget());
                }
                finally
                {
                    // Dispose of the GDI bitmaps.
                    ICONS?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Program.FailFast(Program.GetExceptionMessage(ex), ex);
            }
        }

        private SharpDX.Direct2D1.Bitmap GDIBitmapToSharpDXBitmap(System.Drawing.Bitmap bitmap, SharpDX.Direct2D1.RenderTarget device)
        {
            System.Drawing.Rectangle sourceArea = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
            SharpDX.Direct2D1.BitmapProperties bitmapProperties = new SharpDX.Direct2D1.BitmapProperties(new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.R8G8B8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied));
            SharpDX.Size2 size = new SharpDX.Size2(bitmap.Width, bitmap.Height);

            // Transform pixels from GDI's wild ass BGRA to DXGI-compatible RGBA.
            int stride = bitmap.Width * sizeof(int);
            using (SharpDX.DataStream pixelStream = new SharpDX.DataStream(bitmap.Height * stride, true, true))
            {
                // Lock the source bitmap.
                System.Drawing.Imaging.BitmapData bitmapData = bitmap.LockBits(sourceArea, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                // Convert each pixel.
                for (int y = 0; y < bitmap.Height; ++y)
                {
                    int offset = bitmapData.Stride * y;
                    for (int x = 0; x < bitmap.Width; ++x)
                    {
                        byte B = Marshal.ReadByte(bitmapData.Scan0, offset++);
                        byte G = Marshal.ReadByte(bitmapData.Scan0, offset++);
                        byte R = Marshal.ReadByte(bitmapData.Scan0, offset++);
                        byte A = Marshal.ReadByte(bitmapData.Scan0, offset++);

                        int rgba = R | (G << 8) | (B << 16) | (A << 24);
                        pixelStream.Write(rgba);
                    }
                }

                // Unlock source bitmap now.
                bitmap.UnlockBits(bitmapData);

                // Reset stream position for reading.
                pixelStream.Position = 0;

                // Create the SharpDX bitmap from the DataStream.
                return new SharpDX.Direct2D1.Bitmap(device, size, pixelStream, stride, bitmapProperties);
            }
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual new void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    base.Dispose();
                    inventoryError?.Dispose();
                    inventoryImage?.Dispose();
                    consolasBold?.Dispose();
                    darkRedBrush?.Dispose();
                    redBrush?.Dispose();
                    whiteBrush?.Dispose();
                    greyBrush?.Dispose();
                    blackBrush?.Dispose();
                    greenBrush?.Dispose();
                    yellowBrush?.Dispose();
                    backBrush?.Dispose();
                    foreBrush?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~OverlayDrawer()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        #endregion IDisposable Support
    }
}