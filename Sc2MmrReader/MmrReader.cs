using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Sc2MmrReader {
    /// <summary>
    /// Reads the MMR of kirby every "msPerRead" milliseconds into the file at the given path.
    /// </summary>
    public class MmrReader {
        /// <summary>
        /// Data class to keep track of the access token we were granted and when it
        /// expires.  This class is serialized to and from JSON directly.
        /// </summary>
        public class AccessTokenInfo {
            public String AccessToken { get; set; }     // The access token
            public long ExpirationTimeMs { get; set; }  // Unix timestamp when this token expires
        }

        // Constants
        public const long TicksPerMs = 10000;

        // Maps regions to region IDs that we use on the endpoint.
        public Dictionary<String, int> _regionIdMap = new Dictionary<String, int>();

        // Reader state
        private Thread _mmrReadThread;
        private bool _shouldReadMmr = true;
        private bool _isThreadRunning = false;
        private object _lock = new object();
        private HttpClient _httpClient = new HttpClient();
        private JavaScriptSerializer _jsonSerializer = new JavaScriptSerializer();
        private String _accessCacheFile;

        // Reader configuration
        private ReaderConfig _configuration;

        /// <summary>
        /// Creates an MmrReader using the given configuration.
        /// </summary>
        /// <param name="configuration">The configuration to use.  See ReaderConfig for details.</param>
        public MmrReader(ReaderConfig configuration) {
            _configuration = configuration;
            _accessCacheFile = _configuration.DataDirectory + "/Access.tmp";
            InitRegionIdMap();
        }

        /// <summary>
        /// Initializes a map that maps country codes to their corresponding IDs as defined by the Blizzard API docs.
        /// </summary>
        private void InitRegionIdMap() {
            // From the Blizzard API docs:  (1=US, 2=EU, 3=KO and TW, 5=CN)
            _regionIdMap.Add("US", 1);
            _regionIdMap.Add("EU", 2);
            _regionIdMap.Add("KO", 3);
            _regionIdMap.Add("CN", 5);
        }

        /// <returns>Returns a unix timestamp (ms since the epoch)</returns>
        private long GetNowInMs() {
            return DateTime.Now.ToUniversalTime().Subtract(
                    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                ).Ticks / TicksPerMs;
        }

        /// <summary>
        /// Gets the time to next refresh.
        /// </summary>
        /// <param name="nextRefreshTimeMs">The unix time of the next refresh in milliseconds.</param>
        /// <returns>Returns the number of milliseconds until the next refresh or 0 if it should refresh now.</returns>
        private long GetTimeToNextRefresh(long nextRefreshTimeMs) {
            long time = nextRefreshTimeMs - GetNowInMs();
            if (time < 0)
                time = 0;
            return time;
        }

        /// <summary>
        /// Polls the Blizzard API for the latest MMR and writes it to the output file.
        /// If the access token is out of date it is updated as well first.
        /// </summary>
        private void RefreshMmr() {
            // Check that our authorization token is up to date
            String accessToken = GetAccessToken();

            // Build the URL
            String url = "https://us.api.blizzard.com/sc2/profile/";
            url += _regionIdMap[_configuration.RegionId] + "/";
            url += "1/"; // Realm ID - not sure what this is in SC2
            url += _configuration.ProfileId + "/";
            url += "ladder/";
            url += _configuration.LadderId;
            url += "?locale=en_US";
            url += "&access_token=" + accessToken;

            // Make the request
            Task<string> responseTask = _httpClient.GetStringAsync(url);
            bool succeeded = true;
            try {
                responseTask.Wait();
            } catch (Exception ex) {
                succeeded = false;
                Console.WriteLine("An exception was thrown polling the endpoint: " + ex.Message);
                if (ex.InnerException != null) {
                    Console.WriteLine("\tInnerException: " + ex.InnerException.Message);
                }
            }

            if (succeeded) {
                // The Blizzard API returns a JSON string that contains  
                //    ladder information about the above profile.
                string jsonResponse = responseTask.Result;

                // We expect the response to have a "rankedAndPools.mmr" property with our MMR.
                dynamic ladderData = _jsonSerializer.Deserialize<dynamic>(jsonResponse);
                long mmr = (long)Math.Round((double)ladderData["ranksAndPools"][0]["mmr"]);

                // Dump it to a file
                File.WriteAllText(_configuration.MmrFilePath, mmr.ToString());
            }
        }

        /// <summary>
        /// Checks if our access token is out of date. If it is, requests a new one.
        /// </summary>
        /// <returns>
        /// Returns the current access token or an empty string if we couldn't get one.
        /// </returns>
        private String GetAccessToken() {
            AccessTokenInfo accessTokenInfo = null;

            // Update from our cache file
            if (File.Exists(_accessCacheFile)) {
                String accessFileContents = File.ReadAllText(_accessCacheFile);

                try {
                    accessTokenInfo = _jsonSerializer.Deserialize<AccessTokenInfo>(accessFileContents);
                } catch (Exception serializerEx) {
                    Console.WriteLine("Access token cache file corrupted.  Removing and refreshing the token.  Exception: " + serializerEx.Message);
                    try {
                        File.Delete(_accessCacheFile);
                    } catch (Exception deleteEx) {
                        Console.WriteLine("Could not delete the access cache file: " + deleteEx.Message);
                    }
                }
            }

            // If we had a cached token, check if it's still valid
            if (accessTokenInfo != null) {
                long now = GetNowInMs();
                if (now < accessTokenInfo.ExpirationTimeMs) {
                    // Use this one
                    return accessTokenInfo.AccessToken;
                }
            }

            // Otherwise we need to get a new token from the server.
            //    Make the request
            var content = new Dictionary<string, string> {
                    {"grant_type", "client_credentials"},
                    {"client_id", _configuration.ClientId},
                    {"client_secret", _configuration.ClientSecret}
                };

            Task<HttpResponseMessage> responseTask = _httpClient.PostAsync("https://us.battle.net/oauth/token", new FormUrlEncodedContent(content));
            bool succeeded = true;
            try {
                responseTask.Wait();
            } catch (Exception ex) {
                succeeded = false;
                Console.WriteLine("An exception was thrown requesting an auth token: " + ex.Message);
                if (ex.InnerException != null) {
                    Console.WriteLine("\tInnerException: " + ex.InnerException.Message);
                }
            }

            if (succeeded) {
                HttpResponseMessage result = responseTask.Result;
                Task<string> resultString = result.Content.ReadAsStringAsync();
                try {
                    resultString.Wait();
                } catch (Exception ex) {
                    Console.WriteLine("Exception reading authorization token content: " + ex.Message);
                }

                try {
                    dynamic response = _jsonSerializer.Deserialize<dynamic>(resultString.Result);
                    accessTokenInfo = new AccessTokenInfo();
                    accessTokenInfo.AccessToken = response["access_token"];

                    // The joke is that I have no idea what the unit of "expires_in" is.  It seems like it's in seconds
                    //    so I treat it like that but the docs don't say.  It doesn't matter a whole lot because
                    //    if it expired we will just request another token again.
                    accessTokenInfo.ExpirationTimeMs = 1000 * (long)response["expires_in"];

                    // Serialize to file
                    try {
                        String serializedTokenInfo = _jsonSerializer.Serialize(accessTokenInfo);
                        File.WriteAllText(_accessCacheFile, serializedTokenInfo);
                    } catch (Exception ex) {
                        Console.WriteLine("Failed to cache the access token.  Exception: " + ex.Message);
                    }

                    return accessTokenInfo.AccessToken;
                } catch (Exception ex) {
                    Console.WriteLine("Exception deserializing authorization token content: " + ex.Message);
                }
            }

            return String.Empty;
        }

        /// <summary>
        /// This is the entry point to the background MMR reading thread and just loops reading the MMR at the configured interval.
        /// </summary>
        private void ReadMmrLoop() {
            long nextRefreshTimeMs = GetNowInMs();
            while (_shouldReadMmr) {
                if (GetTimeToNextRefresh(nextRefreshTimeMs) == 0) {
                    // Set the next time
                    nextRefreshTimeMs = GetNowInMs() + _configuration.MsPerRead;

                    // Do the next refresh
                    RefreshMmr();
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

        /// <summary>
        /// Kicks off a background thread to read the MMR from the server and just loops waiting for a quit command.
        /// </summary>
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
}
