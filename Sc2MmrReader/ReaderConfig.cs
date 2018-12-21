using System;

namespace Sc2MmrReader {
    /// <summary>
    /// Keeps track of settings for the config file.  This class needs to exactly match the Config.json.example file so that it can be
    /// deserialized into it.  So if you rename or change parameters here, you must upgrade the config file format as well.
    /// </summary>
    public class ReaderConfig {
        // This is the current version and should always be incremented when changing the config file format.
        public static int ReaderConfigVersion = 1;  

        // Version
        public int Version { get; set; } = 0;          // This identifies the version of the config file.  If it's not set it will be at 0.

        // Config settings:
        public long MsPerRead { get; set; }            // How many milliseconds inbetween reads
        public String DataDirectory { get; set; }      // Where to store cached files
        public String MmrFilePath { get; set; }        // Where to output the MMR to

        // Information about the account to get the MMR for:
        public String RegionId { get; set; }       // Can be "US", "EU", "KO" or "CN"
        public int RealmId { get; set; } = 1;      // Can be 1 or 2.  You get this from the link below ".../profile/<regionId>/<realmId>/...". This defaults to 1 for backwards compatibility.
        public long ProfileId { get; set; }        // Example: https://starcraft2.com/en-us/profile/1/1/1986271/ladders?ladderId=274006, profile ID is 1986271
        public long LadderId { get; set; }         // In the above link, the LadderId is 274006

        // Information about who is connecting:
        public String ClientId { get; set; }       // Get this by making a developer account for the Blizzard API.
        public String ClientSecret { get; set; }   // Same as above.

        /// <summary>
        /// Creates a ReaderConfig with default settings.
        /// </summary>
        /// <returns>Returns the created config.</returns>
        public static ReaderConfig CreateDefault() {
            // Fill out the default settings and version
            ReaderConfig config = new ReaderConfig();
            config.Version = ReaderConfigVersion;
            config.MsPerRead = 5000;
            config.DataDirectory = "";
            config.MmrFilePath = "mmr.txt";

            // The profile information and client info are left blank
            //    because they must be filled out by the user.
            return config;
        }
    }

    // Keep track of old config versions in case we want to be
    //    able to update old versions in a smarter way.
    #region OldConfigVersions
    public class ReaderConfigV0 {
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
    #endregion
}
