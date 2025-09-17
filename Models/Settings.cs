namespace FotoPrint.Models
{
    public class Settings
    {
        public int intervaloImpressaoSegundos { get; set; } = 5;
        public int fotosPorLote { get; set; } = 2;
        public string? caminhoPastaImpressora { get; set; } = "";  
        public string? caminhoPastaBackup { get; set; } = "";      
        public string? titulo { get; set; } = "FotoPrint";  

    }
}
