using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CataBot
{
    class functions
    {
        // Find the current working directory
        public static string location = AppDomain.CurrentDomain.BaseDirectory;
        public static int LevenshteinDistance(string s, string t)
        {
            // Calculate the Levenshtein distance between two strings.
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            if (n == 0)
            {
                return m;
            }
            if (m == 0)
            {
                return n;
            }
            for (int i = 0; i <= n; d[i, 0] = i++)
                ;
            for (int j = 0; j <= m; d[0, j] = j++)
                ;
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
        public static string EventSearch(string[] search, List<string> events)
        {
            // Count the number of events and initialise int arrays to store the number of exact term matches and levenshtein distances.
            int itemCount = events.Count;
            int[] distances = new int[itemCount];
            int[] matches = new int[itemCount];
            int i = 0;
            // Cycle through the matches array.
            for (i = 0; i < itemCount; i++)
            {
                // Initialise at 0 exact word matches.
                matches[i] = 0;
                // Iterate through words in the search term.
                foreach (var substring in search)
                {
                    // Count how many words have exact matches
                    if (events[i].ToString().Substring(events[i].IndexOf(" ") + 1).ToUpper().Contains(substring.ToUpper()))
                    {
                        matches[i]++;
                    }
                }
                // Calculate the Levenshtein distance between the event summary and the search.
                distances[i] = LevenshteinDistance(events[i], string.Join(" ", search));
            }
            // After the number of exact matches to each event has been found, set the Levenshtein distance of any event to -1 if it does not have the maximum number of exact matches.
            for (i = 0; i < itemCount; i++)
            {
                if (matches[i] != matches.Max())
                {
                    distances[i] = -1;
                }
            }
            // Find the shortest Levenshtein distance (all non-best matching terms have a distance of -1) that's greater than 0.
            int bestMatch = distances.Where(x => x >= 0).Min();
            // Find which event has that Levenshtein distance.
            for (i = 0; i < itemCount; i++)
            {
                if (distances[i] == bestMatch)
                {
                    break;
                }
            }
            // Return the vent that is the closest match to what was searched originally.
            return events[i];
        }
        public static string SanitiseDate(string inputdate)
        {
            // Split the date into DD MM YYYY and store in an array
            string[] datecomp = inputdate.Split('/');
            string outputdate = null;
            // Count the lenght of datecomp to see how many sections of the date were given
            int NoDP = datecomp.Length;
            // Assume defaults (current year / month) for missing information and construct date in an order DateTime likes
            if (NoDP == 1)
            {
                outputdate = DateTime.Now.Year.ToString() + "/" + DateTime.Now.Month.ToString() + "/" + datecomp[0].PadLeft(2, '0');
            }
            else if (NoDP == 2)
            {
                outputdate = DateTime.Now.Year.ToString() + "/" + datecomp[1].PadLeft(2, '0') + "/" + datecomp[0].PadLeft(2, '0');
            }
            else if (NoDP == 2)
            {
                outputdate = datecomp[2] + "/" + datecomp[1].PadLeft(2, '0') + "/" + datecomp[0].PadLeft(2, '0');
            }
            // Return the result.
            return outputdate;
        }
        public static string SanitiseMonth(string month)
        {
            // DateTime.Month.Now.ToString() returns single digit results for Jan-Sep, to avoid it finding the month number in the year e.t.c
            // we format the month as -07- (for July) to search with, as this will only match the month.
            if (month.Length == 1)
            {
                month = month.PadLeft(2, '0');
            }
            month = month.PadLeft(3, '-');
            month = month.PadRight(4, '-');
            return month;
        }
        public static string BuildTimeStamp(string date, string time)
        {
            string TimeStamp = null;
            // Build a string that can be parsed by DateTime and used with the Google Calendar API, with a different option depending on Daylight savings.
            if (TimeZone.CurrentTimeZone.IsDaylightSavingTime(DateTime.Now))
            {
                TimeStamp = date.Replace('/', '-') + "T" + time + ":00+01:00";
            }
            else
            {
                TimeStamp = date.Replace('/', '-') + "T" + time + ":00+00:00";
            }
            // Return the timestamp.
            return TimeStamp;
        }
        // Function returns boolean value depending on the first letter of the string passed to it.
        public static bool FLIV(string s)
        {
            // Converts input string to an array of characters
            char[] role = s.ToCharArray();
            // Initialises an array of characters containing the 5 vowels.
            char[] vowels = { 'A', 'E', 'I', 'O', 'U' };
            // Initialise a bool for storing the return value.
            bool isvowel = new bool();
            // Loop over all 5 vowels
            for (int i = 0; i < 5; i++)
            {
                if (role[0] == vowels[i])
                {
                    // If the character matches, i.e. the first letter of the role is one of the vowels, set return value to true and stop looping.
                    isvowel = true;
                    break;
                }
                else
                {
                    // If no character match, the first letter of the role is not a vowel.
                    isvowel = false;
                }
            }
            // Return the return value.
            return isvowel;
        }
        public static bool fileExists(string filename)
        {
            // Initialise an array of available files.
            string[] files = Directory.GetFiles(location + "/sounds/", "*.mp3");
            // Initialise the existence variable to store whether or not the file exists.
            bool existence = new bool();
            // Loop through the list of files.
            foreach (string file in files)
            {
                if ((filename + ".mp3") == file.Substring(file.LastIndexOf('/') + 1))
                {
                    // If the file exists, flag it as existing and stop looping.
                    existence = true;
                    break;
                }
                else
                {
                    // If the file does not exist, flag it as non existant
                    existence = false;
                }
            }
            return existence;
        }
        public static int GenIndex(int start, int end)
        {
            // Generate a random number in the given range and return it.
            Random index = new Random();
            return index.Next(start, end - 1);
        }
        public static int GenIndex(int end)
        {
            // Generate a random number in the given range and return it.
            Random index = new Random();
            return index.Next(0, end - 1);
        }
    }
}