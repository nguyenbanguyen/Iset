﻿using System;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Data.SqlClient;
using System.Data;

namespace Iset
{
    class Program
    {
        static void Main(string[] args) => new Program().Start();
        SqlConnection conn;
        private DiscordClient _client;
        IniFile ini = new IniFile(Directory.GetCurrentDirectory() + @"\config.ini");
        public void Start()
        {
            setupConfigBase();
            _client = new DiscordClient();

            _client.UsingCommands(x =>
            {
                x.PrefixChar = '!';
                x.HelpMode = HelpMode.Private;
                x.AllowMentionPrefix = true;
            });

            //this is used to read ALL text chat, not just commands with the prefix
            /*_client.MessageReceived += async (s, e) =>
            {
                if (!e.Message.IsAuthor)
                {
                    await e.Channel.SendMessage("you said something that wasnt a command");
                }
            };*/
            _client.GetService<CommandService>().CreateCommand("soaps") //create command greet
                   .Description("clears broken soaps") //add description, it will be shown when ~help is used
                   .Do(async e =>
                   {
                       if (!checkPerms(e.User, "soaps"))
                       {
                           await e.Channel.SendMessage("You do not have permission to use this command!");
                           return;
                       }
                       await e.Channel.SendMessage(clearSoaps());
                   });
            _client.GetService<CommandService>().CreateCommand("checkban") //create command greet
                   .Description("checks if a player is banned") //add description, it will be shown when ~help is used
                   .Parameter("username", ParameterType.Required)
                   .Do(async e =>
                   {
                       if (!checkPerms(e.User, "checkban"))
                       {
                           await e.Channel.SendMessage("You do not have permission to use this command!");
                           return;
                       }
                       if (!checkValidInput(e.GetArg("username")))
                       {
                           await e.Channel.SendMessage("Invalid Data.");
                           return;
                       }
                       string msg = "The user " + e.GetArg("username") + " " + isBanned(e.GetArg("username"));
                       await e.Channel.SendMessage(msg);
                   });
            _client.GetService<CommandService>().CreateCommand("getaccountid") //create command greet
       .Description("get the account id for a character") //add description, it will be shown when ~help is used
       .Parameter("username", ParameterType.Required)
       .Do(async e =>
       {
           if (!checkPerms(e.User, "getaccountid"))
           {
               await e.Channel.SendMessage("You do not have permission to use this command!");
               return;
           }
           if (!checkValidInput(e.GetArg("username")))
           {
               await e.Channel.SendMessage("Invalid Data.");
               return;
           }
           await e.Channel.SendMessage(getUserIdFromCharacterName(e.GetArg("username")));
       });
       _client.GetService<CommandService>().CreateCommand("getloginuser") //create command greet
       .Description("get the login string for a character") //add description, it will be shown when ~help is used
       .Parameter("username", ParameterType.Required)
       .Do(async e =>
       {
           if (!checkPerms(e.User, "getloginuser"))
           {
               await e.Channel.SendMessage("You do not have permission to use this command!");
               return;
           }
           if (!checkValidInput(e.GetArg("username")))
           {
               await e.Channel.SendMessage("Invalid Data.");
               return;
           }
           string charid = getUserIdFromCharacterName(e.GetArg("username"));
           if (!string.IsNullOrEmpty(charid))
           {
               await e.Channel.SendMessage(getAccountNameFromID(charid));
           }
           else
           {
               await e.Channel.SendMessage("Invalid character name!");
           }
       });
            _client.GetService<CommandService>().CreateCommand("reset2ndary") //create command greet
                   .Description("resets the secondary password of the specified player") //add description, it will be shown when ~help is used
                   .Parameter("username", ParameterType.Required)
                   .Do(async e =>
                   {
                       if (!checkPerms(e.User, "reset2ndary"))
                       {
                           await e.Channel.SendMessage("You do not have permission to use this command!");
                           return;
                       }
                       if (!checkValidInput(e.GetArg("username")))
                       {
                           await e.Channel.SendMessage("Invalid Data.");
                           return;
                       }
                       string msg = resetSecondary(e.GetArg("username"));
                       await e.Channel.SendMessage(msg);
                   });
            _client.GetService<CommandService>().CreateCommand("ban") //create command greet
                   .Description("ban a player in game") //add description, it will be shown when ~help is used
                   .Parameter("username", ParameterType.Required)
                   .Parameter("reason", ParameterType.Unparsed)
                   .Do(async e =>
                   {
                       if (!checkPerms(e.User, "ban"))
                       {
                           await e.Channel.SendMessage("You do not have permission to use this command!");
                           return;
                       }
                       if (!checkValidInput(e.GetArg("username")))
                       {
                           await e.Channel.SendMessage("Invalid Data.");
                           return;
                       }
                       string banResult = null;
                       if (!String.IsNullOrEmpty(e.GetArg("reason")))
                       {
                           banResult = BanUser(e.GetArg("username"), e.GetArg("reason"));
                       }
                       else
                       {
                           banResult = BanUser(e.GetArg("username"));
                       }
                       await e.Channel.SendMessage(banResult);
                   });
            _client.GetService<CommandService>().CreateCommand("unban") //create command greet
       .Description("ban a player in game") //add description, it will be shown when ~help is used
       .Parameter("username", ParameterType.Required)
       .Do(async e =>
       {
           if (!checkPerms(e.User, "unban"))
           {
               await e.Channel.SendMessage("You do not have permission to use this command!");
               return;
           }
           if (!checkValidInput(e.GetArg("username")))
           {
               await e.Channel.SendMessage("Invalid Data.");
               return;
           }
           string banResult = unbanUser(e.GetArg("username"));
           await e.Channel.SendMessage(banResult);
       });
            _client.GetService<CommandService>().CreateCommand("online") //create command greet
       .Description("Lists online players with usernames") //add description, it will be shown when ~help is used
       .Parameter("list", ParameterType.Optional)
       .Do(async e =>
       {
           if (!checkPerms(e.User, "online"))
           {
               await e.Channel.SendMessage("You do not have permission to use this command!");
               return;
           }
           List<string> players = onlinePlayers();
           string playernames = null;
           if (!String.IsNullOrEmpty(e.GetArg("list")) && e.GetArg("list") == "all")
           {
               int i = 0;
               foreach (string player in players)
               {
                   if (i == 0)
                   {
                       playernames = player;
                   }
                   else
                   {
                       playernames = playernames + ", " + player;
                   }
                   i++;
               }
               await e.Channel.SendMessage("Players Online: " + players.Count().ToString() + " at time: " + DateTime.Now + Environment.NewLine + playernames);
           }
           else
           {
               await e.Channel.SendMessage("Players Online: " + players.Count().ToString() + " at time: " + DateTime.Now);
           }
       });
            _client.GetService<CommandService>().CreateCommand("banlist") //create command greet
       .Description("Lists banned player usernames") //add description, it will be shown when ~help is used
       .Do(async e =>
       {
           if (!checkPerms(e.User, "banlist"))
           {
               await e.Channel.SendMessage("You do not have permission to use this command!");
               return;
           }
           List<string> players = bannedPlayers();
           string playernames = null;
               int i = 0;
               foreach (string player in players)
               {
                   if (i == 0)
                   {
                       playernames = player;
                   }
                   else
                   {
                       playernames = playernames + ", " + player;
                   }
                   i++;
               }
               await e.Channel.SendMessage("Total Players Banned: " + players.Count().ToString() + " as of: " + DateTime.Now + Environment.NewLine + playernames);
       });
            _client.GetService<CommandService>().CreateCommand("findalts") //create command greet
.Description("Lists alts of the specified character") //add description, it will be shown when ~help is used
.Parameter("charactername", ParameterType.Required)
.Do(async e =>
{
    if (!checkPerms(e.User, "findalts"))
    {
        await e.Channel.SendMessage("You do not have permission to use this command!");
        return;
    }
    List<string> players = findAlts(e.GetArg("charactername"));
    string playernames = null;
    int i = 0;
    foreach (string player in players)
    {
        if (i == 0)
        {
            playernames = player;
        }
        else
        {
            playernames = playernames + ", " + player;
        }
        i++;
    }
    await e.Channel.SendMessage("Total characters found: " + players.Count().ToString() + " as of: " + DateTime.Now + Environment.NewLine + playernames);
});
            //
            _client.ExecuteAndWait(async () =>
            {
                await _client.Connect(ini.IniReadValue("discord", "bot-token"), TokenType.Bot);
            });
        }

