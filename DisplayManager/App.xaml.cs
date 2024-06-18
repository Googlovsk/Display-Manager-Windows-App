using System.Linq;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using Forms = System.Windows.Forms;

namespace DisplayManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            MainWindow = new MainWindow();
            MainWindow.Show();

            Forms.NotifyIcon notifyIcon = new Forms.NotifyIcon();
            notifyIcon.Icon = new System.Drawing.Icon("/Resources/display.png");
            notifyIcon.Visible = true;

            base.OnStartup(e);
        }
    }

}
