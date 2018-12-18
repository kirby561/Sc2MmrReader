using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Sc2MmrReader {
    class Program {
        /// <summary>
        /// Reads the MMR of kirby every "msPerRead" milliseconds into the file at the given path.
        /// </summary>
        public class MmrReader {
            public const long TicksPerMs = 10000;

            private long _msPerRead = 0;
            private String _filePath;
            private Thread _mmrReadThread;
            private bool _shouldReadMmr = true;
            private bool _isThreadRunning = false;
            private object _lock = new object();
            private HttpClient _httpClient = new HttpClient();
            private JavaScriptSerializer _jsonSerializer = new JavaScriptSerializer();

            public MmrReader(long msPerRead, String filePath) {
                _msPerRead = msPerRead;
                _filePath = filePath;
            }

            private long GetNowInMs() {
                return DateTime.Now.Ticks / TicksPerMs;
            }

            private long GetTimeToNextRefresh(long nextRefreshTimeMs) {
                long time = nextRefreshTimeMs - GetNowInMs();
                if (time < 0)
                    time = 0;
                return time;
            }

            private void RefreshMmrAsync() {
                Task<string> responseTask = _httpClient.GetStringAsync("https://us.api.blizzard.com/sc2/profile/1/1/1986271/ladder/274006?locale=en_US&access_token=USm8OakZJuNw3uXwu75AR1DKGHzhxgtCjq");

                bool succeeded = true;
                try {
                    responseTask.Wait();
                } catch (Exception ex) {
                    succeeded = false;
                    Console.WriteLine("An exception was thrown polling the endpoint: " + ex.Message);
                }

                if (succeeded) {
                    // The Blizzard API returns a JSON string that contains ladder information about
                    //    the above profile.
                    string jsonResponse = responseTask.Result;

                    // We expect the response to have a "rankedAndPools.mmr" property with our MMR.
                    dynamic ladderData = _jsonSerializer.Deserialize<dynamic>(jsonResponse);
                    long mmr = (long)Math.Round((double)ladderData["ranksAndPools"][0]["mmr"]);

                    // Dump it to a file
                    File.WriteAllText(_filePath, mmr.ToString());
                }
            }

            private void ReadMmrLoop() {
                long nextRefreshTimeMs = GetNowInMs();
                while (_shouldReadMmr) {
                    if (GetTimeToNextRefresh(nextRefreshTimeMs) == 0) {
                        // Set the next time
                        nextRefreshTimeMs = GetNowInMs() + _msPerRead;

                        // Do the next refresh
                        RefreshMmrAsync();
                    }

                    int timeToNextRefresh = (int)GetTimeToNextRefresh(nextRefreshTimeMs);
                    lock (_lock) {
                        Monitor.Wait(_lock, timeToNextRefresh);
                    }
                }

                // Exiting
                lock (_lock) {
                    _isThreadRunning = false;
                    Monitor.PulseAll(_lock);
                }
            }

            public void Run() {
                Console.WriteLine("Running.  Enter Q to quit.");

                _isThreadRunning = true;
                _mmrReadThread = new Thread(ReadMmrLoop);
                _mmrReadThread.Start();

                // Just wait for the quit command while the background thread does its thing.
                bool shouldExit = false;
                while (!shouldExit) {
                    String entry = Console.ReadLine();
                    if (entry == "Q" || entry == "q")
                        shouldExit = true;
                    else
                        Console.WriteLine("Unknown command.  Enter Q to quit.");
                }

                // Stop the thread
                Console.WriteLine("Stopping...");

                // Request the background thread to stop
                lock (_lock) {
                    _shouldReadMmr = false;
                    Monitor.PulseAll(_lock);
                }

                // Wait for it
                lock (_lock) {
                    while (_isThreadRunning) {
                        Monitor.Wait(_lock);
                    }
                }

                Console.WriteLine("Done.  Exiting.");
            }
        }

        static void Main(string[] args) {
            long msPerRead = 5000;
            String exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            String filePath = Path.Combine(new string[] { System.IO.Directory.GetParent(exePath).FullName, "mmr.txt" });
            
            if (args.Length > 1) {
                // Get the msPerRead
                if (!long.TryParse(args[1], out msPerRead)) {
                    Console.WriteLine("Enter a 64 bit integer for the milliseconds per read.");
                    Console.WriteLine("Format:\nSc2MmrReader <MsPerRead> <FilePath>");
                    Environment.Exit(-1);
                }
            } else {
                Console.WriteLine("No milliseconds per read specified.  Using the default value of " + msPerRead);
            }

            if (args.Length > 2) {
                filePath = args[2];
            } else {
                Console.WriteLine("No file path specified.  Using the default path " + filePath);
            }

            MmrReader reader = new MmrReader(msPerRead, filePath);
            reader.Run();
        }
    }
}