        public void setupConfigBase()
        {
            IniFile inidefault = new IniFile(Directory.GetCurrentDirectory() + @"\config.ini");
            if (String.IsNullOrEmpty(inidefault.IniReadValue("discord", "bot-token")))
            {
                inidefault.IniWriteValue("discord", "bot-token", "enter your bot token here!");
            }
            if (String.IsNullOrEmpty(ini.IniReadValue("botconfig", "allowedgroups")))
            {
                inidefault.IniWriteValue("botconfig", "allowedgroups", "000000000000000001,000000000000000002");
                foreach (string group in inidefault.IniReadValue("botconfig", "allowedgroups").Split(','))
                {
                    inidefault.IniWriteValue("permissions", group, "online,banlist,soaps,checkban,reset2ndary,ban,unban,getaccountid,getloginuser,findalts");
                }
            }
            if (String.IsNullOrEmpty(inidefault.IniReadValue("mssql", "username")))
            {
                inidefault.IniWriteValue("mssql", "username", "sa");
            }
            if (String.IsNullOrEmpty(inidefault.IniReadValue("mssql", "password")))
            {
                inidefault.IniWriteValue("mssql", "password", "yourpasswordhere");
            }
            if (String.IsNullOrEmpty(inidefault.IniReadValue("mssql", "ipandport")))
            {
                inidefault.IniWriteValue("mssql", "ipandport", "127.0.0.1,1433");
            }
        }

