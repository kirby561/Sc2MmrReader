# Summary
This is a simple tool that will periodically output the MMR of the given Starcraft 2 account to a file of your choosing.
I find it useful in combination with OBS to show your current MMR without having to manually enter it.
It requires a Blizzard API key which you can get for free but will need to create.  See the configuration instructions for details.

# Configuration
To get started, create a file called "Config.json" (or whatever you want) and copy the contents of "Config.json.example" into it.
Then change the settings to your preferences and put in your own ClientId and ClientSecret.
This is what the different settings are and how to set them:
	* "MsPerRead": This is how often to ask for MMR updates in milliseconds.  I found that MMR typically takes 10 seconds or so anyway to be updated on Blizzard's server so this doesn't have to be too quick.
	* "DataDirectory": This is a directory to store temp files that the application needs.  It can be a path relative to the executable or an absolute path.  "" just means put the files in the same directory as the executable.
	* "MmrFilePath": This is a file path to put the MMR in.  If the file doesn't exist it is created.  If they file exists it is overwritten.  The path can be relative to the executable or an absolute path.
	* "RegionId": This is the region to use.  It can be "US", "EU", "KO" or "CN".
	* "ProfileId": This is the ID of the profile you want to get the MMR for.  You can find this by navigating to Starcraft2.com, log into your Blizzard account and click View Profile. The ProfileId will be the last number in the URL at the top.  For example: in "https://starcraft2.com/en-us/profile/1/1/1986271", the ProfileId is 1986271.
	* "LadderId": This is the ID of the particular Ladder you want the MMR for (since you have separate MMR for each race and for team games).  To find this, go to Ladders on the Blizzard account profile site mentioned above and click the ladder you would like to get the MMR for.  The LadderId will be the last number in the URL at the top.  For example: in "https://starcraft2.com/en-us/profile/1/1/1986271/ladders?ladderId=274006", the LadderId is 274006.
	* "ClientId": You will need to sign up for a Blizzard API key to get a ClientId and ClientSecret.  This will give you access to Blizzard's API for access to ladder information.  To do this, go to https://develop.battle.net/access and log in with your Blizzard account.  If you don't have an Authenticator set up, it will make you do this first.  Once you're logged in, create a new Client.  Click on the Client and it will tell you the ClientId and ClientSecret.  Just copy/paste them in.
	* "ClientSecret": See instructions for ClientId above.

# Running
To run the program, either double click on the exe or run this from command line:
	Sc2MmrReader <ConfigFilePath>
where <ConfigFilePath> is a path to a JSON file with the settings you would like.  See "Config.json.example" for the format and which settings you need.
If you don't specify a config file, Sc2MmrReader will check for a file called "Config.json" in the same directory as the executable.

# Building from Source
This project is written in C# targeting the .NET framework 4.6.1.  It has no dependencies outside of the standard libraries so you can just download Visual Studio Community to compile it:
	https://visualstudio.microsoft.com/downloads/
	
To build it, just open Sc2MmrReader.sln and press Build.

# Reporting Issues
Just write up any issues to the github issue tracker: https://github.com/kirby561/Sc2MmrReader/issues

# Questions and help
Ask any questions you have or discuss Sc2MmrReader in Discord!
https://discord.gg/CSJYR7s
