using FluentFTP;
using System;
using System.Diagnostics;
using System.DirectoryServices;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using System.Drawing; // para Point
using System.Threading; // se for usar algo depois
using System.Net.Sockets;

namespace snapAssist
{
    public partial class Menu : Form
    {
        public string Password = "";

        Form FormOpen = null;
        private System.Timers.Timer ftpCheckTimer;

        public Menu()
        {
            InitializeComponent();
            this.Load += new EventHandler(Menu_Load);
            this.FormClosing += new FormClosingEventHandler(Menu_FormClosing);
        }

        private void Menu_Load(object sender, EventArgs e)
        {
            try
            {
                string localIP = GetLocalIPAddress();
                label2.Text = localIP;

                string randomPassword = GenerateStrongPassword(); // Gerar senha
                Password = randomPassword;
                label5.Text = randomPassword;

                label7.Text = "Pronto para conexão!";
                label7.Location = new Point(10, this.ClientSize.Height - label7.Height - 10);

                // Atualiza a senha do usuário local SNAPASSIST (para RDP/uso local)
                ResetLocalUserPassword("SNAPASSIST", randomPassword);

                // === NOVO: Timer para detectar se está sendo acessado via FTP (porta 21) ===
                ftpCheckTimer = new System.Timers.Timer(10000); // verifica a cada 10s
                ftpCheckTimer.Elapsed += FtpCheckTimer_Elapsed;
                ftpCheckTimer.Start();
            }
            catch (Exception)
            {
            }
        }

        public static void ResetLocalUserPassword(string username, string newPassword)
        {
            using (var machine = new DirectoryEntry("WinNT://" + Environment.MachineName))
            using (var user = machine.Children.Find(username, "User"))
            {
                user.Invoke("SetPassword", new object[] { newPassword });
                user.CommitChanges();
            }
        }

        private string GenerateStrongPassword(int length = 10)
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()-_=+[]{}<>?";

            string allChars = upper + lower + digits + special;

            StringBuilder password = new StringBuilder();
            password.Append(GetRandomChar(upper));
            password.Append(GetRandomChar(lower));
            password.Append(GetRandomChar(digits));
            password.Append(GetRandomChar(special));

            for (int i = password.Length; i < length; i++)
                password.Append(GetRandomChar(allChars));

            return Shuffle(password.ToString());
        }

        private char GetRandomChar(string chars)
        {
            byte[] buffer = new byte[1];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            return chars[buffer[0] % chars.Length];
        }

        private string Shuffle(string input)
        {
            var array = input.ToCharArray();
            var rng = RandomNumberGenerator.Create();
            byte[] box = new byte[1];

            for (int i = array.Length - 1; i > 0; i--)
            {
                rng.GetBytes(box);
                int j = box[0] % (i + 1);

                (array[i], array[j]) = (array[j], array[i]);
            }

            return new string(array);
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Nenhum IP local encontrado!");
        }

        private void Menu_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (ftpCheckTimer != null)
                {
                    ftpCheckTimer.Stop();
                    ftpCheckTimer.Dispose();
                    ftpCheckTimer = null;
                }
            }
            catch { }

            Application.Exit();
        }

        // =========================================================================
        // BOTÃO "CONECTAR" (lado suporte) – tenta conectar no FTP informado e abre o Suporte
        // =========================================================================
        private void button2_Click(object sender, EventArgs e)
        {
            string ftpIp = textBox1.Text;
            string ftpPassword = textBox2.Text;

            try
            {
                bool isConnected = TryConnectToFtp(ftpIp, ftpPassword);

                if (isConnected)
                {
                    Suporte cl = new Suporte(ftpIp, ftpPassword);
                    OpenForm(cl);
                }
                else
                {
                    MessageBox.Show("Falha ao conectar. Verifique o IP e a senha.");
                }
            }
            catch (Exception)
            {
            }
        }

        private bool TryConnectToFtp(string ip, string password)
        {
            var client = new FtpClient(ip)
            {
                // Aqui o usuário é SNAPASSIST, de acordo com sua versão nova
                Credentials = new NetworkCredential("SNAPASSIST", password)
            };

            try
            {
                client.Connect();
                Console.WriteLine("Conexão FTP OK!");

                // Testa um Listagem no root para validar
                foreach (var item in client.GetListing("/"))
                {
                    Console.WriteLine($"{item.Type} - {item.FullName}");
                }

                client.Disconnect();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao conectar: " + ex.Message);
                return false;
            }
        }

        private void OpenForm(Form NewForm)
        {
            try
            {
                if (FormOpen != null)
                {
                    FormOpen.Close();
                    FormOpen.Dispose();
                }
                FormOpen = NewForm;
                FormOpen.Show();
            }
            catch (Exception)
            {
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Password))
            {
                Clipboard.SetText(Password);
            }
        }

        // =========================================================================
        // DETECÇÃO AUTOMÁTICA DE ACESSO (LÓGICA DO MENU ANTIGO)
        // =========================================================================

        // Verifica se há conexões TCP ativas na porta 21 (FTP)
        private bool IsFtpUserConnected()
        {
            try
            {
                // Comando PowerShell que retorna os IPs remotos conectados na porta 21
                string command = @"
$connections = Get-NetTCPConnection | Where-Object { $_.LocalPort -eq 21 -and $_.State -eq 'Established' }
if ($connections) {
    $validConnections = $connections | Where-Object { $_.RemoteAddress -ne '::' -and $_.RemoteAddress }
    if ($validConnections) {
        $validConnections | ForEach-Object { $_.RemoteAddress }
    } else {
        Write-Host 'Nao ha conexoes'
    }
} else {
    Write-Host 'Nao ha conexoes ativas no FTP.'
}";

                ProcessStartInfo psi = new ProcessStartInfo("powershell", "-Command " + command)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                output = output.Trim();

                if (output.Contains("Nao ha conexoes ativas no FTP.") || output.Contains("Nao ha conexoes") || string.IsNullOrWhiteSpace(output))
                {
                    return false;
                }

                // Se chegou aqui é porque veio algum IP remoto na saída
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // (Opcional) Pega o IP remoto conectado na porta 21 para exibir no Cliente / log
        private string GetRemoteFtpClientIp()
        {
            try
            {
                string command = @"
$connections = Get-NetTCPConnection | Where-Object { $_.LocalPort -eq 21 -and $_.State -eq 'Established' }
$valid = $connections | Where-Object { $_.RemoteAddress -ne '::' -and $_.RemoteAddress }
if ($valid) {
    $valid | Select-Object -First 1 -ExpandProperty RemoteAddress
}";

                ProcessStartInfo psi = new ProcessStartInfo("powershell", "-Command " + command)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                output = output.Trim();
                return string.IsNullOrWhiteSpace(output) ? null : output;
            }
            catch
            {
                return null;
            }
        }

        private void FtpCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!IsFtpUserConnected())
                    return;

                string remoteIp = GetRemoteFtpClientIp();

                this.Invoke((MethodInvoker)delegate
                {
                    this.WindowState = FormWindowState.Minimized;

                    if (FormOpen == null || FormOpen.IsDisposed)
                    {
                        // Aqui abrimos o Cliente com o IP do remoto (se não conseguir, usa o IP local)
                        string ipToShow = !string.IsNullOrEmpty(remoteIp) ? remoteIp : GetLocalIPAddress();

                        // Cliente hoje recebe (ip, password) – o password é o que você já gerou no Menu
                        Cliente cl = new Cliente(ipToShow, Password);
                        OpenForm(cl);
                    }
                });
            }
            catch (Exception)
            {
            }
        }
    }
}
