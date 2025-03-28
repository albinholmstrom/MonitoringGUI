namespace MonitoringGUI.Models
{
    //Modell som används för att visa felmeddelanden och felsökningsinformation
    public class ErrorViewModel
    {
        //Innehåller ett ID för den aktuella webbförfrågan (kan hjälpa vid felsökning)
        public string? RequestId { get; set; }

        //Returnerar true om RequestId inte är tomt – används för att avgöra om ID ska visas i vyn
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
