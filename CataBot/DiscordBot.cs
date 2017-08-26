using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Discord.Audio;
using NAudio.Wave;
using System.Net;

namespace CataBot
{
    public class DiscordBot
    {
        // Public bools to store whether or not audio is currently being sent or if the *current* audio file is bad.
        public bool sendingAudio = new bool();
        public bool badfile = new bool();
        // Define the allowed prefixes.
        private char[] allowedPrefixes = { '$', '!', '~', '>', '<', '|' };
        // Default prefix.
        private char Prefix = '$';
        // Public char to store the prefix character
        public char prefix
        {
            get
            {
                return Prefix;
            }
            set
            {
                // If the config-defined prefix is allowed.
                if (allowedPrefixes.Contains(value))
                {
                    Prefix = value;
                }
            }
        }
        // Grab the bot token.
        public string token = Program.config[0];
        // Initialise the image blacklist.
        public List<string> blacklist = new List<string>();
        // Initialise the soundbyte queue.
        public List<string> queue = new List<string>();
        // Initialise the Discord client and Audio client.
        DiscordClient client;
        IAudioClient vClient;
        // Initialise the command service
        CommandService commands;
        // Find the current working directory
        public string location = AppDomain.CurrentDomain.BaseDirectory;
        // Start the bot, pulling the calendar service from the main thread.
        public DiscordBot(CalendarService calendarService, DriveService driveService)
        {
            // Properly initialise the client.
            client = new DiscordClient(xyz =>
            {
                // Set log severity.
                xyz.LogLevel = LogSeverity.Info;
            });
            // Display the log message to the terminal.
            client.Log.Message += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source} { e.Message}");
            // Grab the provided prefix
            prefix = Program.config[1].ToCharArray()[0];
            // Configure the command structure.
            client.UsingCommands(xyz =>
            {
                // Set prefix character so that the bot knows what to look for.
                xyz.PrefixChar = prefix;
                xyz.AllowMentionPrefix = true;
            });
            client.UsingAudio(xyz =>
            {
                // We're only sending audio, not receiving.
                xyz.Mode = AudioMode.Outgoing;
            });
            // Properly initialise the command service for issuing the commands to the bot.
            commands = client.GetService<CommandService>();
            /*
            ██████╗  █████╗ ███████╗██╗ ██████╗
            ██╔══██╗██╔══██╗██╔════╝██║██╔════╝
            ██████╔╝███████║███████╗██║██║     
            ██╔══██╗██╔══██║╚════██║██║██║     
            ██████╔╝██║  ██║███████║██║╚██████╗
            ╚═════╝ ╚═╝  ╚═╝╚══════╝╚═╝ ╚═════╝                                                                                               
             */
            commands.CreateCommand("help").Parameter("args",ParameterType.Multiple).Do(async (e) =>
            {
                // Command displays specified help text.
                // Delete the command message.
                await e.Message.Delete();
                // Initialise the arguments string
                string args = null;
                // If no arguments are passed, or 'all' is passed, we want to display all commands.
                if (e.Args.Length == 0 || e.Args.Contains("all"))
                {
                    args = "general beer events shitpost sound";
                }
                // Else we only want to display the ones that are specified.
                else
                {
                    args = string.Join(" ", e.Args).ToLower();
                }
                // Initialise the helptext list.
                List<string> helptext = new List<string>();
                // Add the basic usage to the helptext.
                helptext.Add($"Command usage : `{prefix}command <argument>`");
                // All text is pre-formatted when it's added to the list.
                // If general commands are specified, add them to the help text.
                if (args.Contains("general"))
                {
                    helptext.Add("**General**");
                    helptext.Add("```                info : display basic info about the bot.\n" +
                        "        whois <user> : display <user>'s role on the server.\n" +
                        "help <command group> : display help for specified command group.\n" +
                        "         list groups : list the command groups.\n" +
                        "         synchronise : sync the images & soundbytes with the Google Drive (Admin/OG FKTS only).\n" +
                        "           changelog : show changelog.```");
                }
                // If beer commands are specified, add them to the help text.
                if (args.Contains("beer"))
                {
                    helptext.Add("**Beer**");
                    helptext.Add($"```  find <beername> : Search for <beername> and present to text channel.\n" +
                        $"          suggest : get a random beer suggestion.\n" +
                        $"giveme <beername> : grab the url of the store page of <beername> if possible.```");
                }
                // If event commands are specified, add them to the help text.
                if (args.Contains("event"))
                {
                    helptext.Add("**Event**");
                    helptext.Add("```      events : display upcoming events.\n" +
                        "view <event> : display more information about <event>.```");
                }
                // If shitposting commands are specified, add them to the help text.
                if (args.Contains("image"))
                {
                    helptext.Add("**Random Image**");
                    helptext.Add("```         image : send a random shitpost.\n" +
                        "     blacklist : display the current blacklist\n" +
                        "clearblacklist : clear the shitpost blacklist.```");
                }
                // If sound commands are specified, add them to the help text.
                if (args.Contains("sound"))
                {
                    helptext.Add("**Sound**");
                    helptext.Add("```play <soundbyte> : play the specified soundbyte.\n" +
                        "     list sounds : list available soundbytes.\n" +
                        "           queue : display the current queue\n" +
                        "     clear queue : clears the queue\n" +
                        "      disconnect : force the bot to disconnect from the voice channel.```");
                }
                // Join the list into a continuous string, seperated by carriage returns
                string helpmessage = string.Join("\n", helptext.ToArray());
                // Inform the user help is on the way, and send the help.
                await e.Channel.SendMessage($"Sending requested help for {e.User.Mention}");
                await e.Channel.SendMessage($"{helpmessage}");
                // Inform the console of the command.
                Console.WriteLine($"[{e.Server}] Displaying help for {e.User.Name}");
            });
            commands.CreateCommand("whois").Parameter("otheruser", ParameterType.Multiple).Do(async (e) =>
            {
                // Command displas the primary role of the specified user.
                // Notes the command being issued and who issued it into the terminal.
                Console.WriteLine($"[{e.Server}] Command 'whois' called by {e.User.Name}");
                // Stores the provided name as a string.
                String name = string.Join(" ", e.Args);
                // Creates a DiscordUser object and finds the user in the server.
                Discord.User singleuser = e.Server.FindUsers(name).FirstOrDefault(t => t.Name == name); ;
                // If the user exists...
                if (singleuser != null)
                {
                    // If the users role is not 'everyone'
                    if (singleuser.Roles.First().ToString() != "everyone")
                    {
                        // Converts role to a string, sends it to the FLIV function and tests if it begins with a vowel, this is for grammar.
                        if (functions.FLIV(singleuser.Roles.First().ToString().ToUpper()))
                        {
                            // Send the message back to the channel with correct grammar (eg. an Admin).
                            await e.Channel.SendMessage($"{singleuser.Name} is an {singleuser.Roles.First()}");
                        }
                        else
                        {
                            // Send the message back to the channel with correct grammar (eg. a Moderator).
                            await e.Channel.SendMessage($"{singleuser.Name} is a {singleuser.Roles.First()}");
                        }
                    }
                    else
                    {
                        // If the user has no special role, they're just a normal person.
                        await e.Channel.SendMessage($"{singleuser.Name} is just some random person.");
                    }
                }
                else
                {
                    // In the case of not finding the user, reports this to the terminal.
                    Console.WriteLine($"[{e.Server}] Command failed. No User '{e.GetArg("otheruser")}'");
                    // Also informs the command issuer by sending a message to the channel.
                    await e.Channel.SendMessage($"Could not find user: {e.GetArg("otheruser")}");
                }
            });
            // Command to display the author and status of this bot.
            commands.CreateCommand("info").Do(async (e) =>
            {
                // Command displays the bot info.
                // Delete the command message.
                await e.Message.Delete();
                // Notes the command being issued and who issued it into the terminal.
                Console.WriteLine($"[{e.Server}] Full bot info shown for {e.User.Name}");
                // Sends the information back to the channel
                await e.Channel.SendMessage($"CataBot v4, a Discord Bot made by Catalan.\n" +
                    $"v4 adds Google Drive interaction.");
            });
            commands.CreateCommand("list").Parameter("args", ParameterType.Required).Do(async (e) =>
            {
                // Command lists the sounbytes or command groups
                // Delete the command message.
                await e.Message.Delete();
                // Inform the user that their list will be shown.
                await e.Channel.SendMessage($"List shown for {e.User.Mention}");
                // Display the correct list.
                if (e.GetArg("args").ToLower() == "sounds")
                {
                    // List the available soundbytes to the text channel.
                    await e.Channel.SendMessage($"*Available soundbytes:*");
                    string[] sounds = Directory.GetFiles(location + "sounds/");
                    for (int i = 0; i < sounds.Length; i++)
                    {
                        sounds[i] = sounds[i].Substring(sounds[i].LastIndexOf('/') + 1);
                        sounds[i] = sounds[i].Substring(0, sounds[i].IndexOf(".mp3"));
                    }
                    await e.Channel.SendMessage($"```{string.Join("\n", sounds)}```");
                    Console.WriteLine($"[{e.Server}] Soundbyte list shown for {e.User.Name}");
                }
                else if (e.GetArg("args").ToLower() == "groups")
                {
                    // Display what the command groups are to the text channel.
                    await e.Channel.SendMessage($"*Command Groups:*");
                    await e.Channel.SendMessage($"```null" +
                        $"\nGeneral" +
                        $"\nBeer" +
                        $"\nEvents" +
                        $"\nImages" +
                        $"\nSound```");
                    Console.WriteLine($"[{e.Server}] Command groups shown for {e.User.Name}");
                }
                else
                {
                    // Inform the user if the specified list does not exist.
                    await e.Channel.SendMessage($"{e.User.Mention} that list doesn't exist!");
                }
            });
            commands.CreateCommand("synchronise").Alias(new string[] { "sync" }).Do(async (e) =>
            {
                // Command send a random shitpost to the text channel.
                // Delete the command message.
                await e.Message.Delete();
                // Specificy the roles need to issue this command,
                string[] reqRoles = { "Admin", "OG FKTS" };
                // If the user has the required permissions, execute the command as normal.
                if (reqRoles.Any(s => e.User.Roles.Any(r => r.Name.Contains(s))))
                {
                    // Inform the user that the shitpost is being sent
                    await e.Channel.SendMessage($"Synchronising files for {e.User.Mention}");
                    // Set the file extensions.
                    string[] imageExtensions = { ".jpg", ".png" };
                    string[] soundExtensions = { ".mp3", ".wav" };
                    try
                    {
                        // Pull the metadata for all image/sound files.
                        List<Google.Apis.Drive.v3.Data.File> iFiles = drive.GetFiles(driveService, imageExtensions);
                        List<Google.Apis.Drive.v3.Data.File> sFiles = drive.GetFiles(driveService, soundExtensions);
                        // Synchronise the folders.
                        drive.syncFiles(driveService, iFiles, "images/", e.Channel);
                        drive.syncFiles(driveService, sFiles, "sounds/", e.Channel);
                        await e.Channel.SendMessage("GDrive synchronisation complete.");
                        Console.WriteLine($"[{e.Server}] Synchronised with GDrive ({e.User.Name})");
                    }
                    catch
                    {
                        // If it fails, throw an error message to the user & console.
                        Console.WriteLine($"[{e.Server}] Failed to sync");
                        await e.Channel.SendMessage("GDrive synchronisation failed.");
                    }
                }
                else
                {
                    // If the user doesn't have the correct permissions, inform them.
                    await e.Channel.SendMessage($"{e.User.Mention} Insufficient permissions to  sync files.");
                }
            });
            commands.CreateCommand("exit").Do(async (e) =>
            {
                // Command shuts down the bot.
                // Delete the command message.
                await e.Message.Delete();
                await e.Channel.SendMessage($"***Shutting down CataBot***");
                // If user is an admin.
                if (e.User.Roles.Any(s => s.Name.Contains("Admin")))
                {
                    Environment.Exit(0);
                }
                else
                {
                    await e.Channel.SendMessage($"**YOU DO NOT HAVE ENOUGH BADGES TO TRAIN ME** {e.User.Mention}");
                }
            });
            /*
             ██████╗ █████╗ ██╗     ███████╗███╗   ██╗██████╗  █████╗ ██████╗ 
            ██╔════╝██╔══██╗██║     ██╔════╝████╗  ██║██╔══██╗██╔══██╗██╔══██╗
            ██║     ███████║██║     █████╗  ██╔██╗ ██║██║  ██║███████║██████╔╝
            ██║     ██╔══██║██║     ██╔══╝  ██║╚██╗██║██║  ██║██╔══██║██╔══██╗
            ╚██████╗██║  ██║███████╗███████╗██║ ╚████║██████╔╝██║  ██║██║  ██║
             ╚═════╝╚═╝  ╚═╝╚══════╝╚══════╝╚═╝  ╚═══╝╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═╝ 
             */
            commands.CreateCommand("events").Do(async (e) =>
            {
                // Command displays all upcoming events.
                // Message handling for the command & log.
                Console.WriteLine($"[{e.Server}] Upcoming events shown for {e.User.Name}");
                await e.Message.Delete();
                // Set parameters and initial order for calendar events.
                calendarService = calendar.prepEvents(calendarService);
                // Begin the event feed.
                await e.Channel.SendMessage($"**Upcoming events:**");
                if (calendarService.Events.List("primary").Execute().Items != null && calendarService.Events.List("primary").Execute().Items.Count > 0)
                {
                    // Initialise event list
                    List<calendar.EventListItem> eventList = calendar.eshortDetails(calendarService);
                    // Create two new lists for events this month and next, Splitting the events list into the two new lists based on month.
                    List<calendar.EventListItem> ThisMonth = eventList.Where(x => x.ToString().Contains(functions.SanitiseMonth(DateTime.Now.Month.ToString()))).ToList();
                    List<calendar.EventListItem> NextMonth = eventList.Where(x => x.ToString().Contains(functions.SanitiseMonth((DateTime.Now.Month + 1).ToString()))).ToList();
                    // Builds the list into a multi-line string that can be printed.
                    string events = string.Join("\n", eventList.ToArray());
                    string eventsthismonth = string.Join("\n", ThisMonth.ToArray());
                    string eventsnextmonth = string.Join("\n", NextMonth.ToArray());
                    // Print out the upcoming events, handling if there are none in the appropriate month.
                    await e.Channel.SendMessage($"*This Month:*");
                    if (ThisMonth.Count != 0)
                    {
                        await e.Channel.SendMessage($"```{eventsthismonth}```");
                    }
                    else
                    {
                        await e.Channel.SendMessage($"```*No events this month*```");
                    }
                    // Only display next month if there are any events.
                    if (ThisMonth.Count < 1)
                    {
                        await e.Channel.SendMessage($"*Later:*");
                        await e.Channel.SendMessage($"```{eventsnextmonth}```");
                    }
                }
                else
                {
                    // If there are no upcoming events at all, return this.
                    await e.Channel.SendMessage($"No upcoming events found.");
                }
            });
            commands.CreateCommand("view").Parameter("eventSearch", ParameterType.Multiple).Do(async (e) =>
            {
                // Command displays a specific event to the text channel.
                // Delete the command message.
                await e.Message.Delete();
                List<Event> events = new List<Event>(calendarService.Events.List("primary").Execute().Items);
                calendarService = calendar.prepEvents(calendarService);
                if (events != null && events.Count > 0)
                {
                    string summaryMatch = calendar.matchEvent(calendarService, e.Args);
                    string[] details = calendar.eventDetails(calendarService, summaryMatch);
                    await e.Channel.SendMessage($"**Event:** *{details[0]}*\n" +
                        $"`Starts: {details[3]} | {details[2]}`\n" +
                        $"`Ends:   {details[5]} | {details[4]}`\n" +
                        $"Description:\n" +
                        $"```{details[1]}```\n");
                }
                else
                {
                    await e.Channel.SendMessage($"No upcoming events to view");
                }
            });
            /*
            ██████╗ ███████╗███████╗██████╗ 
            ██╔══██╗██╔════╝██╔════╝██╔══██╗
            ██████╔╝█████╗  █████╗  ██████╔╝
            ██╔══██╗██╔══╝  ██╔══╝  ██╔══██╗
            ██████╔╝███████╗███████╗██║  ██║
            ╚═════╝ ╚══════╝╚══════╝╚═╝  ╚═╝
             */
            List<string> beers = new List<string>();
            beers.Add("brewery");
            beers.Add("country");
            beers.Add("styles");
            commands.CreateCommand("find").Parameter("beer", ParameterType.Multiple).Do(async (e) =>
            {
                // Command finds the specified beer and displays it.
                // Delete the command message.
                await e.Message.Delete();
                // Grab the name of the beer to look for.
                string BeerToSearch = string.Join(" ", e.Args);
                // Inform the user that a beer will be searched.
                await e.Channel.SendMessage($"Searching '*{BeerToSearch}*' for {e.User.Mention}");
                // Search for the beer and pull the URL.
                string url = beer.Search(BeerToSearch, beers);
                // Initialise lists to store beer info.
                List<string> MessageItem = new List<string>();
                List<string> Message = new List<string>();
                if (url != "Not found!")
                {
                    // If beer was found, retrieve the details inc. image link
                    MessageItem = beer.Retrieve(url, e.Channel, "RandomBeer");
                    // Send beer name.
                    await e.Channel.SendMessage($"**{MessageItem[1]}**");
                    // Initialise a WebClient to grab the image.
                    WebClient webClient = new WebClient();
                    // Download the image and save it.
                    webClient.DownloadFile(MessageItem[0], "temp/beer-image.png");
                    // Dispose of the WebClient now that it is no longer needed.
                    webClient.Dispose();
                    // Send the image that was downloaded.
                    await e.Channel.SendFile("temp/beer-image.png");
                    // Send the remaining beer details.
                    string message = string.Join("\n", MessageItem.GetRange(2, MessageItem.Count - 2).ToArray());
                    await e.Channel.SendMessage($"{message}");
                    // Inform the console of the command.
                    Console.WriteLine($"[{e.Server}] beer found for {e.User.Name} ({MessageItem[1]})");
                }
                else
                {
                    await e.Channel.SendMessage($"Couldn't find '*{BeerToSearch}*'");
                    // Inform the console of the command.
                    Console.WriteLine($"[{e.Server}] beer not found for {e.User.Name} ({BeerToSearch})");
                }
            });
            commands.CreateCommand("suggest").Do(async (e) =>
            {
                // Command suggests a beer for the user and displays it.
                // Delete the command message.
                await e.Message.Delete();
                // Inform the user that a beer will be suggested.
                await e.Channel.SendMessage($"Suggesting a beer for {e.User.Mention}");
                // Search for a random beer and pull the URL.
                string url = beer.Search("RandomBeer", beers);
                // Initialise lists to store beer info.
                List<string> MessageItem = new List<string>();
                List<string> Message = new List<string>();
                // Error handling if no URL could be pulled for whatever reason.
                if (url != "Not found!")
                {
                    // If beer was found, retrieve the details inc. image link
                    MessageItem = beer.Retrieve(url, e.Channel, "RandomBeer");
                    // Send beer name.
                    await e.Channel.SendMessage($"**{MessageItem[1]}**");
                    // Initialise a WebClient to grab the image.
                    WebClient webClient = new WebClient();
                    // Download the image and save it.
                    webClient.DownloadFile(MessageItem[0], "temp/beer-image.png");
                    // Dispose of the WebClient now that it is no longer needed.
                    webClient.Dispose();
                    // Send the image that was downloaded.
                    await e.Channel.SendFile("temp/beer-image.png");
                    // Send the remaining beer details.
                    string message = string.Join("\n", MessageItem.GetRange(2, MessageItem.Count - 2).ToArray());
                    await e.Channel.SendMessage($"{message}");
                    // Inform the console of the command.
                    Console.WriteLine($"[{e.Server}] beer suggested for {e.User.Name} ({MessageItem[1]})");
                }
                else
                {
                    await e.Channel.SendMessage($"Couldn't suggest a beer");
                    // Inform the console of the command.
                    Console.WriteLine($"[{e.Server}] beer could not be suggested for {e.User.Name}");
                }

            });
            commands.CreateCommand("giveme").Parameter("BeerToSearch", ParameterType.Multiple).Do(async (e) =>
            {
                // Command provides URL to the specified beer.
                // Delete the command message.
                await e.Message.Delete();
                // Grab the name of the beer to look for.
                string BeerToSearch = string.Join(" ", e.Args);
                // Inform the user that the link is being grabbed.
                await e.Channel.SendMessage($"Grabbing store page for {BeerToSearch} for {e.User.Mention}");
                // Find and pull the URL for the beer.
                string url = beer.Search(BeerToSearch, beers);
                // Send the URL to the channel.
                await e.Channel.SendMessage($"**URL:** {url}");
                // Inform the console of the command.
                Console.WriteLine($"[{e.Server}] beer URL pulled for {e.User.Name} ({BeerToSearch})");
            });
            /*
            ███████╗ ██████╗ ██╗   ██╗███╗   ██╗██████╗ 
            ██╔════╝██╔═══██╗██║   ██║████╗  ██║██╔══██╗
            ███████╗██║   ██║██║   ██║██╔██╗ ██║██║  ██║
            ╚════██║██║   ██║██║   ██║██║╚██╗██║██║  ██║
            ███████║╚██████╔╝╚██████╔╝██║ ╚████║██████╔╝
            ╚══════╝ ╚═════╝  ╚═════╝ ╚═╝  ╚═══╝╚═════╝ 
            */
            commands.CreateCommand("play").Parameter("audiofile", ParameterType.Required).Do(async (e) =>
            {
                // Command plays a specified soundbyte.
                // Delete the command message.
                await e.Message.Delete();
                // Get audio file selection.
                string audiofile = e.GetArg("audiofile");
                // Check if the requested file exists.
                if (functions.fileExists(audiofile))
                {
                    // Inform the user that the soundbyte is being played.
                    await e.Channel.SendMessage($"Playing '*{audiofile}*' for {e.User.Mention}");
                    // Add the soundbyte to the queue.
                    queue.Add(audiofile);
                    // If we're not sending audio, we will need to start sending audio.
                    if (!sendingAudio)
                    {
                        sendingAudio = true;
                        // Check if the user is in a voice channel.
                        if (e.User.VoiceChannel != null)
                        {
                            try
                            {
                                // If file exists, and user is in a voice channel, play it using the SendAudio function (further down).
                                SendAudio(e.User.VoiceChannel, e.Channel, e.Server);
                                // Inform the console.
                                Console.WriteLine($"[{e.Server}] {audiofile} soundbyte played for {e.User.Name}");
                            }
                            catch
                            {
                                // Inform the console.
                                Console.WriteLine($"[{e.Server}] {audiofile} soundbyte could not be played for {e.User.Name}");
                            }
                        }
                        else
                        {
                            // If user not in a voice channel, inform the terminal and tell the user to join a voice channel.
                            Console.WriteLine($"[{e.Server}] user {e.User.Name} not in voice channel");
                            await e.Channel.SendMessage($"{e.User.Mention} join a voice channel to use the soundboard!");
                        }
                    }
                }
                else
                {
                    // If the file does not exist, inform the user and the terminal.
                    await e.Channel.SendMessage($"{e.User.Mention} Couldn't find sound '{audiofile}'");
                    // Inform the console of the command.
                    Console.WriteLine($"[{e.Server}] file {audiofile} could not be found, or does not exist.");
                }
            });
            commands.CreateCommand("queue").Do(async (e) =>
            {
                // Command displays the queue.
                // Delete the command message.
                await e.Message.Delete();
                // Inform the user the queue will be shown.
                await e.Channel.SendMessage($"Showing queue for {e.User.Name}");
                // Grab the queue and turn it into a carriage return deliminated string.
                string Queue = string.Join("\n",queue.ToArray());
                // Send the list to the text channel and inform the console.
                await e.Channel.SendMessage("*Soundbyte queue:*");
                await e.Channel.SendMessage($"```{Queue}```");
                Console.WriteLine($"[{e.Server}] Showing soundbyte queue for {e.User.Name}");
            });
            commands.CreateCommand("clearqueue").Alias(new string[] { "cq" }).Do(async (e) =>
            {
                // Command clears the soundbyte queue.
                // Delete the command message.
                await e.Message.Delete();
                // Inform the user the queue is being cleared.
                await e.Channel.SendMessage($"Clearing queue for {e.User.Mention}");
                // Clear the blacklist.
                queue.Clear();
                // Inform the console of the command.
                Console.WriteLine($"[{e.Server}] Queue cleared by {e.User.Name}");
            });
            commands.CreateCommand("disconnect").Do(async (s) =>
            {
                // Command forces the bot to disconnect from the voice channel.
                // Delete the command message.
                await s.Message.Delete();
                // Disconnect the client from the voice channel.
                await vClient.Disconnect();
                // Clear the queue to avoid confusion upon restart of audio sending.
                queue.Clear();
            });
            /*
            ██╗███╗   ███╗ █████╗  ██████╗ ███████╗███████╗
			██║████╗ ████║██╔══██╗██╔════╝ ██╔════╝██╔════╝
			██║██╔████╔██║███████║██║  ███╗█████╗  ███████╗
			██║██║╚██╔╝██║██╔══██║██║   ██║██╔══╝  ╚════██║
			██║██║ ╚═╝ ██║██║  ██║╚██████╔╝███████╗███████║
			╚═╝╚═╝     ╚═╝╚═╝  ╚═╝ ╚═════╝ ╚══════╝╚══════╝
            */
            commands.CreateCommand("image").Do(async (e) =>
            {
                // Command send a random shitpost to the text channel.
                // Delete the command message.
                await e.Message.Delete();
                // Inform the user that the shitpost is being sent
                await e.Channel.SendMessage($"Sending random image for {e.User.Mention}");
                // Get all the filenames of the correct filetype from the google drive.
                string[] files = Directory.GetFiles(location + "images/");
                // Initialise a string to hold the file.
                string chosenfile = null;
                do
                {
                    // Pick a random file.
                    chosenfile = files[functions.GenIndex(files.Length)];
                    // Check if the file is on the blacklist
                    if (!blacklist.ToArray().Contains(chosenfile.Substring(chosenfile.LastIndexOf('/') + 1)))
                    {
                        // Add the sent file to the blacklist.
                        blacklist.Add(chosenfile.Substring(chosenfile.LastIndexOf('/') + 1));
                        // If it isn't, try to send the file.
                        try
                        {
                            // Send the file.
                            await e.Channel.SendFile(chosenfile);
                            // Inform the console.
                            Console.WriteLine($"[{e.Server}] Sending random image ('{chosenfile.Substring(chosenfile.LastIndexOf('/') + 1)}') for {e.User.Name}");
                        }
                        catch
                        {
                            // Inform the console.
                            Console.WriteLine($"[{e.Server}] Sending random image failure. Failed to send '{chosenfile.Substring(chosenfile.LastIndexOf('/') + 1)}' for {e.User.Name}");
                        }
                        break;
                    }
                    // If it is, repeat until it a non-blacklisted file is found and sent.
                } while (true);
                // If the blacklist is full, forget the first item.
                if (blacklist.Count > 100)
                {
                    blacklist.RemoveAt(0);
                }
                // Inform the console of the command.
                Console.WriteLine($"[{e.Server}] Blacklist updated [{blacklist.Count}]");
            });
            commands.CreateCommand("blacklist").Do(async (e) =>
            {
                // Delete the command message.
                await e.Message.Delete();
                // Inform the user the queue will be cleared.
                await e.Channel.SendMessage($"Showing blacklist for {e.User.Name}");
                string[] blcklst = blacklist.ToArray();
                for (int i = 0; i < blcklst.Length; i++)
                {
                    blcklst[i] = blcklst[i].Substring(blcklst[i].LastIndexOf('/'));
                }
                // Grab the queue and turn it into a carriage return deliminated string.
                string Blacklist = string.Join("\n", blcklst);
                // Send the list to the text channel and inform the console.
                await e.Channel.SendMessage("*Blacklist:*");
                await e.Channel.SendMessage($"```{string.Join("\n",blacklist.ToArray())}```");
                Console.WriteLine($"[{e.Server}] Showing blacklist for {e.User.Name}");
            });
            commands.CreateCommand("clearblacklist").Do(async (e) =>
            {
                // Command clears the shitpost blacklist.
                // Delete the command message.
                await e.Message.Delete();
                // Inform the user the blacklist is being cleared.
                await e.Channel.SendMessage($"Clearing random image blacklist for {e.User.Mention}");
                // Clear the blacklist.
                blacklist.Clear();
                // Inform the console of the command.
                Console.WriteLine($"[{e.Server}] Blacklist cleared by {e.User.Name}");
            });
            client.ExecuteAndWait(async () =>
            {
                // Connects the client using it's unique token.
                await client.Connect("BOT TOKEN HERE", TokenType.Bot);
                // Sets the game to be the help prompt, so users can easily issue the help command.
                client.SetGame($"v4 - {prefix}help for help.");
            });
        }
        /*
        ██╗███╗   ██╗████████╗███████╗██████╗ ███╗   ██╗ █████╗ ██╗     
        ██║████╗  ██║╚══██╔══╝██╔════╝██╔══██╗████╗  ██║██╔══██╗██║     
        ██║██╔██╗ ██║   ██║   █████╗  ██████╔╝██╔██╗ ██║███████║██║     
        ██║██║╚██╗██║   ██║   ██╔══╝  ██╔══██╗██║╚██╗██║██╔══██║██║     
        ██║██║ ╚████║   ██║   ███████╗██║  ██║██║ ╚████║██║  ██║███████╗
        ╚═╝╚═╝  ╚═══╝   ╚═╝   ╚══════╝╚═╝  ╚═╝╚═╝  ╚═══╝╚═╝  ╚═╝╚══════╝
        */
        // The following two functions adapted from code recieved from Katazz Trofee
        // Function to handle sending audio
        public async void SendAudio(Discord.Channel chnl, Discord.Channel txtchnl, Server server)
        {
            // Join a channel
            if (chnl.Type == ChannelType.Voice)
            {
                // We use GetService to find the AudioService that we installed earlier. 
                // Join the Voice Channel, and return the IAudioClient.
                try
                {
                    vClient = await client.GetService<AudioService>().Join(chnl);
                }
                catch (Exception ex)
                {
                    // Display the exception if the client fails to join the voice channel.
                    Console.WriteLine($"[{server}] Failed to join voice channel: {ex}");
                }
            }
            // Play the queue.
            while (queue.Count != 0)
            {
                // Send the first item in the queue.
                await Task.Run(() => SendAudioTask(location + "/sounds/" + queue[0] + ".mp3"));
                // Remove the item that was just played from the queue.
                queue.RemoveAt(0);
            }
            //await Task.Run(() => SendAudioTask(filePath));
            if (badfile)
            {
                // If file is bad, inform the console and user. Then set the badfile flag.
                Console.WriteLine($"[{server}] Couldn't play soundbyte; Bad file {queue[0]}");
                await txtchnl.SendMessage("Bad file - " + queue[0].Substring(queue[0].LastIndexOf('/')));
                badfile = false;
            }
            // Leave the channel
            if (vClient != null)
                await vClient.Disconnect();
        }
        // Subtask for async operation of above function
        public void SendAudioTask(string filePath)
        {
            // Flag to prevent multiple simultaneous audio
            sendingAudio = true;
            try
            {
                // Get the number of AudioChannels our AudioService has been configured to use.
                var channelCount = client.GetService<AudioService>().Config.Channels;
                // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.
                var OutFormat = new WaveFormat(48000, 16, channelCount);
                // Select the right type of reader, based on the file extension.
                MediaFoundationResampler resampler = null;
                if (filePath.ToLower().Contains(".mp3"))
                {
                    // MP3 file
                    // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
                    var Reader = new Mp3FileReader(filePath);
                    resampler = new MediaFoundationResampler(Reader, OutFormat);
                }
                if (filePath.ToLower().Contains(".wav"))
                {
                    // WAV file
                    // Create a new Disposable WaveFileReader, to read audio from the filePath parameter
                    var Reader = new WaveFileReader(filePath);
                    resampler = new MediaFoundationResampler(Reader, OutFormat);
                }
                // Only attempt output if we have a good file type
                if (resampler != null)
                {
                    // Set the quality of the resampler to 60, the highest quality
                    resampler.ResamplerQuality = 60;
                    // Establish the size of our AudioBuffer
                    int blockSize = OutFormat.AverageBytesPerSecond / 50;
                    byte[] buffer = new byte[blockSize];
                    int byteCount;
                    try
                    {
                        // Read audio into our buffer, and keep a loop open while data is present
                        while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0)
                        {
                            if (byteCount < blockSize)
                            {
                                // Incomplete Frame, pad it
                                for (int i = byteCount; i < blockSize; i++)
                                    buffer[i] = 0;
                            }
                            // Send the buffer to Discord
                            vClient.Send(buffer, 0, blockSize);
                        }
                        vClient.Wait();
                    }
                    catch (Exception ex)
                    {
                        // Do nothing, just absorb the exception- likely as not we were cancelled.
                    }
                }
            }
            catch (Exception ex)
            {
                // Set a flag so we can annunciate a bad file
                badfile = true;
            }
            //Set flag to say we're no longer sending audio.
            sendingAudio = false;
        }
    }
}