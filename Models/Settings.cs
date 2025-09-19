namespace FotoPrint.Models
{
    public class Settings
    {
        public int intervaloImpressaoSegundos { get; set; }
        public int fotosPorLote { get; set; } 
        public string? caminhoPastaImpressora { get; set; } = "";
        public string? caminhoPastaBackup { get; set; } = "";
        public string? caminhoPastaTransicao { get; set; } = ""; 
        public string? titulo { get; set; } = "FotoPrint";
    }
}
