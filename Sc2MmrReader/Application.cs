using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sc2MmrReader {
    /// <summary>
    /// This is the main application.
    /// </summary>
    public class Application {
        private String _exePath;
        private String[] _programArgs;

        /// <summary>
        /// Creates a new application with the given exe information.
        /// </summary>
        /// <param name="exePath">The path to the executable.</param>
        /// <param name="args">The program arguments that were passed in.</param>
        public Application(String exePath, String[] args) {
            _exePath = exePath;
            _programArgs = args;
        }

        /// <summary>
        /// Runs the application.  Does not return until the application should close.
        /// </summary>
        public void Run() {
            String[] args = _programArgs;
            String exeParent = System.IO.Directory.GetParent(_exePath).FullName;
            String configFilePath = Path.Combine(new string[] { exeParent, "Config.json" });

            if (args.Length > 1) {
                configFilePath = args[1];
            }

            ReaderConfig config = RunConfigFileFlow(exeParent, configFilePath);
            if (config == null) {
                return; // We're done.
            }

            // Read MMR
            MmrReader reader = new MmrReader(config);
            reader.Run();
        }

        /// <summary>
        /// Reads the config file or walks the user through creating or upgrading one.
        /// </summary>
        /// <param name="exeParent">The directory the executable is in.</param>
        /// <param name="configFilePath">The path to the config file we should use or create.</param>
        /// <returns>Returns the config file to use or null if the application should exit.</returns>
        private ReaderConfig RunConfigFileFlow(String exeParent, String configFilePath) {
            // First make sure the file exists
            if (!File.Exists(configFilePath)) {
                RunCreateConfigFileFlow(configFilePath);
            }

            ReaderConfig config = ConfigFileManager.ReadConfigFile(configFilePath);
            if (config == null) {
                Console.WriteLine("There was a problem reading the config file.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return null;
            }

            // Check the version of the config file
            if (config.Version < ReaderConfig.ReaderConfigVersion) {
                Console.WriteLine("The specified config file is an older version.  See Config.json.example for the current format.");
                Console.WriteLine("Would you like Sc2MmrReader to attempt to upgrade to the new format?  New parameters will get default values.");

                String response = GetInputFromUser(new String[] { "yes", "no" });

                // Just quit if they said no.  They can set the config settings themselves.
                if (response == "no") {
                    return null;
                }

                // Otherwise update the config file
                config.Version = ReaderConfig.ReaderConfigVersion;
                if (ConfigFileManager.SaveConfigFile(config, configFilePath)) {
                    Console.WriteLine("The config file has been updated.  Please check that the settings are correct and then restart Sc2MmrReader.  The path is:");
                    Console.WriteLine("\t" + configFilePath);
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();
                    return null;
                } else {
                    Console.WriteLine("There was a problem saving the config file.  Please update it manually.");
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();
                    return null;
                }

            } else if (config.Version > ReaderConfig.ReaderConfigVersion) {
                // If it's too new, tell them to update
                Console.WriteLine("The specified config file version is for a new version of Sc2MmrReader.  Please update to the latest version or downgrade your config file to version " + ReaderConfig.ReaderConfigVersion);
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return null;
            }

            // Make the paths absolute with respect to the root of the EXE file if they are relative
            if (!Path.IsPathRooted(config.MmrFilePath)) {
                config.MmrFilePath = Path.Combine(new string[] { exeParent, config.MmrFilePath });
            }

            if (!Path.IsPathRooted(config.DataDirectory)) {
                config.DataDirectory = Path.Combine(new string[] { exeParent, config.DataDirectory });
            }

            Console.WriteLine("Using the following config file: " + configFilePath);
            Console.WriteLine("Outputting MMR to: " + config.MmrFilePath);
            Console.WriteLine();

            return config;
        }

        /// <summary>
        /// Walks the user through creating a new config file.  If the user follows the flow, the file will be created by the end.
        /// If they say no or cancel partway through there will not be a file afterwards.
        /// </summary>
        /// <param name="configFilePath">The path they specified to create it at.</param>
        private void RunCreateConfigFileFlow(String configFilePath) {
            Console.WriteLine("You do not appear to have a config file at: ");
            Console.WriteLine("\t" + configFilePath);
            Console.WriteLine();
            Console.WriteLine("Would you like to create one? ");
            String response = GetInputFromUser(new String[]{ "yes", "no" });
            Console.WriteLine();
            if (response == "yes") {
                // Load in the default values from the default file:
                ReaderConfig config = ReaderConfig.CreateDefault();
                Console.WriteLine();
                Console.WriteLine("First we'll need the information for the ladder you want to get the MMR for.");
                Console.WriteLine("Please enter the URL to the ladder you would like to monitor.");
                Console.WriteLine("You can find this by doing the following:");
                Console.WriteLine("  1) Navigate to Starcraft2.com");
                Console.WriteLine("  2) Log in to your Blizzard account");
                Console.WriteLine("  3) Navigate to View Profile-->Ladders-->(Pick a Ladder).  Note: The ladders are listed under \"CURRENT SEASON LEAGUES\"");
                Console.WriteLine("  4) Copy the URL of the page and paste it below.");
                Console.WriteLine("        Example: https://starcraft2.com/en-us/profile/1/1/1986271/ladders?ladderId=274006");
                Console.WriteLine();
                Console.Write("  (enter a URL): ");
                response = Console.ReadLine();
                while (response.ToLower() != "q" && !ParseAccountUrlIntoConfig(response, config)) {
                    Console.WriteLine("That URL is not valid.  Make sure you get the URL of a specific ladder and not your profile, or enter q to quit.");
                    Console.Write("  (enter a URL): ");
                    response = Console.ReadLine();
                }
                Console.WriteLine();

                // If they asked to quit, just stop.
                if (response.ToLower() == "q") {
                    Console.WriteLine("Exiting.");
                    return;
                }

                // Otherwise, the URL parsed sucessfully.
                Console.WriteLine("Great!  That URL seems valid.");
                Console.WriteLine();
                response = "";
                while (String.IsNullOrEmpty(response)) {
                    Console.WriteLine("Please enter your Client ID (See README.md if you dont know what that is).");
                    Console.Write("  (ClientId): ");
                    response = Console.ReadLine();
                }
                Console.WriteLine();

                // Set their response on the config.
                config.ClientId = response;

                Console.WriteLine();
                response = "";
                while (String.IsNullOrEmpty(response)) {
                    Console.WriteLine("Please enter your Client Secret (See README.md if you dont know what that is).");
                    Console.Write("  (ClientSecret): ");
                    response = Console.ReadLine();
                }
                Console.WriteLine();

                // Set their secret on the config
                config.ClientSecret = response;

                if (!ConfigFileManager.SaveConfigFile(config, configFilePath)) {
                    Console.WriteLine("Uh oh, we were not able to save the config to: ");
                    Console.WriteLine("\t" + configFilePath);
                    Console.WriteLine("Please check that is a valid path and Sc2MmrReader has access to write there and try again!");
                    return;
                }

                // Print a success message and continue on to the rest of the app with the info that was written.
                Console.WriteLine("All set!  You can check the following path to verify your settings are correct at any time:");
                Console.WriteLine("\t" + configFilePath);
                Console.WriteLine();
            } else {
                // Just return
            }
        }

        /// <summary>
        /// Parses the ladder and profile information out of the given account URL into the given config.
        /// The given config is only modified if the URL parses correctly.
        /// </summary>
        /// <param name="url">A Blizzard ladder URL of the form: https://starcraft2.com/[Locale]/profile/[RegionId]/[RealmId]/[ProfileId]/ladders?ladderId=[LadderId]</param>
        /// <param name="config">The config to populate</param>
        /// <returns>Returns true if the URL was the right format and was successfully parsed.  False otherwise.</returns>
        private bool ParseAccountUrlIntoConfig(String url, ReaderConfig config) {
            String[] regionIdMap = new String[] { "", "US", "EU", "KO", "", "CN" };
            const String ladderIdRegex = "\\/profile\\/([0-9]{1})\\/([0-9]{1})\\/([0-9]*)\\/ladders\\?ladderId=([0-9]*)";
            Match match = Regex.Match(url, ladderIdRegex, RegexOptions.None, new TimeSpan(0, 0, 5));
            if (match.Success) {
                // There should be 5 groups (4 + the full match at [0])
                if (match.Groups.Count != 5)
                    return false;

                long regionId = -1;
                if (!long.TryParse(match.Groups[1].Value, out regionId))
                    return false;
                if (regionId >= regionIdMap.Length) {
                    Console.WriteLine("Unknown RegionId in the URL: " + regionId);
                    return false;
                }

                int realmId = -1;
                if (!int.TryParse(match.Groups[2].Value, out realmId))
                    return false;

                long profileId = -1;
                if (!long.TryParse(match.Groups[3].Value, out profileId))
                    return false;

                long ladderId = -1;
                if (!long.TryParse(match.Groups[4].Value, out ladderId))
                    return false;

                // Fill out the config
                config.RegionId = regionIdMap[regionId];
                config.RealmId = realmId;
                config.ProfileId = profileId;
                config.LadderId = ladderId;

                return true;
            } else {
                // No matches found - the URL is probably not formatted correctly
                return false;
            }
        }

        /// <summary>
        /// Asks the user to enter one of the given options and returns what they picked.
        /// </summary>
        /// <param name="options">An array of options for the user to pick from.</param>
        /// <returns>Returns one of the options in the given array.</returns>
        private String GetInputFromUser(String[] options) {
            String optionsLine = "  (";
            int index = 0;
            foreach (String option in options) {
                optionsLine += options[index] + "/";
                index++;
            }
            optionsLine = optionsLine.Substring(0, optionsLine.Length - 1) + "): ";

            String input = "";
            while (!options.Contains(input)) {
                Console.Write(optionsLine);
                input = Console.ReadLine();
            }

            return input;
        }
    }
}
