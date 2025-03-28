namespace MonitoringGUI.Models
{
    //Modell som representerar en användare i systemet
    public class User
    {
        //Unikt ID för användaren
        public int UserID { get; set; }

        //Användarnamn (t.ex. för inloggning)
        public string Username { get; set; }

        //Lösenordets hashade version (inte det faktiska lösenordet)
        public string PasswordHash { get; set; }

        //Användarens e-postadress
        public string EmailAddress { get; set; }

        //ID som avgör vilken roll användaren har (t.ex. 1 = admin, 2 = handledare, 3 = vanlig användare)
        public int RoleID { get; set; }
    }
}
