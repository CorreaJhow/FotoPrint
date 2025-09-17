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

            var cfg = _config.Load();

            // Pasta staging: geralmente temporária, pode ficar no wwwroot 
            _staging = Path.Combine(env.WebRootPath, "Staging");
            if (!Directory.Exists(_staging)) Directory.CreateDirectory(_staging);

            // Pasta Impressão: prioriza config absoluto válido, senão usa pasta padrão fora do wwwroot
            if (!string.IsNullOrWhiteSpace(cfg.caminhoPastaImpressora) && Path.IsPathRooted(cfg.caminhoPastaImpressora))
                _impressao = cfg.caminhoPastaImpressora;
            else
                _impressao = Path.Combine(env.ContentRootPath, "Impressao"); // pasta na raiz do projeto, fora do wwwroot

            if (!Directory.Exists(_impressao)) Directory.CreateDirectory(_impressao);

            // Pasta Backup: prioritiza config absoluto válido ou pasta padrão fora do wwwroot
            if (!string.IsNullOrWhiteSpace(cfg.caminhoPastaBackup) && Path.IsPathRooted(cfg.caminhoPastaBackup))
                _backup = cfg.caminhoPastaBackup;
            else
                _backup = Path.Combine(env.ContentRootPath, "Backup");

            if (!Directory.Exists(_backup)) Directory.CreateDirectory(_backup);

            _logger.LogInformation($"Staging folder: {_staging}");
            _logger.LogInformation($"Impressao folder: {_impressao}");
            _logger.LogInformation($"Backup folder: {_backup}");
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
