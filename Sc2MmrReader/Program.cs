using System;
using System.IO;
using System.Web.Script.Serialization;

namespace Sc2MmrReader {
    class Program {
        /// <summary>
        /// Application entry point.  Reads the config file and starts the MmrReader.
        /// </summary>
        /// <param name="args">Application arguments.  Accepts an optional path to the config file.  "Config.json" is used if none is specified.</param>
        public static void Main(string[] args) {
            String exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            Application app = new Application(exePath, args);
            app.Run();
        }
    }
}
