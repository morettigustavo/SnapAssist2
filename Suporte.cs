using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using FluentFTP;

namespace snapAssist
{
    public partial class Suporte : Form
    {
        private bool isDragging = false;
        private Point lastCursor;
        private Point initialDragPoint;
        private bool mouseMoved = false;

        private Timer timer;
        private readonly string ftpIp;
        private readonly string ftpPassword;

        Image ImageShow = null;
        private bool isImageLoading = false;
        FtpClient ftpClient = null;

        public Suporte(string ip, string password)
        {
            InitializeComponent();
            this.ftpIp = ip;
            this.ftpPassword = password;

            this.Resize += Form1_Resize;

            this.pictureBox1.MouseClick += PictureBox1_MouseClick;
            this.pictureBox1.MouseDoubleClick += PictureBox1_MouseDoubleClick;
            this.pictureBox1.MouseDown += PictureBox1_MouseDown;
            this.pictureBox1.MouseMove += PictureBox1_MouseMove;
            this.pictureBox1.MouseUp += PictureBox1_MouseUp;

            this.KeyPreview = true;
            this.KeyDown += Suporte_KeyDown;
            this.KeyPress += Suporte_KeyPress;

            timer = new Timer();
            timer.Interval = 1;
            timer.Tick += new EventHandler(LoadImage);
            timer.Start();
        }

        // ================================================================
        // FLUENTFTP - CLIENTE PARA QUALQUER VERSÃO
        // ================================================================
        private FtpClient CreateFtpClient()
        {
            var client = new FtpClient(ftpIp)
            {
                Credentials = new NetworkCredential("SNAPASSIST", ftpPassword)
            };

            return client;
        }

        // ================================================================
        // CARREGAR A IMAGEM DO FTP
        // ================================================================
        private void LoadImage(object sender, EventArgs e)
        {
            if (isImageLoading)
                return;

            isImageLoading = true;

            try
            {
                using (var client = CreateFtpClient())
                {
                    client.Connect();

                    using (var stream = client.OpenRead("/screenshot.png"))
                    {
                        if (stream != null)
                        {
                            using (var ms = new MemoryStream())
                            {
                                stream.CopyTo(ms);
                                ms.Position = 0;

                                pictureBox1.Image?.Dispose();
                                pictureBox1.Image = null;

                                ImageShow = Image.FromStream(ms);

                                pictureBox1.Image = ImageShow;
                                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                                pictureBox1.Dock = DockStyle.Fill;
                            }
                        }
                    }

                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro FluentFTP: {ex.Message}");
            }
            finally
            {
                isImageLoading = false;
            }
        }

        // ================================================================
        // LOG NO FTP
        // ================================================================
        private void UpdateLog(string logMessage)
        {
            try
            {
                using (var client = CreateFtpClient())
                {
                    client.Connect();

                    // Abre o arquivo remoto para append (cria se não existir)
                    using (var stream = client.OpenAppend("/mouse_log.txt"))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        // Aqui já colocamos o timestamp + mensagem
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {logMessage}");
                        writer.Flush(); // garante que os dados vão para o stream
                    }

                    client.Disconnect();
                }
            }
            catch (Exception)
            {
                // Mantém silencioso como no seu código original
            }
        }


        // ================================================================
        // EVENTOS DO MOUSE E TECLADO (mantidos iguais)
        // ================================================================
        private void Form1_Resize(object sender, EventArgs e)
        {
            pictureBox1.Size = this.ClientSize;
        }

        private void PictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (!mouseMoved && ImageShow != null)
            {
                int adjustedX = (int)(e.X * (float)ImageShow.Width / pictureBox1.Width);
                int adjustedY = (int)(e.Y * (float)ImageShow.Height / pictureBox1.Height);

                if (e.Button == MouseButtons.Left)
                    LogMouseAction($"Clique: {{X={adjustedX}, Y={adjustedY}}}");
                else if (e.Button == MouseButtons.Right)
                    LogMouseAction($"Clique Direito: {{X={adjustedX}, Y={adjustedY}}}");
            }
        }

        private void PictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (ImageShow == null) return;

            int adjustedX = (int)(e.X * (float)ImageShow.Width / pictureBox1.Width);
            int adjustedY = (int)(e.Y * (float)ImageShow.Height / pictureBox1.Height);

            LogMouseAction($"Clique Duplo: {{X={adjustedX}, Y={adjustedY}}}");
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                mouseMoved = false;
                initialDragPoint = e.Location;
            }
        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && e.Location != initialDragPoint)
            {
                mouseMoved = true;
                lastCursor = e.Location;
            }
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragging && mouseMoved && ImageShow != null)
            {
                isDragging = false;

                int adjustedStartX = (int)(initialDragPoint.X * (float)ImageShow.Width / pictureBox1.Width);
                int adjustedStartY = (int)(initialDragPoint.Y * (float)ImageShow.Height / pictureBox1.Height);
                int adjustedEndX = (int)(lastCursor.X * (float)ImageShow.Width / pictureBox1.Width);
                int adjustedEndY = (int)(lastCursor.Y * (float)ImageShow.Height / pictureBox1.Height);

                LogMouseAction($"Arrasto: {{X={adjustedStartX}, Y={adjustedStartY}}} -> {{X={adjustedEndX}, Y={adjustedEndY}}}");
            }

            isDragging = false;
        }

        private void LogMouseAction(string action)
        {
            UpdateLog(action);
        }

        private void Suporte_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
                return;

            if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
                return;

            if (e.KeyCode == Keys.Space)
                return;

            UpdateLog($"Tecla: {e.KeyCode}");
        }

        private void Suporte_KeyPress(object sender, KeyPressEventArgs e)
        {
            char c = e.KeyChar;

            if (char.IsControl(c))
                return;

            UpdateLog($"Tecla: {c}");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }

            base.OnFormClosed(e);
        }
    }
}
