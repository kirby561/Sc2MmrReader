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
