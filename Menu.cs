using System;
using System.Diagnostics;
using System.Net;
using System.Reflection.Emit;
using System.Timers;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace snapAssist
{
    public partial class Menu : Form
    {
        Form FormOpen = null;
        private System.Timers.Timer ftpCheckTimer;

        public Menu()
        {
            InitializeComponent();
            this.Load += new EventHandler(Menu_Load);
            this.FormClosing += new FormClosingEventHandler(Menu_FormClosing); // Conectar evento de fechamento
        }

        private async void Menu_Load(object sender, EventArgs e)
        {
            try
            {
                RemoveFtpConfiguration();

                string localIP = GetLocalIPAddress();
                label2.Text = localIP; 
                string randomPassword = GenerateRandomPassword(); // Gerar senha
                label5.Text = randomPassword; 

                // Inicializar o Timer
                ftpCheckTimer = new System.Timers.Timer(10000); // 10 segundos (10000 ms)
                ftpCheckTimer.Elapsed += FtpCheckTimer_Elapsed;
                ftpCheckTimer.Start(); 

                // Configuração do FTP
                await Task.Run(() =>
                {
                    // Adicionando a regra de firewall para permitir conexões FTP na porta 21
                    ExecuteCommand("netsh advfirewall firewall add rule name=\"FTP\" protocol=TCP dir=in localport=21 action=allow");

                    // Habilitando os recursos necessários para o servidor FTP no Windows
                    ExecuteCommand("dism /online /enable-feature /featurename:IIS-FTPServer /all");
                    ExecuteCommand("dism /online /enable-feature /featurename:IIS-WebServerRole /all");
                    ExecuteCommand("dism /online /enable-feature /featurename:IIS-FTPExtensibility /all");

                    // Criando o diretório FTP
                    ExecuteCommand("mkdir C:\\FTP");

                    // Criando o site FTP no IIS
                    ExecuteCommand($"powershell -Command \"Import-Module WebAdministration; New-WebFtpSite -Name '{localIP}' -Port 21 -PhysicalPath 'C:\\FTP' -Force\"");

                    // Configurando a vinculação do site FTP para a porta 21 no IP local
                    ExecuteCommand($"powershell -Command \"Set-ItemProperty IIS:\\Sites\\{localIP} -Name Bindings -Value @{{'protocol'='ftp';'bindingInformation'='*:21:0.0.0.0'}}\"");

                    // Criando o usuário 'ftpUser' e atribuindo uma senha
                    string ftpUserName = "ftpUser";
                    ExecuteCommand($"net user {ftpUserName} {randomPassword} /add");

                    // Definindo permissões de leitura e escrita para o diretório FTP
                    ExecuteCommand($"powershell -Command \"$acl = Get-Acl 'C:\\FTP'; $rule = New-Object System.Security.AccessControl.FileSystemAccessRule('{ftpUserName}', 'Read,Write', 'Allow'); $acl.SetAccessRule($rule); Set-Acl 'C:\\FTP' $acl\"");

                    // Desabilitando a autenticação anônima para o FTP
                    ExecuteCommand("powershell -Command \"Set-WebConfigurationProperty -pspath 'MACHINE/WEBROOT/APPHOST' -filter 'system.ftpServer/security/authentication/anonymousAuthentication' -name enabled -value false\"");

                    // Configurando SSL para o FTP (sem criptografia para o controle e dados)
                    ExecuteCommand($"powershell -Command \"Import-Module WebAdministration; Set-ItemProperty -Path 'IIS:\\Sites\\{localIP}' -Name 'ftpServer.security.ssl.controlChannelPolicy' -Value 0; Set-ItemProperty -Path 'IIS:\\Sites\\{localIP}' -Name 'ftpServer.security.ssl.dataChannelPolicy' -Value 0\"");

                    // Adicionando a configuração de autorização no IIS para permitir leitura e escrita para o usuário "ftpuser"
                    ExecuteCommand($"powershell -Command \"Import-Module WebAdministration; Add-WebConfigurationProperty -Filter '/system.ftpServer/security/authorization' -Name '.' -Value @{{accessType='Allow'; users='*'; permissions='Read, Write'}} -PSPath 'IIS:\\'\"");

                    // Iniciando e reiniciando o site FTP
                    ExecuteCommand($"powershell -Command \"Start-WebItem 'IIS:\\Sites\\{localIP}'\"");
                    ExecuteCommand($"powershell -Command \"Restart-WebItem 'IIS:\\Sites\\{localIP}'\"");
                });

                label7.Text = "Pronto para conexão!";
                label7.Location = new Point(10, this.ClientSize.Height - label7.Height - 10);


            }
            catch (Exception ex)
            {
            }
        }

        private string GenerateRandomPassword(int length = 8)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            Random random = new Random();
            char[] res = new char[length];

            for (int i = 0; i < length; i++)
            {
                res[i] = valid[random.Next(valid.Length)];
            }
            string password = new string(res);
            return new string(password);
        }

        private void ExecuteCommand(string command)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/C " + command;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
            }
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Nenhum IP local encontrado!");
        }

        private void Menu_FormClosing(object sender, FormClosingEventArgs e)
        {
            RemoveFtpConfiguration();

            Application.Exit();
        }

        private void RemoveFtpConfiguration()
        {
            try
            {
                string localIP = GetLocalIPAddress();

                // Remover o site FTP no IIS
                ExecuteCommand($"powershell -Command \"Import-Module WebAdministration; if (Get-Website -Name '{localIP}') {{ Remove-Website -Name '{localIP}' }}\"");

                // Remover regras de firewall criadas
                ExecuteCommand("netsh advfirewall firewall delete rule name=\"FTP\"");

                // Excluir o usuário FTP
                string ftpUserName = "ftpUser"; // Nome do usuário FTP
                ExecuteCommand($"net user {ftpUserName} /delete");

                // Remover a pasta FTP, se necessário
                ExecuteCommand("rmdir /S /Q C:\\FTP"); 

            }
            catch (Exception ex)
            {
            }
        }

        private bool IsFtpUserConnected()
        {
            try
            {
                // Comando PowerShell para verificar as conexões ativas na porta 21 (FTP)
                string command = @"
            $connections = Get-NetTCPConnection | Where-Object { $_.LocalPort -eq 21 }
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

                // Iniciar o processo PowerShell
                ProcessStartInfo psi = new ProcessStartInfo("powershell", "-Command " + command)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Corrigir problemas de codificação e espaços extras
                output = output.Trim();

                // Verificar se a saída contém a mensagem de "sem conexões"
                if (output.Contains("Nao ha conexoes ativas no FTP.") || output.Contains("Nao ha conexoes"))
                {
                    return false; // Não há conexões ativas
                }
                else
                {
                    // Se houver conexões ativas, exibir a mensagem de conexões
                    return true; // Há conexões ativas
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            string ftpIp = textBox1.Text;  
            string ftpPassword = textBox2.Text;

            try
            {
                // Tentar conectar ao FTP
                bool isConnected = await TryConnectToFtpAsync(ftpIp, ftpPassword);

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
            catch (Exception ex)
            {
            }
        }

        private async Task<bool> TryConnectToFtpAsync(string ip, string password)
        {
            try
            {
                string ftpAddress = $"ftp://{ip}/";
                var ftpRequest = (FtpWebRequest)WebRequest.Create(ftpAddress);
                ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                ftpRequest.Credentials = new NetworkCredential("ftpUser", password);

                ftpRequest.UsePassive = true;

                using (FtpWebResponse response = (FtpWebResponse)await ftpRequest.GetResponseAsync())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
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
            catch (Exception ex) { }
        }

        private string GetFtpUserConnectedIp()
        {
            foreach (var ip in System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName()))
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();  
                }
            }
            return null;
        }

        private void FtpCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (IsFtpUserConnected())
                {
                    string ftpIp = GetFtpUserConnectedIp();

                    // Se houver uma conexão ativa, abrir o formulário Cliente
                    this.Invoke((MethodInvoker)delegate
                    {
                        this.WindowState = FormWindowState.Minimized;

                        if (FormOpen == null || FormOpen.IsDisposed)
                        {
                            Cliente cl = new Cliente(ftpIp);
                            OpenForm(cl); 
                        }
                    });
                }
            }
            catch (Exception ex)
            {
            }
        }

    }
}
