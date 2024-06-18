using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace DisplayManager
{
    public partial class App : System.Windows.Application
    {
        
        private readonly Forms.NotifyIcon _notifyIcon;
        private bool _contextMenuOpen;
        public App()
        {
            _notifyIcon = new Forms.NotifyIcon();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            MainWindow = new MainWindow();
            MainWindow.Visibility = Visibility.Hidden;
            MainWindow.ShowInTaskbar = false;

            // Настройка NotifyIcon
            _notifyIcon.Icon = new System.Drawing.Icon("Resources/display.ico");
            _notifyIcon.Text = "Display Manager";
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenuStrip = new Forms.ContextMenuStrip();

            // Обработчик для клика по иконке
            _notifyIcon.MouseClick += NotifyIcon_Click;

            DisplayMenuItems(_notifyIcon.ContextMenuStrip);
            base.OnStartup(e);
        }
        private void DisplayMenuItems(Forms.ContextMenuStrip menuStrip)
        {
            var displays = System.Windows.Forms.Screen.AllScreens;
            var displayMenu = new Forms.ToolStripMenuItem("Мониторы");

            foreach (var display in displays)
            {
                var displayItem = new Forms.ToolStripMenuItem(display.DeviceName);
                displayItem.Tag = display;
                displayItem.DropDownItems.Add("Обычная ориентация", null, (s, e) => RotateDisplay(display.DeviceName, 0));
                displayItem.DropDownItems.Add("Повернуть на 90 градусов", null, (s, e) => RotateDisplay(display.DeviceName, 90));
                displayItem.DropDownItems.Add("Повернуть на 180 градусов", null, (s, e) => RotateDisplay(display.DeviceName, 180));
                displayItem.DropDownItems.Add("Повернуть на 270 градусов", null, (s, e) => RotateDisplay(display.DeviceName, 270));
                displayMenu.DropDownItems.Add(displayItem);
                
            }
            menuStrip.Items.Add(displayMenu);
            menuStrip.Items.Add(new Forms.ToolStripSeparator());
            menuStrip.Items.Add("Выход", null, (s, e) => Shutdown());
        }
        private void RotateDisplay(string deviceName, int angle)
        {
            // Тут нужно добавить реализацию поворота дисплея
            // Например, используя DisplaySwitch или WinAPI для изменения ориентации дисплея
            // Пример использования DisplaySwitch:
            // DisplaySwitch.DisplayRotate(deviceName, angle);
        }
        private void NotifyIcon_Click(object? sender, Forms.MouseEventArgs e)
        {
            if (_contextMenuOpen)
            {
                _notifyIcon.ContextMenuStrip.Close();
                _contextMenuOpen = false;
            }
            else
            {
                if (e.Button == Forms.MouseButtons.Left || e.Button == Forms.MouseButtons.Right)
                {
                    _notifyIcon.ContextMenuStrip.Show(Control.MousePosition);
                    _contextMenuOpen = true;
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon.Dispose();
            base.OnExit(e);
        }
    }

}
