using FotoPrint.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FotoPrint.Services
{
    public class PrintSchedulerService : BackgroundService
    {
        private readonly ILogger<PrintSchedulerService> _logger;
        private readonly ConfigService _config;
        private readonly string _staging;
        private readonly string _impressao;
        private readonly string _backup;

        public PrintSchedulerService(ILogger<PrintSchedulerService> logger, IWebHostEnvironment env, ConfigService config)
        {
            _logger = logger;
            _config = config;
            _staging = Path.Combine(env.WebRootPath, "Staging");
            _impressao = Path.Combine(env.WebRootPath, "Impressao");
            _backup = Path.Combine(env.WebRootPath, "Backup");
            Directory.CreateDirectory(_staging);
            Directory.CreateDirectory(_impressao);
            Directory.CreateDirectory(_backup);
        }

        /// <summary>
        /// Método para ser chamado após o envio, que aguarda o intervalo e move as fotos para impressão e backup conforme lote.
        /// </summary>
        /// <param name="intervaloEmSegundos">Segundos para esperar antes de mover os arquivos</param>
        /// <param name="lote">Quantidade de fotos para processar nesta vez</param>
        /// <param name="cancellationToken">Token para cancelar a operação</param>
        public async Task IniciarProcessoImpressaoAsync(int intervaloEmSegundos, int lote, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Aguardando {Intervalo} segundos antes de iniciar impressão", intervaloEmSegundos);

            await Task.Delay(TimeSpan.FromSeconds(intervaloEmSegundos), cancellationToken);

            _logger.LogInformation("Iniciando movimentação dos arquivos para pasta de impressão e backup");

            var arquivos = Directory.Exists(_staging)
                ? Directory.GetFiles(_staging).OrderBy(File.GetCreationTimeUtc).Take(lote).ToList()
                : new List<string>();

            foreach (var arquivo in arquivos)
            {
                try
                {
                    var destImpressora = Path.Combine(_impressao, Path.GetFileName(arquivo));
                    var destBackup = Path.Combine(_backup, Path.GetFileName(arquivo));

                    if (!File.Exists(destImpressora))
                    {
                        File.Move(arquivo, destImpressora);
                        _logger.LogInformation("Movido para impressão: {FileName}", Path.GetFileName(arquivo));
                    }

                    if (!File.Exists(destBackup))
                    {
                        File.Copy(destImpressora, destBackup);
                        _logger.LogInformation("Copiado para backup: {FileName}", Path.GetFileName(arquivo));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao mover arquivo {FileName}", Path.GetFileName(arquivo));
                }
            }

            _logger.LogInformation("Processo de impressão finalizado");
        }

        /// <summary>
        /// Para compatibilidade com BackgroundService, fica sobrescrito porém vazio.
        /// </summary>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Não fazer nada em loop automático, processo será disparado por método externo
            return Task.CompletedTask;
        }
    }
}