        public bool checkPerms(User discordUser, string command)
        {
            var userRoles = discordUser.Roles;
            IniFile ini = new IniFile(Directory.GetCurrentDirectory() + @"\config.ini");
            string allowed = ini.IniReadValue("botconfig", "allowedgroups");
            if (allowed.Split(',').Count() > 0)
            {
                foreach (string cnfuser in allowed.Split(','))
                {
                    ulong userid = 0;
                    ulong.TryParse(cnfuser, out userid);
                    if (userRoles.Any(input => input.Id == userid))
                    {
                        foreach (string userperm in ini.IniReadValue("permissions", userid.ToString()).Split(','))
                        {
                            if (command == userperm)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public string clearSoaps()
        {
            try
            {
                using (conn = new SqlConnection())
                {
                    conn.ConnectionString = "Server=192.151.151.5,1433; Database=heroesShare; User Id=sa; password=2k5Z*JPjL^%LAb_s";
                    string oString = "DELETE FROM ChannelBuff";
                    SqlCommand oCmd = new SqlCommand(oString, conn);
                    conn.Open();
                    oCmd.ExecuteNonQuery();
                    conn.Close();
                    return "Soaps have been cleared!";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string unbanUser(string userName)
        {
            string banResult = null;
            string characterId = getUserIdFromCharacterName(userName);
            string accountName = null;
            if (!string.IsNullOrEmpty(characterId))
            {
                accountName = getAccountNameFromID(characterId);
                if (string.IsNullOrEmpty(accountName))
                {
                    accountName = userName;
                }
            }
            else
            {
                accountName = userName;
            }
            string preBanCheck = isBanned(accountName);
            if (string.IsNullOrEmpty(preBanCheck) || preBanCheck.Contains("is _not_ banned."))
            {
                return "The user " + userName + " is not banned.";
            }
            //INSERT INTO UserBan ([ID], [Status], [ExpireTime], [Reason]) VALUES (N'Gigawiz', '4', '2099-03-25 17:00:00.000', N'Testing');
            try
            {
                using (conn = new SqlConnection())
                {
                    conn.ConnectionString = "Server="+ini.IniReadValue("mssql", "ipandport")+"; Database=heroes; User Id="+ ini.IniReadValue("mssql", "username") + "; password=" + ini.IniReadValue("mssql", "password");
                    string oString = "DELETE FROM UserBan WHERE ID = @fName";
                    SqlCommand oCmd = new SqlCommand(oString, conn);
                    oCmd.Parameters.AddWithValue("@fName", accountName);
                    conn.Open();
                    oCmd.ExecuteNonQuery();
                    conn.Close();
                }
                //"The user " + userName + " was successfully banned.";
                string banStatus = isBanned(accountName);
                if (string.IsNullOrEmpty(banStatus) || banStatus.Contains("is _not_ banned."))
                {
                    banResult = "The user " + userName + " was successfully unbanned.";
                }
                else
                {
                    banResult = "The user " + userName + " was _NOT_ unbanned.";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return banResult;
        }

        public string resetSecondary(string playername)
        {
            string retmsg = "is _not_ banned.";
            string characterName = getUserIdFromCharacterName(playername);
            string accountName = null;
            if (!string.IsNullOrEmpty(characterName))
            {
                accountName = getAccountNameFromID(characterName);
                if (string.IsNullOrEmpty(accountName))
                {
                    accountName = playername;
                }
            }
            else
            {
                accountName = playername;
            }
            try
            {
                using (conn = new SqlConnection())
                {
                    conn.ConnectionString = "Server="+ini.IniReadValue("mssql", "ipandport")+"; Database=heroes; User Id="+ ini.IniReadValue("mssql", "username") + "; password=" + ini.IniReadValue("mssql", "password");
                    string oString = "UPDATE [User] SET SecondPassword=@fSecondary WHERE Name=@fName";
                    SqlCommand oCmd = new SqlCommand(oString, conn);
                    oCmd.Parameters.AddWithValue("@fName", accountName);
                    oCmd.Parameters.AddWithValue("@fSecondary", DBNull.Value);
                    conn.Open();
                    oCmd.ExecuteNonQuery();
                    conn.Close();
                }
                retmsg =  "The secondary password for " + playername + " has been reset.";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return retmsg;
        }

        public bool checkValidInput(string input)
        {
            if (input.All(char.IsLetterOrDigit))
            {
                return true;
            }
            return false;
        }

        public string BanUser(string userName, string banMessage = "You have been permanently banned. Contact staff for additional info.", int duration = 0)
        {
            string banResult = null;
            string characterId = getUserIdFromCharacterName(userName);
            string accountName = null;
            if (!string.IsNullOrEmpty(characterId))
            {
                accountName = getAccountNameFromID(characterId);
                if (string.IsNullOrEmpty(accountName))
                {
                    accountName = userName;
                }
            }
            else
            {
                accountName = userName;
            }
            string preBanCheck = isBanned(accountName);
            if (!string.IsNullOrEmpty(preBanCheck) && preBanCheck.Contains("is banned."))
            {
                return "The user " + userName + " is all ready banned.";
            }
            //INSERT INTO UserBan ([ID], [Status], [ExpireTime], [Reason]) VALUES (N'Gigawiz', '4', '2099-03-25 17:00:00.000', N'Testing');
            try
            {
                using (conn = new SqlConnection())
                {
                    conn.ConnectionString = "Server="+ini.IniReadValue("mssql", "ipandport")+"; Database=heroes; User Id="+ ini.IniReadValue("mssql", "username") + "; password=" + ini.IniReadValue("mssql", "password");
                    string oString = "INSERT INTO UserBan ([ID], [Status], [ExpireTime], [Reason]) VALUES (@fName, '4', '2099-03-25 17:00:00.000', @fReason);";
                    SqlCommand oCmd = new SqlCommand(oString, conn);
                    oCmd.Parameters.AddWithValue("@fName", accountName);
                    oCmd.Parameters.AddWithValue("@fReason", banMessage);
                    conn.Open();
                    oCmd.ExecuteNonQuery();
                    conn.Close();
                }
                //"The user " + userName + " was successfully banned.";
                string banStatus = isBanned(accountName);
                if (!string.IsNullOrEmpty(banStatus) && banStatus.Contains("is banned."))
                {
                    banResult = "The user " + userName + " was successfully banned.";
                }
                else
                {
                    banResult = "The user " + userName + " was _NOT_ banned.";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return banResult;
        }
       
        public string getAccountNameFromID(string accountID)
        {
            string userid = null;
            try
            {
                using (conn = new SqlConnection())
                {
                    conn.ConnectionString = "Server="+ini.IniReadValue("mssql", "ipandport")+"; Database=heroes; User Id="+ ini.IniReadValue("mssql", "username") + "; password=" + ini.IniReadValue("mssql", "password");
                    string oString = "SELECT Name FROM [User] WHERE ID=@fName";
                    SqlCommand oCmd = new SqlCommand(oString, conn);
                    oCmd.Parameters.AddWithValue("@fName", accountID);
                    conn.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            if (!string.IsNullOrEmpty(oReader["Name"].ToString()))
                            {
                                userid = oReader["Name"].ToString();
                            }
                        }
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return userid;
        }
        
        public string getUserIdFromCharacterName(string characterName)
        {
            string userid = null;
            try
            {
                using (conn = new SqlConnection())
                {
                    conn.ConnectionString = "Server="+ini.IniReadValue("mssql", "ipandport")+"; Database=heroes; User Id="+ ini.IniReadValue("mssql", "username") + "; password=" + ini.IniReadValue("mssql", "password");
                    string oString = "Select * from CharacterInfo where Name=@fName";
                    SqlCommand oCmd = new SqlCommand(oString, conn);
                    oCmd.Parameters.AddWithValue("@fName", characterName);
                    conn.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            if (!string.IsNullOrEmpty(oReader["UID"].ToString()))
                            {
                                userid = oReader["UID"].ToString();
                            }
                        }
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return userid;
        }

        public string isBanned(string playername)
        {
            string bannedStatus = "is _not_ banned.";
            string characterName = getUserIdFromCharacterName(playername);
            string accountName = null;
            if (!string.IsNullOrEmpty(characterName))
            {
                accountName = getAccountNameFromID(characterName);
                if (string.IsNullOrEmpty(accountName))
                {
                    accountName = playername;
                }
            }
            else
            {
                accountName = playername;
            }
            try
            {
                using (conn = new SqlConnection())
                {
                    conn.ConnectionString = "Server="+ini.IniReadValue("mssql", "ipandport")+"; Database=heroes; User Id="+ ini.IniReadValue("mssql", "username") + "; password=" + ini.IniReadValue("mssql", "password");
                    string oString = "Select * from UserBan where ID=@fName";
                    SqlCommand oCmd = new SqlCommand(oString, conn);
                    oCmd.Parameters.AddWithValue("@fName", accountName);
                    conn.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            if (!string.IsNullOrEmpty(oReader["ID"].ToString()))
                            {
                                bannedStatus = "is banned." + Environment.NewLine + "Ban Expires on: " + oReader["ExpireTime"].ToString() + Environment.NewLine + "Ban Reason: " + oReader["Reason"].ToString();
                            }
                        }
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return bannedStatus;
        }

        public List<string> onlinePlayers()
        {
            List<string> onlineNames = new List<string>();
            try
            {
                using (conn = new SqlConnection())
                {
                    conn.ConnectionString = "Server=" + ini.IniReadValue("mssql", "ipandport") + "; Database=heroes; User Id=" + ini.IniReadValue("mssql", "username") + "; password=" + ini.IniReadValue("mssql", "password");
                    string oString = "SELECT * FROM CharacterInfo WHERE IsConnected=1 ORDER BY Name ASC";
                    SqlCommand oCmd = new SqlCommand(oString, conn);
                    conn.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            onlineNames.Add(oReader["Name"].ToString());
                        }
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                onlineNames.Add("Error in query!");
                onlineNames.Add(ex.Message);
            }
            return onlineNames;
        }

        public List<string> findAlts(string charactername)
        {
            List<string> characterNames = new List<string>();
            string submittedCharacterAccID = getUserIdFromCharacterName(charactername);
            if (String.IsNullOrEmpty(submittedCharacterAccID))
            {
                return null;
            }
            try
            {
                using (conn = new SqlConnection())
                {
                    conn.ConnectionString = "Server="+ini.IniReadValue("mssql", "ipandport")+"; Database=heroes; User Id="+ ini.IniReadValue("mssql", "username") + "; password=" + ini.IniReadValue("mssql", "password");
                    string oString = "SELECT * FROM CharacterInfo WHERE UID=@fName";
                    SqlCommand oCmd = new SqlCommand(oString, conn);
                    oCmd.Parameters.AddWithValue("@fName", submittedCharacterAccID);
                    conn.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            characterNames.Add(oReader["Name"].ToString());
                        }
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                characterNames.Add("Error in query!");
                characterNames.Add(ex.Message);
            }
            return characterNames;
        }

        public List<string> bannedPlayers()
        {
            List<string> onlineNames = new List<string>();
            try
            {
                using (conn = new SqlConnection())
                {
                    conn.ConnectionString = "Server="+ini.IniReadValue("mssql", "ipandport")+"; Database=heroes; User Id="+ ini.IniReadValue("mssql", "username") + "; password=" + ini.IniReadValue("mssql", "password");
                    string oString = "SELECT * FROM UserBan";
                    SqlCommand oCmd = new SqlCommand(oString, conn);
                    conn.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            onlineNames.Add(oReader["ID"].ToString());
                        }
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                onlineNames.Add("Error in query!");
                onlineNames.Add(ex.Message);
            }
            return onlineNames;
        }
    }
}
