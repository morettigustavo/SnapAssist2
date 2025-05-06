using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;
using System.Collections.Generic;
using Timer = System.Windows.Forms.Timer;
using System.Runtime.InteropServices;

namespace snapAssist
{
    public partial class Cliente : Form
    {
        private Timer timer;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern short VkKeyScan(char ch);
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr SetForegroundWindow(IntPtr hWnd);
        public Cliente(string ip)
        {
            InitializeComponent();
            InitializeTimer();
            label1.Text = $"SnapAssist sendo acessado pelo IP: {ip}";
            label1.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void InitializeTimer()
        {
            timer = new Timer();
            timer.Interval = 500; // Intervalo de 500ms
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            CaptureScreen();
            ProcessMouseAndKeyboardLog();
        }

        private void CaptureScreen()
        {
            try
            {
                // Define os limites da tela
                Rectangle bounds = Screen.GetBounds(Point.Empty);
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        // Captura a tela
                        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);

                        // Obtém a posição do mouse
                        Point mousePosition = Cursor.Position;

                        // Desenha o ponteiro do mouse na captura
                        DrawCursor(g, mousePosition);
                    }

                    // Salvar a captura de tela
                    SaveScreenshotToFTP(bitmap);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao capturar a tela: {ex.Message}");
            }
        }

