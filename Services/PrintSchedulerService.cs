using FotoPrint.Models;
using Microsoft.Extensions.Hosting;

namespace FotoPrint.Services
{
    public class PrintSchedulerService : BackgroundService
    {
        private readonly ILogger<PrintSchedulerService> _logger;
        private readonly ConfigService _config;
        private readonly string _staging;
        private readonly string _impressao;

        public PrintSchedulerService(ILogger<PrintSchedulerService> logger, IWebHostEnvironment env, ConfigService config)
        {
            _logger = logger;
            _config = config;
            _staging = Path.Combine(env.WebRootPath, "Staging");
            _impressao = Path.Combine(env.WebRootPath, "Impressao");
            Directory.CreateDirectory(_staging);
            Directory.CreateDirectory(_impressao);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PrintScheduler iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                Settings s = _config.Load();
                int intervalo = Math.Max(5, s.intervaloImpressaoSegundos);
                int lote = s.fotosPorLote is >= 2 and <= 3 ? s.fotosPorLote : 2;

                try
                {
                    var pics = Directory.Exists(_staging)
                        ? Directory.GetFiles(_staging).OrderBy(File.GetCreationTimeUtc).ToList()
                        : new List<string>();

                    if (pics.Count > 0)
                    {
                        var toMove = pics.Take(lote).ToList();
                        foreach (var file in toMove)
                        {
                            var dest = Path.Combine(_impressao, Path.GetFileName(file));
                            if (!File.Exists(dest))
                            {
                                File.Move(file, dest);
                                _logger.LogInformation("Enviado para impressao: {file}", Path.GetFileName(file));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no agendador de impressao");
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalo), stoppingToken);
            }
        }
    }
}
