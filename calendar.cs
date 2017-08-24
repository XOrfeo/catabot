using System;
using System.Collections.Generic;
using Google.Apis.Calendar.v3;

namespace CataBot
{
    class calendar
    {
        // Create event list items to hold the specific details we want to display.
        public struct EventListItem
        {
            // Hold basic summary and start time/date.
            public string eSummary;
            public string eTime;
            public string eDate;
            // Define the form the list entry.
            public override string ToString()
            {
                return eSummary + " " + eTime + "    " + eDate;
            }
        }
        public static CalendarService prepEvents(CalendarService calendarService)
        {
            // Class orders the list of events in limits the pull to the next 10 (non recurring + not deleted) events.
            calendarService.Events.List("primary").TimeMin = DateTime.Now;
            calendarService.Events.List("primary").ShowDeleted = false;
            calendarService.Events.List("primary").SingleEvents = true;
            calendarService.Events.List("primary").MaxResults = 10;
            calendarService.Events.List("primary").OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            return calendarService;
        }
        public static string matchEvent(CalendarService calendarService, string[] search)
        {
            // Initialise a list to hold the event summaries.
            List<string> summaries = new List<string>();
            // Iterate through the pulled events.
            foreach (var eventItem in calendarService.Events.List("primary").Execute().Items)
            {
                // To handle prepEvents errors, makes sure only future events (or currently occuring events) are stored. 
                if (eventItem.End.DateTime > DateTime.Now)
                {
                    // Add the event to the summaries list
                    summaries.Add(eventItem.Summary);
                }
            }
            // Send the list of summaries along with the search term to the EventSearch function, and return the result.
            return functions.EventSearch(search, summaries);
        }
        public static string[] eventDetails(CalendarService calendarService, string summaryMatch)
        {
            // Initialise array to store specific event details.
            string[] details = new string[7];
            // Iterate through the events.
            foreach (var eventItem in calendarService.Events.List("primary").Execute().Items)
            {
                // When the specified event is found.
                if (eventItem.Summary == summaryMatch)
                {
                    // Pull the start and end time/date as DateTime objects.
                    DateTime start = DateTime.Parse(eventItem.Start.DateTimeRaw);
                    DateTime end = DateTime.Parse(eventItem.End.DateTimeRaw);
                    // Pull the timezone, cut down to the 3 character timecode (i.e. 'GMT').
                    string timezone = TimeZone.CurrentTimeZone.StandardName.Substring(0, TimeZone.CurrentTimeZone.StandardName.IndexOf(' '));
                    // If we're in Daylight savings we'll add +01:00 to the timezone.
                    if (TimeZone.CurrentTimeZone.IsDaylightSavingTime(start))
                    {
                        timezone = string.Join("+", timezone, "1:00");
                    }
                    else
                    {
                        timezone = timezone.PadRight(8, ' ');
                    }
                    // Pull specific details of the event and store in the details array.
                    details[0] = eventItem.Summary;
                    details[1] = eventItem.Description;
                    details[2] = start.ToShortDateString().Replace('/', '-');
                    details[3] = start.ToShortTimeString() + " " + timezone;
                    details[4] = end.ToShortDateString().Replace('/', '-');
                    details[5] = end.ToShortTimeString() + " " + timezone;
                    // After successfully finding the event, there's no need to look at the rest.
                    break;
                }
            }
            // Return the details array.
            return details;
        }
        public static List<EventListItem> eshortDetails(CalendarService calendarService)
        {
            // Initialise the short details list to display multiple events.
            List<EventListItem> eventList = new List<EventListItem>();
            // Iterate through the events
            foreach (var eventItem in calendarService.Events.List("primary").Execute().Items)
            {
                // To handle prepEvents errors, makes sure only future events (or currently occuring events) are stored. 
                if (eventItem.End.DateTime > DateTime.Now)
                {
                    // Grab start date & time of the event.
                    DateTime datetime = DateTime.Parse(eventItem.Start.DateTimeRaw);
                    // Grab first part of timezone.
                    string timezone = TimeZone.CurrentTimeZone.StandardName.Substring(0, TimeZone.CurrentTimeZone.StandardName.IndexOf(' '));
                    // If we're in Daylight savings we'll add +01:00 to the timezone.
                    if (TimeZone.CurrentTimeZone.IsDaylightSavingTime(datetime))
                    {
                        timezone = string.Join("+", timezone, "1:00");
                    }
                    else
                    {
                        timezone = timezone.PadRight(8, ' ');
                    }
                    // Grab the event summary.
                    string summary = eventItem.Summary.ToString();
                    // We want to fit the summary to a 35 character space, padding with spaces where needed.
                    int stringlength = summary.Length;
                    if (stringlength < 35)
                    {
                        summary = summary.PadRight(35, ' ');
                    }
                    else if (stringlength > 35)
                    {
                        summary = summary.Substring(0, 33);
                        // If the summary is too long, we show there's more by replacing the last two characters with dots.
                        summary = summary.PadRight(35, '.');
                    }
                    // Create and event entry for the list of events and set the parameters.
                    EventListItem entry = new EventListItem();
                    entry.eSummary = summary;
                    entry.eDate = datetime.ToShortDateString().Replace('/', '-');
                    entry.eTime = datetime.ToShortTimeString() + " " + timezone;
                    //  Add the entry to the list.
                    eventList.Add(entry);
                }
            }
            // Sort the list by date (soonest first).
            eventList.Sort((x, y) => x.eDate.CompareTo(y.eDate));
            // Return the event list.
            return eventList;
        }
    }
}