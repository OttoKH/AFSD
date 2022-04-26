using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AFSD
{
    internal class ApplicationSettings
    {
        private char delimiter = '~';
        private string SettingsFilename = AppDomain.CurrentDomain.BaseDirectory + "settings.cfg";
        public int DisplayOffTimer;
        public string ftpHost;
        public string ftpPort;
        public string ftpPath;
        public string ftpPathHTTP;
        public string ftpUsername;
        public string ftpPassword;
        public string twilioSID;
        public string twilioTOKEN;
        public string twilioNUM;
        public string twilioUSER;
        public string twilioPASS;

        public ApplicationSettings()
        {
            // Initialize default settings values
            DisplayOffTimer = 120;

            // FTP default settings
            ftpHost = "localhost";
            ftpPath = "/";
            ftpPathHTTP = "/";
            ftpPort = "21";
            ftpUsername = "username";
            ftpPassword = "password";

            // Twilio placeholder values
            twilioSID = "TWILIO_ACCOUNT_SID";
            twilioTOKEN = "TWILIO_AUTH_TOKEN";
            twilioNUM = "TWILIO_PHONE_NUMBER";
            twilioUSER = "TWILIO_USERNAME";
            twilioPASS = "TWILIO_PASSWORD";

        }
        public int SaveSettingsFile()
        {
            int returnCode = 0;
            try
            {
                using (StreamWriter writer = File.CreateText(SettingsFilename))
                {
                    
                    writer.WriteLine("DisplayOffTimer"+delimiter+DisplayOffTimer.ToString());
                    writer.WriteLine("ftpHost" + delimiter + ftpHost.ToString());
                    writer.WriteLine("ftpPath" + delimiter + ftpPath.ToString());
                    writer.WriteLine("ftpPathHTTP" + delimiter + ftpPathHTTP.ToString());
                    writer.WriteLine("ftpPort" + delimiter + ftpPort.ToString());
                    writer.WriteLine("ftpUsername" + delimiter + ftpUsername.ToString());
                    writer.WriteLine("ftpPassword" + delimiter + ftpPassword.ToString());
                    writer.WriteLine("twilioSID" + delimiter + twilioSID.ToString());
                    writer.WriteLine("twilioTOKEN" + delimiter + twilioTOKEN.ToString());
                    writer.WriteLine("twilioNUM" + delimiter + twilioNUM.ToString());
                    writer.WriteLine("twilioUSER" + delimiter + twilioUSER.ToString());
                    writer.WriteLine("twilioPASS" + delimiter + twilioPASS.ToString());

                }
            }
            catch (Exception exp)
            {
                Console.Write(exp.Message);
            }
            return returnCode;
        }
        public int LoadSettingsFile()
        {

            if (!File.Exists(SettingsFilename))
            {
                SaveSettingsFile();
            }

            int returnCode = 0;
            try
            {
                using (StreamReader reader = File.OpenText(SettingsFilename))
                {
                    while (!reader.EndOfStream) 
                    {
                        string line = reader.ReadLine();
                        if (line != null && line != "")
                        {
                            string[] values = line.Split(delimiter);
                            if (values.Length == 2)
                            {
                                // Assign values to settings
                                if (values[0].Equals("DisplayOffTimer"))
                                {
                                    this.DisplayOffTimer = int.Parse(values[1]);
                                }
                                else if (values[0].Equals("ftpHost"))
                                {
                                    this.ftpHost = values[1]; 
                                }
                                else if (values[0].Equals("ftpPath"))
                                {
                                    this.ftpPath = values[1];
                                }
                                else if (values[0].Equals("ftpPathHTTP"))
                                {
                                    this.ftpPathHTTP = values[1];
                                }
                                else if (values[0].Equals("ftpPort"))
                                {
                                    this.ftpPort = values[1];
                                }
                                else if (values[0].Equals("ftpUsername"))
                                {
                                    this.ftpUsername = values[1];
                                }
                                else if (values[0].Equals("ftpPassword"))
                                {
                                    this.ftpPassword = values[1];
                                }
                                else if (values[0].Equals("twilioSID"))
                                {
                                    this.twilioSID = values[1];
                                }
                                else if (values[0].Equals("twilioTOKEN"))
                                {
                                    this.twilioTOKEN = values[1];
                                }
                                else if (values[0].Equals("twilioNUM"))
                                {
                                    this.twilioNUM = values[1];
                                }
                                else if (values[0].Equals("twilioUSER"))
                                {
                                    this.twilioUSER = values[1];
                                }
                                else if (values[0].Equals("twilioPASS"))
                                {
                                    this.twilioPASS = values[1];
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error. length of line not valid: " + line.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                Console.Write(exp.Message);
            }
            return returnCode;
        }
    }
}
