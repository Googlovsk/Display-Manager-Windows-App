using System.Data;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using PInvoke;
using static PInvoke.User32;
using System.Windows.Controls;
using System.Globalization;

namespace DisplayManager
{
    public partial class App : System.Windows.Application
    {
        private static int originalPelsWidth;
        private static int originalPelsHeight;
        private static bool originalSettingsInitialized = false;
        public static int OriginalPelsWidth
        {
            get { return originalPelsWidth; }
        }

        public static int OriginalPelsHeight
        {
            get { return originalPelsHeight; }
        }

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
            // Setting up NotifyIcon
            _notifyIcon.Icon = new System.Drawing.Icon("Resources/display.ico");
            _notifyIcon.Text = "Display Manager";
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenuStrip = new Forms.ContextMenuStrip();

            // Обработчик клика для иконки
            // Click handler for the icon
            _notifyIcon.MouseClick += NotifyIcon_Click;

            // Определение текущего языка в системе
            // Determine the current language in the system
            string currentLanguage = CultureInfo.CurrentCulture.Name;

            // Адаптация интерфейса в зависимости от текущего языка
            // Adaptation of the interface depending on the current language
            if (currentLanguage.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            {
                DisplayMenuItems(_notifyIcon.ContextMenuStrip, "ru"); // Установка языкового кода "ru" для русской локализации
                                                                      // Setting the language code "ru" for Russian localization
            }
            else
            {
                DisplayMenuItems(_notifyIcon.ContextMenuStrip, "en"); // Английская локализация по умолчанию
                                                                      // English localization by default
            }
            base.OnStartup(e);
        }
        private void DisplayMenuItems(Forms.ContextMenuStrip menuStrip, string lang)
        {
            var displays = System.Windows.Forms.Screen.AllScreens;
            var displayMenu = new Forms.ToolStripMenuItem("Мониторы");

            foreach (var display in displays)
            {
                var displayItem = new Forms.ToolStripMenuItem(display.DeviceName);
                displayItem.Tag = display;
                displayItem.DropDownItems.Add(lang == "ru" ? "Обычная ориентация" : "Normal Orientation", null, (s, e) => RotateDisplay(display.DeviceName, NativeMethods.DMDO_DEFAULT));
                displayItem.DropDownItems.Add(lang == "ru" ? "Повернуть на 90 градусов" : "Rotate 90", null, (s, e) => RotateDisplay(display.DeviceName, NativeMethods.DMDO_90));
                displayItem.DropDownItems.Add(lang == "ru" ? "Повернуть на 180 градусов" : "Rotate 180", null, (s, e) => RotateDisplay(display.DeviceName, NativeMethods.DMDO_180));
                displayItem.DropDownItems.Add(lang == "ru" ? "Повернуть на 270 градусов" : "Rotate 270", null, (s, e) => RotateDisplay(display.DeviceName, NativeMethods.DMDO_270));
                displayMenu.DropDownItems.Add(displayItem);

            }
            menuStrip.Items.Add(displayMenu);
            menuStrip.Items.Add(new Forms.ToolStripSeparator());
            menuStrip.Items.Add(lang == "ru" ? "Выход" : "Exit", null, (s, e) => Shutdown());
        }
        public static bool RotateDisplay(string deviceName, int orientation)
        {
            DEVMODE devMode = GetDisplaySettings(deviceName);
            if (devMode.dmSize == 0)
            {
                return false; // Ошибка при получении текущих настроек
                              // Error getting current settings
            }

            // Сброс настроек дисплея
            // Reset display settings
            if (!ResetDisplaySettings(deviceName, ref devMode))
            {
                return false; // Ошибка при сбросе настроек
                              // Error when resetting settings
            }

            // Установка новой ориентации
            // Setting a new orientation
            if (!SetDisplayOrientation(ref devMode, orientation))
            {
                // Возврат к исходным настройкам, если не удалось сохранить новые
                // Return to original settings if new ones could not be saved
                RestoreOriginalDisplaySettings(deviceName, ref devMode);
                return false;
            }

            // Применение новой конфигурации дисплея
            // Applying a new display configuration
            if (!ApplyDisplaySettings(deviceName, ref devMode))
            {
                // Возврат к исходным настройкам, если не удалось сохранить новые
                // Return to original settings if new ones could not be saved
                RestoreOriginalDisplaySettings(deviceName, ref devMode);
                return false;
            }

            return true;
        }
        private static DEVMODE GetDisplaySettings(string deviceName)
        {
            DEVMODE devMode = new DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            if (NativeMethods.EnumDisplaySettings(deviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref devMode) == 0)
            {
                MessageBox.Show($"Не удалось получить текущие настройки дисплея для устройства {deviceName}");
                return new DEVMODE(); // Возврат пустой структуры в случае ошибки
                                      // Return an empty structure on error
            }

            // Сохранение текущих параметров разрешения, если они еще не были сохранены
            // Save current resolution settings if they have not already been saved
            if (!originalSettingsInitialized)
            {
                originalPelsWidth = devMode.dmPelsWidth;
                originalPelsHeight = devMode.dmPelsHeight;
                originalSettingsInitialized = true;
            }

            return devMode;
        }

