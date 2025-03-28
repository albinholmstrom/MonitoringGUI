namespace MonitoringGUI.Models
{
    // Modell som representerar statistik för ett API-endpoint
    public class ApiStat
    {
        // Namnet på endpointen, t.ex. "/login" eller "/employees"
        public string Endpoint { get; set; }

        // Antal gånger denna endpoint har anropats
        public int RequestCount { get; set; }
    }
}
