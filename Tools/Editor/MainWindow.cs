using System.Windows.Forms;

namespace Editor
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();

            Text = Jazz2.Game.App.AssemblyTitle + " v" + Jazz2.Game.App.AssemblyVersion;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            bool isUserCloseReason;
            switch (e.CloseReason) {
                default:
                case CloseReason.ApplicationExitCall:
                case CloseReason.WindowsShutDown:
                case CloseReason.TaskManagerClosing:
                    isUserCloseReason = false;
                    break;
                case CloseReason.FormOwnerClosing:
                case CloseReason.MdiFormClosing:
                case CloseReason.UserClosing:
                    isUserCloseReason = true;
                    break;
            }

            bool isClosedByUser =
                isUserCloseReason /*&&
                !this.nonUserClosing &&
                !App.IsReloadingPlugins*/;

            e.Cancel = !App.Terminate(isClosedByUser);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            Application.Exit();
        }
    }
}