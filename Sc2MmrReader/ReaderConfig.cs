using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sc2MmrReader {
    /// <summary>
    /// Keeps track of settings for the config file.  This class needs to exactly match the Config.json.example file so that it can be
    /// deserialized into it.  So if you rename or change parameters here, you must upgrade the config file format as well.
    /// </summary>
    public class ReaderConfig {
        // Config settings:
        public long MsPerRead { get; set; }            // How many milliseconds inbetween reads
        public String DataDirectory { get; set; }      // Where to store cached files
        public String MmrFilePath { get; set; }        // Where to output the MMR to

        // Information about the account to get the MMR for:
        public String RegionId { get; set; }       // Can be "US", "EU", "KO" or "CN"
        public long ProfileId { get; set; }        // Example: https://starcraft2.com/en-us/profile/1/1/1986271/ladders?ladderId=274006, profile ID is 1986271
        public long LadderId { get; set; }         // In the above link, the LadderId is 274006

        // Information about who is connecting:
        public String ClientId { get; set; }       // Get this by making a developer account for the Blizzard API.
        public String ClientSecret { get; set; }   // Same as above.
    }
}
