using System;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Text;
using System.Collections.Generic;
/*
██████╗ █████╗ ████████╗ █████╗ ██████╗  ██████╗ ████████╗
██╔════╝██╔══██╗╚══██╔══╝██╔══██╗██╔══██╗██╔═══██╗╚══██╔══╝
██║     ███████║   ██║   ███████║██████╔╝██║   ██║   ██║   
██║     ██╔══██║   ██║   ██╔══██║██╔══██╗██║   ██║   ██║   
╚██████╗██║  ██║   ██║   ██║  ██║██████╔╝╚██████╔╝   ██║   
╚═════╝╚═╝  ╚═╝   ╚═╝   ╚═╝  ╚═╝╚═════╝  ╚═════╝    ╚═╝   
*/
namespace CataBot
{
    class Program
    {
        public static List<string> config = new List<string>();
        // Set scopes for google OAuth2 authentication.
        static string[] Scopes = { "https://www.googleapis.com/auth/calendar", "https://www.googleapis.com/auth/drive" };
        // Set appliaction name.
        static string ApplicationName = "CataBot";
        static void Main(string[] args)
        {
            // Define an exit process.
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            // Initiialise the credential that will allow the bot to login.
            UserCredential credential;
            // open the client id .JSON file.
            using (var stream =
                new FileStream(GOOGLE SECRET JSON, FileMode.Open, FileAccess.Read))
            {
                // Build the path to the credentials file.
                string credPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/catabot_id.json");
                // Setup the authorisation broker to allow us to login.
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    GOOGLE ACCOUNT EMAIL,
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                // Inform the console that the credential was successfully setup.
                Console.WriteLine("[Info] Credential file saved to: " + credPath);
            }
            // Initialise the Google Calendar service, using the credential and and apllication name defined earlier.
            var calendarService = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
            // Initialise the Google Drive service, using the credential and and apllication name defined earlier.
            var driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
            // Read all lines of config file
            string[] lines = File.ReadAllLines("catabot_config.txt");
            foreach (string line in lines)
            {
                config.Add(line.Substring(line.IndexOf(':')+2));
                Console.WriteLine($"{line.Substring(line.IndexOf(':') + 2)}");
            }
            // Initialise the Discord Bot, sending it both Google services.
            DiscordBot bot = new DiscordBot(calendarService, driveService);
        }
        static void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("I'm out of here");
            Console.Read();
        }
    }
}