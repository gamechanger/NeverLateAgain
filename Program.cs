using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace DotnetNeverLateAgain
{
    public class Program
    {
        public static string command2 { get; } = "osascript -e \"beep beep beep beep beep beep beep\"";

        static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        static string ApplicationName = "Google Calendar API .NET Quickstart";

        public static void Main(string[] args)
        {
            Console.WriteLine("Please enter your gc.com email: ");
            var email = Console.ReadLine();
            Console.WriteLine();
            Console.WriteLine("Okay, gathering your day...");
            Console.WriteLine();
            Thread.Sleep(400);
      
            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var myEventsToday = service.Events.List(email);
            myEventsToday.TimeMin = DateTime.Today;
            myEventsToday.SingleEvents = true;
            myEventsToday.TimeMax = DateTime.Today.AddDays(1);
            myEventsToday.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var fullTeamEventsToday = service.Events.List("gamechanger.io_8ag52p72ocos9b61g7tcdt98ds@group.calendar.google.com");
            fullTeamEventsToday.TimeMin = DateTime.Today;
            fullTeamEventsToday.SingleEvents = true;
            fullTeamEventsToday.TimeMax = DateTime.Today.AddDays(1);
            fullTeamEventsToday.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var fullTeamEventsTodayExecuted = fullTeamEventsToday.Execute();
            var myEventsTodayExecuted = myEventsToday.Execute();

            var allDaySummaries = new List<string>();

            var allEvents = new List<Event>();
            allEvents.AddRange(fullTeamEventsTodayExecuted.Items);
            allEvents.AddRange(myEventsTodayExecuted.Items);
            allEvents.Sort(CompareEventTimes);

            allDaySummaries.AddRange(SetItUp(allEvents));

            Thread.Sleep(4000);

            Console.WriteLine("Here are the all-day events:");
            Thread.Sleep(400);
            foreach (var summary in allDaySummaries)
                Console.WriteLine(summary);

            var nonAllDayEvents = new List<Event>();

            foreach (var currEvent in allEvents) {
                if (currEvent.Start.DateTime != null)
                    nonAllDayEvents.Add(currEvent);
            }

            Thread.Sleep(4000);

            Console.WriteLine();
            Console.WriteLine("Okay now I am gonna figure out when you should go to lunch.....maybe.....");
            Console.WriteLine();

            Thread.Sleep(3000);

            if (nonAllDayEvents.Count > 2)
            {
                var index = 0;
                while (index < allEvents.Count && allEvents[index].Start.DateTime < DateTime.Today.AddHours(13))// this depends on the list being sorted
                    index++;

                if (index == 0)
                {
                    Console.WriteLine("Go to lunch before " + nonAllDayEvents[0].Start.DateTime);
                }
                else if (index < allEvents.Count)
                {
                    if (DateTime.Parse(allEvents[index - 1].End.DateTimeRaw).Subtract(DateTime.Parse(allEvents[index].Start.DateTimeRaw)).TotalMinutes > 30)
                        Console.WriteLine("It seems like going to lunch between " +  allEvents[index - 1].End + " and " + allEvents[index].Start + " seems reasonable");
                }
                else {
                    Console.WriteLine("Just go to lunch when you don't have a meeting okay, I don't know");
                }
            }
            else if (nonAllDayEvents.Count == 1)
                Console.WriteLine("Just don't go to lunch between " + nonAllDayEvents[0].Start.DateTime + " and " + nonAllDayEvents[0].End.DateTime + ", that's all you got today");
            else // 0 events
                Console.WriteLine("You got an open day buddy. Go to lunch whenever!");

            Thread.Sleep(300);

            Console.WriteLine();
            Console.WriteLine("Okay, you can ignore me now, I will remind you one minute before your meetings");
            Console.WriteLine();

            CreateHostBuilder(args).Build().Run(); // this is honestly just an easy way to keep the application running
        }

        public static void BeepBeep(Event eventItem) {
            Console.WriteLine("Get up and go to " + eventItem.Location + " to have " + eventItem.Summary);
            command2.Bash();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static List<string> SetItUp(IList<Event> events) {
            var allDayEvents = new List<string>();

            if (events != null && events.Count > 0)
            {
                foreach (var eventItem in events)
                {
                    string when = eventItem.Start.DateTime.ToString();
                    if (!string.IsNullOrEmpty(when)) // if start has .Date and not .DateTime it is an all day event 
                    {
                        var dateNow = DateTime.Now;
                        var date = DateTime.Parse(when);

                        if (date > dateNow.AddMinutes(1))
                        {
                            var ts = date - dateNow.AddMinutes(1);
                            Console.WriteLine("I am adding a beep for you for " + eventItem.Summary + " in " + Math.Round(ts.TotalMinutes) + " minutes.");
                            Task.Delay(ts).ContinueWith((x) =>
                            {
                                BeepBeep(eventItem);
                            });
                        }
                        else Console.WriteLine("Event " + eventItem.Summary + " has already happened");

                        Console.WriteLine();
                    }
                    else allDayEvents.Add(eventItem.Summary);
                }
            }
            else Console.WriteLine("No upcoming events found.");

            Console.WriteLine();

            return allDayEvents;
        }

        public static int CompareEventTimes(Event x, Event y)
        {
            return x.Start.DateTime > y.Start.DateTime ? 1 : -1;
        }
    }

    public static class ShellHelper
    {
        public static string Bash(this string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }
}