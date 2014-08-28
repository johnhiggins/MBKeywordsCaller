using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;

namespace MBKeywords
{
    
    class Program
    {
        // app.config file contains the paths of where the keywords web service is, the path to the text file to be created and the path to a log file if needed (not currently used)
        static void Main(string[] args)
        {
            string sURLPath = ReadSetting("webServicePath"); // this is the web url of the service that returns the keywords (defined in a seperate project)

            string startDate = string.Empty; // string representation of start date to pass to web service
            string endDate = string.Empty; // string representation of end date to pass to web service
            string ServiceURL = string.Empty;
            int numDaysperBlock = 15; // number of days in the date range from which to get keywords

            DateTime dStartDate = DateTime.Today; 
            DateTime dEndDate = DateTime.Parse("08/25/2014"); // most recent date to start retrieving keywords from
            DateTime dLastDate = DateTime.Parse("12/31/2009"); // stop trying to get keywords if the date range is less than this date

            // keywords are read in date blocks, where the startdate is earlier in time than the end date. i.e api_test.aspx?startdate=08202014&enddate=08302014
            
            dStartDate = dEndDate.AddDays(-numDaysperBlock);

            startDate = dStartDate.ToString("yyyyMMdd");
            endDate = dEndDate.ToString("yyyyMMdd");

            WriteLogEntry("MB Keywords Started");

            WebClient wc = new WebClient();
            wc.UseDefaultCredentials = true;
            wc.DownloadStringCompleted += GetKeywordsFromServiceCompleted;

            //get the keywords from the service in date blocks
            // depending on the credentials and how MB is configured, each web service call could show as a new connected user in MB Enterprise manage
            // if so - run with a small date range, check for connected users, disconnect multiple instances of the user
            // since this only ran once, not a big concern.

            while (startDate.Length >= 8)
            {
                try
                {
                    Console.WriteLine("Getting Keywords for: " + startDate + " to: " + endDate);

                    ServiceURL = sURLPath + "?startdate=" + startDate + "&enddate=" + endDate;
                    // call the service and wait for a response
                    wc.DownloadStringAsync(new Uri(ServiceURL));

                    WriteLogEntry("Calling: " + ServiceURL);

                    while (wc.IsBusy)
                    {
                        // runs while web client is busy, it's a bit of a hack - but this is a one off...
                    }

                    //decrement the date block for the next g round
                    dEndDate = dStartDate.AddDays(-1);
                    dStartDate = dEndDate.AddDays(-numDaysperBlock);

                    startDate = dStartDate.ToString("yyyyMMdd");
                    endDate = dEndDate.ToString("yyyyMMdd");

                    // if the start date is earlier in time than the last date set above, then the loop will end
                    if (dStartDate.Date < dLastDate.Date)
                    {
                        startDate = string.Empty;
                    }
                }catch (Exception ex)
                {
                    Console.WriteLine("There was an error calling the web service. " + ex.Message.ToString());
                    WriteLogEntry("Error calling service: " +ex.Message.ToString());
                }

            }

            WriteLogEntry("MB Keywords Finished");
            Console.WriteLine("All Keywords retrieved. Press any key to exit");
            Console.ReadLine();
        }

        static string ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string result = appSettings[key] ?? "Not Found";
                return result;
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
                return "";
            }
        }

        static void WriteLogEntry(string logMessage)
        {
            
            DateTime logNow = DateTime.Now; // used for time stamp in log

            // uncomment if you really need a log file created when this runs
            //string logPath = ReadSetting("logFilePath");
            //try
            //{
            //    if (!File.Exists(logPath))
            //    {
            //        File.Create(logPath).Dispose();
            //        TextWriter tw = new StreamWriter(logPath);
            //        tw.WriteLine(logNow.ToString() + " " + logMessage);
            //        tw.Close();
            //    }
            //    else if (File.Exists(logPath))
            //    {
            //        TextWriter tw = new StreamWriter(logPath, true);
            //        tw.WriteLine(logNow.ToString() + " " + logMessage);
            //        tw.Close();
            //    }
            //}
            //catch (Exception)
            //{
            //    Console.WriteLine("Error writing to the log.");
            //    //throw;
            //}
            //Console.WriteLine(logNow.ToString() + " LOG: " + logMessage);
        }

        static void GetKeywordsFromServiceCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            // called when there is a reposnse from the web service
            if (e.Error == null)
            {
                WriteLogEntry("Response received from service call");
                Console.WriteLine("Keywords Retrieved for current date range");
                string res = Convert.ToString(e.Result);

                string filePath = ReadSetting("outputFilePath");

                // something was returned from the service, so write it to a text file whose path is defined in app.config
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Dispose();
                    TextWriter tw = new StreamWriter(filePath);
                    tw.WriteLine(res);
                    tw.Close();
                }
                else if (File.Exists(filePath))
                {
                    TextWriter tw = new StreamWriter(filePath, true);
                    tw.WriteLine(res);
                    tw.Close();
                }
            }
            else
            {
                Console.WriteLine("There was an error reading the results of the web service");
                Console.WriteLine(e.Error.Message);
                WriteLogEntry("Error received from service call: " + e.Error.Message);
            }
            
        }
    }
}
