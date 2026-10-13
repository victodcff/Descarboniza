using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace Descarboniza.Services
{
    public class IaProcessHostedService : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<IaProcessHostedService> _logger;
        private Process? _process;

        public IaProcessHostedService(
            IConfiguration config,
            IWebHostEnvironment env,
            ILogger<IaProcessHostedService> logger)
        {
            _config = config;
            _env = env;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            if (!_config.GetValue<bool>("IaService:AutoStart"))
            {
                _logger.LogWarning("[IA] AutoStart desligado (IaService:AutoStart). O site não vai iniciar a API.");
                return;
            }

            var baseUrl = new Uri(_config["IaService:BaseUrl"] ?? "http://127.0.0.1:8000/");

            var pasta = Path.GetFullPath(Path.Combine(
                _env.ContentRootPath,
                _config["IaService:ApiFolder"] ?? "..\\DescarbonizaIA-API"));

            var python = Path.Combine(pasta, ".venv", "Scripts", "python.exe");

            _logger.LogWarning("[IA] Pasta da API: {Pasta}", pasta);

            if (!File.Exists(Path.Combine(pasta, "main.py")))
            {
                _logger.LogError("[IA] main.py não encontrado nessa pasta. Ajuste IaService:ApiFolder.");
                return;
            }

            if (!File.Exists(python))
            {
                _logger.LogError("[IA] Python não encontrado em {Python}. Crie a .venv dentro da pasta da API.", python);
                return;
            }

            if (!File.Exists(Path.Combine(pasta, ".env")))
            {
                _logger.LogWarning("[IA] Arquivo .env não encontrado na pasta da API (chave do Gemini).");
            }

            // Uma API de uma execução anterior pode ter ficado rodando com código velho
            EncerrarApiAntiga(baseUrl);

            var falhasSeguidas = 0;

            try
            {
                while (!stoppingToken.IsCancellationRequested && falhasSeguidas < 5)
                {
                    if (PortaEmUso(baseUrl.Host, baseUrl.Port))
                    {
                        // Porta ocupada por outro programa. Apenas monitora.
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }

                    var inicio = DateTime.UtcNow;

                    _logger.LogWarning("[IA] Iniciando a API em {Url}...", baseUrl);

                    try
                    {
                        using var processo = Iniciar(python, pasta, baseUrl);
                        _process = processo;

                        await processo.WaitForExitAsync(stoppingToken);

                        _logger.LogError("[IA] A API encerrou com código {Codigo}.", processo.ExitCode);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[IA] Falha ao iniciar a API.");
                    }

                    falhasSeguidas = (DateTime.UtcNow - inicio).TotalSeconds < 15
                        ? falhasSeguidas + 1
                        : 0;

                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }

                if (falhasSeguidas >= 5)
                {
                    _logger.LogError("[IA] A API falhou 5 vezes seguidas. Veja as linhas [IA] acima.");
                }
            }
            catch (OperationCanceledException)
            {
                // Site encerrando.
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_process is { HasExited: false })
                    _process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[IA] Não foi possível encerrar a API.");
            }

            await base.StopAsync(cancellationToken);
        }

        private void EncerrarApiAntiga(Uri baseUrl)
        {
            if (!PortaEmUso(baseUrl.Host, baseUrl.Port))
                return;

            var pid = PidNaPorta(baseUrl.Port);

            if (pid is null)
            {
                _logger.LogWarning("[IA] A porta {Porta} já está em uso e não achei o processo dela.", baseUrl.Port);
                return;
            }

            try
            {
                using var processo = Process.GetProcessById(pid.Value);

                if (!processo.ProcessName.StartsWith("python", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError(
                        "[IA] A porta {Porta} está ocupada por {Nome}, que não é Python. Libere a porta.",
                        baseUrl.Port, processo.ProcessName);
                    return;
                }

                _logger.LogWarning("[IA] Encerrando API antiga (PID {Pid}) para iniciar a versão atual.", pid);

                processo.Kill(entireProcessTree: true);
                processo.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[IA] Não foi possível encerrar a API antiga.");
            }
        }

        // Lê o PID de quem está escutando a porta (não depende do idioma do Windows)
        private static int? PidNaPorta(int porta)
        {
            try
            {
                var info = new ProcessStartInfo("netstat", "-ano -p tcp")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using var netstat = Process.Start(info);

                if (netstat is null)
                    return null;

                var saida = netstat.StandardOutput.ReadToEnd();
                netstat.WaitForExit(3000);

                foreach (var linha in saida.Split('\n'))
                {
                    var partes = linha.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (partes.Length >= 5 &&
                        partes[1].EndsWith(":" + porta) &&
                        partes[2].EndsWith(":0") &&
                        int.TryParse(partes[4].Trim(), out var pid))
                    {
                        return pid;
                    }
                }
            }
            catch
            {
                // Sem permissão ou sem netstat: o chamador trata como "não achei".
            }

            return null;
        }

        private Process Iniciar(string python, string pasta, Uri baseUrl)
        {
            var info = new ProcessStartInfo
            {
                FileName = python,
                WorkingDirectory = pasta,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            info.ArgumentList.Add("-m");
            info.ArgumentList.Add("uvicorn");
            info.ArgumentList.Add("main:app");
            info.ArgumentList.Add("--host");
            info.ArgumentList.Add(baseUrl.Host);
            info.ArgumentList.Add("--port");
            info.ArgumentList.Add(baseUrl.Port.ToString());

            info.Environment["PYTHONIOENCODING"] = "utf-8";
            info.Environment["PYTHONUNBUFFERED"] = "1";

            var processo = new Process { StartInfo = info };

            processo.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) _logger.LogInformation("[IA] {Linha}", e.Data);
            };

            processo.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) _logger.LogInformation("[IA] {Linha}", e.Data);
            };

            processo.Start();
            processo.BeginOutputReadLine();
            processo.BeginErrorReadLine();

            return processo;
        }

        private static bool PortaEmUso(string host, int porta)
        {
            try
            {
                using var cliente = new TcpClient();
                return cliente.ConnectAsync(host, porta).Wait(300);
            }
            catch
            {
                return false;
            }
        }
    }
}