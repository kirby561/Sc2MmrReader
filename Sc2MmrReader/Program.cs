using System;
using System.IO;
using System.Web.Script.Serialization;

namespace Sc2MmrReader {
    class Program {
        /// <summary>
        /// Reads the configuration file at the given path.
        /// </summary>
        /// <param name="path">A path to a JSON config file to read.  The JSON file must exactly match the ReaderConfig class.</param>
        /// <returns>Returns the configuration read in the file or null if there was an error.  The error is printed to stdout as well.</returns>
        private static ReaderConfig ReadConfigFile(String path) {
            String configFileText = null;
            try {
                configFileText = File.ReadAllText(path);
            } catch (IOException ex) {
                Console.WriteLine("Unable to open the config file at " + path);
                Console.WriteLine("Does the file exist?");
                Console.WriteLine("Exception: " + ex.Message);
                return null;
            }

            JavaScriptSerializer deserializer = new JavaScriptSerializer();
            ReaderConfig configFile = null;

            try {
                configFile = deserializer.Deserialize<ReaderConfig>(configFileText);
            } catch (Exception ex) {
                Console.WriteLine("Could not deserialize the config file.  Is the format correct?  See Config.json.example for correct usage.");
                Console.WriteLine("Exception: " + ex.Message);
                if (ex.InnerException != null) {
                    Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
                }
            }

            return configFile;
        }

        /// <summary>
        /// Saves the given config file to the given path.
        /// </summary>
        /// <param name="config">The config to write.</param>
        /// <param name="path">The path to save the config file to</param>
        /// <returns>Returns true if it succeeded, false if there was an error.</returns>
        private static bool SaveConfigFile(ReaderConfig config, String path) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            String configString = null;
            try {
                configString = serializer.Serialize(config);
            } catch (Exception ex) {
                Console.WriteLine("Could not serialize the given config.");
                Console.WriteLine("Exception: " + ex.Message);
                if (ex.InnerException != null) {
                    Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
                }

                return false;
            }

            // Do a poor mans prettify since JavaScriptSerializer doesn't support
            //    formatting the output.  This could be improved by using an actual library like JSON.net
            configString = configString.Insert(configString.IndexOf("{") + 1, Environment.NewLine + "\t");
            configString = configString.Insert(configString.LastIndexOf("}"), Environment.NewLine);
            configString = configString.Replace("\":", "\": ");
            configString = configString.Replace(",\"", "," + Environment.NewLine + "\t\"");

            // Write it to a file
            try {
                File.WriteAllText(path, configString);
            } catch (Exception ex) {
                Console.WriteLine("Could not write the config file to " + path);
                Console.WriteLine("Exception: " + ex.Message);
                if (ex.InnerException != null) {
                    Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
                }
            }   

            return true;
        }

        /// <summary>
        /// Application entry point.  Reads the config file and starts the MmrReader.
        /// </summary>
        /// <param name="args">Application arguments.  Accepts an optional path to the config file.  "Config.json" is used if none is specified.</param>
        public static void Main(string[] args) {
            String exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            String exeParent = System.IO.Directory.GetParent(exePath).FullName;
            String configFilePath = Path.Combine(new string[] { exeParent, "Config.json" });
            
            if (args.Length > 1) {
                configFilePath = args[1];
            } else {
                Console.WriteLine("No config file specified.  Using default path: " + configFilePath);
            }

            ReaderConfig config = ReadConfigFile(configFilePath);
            if (config == null) {
                Console.WriteLine("There was a problem reading the config file.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            // Check the version of the config file
            if (config.Version < ReaderConfig.ReaderConfigVersion) {
                Console.WriteLine("The specified config file is an older version.  See Config.json.example for the current format.");
                Console.WriteLine("Would you like Sc2MmrReader to attempt to upgrade to the new format?  New parameters will get default values.");

                String response = "";
                while (response != "yes" && response != "no") {
                    Console.WriteLine("Enter yes or no:");
                    response = Console.ReadLine();
                }
                
                // Just quit if they said no.  They can set the config settings themselves.
                if (response == "no") {
                    return;
                }

                // Otherwise update the config file
                config.Version = ReaderConfig.ReaderConfigVersion;
                if (SaveConfigFile(config, configFilePath)) {
                    Console.WriteLine("The config file has been updated.  Please check that the settings are correct and then restart Sc2MmrReader.  The path is:");
                    Console.WriteLine("\t" + configFilePath);
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();
                    return;
                } else {
                    Console.WriteLine("There was a problem saving the config file.  Please update it manually.");
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();
                    return;
                }
                
            } else if (config.Version > ReaderConfig.ReaderConfigVersion) {
                // If it's too new, tell them to update
                Console.WriteLine("The specified config file version is for a new version of Sc2MmrReader.  Please update to the latest version or downgrade your config file to version " + ReaderConfig.ReaderConfigVersion);
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            // Make the paths absolute with respect to the root of the EXE file if they are relative
            if (!Path.IsPathRooted(config.MmrFilePath)) {
                config.MmrFilePath = Path.Combine(new string[] { exeParent, config.MmrFilePath });
            }

            if (!Path.IsPathRooted(config.DataDirectory)) {
                config.DataDirectory = Path.Combine(new string[] { exeParent, config.DataDirectory });
            }

            MmrReader reader = new MmrReader(config);
            reader.Run();
        }
    }
}
