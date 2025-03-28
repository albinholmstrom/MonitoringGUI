namespace MonitoringGUI.Models
{
    //Modell som anv�nds f�r att visa felmeddelanden och fels�kningsinformation
    public class ErrorViewModel
    {
        //Inneh�ller ett ID f�r den aktuella webbf�rfr�gan (kan hj�lpa vid fels�kning)
        public string? RequestId { get; set; }

        //Returnerar true om RequestId inte �r tomt � anv�nds f�r att avg�ra om ID ska visas i vyn
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
