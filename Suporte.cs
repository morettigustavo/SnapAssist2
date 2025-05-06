using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace snapAssist
{
    public partial class Suporte : Form
    {
        private bool isDragging = false;
        private Point lastCursor;
        private Point initialDragPoint;
        private bool mouseMoved = false;

        private Timer timer;
        private string ftpIp;
        private string ftpPassword;
        public Suporte(string ip, string password)
        {
            InitializeComponent();
            this.ftpIp = ip;
            this.ftpPassword = password;

            this.Resize += Form1_Resize;

            // Adiciona os manipuladores de eventos do mouse ao PictureBox
            this.pictureBox1.MouseClick += PictureBox1_MouseClick;
            this.pictureBox1.MouseDoubleClick += PictureBox1_MouseDoubleClick;
            this.pictureBox1.MouseDown += PictureBox1_MouseDown;
            this.pictureBox1.MouseMove += PictureBox1_MouseMove;
            this.pictureBox1.MouseUp += PictureBox1_MouseUp;

            this.KeyPreview = true; // Permite que o formulário capture eventos de teclado antes dos controles
            this.KeyDown += Suporte_KeyDown;

            timer = new Timer();
            timer.Interval = 500; // 5000 milliseconds = 5 seconds
            timer.Tick += new EventHandler(LoadImage);
            timer.Start();
        }
        Image ImageShow = null;
        private bool isImageLoading = false;  // Flag para controlar se a imagem está sendo carregada

        private void LoadImage(object sender, EventArgs e)
        {
            if (isImageLoading)
            {
                return;
            }

            try
            {
                isImageLoading = true;

                string ftpImagePath = $"ftp://{ftpIp}/screenshot.png";
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpImagePath);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential("ftpUser", ftpPassword);

                // Define o modo passivo como false para usar o modo ativo
                request.UsePassive = false;

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                {
                    // Copiar o stream para um MemoryStream para abrir a imagem em modo de leitura
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        memoryStream.Position = 0; // Reiniciar o ponteiro para o início

                        pictureBox1.Image?.Dispose();
                        pictureBox1.Image = null;

                        ImageShow = Image.FromStream(memoryStream);
                        pictureBox1.Image = ImageShow;
                        pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                        pictureBox1.Dock = DockStyle.Fill;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar a imagem: {ex.Message}");
            }
            finally
            {
                isImageLoading = false;
            }

        }


        private void Form1_Resize(object sender, EventArgs e)
        {
            pictureBox1.Size = this.ClientSize;
        }

        private void PictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (!mouseMoved)
            {
                // Ajusta as coordenadas considerando o tamanho da imagem e o tamanho da PictureBox
                int adjustedX = (int)(e.X * (float)ImageShow.Width / pictureBox1.Width);
                int adjustedY = (int)(e.Y * (float)ImageShow.Height / pictureBox1.Height);

                if (e.Button == MouseButtons.Left)
                {
                    LogMouseAction($"Clique: {{X={adjustedX}, Y={adjustedY}}}");
                }
                else if (e.Button == MouseButtons.Right)
                {
                    LogMouseAction($"Clique com Botão Direito: {{X={adjustedX}, Y={adjustedY}}}");
                }
            }
        }

        private void PictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Ajusta as coordenadas para o clique duplo
            int adjustedX = (int)(e.X * (float)ImageShow.Width / pictureBox1.Width);
            int adjustedY = (int)(e.Y * (float)ImageShow.Height / pictureBox1.Height);
            LogMouseAction($"Duplo Clique: {{X={adjustedX}, Y={adjustedY}}}");
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
            if (isDragging)
            {
                if (e.Location != initialDragPoint) // Verifica se o mouse realmente se moveu
                {
                    mouseMoved = true;
                    lastCursor = e.Location; // Atualiza a posição do cursor durante o arrasto
                }
            }
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragging && mouseMoved)
            {
                isDragging = false;

                // Ajusta as coordenadas do início e do final do arrasto
                int adjustedStartX = (int)(initialDragPoint.X * (float)ImageShow.Width / pictureBox1.Width);
                int adjustedStartY = (int)(initialDragPoint.Y * (float)ImageShow.Height / pictureBox1.Height);
                int adjustedEndX = (int)(lastCursor.X * (float)ImageShow.Width / pictureBox1.Width);
                int adjustedEndY = (int)(lastCursor.Y * (float)ImageShow.Height / pictureBox1.Height);

                string action = $"Arrastando: {{X={adjustedStartX}, Y={adjustedStartY}}} até {{X={adjustedEndX}, Y={adjustedEndY}}}";
                LogMouseAction(action);
            }
            else
            {
                isDragging = false;
            }
        }



        private void LogMouseAction(string action)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {action}";
            UpdateLog(logMessage);
        }

        private void Suporte_KeyDown(object sender, KeyEventArgs e)
        {
            // Verifica se a tecla pressionada é uma letra
            if (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
            {
                // Verifica se o Caps Lock está ativo e se a tecla Shift não está pressionada
                bool isCapsLockActive = Control.IsKeyLocked(Keys.CapsLock);

                // Se o Caps Lock estiver ativado, ou se a tecla Shift estiver pressionada, a letra será maiúscula
                if (isCapsLockActive ^ e.Shift) // XOR lógico: CapsLock e Shift não podem estar ambos ativados ou desativados simultaneamente
                {
                    // Maiúscula
                    string keyAction = $"Tecla Pressionada: {e.KeyCode.ToString()}";
                    UpdateLog(keyAction);
                }
                else
                {
                    // Minúscula
                    string keyAction = $"Tecla Pressionada: {e.KeyCode.ToString().ToLower()}";
                    UpdateLog(keyAction);
                }
            }
            else
            {
                // Para outras teclas, apenas registra a tecla normalmente
                string keyAction = $"Tecla Pressionada: {e.KeyCode}";
                UpdateLog(keyAction);
            }
        }


        private void UpdateLog(string logMessage)
        {
            try
            {
                string ftpLogPath = $"ftp://{ftpIp}/mouse_log.txt";

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpLogPath);
                request.Method = WebRequestMethods.Ftp.AppendFile; // Usar "AppendFile" para adicionar dados ao arquivo existente
                request.UsePassive = false;
                request.Credentials = new NetworkCredential("ftpUser", ftpPassword);

                using (Stream requestStream = request.GetRequestStream())
                {
                    using (StreamWriter writer = new StreamWriter(requestStream))
                    {
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {logMessage}");
                    }
                }

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {

                }
            }
            catch (Exception ex)
            {
            }
        }


        private void Suporte_Load(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
