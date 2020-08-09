using System;
using System.Windows.Forms;

namespace RECVXSRT
{
    public partial class AttachUI : Form
    {
        private System.Timers.Timer processPollingTimer;

        public AttachUI()
        {
            InitializeComponent();

            this.ContextMenu = Program.contextMenu;

            processPollingTimer = new System.Timers.Timer() { AutoReset = false, Interval = 250 };
            processPollingTimer.Elapsed += ProcessPollingTimer_Elapsed;
            processPollingTimer.Start();
        }

        private void ProcessPollingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Program.GetProcessInfo();
            }
            finally
            {
                if (Program.mainProcess == null)
                    ((System.Timers.Timer)sender).Start();
                else
                    CloseForm();
            }
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