using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;
using System.Collections.Generic;
using Timer = System.Windows.Forms.Timer;
using System.Runtime.InteropServices;
using FluentFTP;

namespace snapAssist
{
    public partial class Cliente : Form
    {
        private Timer timer;

        private readonly string ftpIp;
        private readonly string ftpPassword;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern short VkKeyScan(char ch);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        // Ajustei o construtor para receber IP e senha do FTP
        public Cliente(string ip, string password)
        {
            InitializeComponent();

            this.ftpIp = ip;
            this.ftpPassword = password;

            InitializeTimer();
            label1.Text = $"SnapAssist sendo acessado pelo IP: {ip}";
            label1.TextAlign = ContentAlignment.MiddleCenter;
        }

        // ================================================================
        // FLUENTFTP - CLIENTE
        // ================================================================
        private FtpClient CreateFtpClient()
        {
            var client = new FtpClient(ftpIp)
            {
                Credentials = new NetworkCredential("SNAPASSIST", ftpPassword)
            };

            return client;
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

        // ================================================================
        // CAPTURA DE TELA E ENVIO PARA FTP
        // ================================================================
        private void CaptureScreen()
        {
            try
            {
                Rectangle bounds = Screen.GetBounds(Point.Empty);

                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);

                        Point mousePosition = Cursor.Position;
                        DrawCursor(g, mousePosition);
                    }

                    SaveScreenshotToFTP(bitmap);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Erro ao capturar a tela: {ex.Message}");
            }
        }

        private void SaveScreenshotToFTP(Bitmap bitmap)
        {
            try
            {
                using (var client = CreateFtpClient())
                {
                    client.Connect();

                    using (var ms = new MemoryStream())
                    {
                        bitmap.Save(ms, ImageFormat.Png);
                        ms.Position = 0;

                        // Envia para /screenshot.png (sobrescrevendo sempre)
                        client.UploadStream(ms, "/screenshot.png", FtpRemoteExists.Overwrite, true);
                    }

                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao enviar a captura de tela para o FTP: {ex.Message}");
            }
        }

        private void DrawCursor(Graphics g, Point mousePosition)
        {
            Cursor cursor = Cursors.Default;
            int cursorX = mousePosition.X - cursor.Size.Width / 2;
            int cursorY = mousePosition.Y - cursor.Size.Height / 2;

            cursor.Draw(g, new Rectangle(cursorX, cursorY, cursor.Size.Width, cursor.Size.Height));
        }

        // ================================================================
        // LEITURA DO LOG NO FTP E PROCESSAMENTO
        // ================================================================
        private void ProcessMouseAndKeyboardLog()
        {
            try
            {
                using (var client = CreateFtpClient())
                {
                    client.Connect();

                    // Se não existir log ainda, não faz nada
                    if (!client.FileExists("/mouse_log.txt"))
                    {
                        client.Disconnect();
                        return;
                    }

                    var lines = new List<string>();

                    // Baixa o conteúdo do mouse_log.txt para memória
                    using (var stream = client.OpenRead("/mouse_log.txt"))
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            if (!string.IsNullOrWhiteSpace(line))
                                lines.Add(line);
                        }
                    }

                    // Depois de ler tudo, apagamos o arquivo remoto
                    // para evitar reprocessar os mesmos eventos
                    client.DeleteFile("/mouse_log.txt");

                    client.Disconnect();

                    // Processa os eventos localmente
                    foreach (var line in lines)
                    {
                        ProcessEvent(line);
                        Thread.Sleep(200);
                    }
                }
            }
            catch (Exception ex)
            {
                // Se der erro de conexão / leitura, só ignora nesta rodada
                Console.WriteLine($"Erro ao processar log do FTP: {ex.Message}");
            }
        }

        // ================================================================
        // INTERPRETAÇÃO DO EVENTO
        // ================================================================
        private void ProcessEvent(string line)
        {
            // Regex ignora o timestamp no começo, pois só procura o padrão específico
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
                    return;

                SimulateKeyPress(key);
            }
        }

        // ================================================================
        // SIMULAÇÃO DE TECLADO
        // ================================================================
        private void SimulateKeyPress(string key)
        {
            byte vkCode;
            bool shiftRequired = false;

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

                    if (char.IsUpper(key[0]))
                    {
                        shiftRequired = true;
                    }
                    break;
            }

            const uint KEYEVENTF_KEYDOWN = 0x0000;
            const uint KEYEVENTF_KEYUP = 0x0002;

            if (shiftRequired)
            {
                keybd_event((byte)Keys.ShiftKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            }

            keybd_event(vkCode, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(50);
            keybd_event(vkCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            if (shiftRequired)
            {
                keybd_event((byte)Keys.ShiftKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        // ================================================================
        // SIMULAÇÃO DE MOUSE
        // ================================================================
        private void MouseClick(int x, int y)
        {
            Cursor.Position = new Point(x, y);
            MouseEvent(MouseEventFlags.LeftDown);
            Thread.Sleep(100);
            MouseEvent(MouseEventFlags.LeftUp);
            Console.WriteLine($"Clique em {x}, {y}");
        }

        private void MouseDrag(int startX, int startY, int endX, int endY)
        {
            Cursor.Position = new Point(startX, startY);
            MouseEvent(MouseEventFlags.LeftDown);
            Thread.Sleep(100);
            Cursor.Position = new Point(endX, endY);
            Thread.Sleep(100);
            MouseEvent(MouseEventFlags.LeftUp);
            Console.WriteLine($"Arrastando de {startX}, {startY} até {endX}, {endY}");
        }

        private void MouseDoubleClick(int x, int y)
        {
            Cursor.Position = new Point(x, y);
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
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int formWidth = this.Width;

            this.Location = new Point((screenWidth - formWidth) / 2, 0);
        }

        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
    }
}
