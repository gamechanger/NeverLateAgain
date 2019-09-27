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

            var myEventsToday = service.Events.List("steven.popovich@gc.com");
            myEventsToday.TimeMin = DateTime.Today;
            myEventsToday.ShowDeleted = false;
            myEventsToday.SingleEvents = true;
            myEventsToday.TimeMax = DateTime.Today.AddDays(1);
            myEventsToday.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var fullTeamEventsToday = service.Events.List("gamechanger.io_8ag52p72ocos9b61g7tcdt98ds@group.calendar.google.com");
            fullTeamEventsToday.TimeMin = DateTime.Today;
            fullTeamEventsToday.ShowDeleted = false;
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

            Console.WriteLine("Here are all the all-day events:");
            foreach (var summary in allDaySummaries)
                Console.WriteLine(summary);

            CreateHostBuilder(args).Build().Run();
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