        private static bool ResetDisplaySettings(string deviceName, ref DEVMODE devMode)
        {
            DISP_CHANGE resetResult = NativeMethods.ChangeDisplaySettingsEx(deviceName, ref devMode, IntPtr.Zero, DisplaySettingsFlags.CDS_RESET, IntPtr.Zero);
            if (resetResult != DISP_CHANGE.Successful && resetResult != DISP_CHANGE.Restart)
            {
                MessageBox.Show($"Не удалось сбросить настройки дисплея: {resetResult}");
                return false;
            }

            return true;
        }
        
        private static bool SetDisplayOrientation(ref DEVMODE devMode, int orientation)
        {
            devMode.dmFields = DM.Position | DM.PelsWidth | DM.PelsHeight | DM.DisplayOrientation | DM.BitsPerPixel | DM.DisplayFrequency;
            devMode.dmDisplayOrientation = orientation;


            if(orientation == NativeMethods.DMDO_DEFAULT || orientation == NativeMethods.DMDO_180)
            {
                devMode.dmPelsWidth = OriginalPelsWidth;
                devMode.dmPelsHeight = OriginalPelsHeight;
            }
            else if(orientation == NativeMethods.DMDO_90 || orientation == NativeMethods.DMDO_270)
            {
                devMode.dmPelsWidth = OriginalPelsHeight;
                devMode.dmPelsHeight = OriginalPelsWidth;
            }
            else
            {
                devMode.dmPelsWidth = OriginalPelsWidth;
                devMode.dmPelsHeight = OriginalPelsHeight;
            }

            return true; // Всегда возвращает true, т.к. ошибки при установке ориентации будут обработаны далее
                         // Always returns true, because errors when setting orientation will be processed further
        }
        private static bool ApplyDisplaySettings(string deviceName, ref DEVMODE devMode)
        {
            DISP_CHANGE result = NativeMethods.ChangeDisplaySettingsEx(deviceName, ref devMode, IntPtr.Zero, DisplaySettingsFlags.CDS_UPDATEREGISTRY, IntPtr.Zero);
            if (result != DISP_CHANGE.Successful && result != DISP_CHANGE.Restart)
            {
                MessageBox.Show($"Не удалось установить новую ориентацию: {result}");
                return false;
            }
            return true;
        }