        private void SaveScreenshotToFTP(Bitmap bitmap)
        {
            try
            {
                string ftpDirectory = @"C:\FTP";

                if (!Directory.Exists(ftpDirectory))
                {
                    Directory.CreateDirectory(ftpDirectory);
                }

                string fileName = Path.Combine(ftpDirectory, "screenshot.png");

                // Salva a imagem no diretório FTP
                bitmap.Save(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar a captura de tela: {ex.Message}");
            }
        }

        private void DrawCursor(Graphics g, Point mousePosition)
        {
            // Desenha o cursor do mouse
            Cursor cursor = Cursors.Default; 
            int cursorX = mousePosition.X - cursor.Size.Width / 2; // Centraliza o cursor
            int cursorY = mousePosition.Y - cursor.Size.Height / 2;

            // Desenha o cursor na imagem capturada
            cursor.Draw(g, new Rectangle(cursorX, cursorY, cursor.Size.Width, cursor.Size.Height));
        }

        private void ProcessMouseAndKeyboardLog()
        {
            string filePath = @"C:\FTP\mouse_log.txt";
            if (!File.Exists(filePath))
            {
                return;
            }

            List<string> lines = new List<string>(File.ReadAllLines(filePath));

            while (lines.Count > 0)
            {
                string line = lines[0];
                ProcessEvent(line);
                lines.RemoveAt(0);
                File.WriteAllLines(filePath, lines);
                Thread.Sleep(200);
            }
        }

        // Processa o evento detectado no log
        private void ProcessEvent(string line)
        {
            var clickRegex = new Regex(@"Clique: {X=(\d+), Y=(\d+)}");
            var dragRegex = new Regex(@"Arrastando: {X=(\d+), Y=(\d+)} até {X=(\d+), Y=(\d+)}");
            var doubleClickRegex = new Regex(@"Duplo Clique: {X=(\d+), Y=(\d+)}");
            var keyPressRegex = new Regex(@"Tecla Pressionada: (\w+)");

            if (clickRegex.IsMatch(line))
            {
                var match = clickRegex.Match(line);
                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);
                MouseClick(x, y);
            }
            else if (dragRegex.IsMatch(line))
            {
                var match = dragRegex.Match(line);
                int startX = int.Parse(match.Groups[1].Value);
                int startY = int.Parse(match.Groups[2].Value);
                int endX = int.Parse(match.Groups[3].Value);
                int endY = int.Parse(match.Groups[4].Value);
                MouseDrag(startX, startY, endX, endY);
            }
            else if (doubleClickRegex.IsMatch(line))
            {
                var match = doubleClickRegex.Match(line);
                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);
                MouseDoubleClick(x, y);
            }
            else if (keyPressRegex.IsMatch(line))
            {
                var match = keyPressRegex.Match(line);
                string key = match.Groups[1].Value;

                if (key == "Capital" || key == "ShiftKey")
                {
                    return;  // Não faz nada se for Capital ou ShiftKey
                }
                SimulateKeyPress(key);
            }
        }
        private void SimulateKeyPress(string key)
        {
            byte vkCode;
            bool shiftRequired = false;

            // Trata separadamente teclas como espaço, enter, tab, backspace, etc.
            switch (key)
            {
                case "Space":
                    vkCode = (byte)Keys.Space;
                    break;
                case "Enter":
                    vkCode = (byte)Keys.Enter;
                    break;
                case "Tab":
                    vkCode = (byte)Keys.Tab;
                    break;
                case "Backspace":
                    vkCode = (byte)Keys.Back;
                    break;
                case "D0":
                    vkCode = (byte)Keys.D0;
                    break;
                case "D1":
                    vkCode = (byte)Keys.D1;
                    break;
                case "D2":
                    vkCode = (byte)Keys.D2;
                    break;
                case "D3":
                    vkCode = (byte)Keys.D3;
                    break;
                case "D4":
                    vkCode = (byte)Keys.D4;
                    break;
                case "D5":
                    vkCode = (byte)Keys.D5;
                    break;
                case "D6":
                    vkCode = (byte)Keys.D6;
                    break;
                case "D7":
                    vkCode = (byte)Keys.D7;
                    break;
                case "D8":
                    vkCode = (byte)Keys.D8;
                    break;
                case "D9":
                    vkCode = (byte)Keys.D9;
                    break;
                default:
                    char upperKey = char.ToUpper(key[0]);
                    vkCode = (byte)VkKeyScan(upperKey);

                    // Verifica se a tecla precisa do Shift para ser maiúscula
                    if (char.IsUpper(key[0]))
                    {
                        shiftRequired = true;
                    }
                    break;
            }

            const uint KEYEVENTF_KEYDOWN = 0x0000;
            const uint KEYEVENTF_KEYUP = 0x0002;

            // Simula o pressionamento da tecla Shift se necessário
            if (shiftRequired)
            {
                keybd_event((byte)Keys.ShiftKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            }

            // Simula o pressionamento da tecla
            keybd_event(vkCode, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(50);
            keybd_event(vkCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // Libera a tecla Shift se foi pressionada
            if (shiftRequired)
            {
                keybd_event((byte)Keys.ShiftKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }





        private void MouseClick(int x, int y)
        {
            Cursor.Position = new System.Drawing.Point(x, y);
            MouseEvent(MouseEventFlags.LeftDown);
            Thread.Sleep(100); // Atraso entre os eventos
            MouseEvent(MouseEventFlags.LeftUp);
            Console.WriteLine($"Clique em {x}, {y}");
        }

        // Função para simular arrasto do mouse
        private void MouseDrag(int startX, int startY, int endX, int endY)
        {
            Cursor.Position = new System.Drawing.Point(startX, startY);
            MouseEvent(MouseEventFlags.LeftDown);
            Thread.Sleep(100); // Atraso entre os eventos
            Cursor.Position = new System.Drawing.Point(endX, endY);
            Thread.Sleep(100); // Atraso entre os eventos
            MouseEvent(MouseEventFlags.LeftUp);
            Console.WriteLine($"Arrastando de {startX}, {startY} até {endX}, {endY}");
        }

        // Função para simular duplo clique do mouse
        private void MouseDoubleClick(int x, int y)
        {
            // Simulando duplo clique do mouse
            Cursor.Position = new System.Drawing.Point(x, y);
            MouseEvent(MouseEventFlags.LeftDown);
            Thread.Sleep(100);
            MouseEvent(MouseEventFlags.LeftUp);
            Thread.Sleep(100);
            MouseEvent(MouseEventFlags.LeftDown);
            Thread.Sleep(100);
            MouseEvent(MouseEventFlags.LeftUp);
            Console.WriteLine($"Duplo clique em {x}, {y}");
        }

        private void MouseEvent(MouseEventFlags value)
        {
            mouse_event((int)value, 0, 0, 0, 0);
        }

        // Definição das flags de eventos do mouse
        [Flags]
        public enum MouseEventFlags : int
        {
            Move = 0x0001,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            Wheel = 0x0800,
            XDown = 0x0080,
            XUp = 0x0100
        }
        private void Cliente_Load(object sender, EventArgs e)
        {
            // Centralizar horizontalmente e posicionar no topo
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            int formWidth = this.Width;
            int formHeight = this.Height;

            // Definir a posição do formulário (centrado horizontalmente e no topo da tela)
            this.Location = new Point((screenWidth - formWidth) / 2, 0);


        }

        // Importando a função mouse_event da User32.dll
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
    }
}
