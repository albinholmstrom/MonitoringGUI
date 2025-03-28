namespace MonitoringGUI.Models
{
    //Modell som representerar status för en specifik tjänst eller API
    public class ApiStatus
    {
        //Namnet på tjänsten, t.ex. "LoginService" eller "MonitoringService"
        public string Service { get; set; }

        //Statusmeddelande, t.ex. "OK", "Ned", "Timeout"
        public string Status { get; set; }

        //Färg som kan användas i gränssnittet för att visa status visuellt (t.ex. grön/röd)
        public string Color { get; set; }
    }
}