        private static void RestoreOriginalDisplaySettings(string deviceName, ref DEVMODE devMode)
        {
            devMode.dmFields = DM.PelsWidth | DM.PelsHeight;
            devMode.dmPelsWidth = OriginalPelsWidth;
            devMode.dmPelsHeight = OriginalPelsHeight;

            NativeMethods.ChangeDisplaySettingsEx(deviceName, ref devMode, IntPtr.Zero, DisplaySettingsFlags.CDS_UPDATEREGISTRY, IntPtr.Zero);
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
                    _notifyIcon.ContextMenuStrip.Show(Forms.Control.MousePosition);
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

    /// <summary>
    /// Весь код ниже взят из интернета
    /// All the code below is taken from the Internet
    /// </summary>
    internal class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern DISP_CHANGE ChangeDisplaySettingsEx(
            string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd,
            DisplaySettingsFlags dwflags, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool EnumDisplayDevices(
            string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice,
            uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        internal static extern int EnumDisplaySettings(
            string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        public const int DMDO_DEFAULT = 0;
        public const int DMDO_90 = 1;
        public const int DMDO_180 = 2;
        public const int DMDO_270 = 3;

        public const int ENUM_CURRENT_SETTINGS = -1;

    }

    // See: https://msdn.microsoft.com/en-us/library/windows/desktop/dd183565(v=vs.85).aspx
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
    internal struct DEVMODE
    {
        public const int CCHDEVICENAME = 32;
        public const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        [System.Runtime.InteropServices.FieldOffset(0)]
        public string dmDeviceName;
        [System.Runtime.InteropServices.FieldOffset(32)]
        public Int16 dmSpecVersion;
        [System.Runtime.InteropServices.FieldOffset(34)]
        public Int16 dmDriverVersion;
        [System.Runtime.InteropServices.FieldOffset(36)]
        public Int16 dmSize;
        [System.Runtime.InteropServices.FieldOffset(38)]
        public Int16 dmDriverExtra;
        [System.Runtime.InteropServices.FieldOffset(40)]
        public DM dmFields;

        [System.Runtime.InteropServices.FieldOffset(44)]
        Int16 dmOrientation;
        [System.Runtime.InteropServices.FieldOffset(46)]
        Int16 dmPaperSize;
        [System.Runtime.InteropServices.FieldOffset(48)]
        Int16 dmPaperLength;
        [System.Runtime.InteropServices.FieldOffset(50)]
        Int16 dmPaperWidth;
        [System.Runtime.InteropServices.FieldOffset(52)]
        Int16 dmScale;
        [System.Runtime.InteropServices.FieldOffset(54)]
        Int16 dmCopies;
        [System.Runtime.InteropServices.FieldOffset(56)]
        Int16 dmDefaultSource;
        [System.Runtime.InteropServices.FieldOffset(58)]
        Int16 dmPrintQuality;

        [System.Runtime.InteropServices.FieldOffset(44)]
        public POINTL dmPosition;
        [System.Runtime.InteropServices.FieldOffset(52)]
        public Int32 dmDisplayOrientation;
        [System.Runtime.InteropServices.FieldOffset(56)]
        public Int32 dmDisplayFixedOutput;

        [System.Runtime.InteropServices.FieldOffset(60)]
        public short dmColor;
        [System.Runtime.InteropServices.FieldOffset(62)]
        public short dmDuplex;
        [System.Runtime.InteropServices.FieldOffset(64)]
        public short dmYResolution;
        [System.Runtime.InteropServices.FieldOffset(66)]
        public short dmTTOption;
        [System.Runtime.InteropServices.FieldOffset(68)]
        public short dmCollate;
        [System.Runtime.InteropServices.FieldOffset(72)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        [System.Runtime.InteropServices.FieldOffset(102)]
        public Int16 dmLogPixels;
        [System.Runtime.InteropServices.FieldOffset(104)]
        public Int32 dmBitsPerPel;
        [System.Runtime.InteropServices.FieldOffset(108)]
        public Int32 dmPelsWidth;
        [System.Runtime.InteropServices.FieldOffset(112)]
        public Int32 dmPelsHeight;
        [System.Runtime.InteropServices.FieldOffset(116)]
        public Int32 dmDisplayFlags;
        [System.Runtime.InteropServices.FieldOffset(116)]
        public Int32 dmNup;
        [System.Runtime.InteropServices.FieldOffset(120)]
        public Int32 dmDisplayFrequency;
    }

    // See: https://msdn.microsoft.com/en-us/library/windows/desktop/dd183569(v=vs.85).aspx
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct DISPLAY_DEVICE
    {
        [MarshalAs(UnmanagedType.U4)]
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        [MarshalAs(UnmanagedType.U4)]
        public DisplayDeviceStateFlags StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    // See: https://msdn.microsoft.com/de-de/library/windows/desktop/dd162807(v=vs.85).aspx
    [StructLayout(LayoutKind.Sequential)]
    internal struct POINTL
    {
        long x;
        long y;
    }

    internal enum DISP_CHANGE : int
    {
        Successful = 0,
        Restart = 1,
        Failed = -1,
        BadMode = -2,
        NotUpdated = -3,
        BadFlags = -4,
        BadParam = -5,
        BadDualView = -6
    }

    /// http://www.pinvoke.net/default.aspx/Enums/DisplayDeviceStateFlags.html
    [Flags()]
    internal enum DisplayDeviceStateFlags : int
    {
        /// <summary>The device is part of the desktop.</summary>
        AttachedToDesktop = 0x1,
        MultiDriver = 0x2,
        /// <summary>The device is part of the desktop.</summary>
        PrimaryDevice = 0x4,
        /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
        MirroringDriver = 0x8,
        /// <summary>The device is VGA compatible.</summary>
        VGACompatible = 0x10,
        /// <summary>The device is removable; it cannot be the primary display.</summary>
        Removable = 0x20,
        /// <summary>The device has more display modes than its output devices support.</summary>
        ModesPruned = 0x8000000,
        Remote = 0x4000000,
        Disconnect = 0x2000000
    }

    // http://www.pinvoke.net/default.aspx/user32/ChangeDisplaySettingsFlags.html
    [Flags()]
    internal enum DisplaySettingsFlags : int
    {
        CDS_NONE = 0,
        CDS_UPDATEREGISTRY = 0x00000001,
        CDS_TEST = 0x00000002,
        CDS_FULLSCREEN = 0x00000004,
        CDS_GLOBAL = 0x00000008,
        CDS_SET_PRIMARY = 0x00000010,
        CDS_VIDEOPARAMETERS = 0x00000020,
        CDS_ENABLE_UNSAFE_MODES = 0x00000100,
        CDS_DISABLE_UNSAFE_MODES = 0x00000200,
        CDS_RESET = 0x40000000,
        CDS_RESET_EX = 0x20000000,
        CDS_NORESET = 0x10000000
    }

    [Flags()]
    internal enum DM : int
    {
        Orientation = 0x00000001,
        PaperSize = 0x00000002,
        PaperLength = 0x00000004,
        PaperWidth = 0x00000008,
        Scale = 0x00000010,
        Position = 0x00000020,
        NUP = 0x00000040,
        DisplayOrientation = 0x00000080,
        Copies = 0x00000100,
        DefaultSource = 0x00000200,
        PrintQuality = 0x00000400,
        Color = 0x00000800,
        Duplex = 0x00001000,
        YResolution = 0x00002000,
        TTOption = 0x00004000,
        Collate = 0x00008000,
        FormName = 0x00010000,
        LogPixels = 0x00020000,
        BitsPerPixel = 0x00040000,
        PelsWidth = 0x00080000,
        PelsHeight = 0x00100000,
        DisplayFlags = 0x00200000,
        DisplayFrequency = 0x00400000,
        ICMMethod = 0x00800000,
        ICMIntent = 0x01000000,
        MediaType = 0x02000000,
        DitherType = 0x04000000,
        PanningWidth = 0x08000000,
        PanningHeight = 0x10000000,
        DisplayFixedOutput = 0x20000000
    }

}
