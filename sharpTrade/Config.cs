
// an extract from sharpBW :)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace sharpTrade
{
    class Config
    {
        public static readonly String CONF_FILENAME = "sharpTrade.conf";

        private static Config instance = null;
        public static Config Instance           { get { return instance; } }


        // init() will load/reload conf file (or will load defaults and try to save conf if something goes wrong loading). multiple calls are permitted
        public static void init()
        {
            instance = new Config();
            if (!instance.load())        // if load fails
            {
//                instance.save();         // try to save the file... bad idea, no need to save an object we can recreate anytime
            }
        }

        // use the following 2funcs to: obtain a working copy of the Config.Instance object, (modify with setters), Commit the updated Config
        public static Config Clone()            { return new Config(instance); }
        public static void Commit(Config conf)  { instance = conf; }




        
        private Config()
        {
            incomingdirectory = "./files_csv/";
            username = "username";
            password = "password";

            timeout_connect = 7500;
            timeout_read = 5500;
            max_threads = 1;
        }



        private Config(Config conf)
        {
            incomingdirectory = conf.incomingdirectory;
            username = conf.username;
            password = conf.password;

            timeout_connect = conf.timeout_connect;
            timeout_read = conf.timeout_read;
            max_threads = conf.max_threads;
        }


        public bool load()  { return load(CONF_FILENAME); }
        public bool save()  { return save(CONF_FILENAME); }

        

        private bool load(String filename)
        {
            try
            {
                StreamReader SR = new StreamReader(filename);
                String s;

                while ((s = SR.ReadLine()) != null)
                {
                    s = s.Trim();
                    
                    try
                    {
                        if (s.StartsWith("<incomingdirectory>"))
                            IncomingDirectory = s.Replace("<incomingdirectory>", "").Replace("</incomingdirectory>", "").Trim();
                        else if (s.StartsWith("<username>"))
                            Username = s.Replace("<username>", "").Replace("</username>", "").Trim();
                        else if (s.StartsWith("<password>"))
                            Password = s.Replace("<password>", "").Replace("</password>", "").Trim();

                        else if (s.StartsWith("<timeout_connect>"))
                            TimeoutConnect = Int32.Parse(s.Replace("<timeout_connect>", "").Replace("</timeout_connect>", "").Trim());
                        else if (s.StartsWith("<timeout_read>"))
                            TimeoutRead = Int32.Parse(s.Replace("<timeout_read>", "").Replace("</timeout_read>", "").Trim());
                        else if (s.StartsWith("<max_threads>"))
                            MaxThreads = Int32.Parse(s.Replace("<max_threads>", "").Replace("</max_threads>", "").Trim());
                    }
                    catch (FormatException) { }
                }

                SR.Close();

            }
            catch (IOException) { return false; }

            return true;
        }



        private bool save(String filename)
        {
            try
            {
                StreamWriter SW = new StreamWriter(filename, false);

                SW.WriteLine("<sharpTrade_config>");
                
                SW.WriteLine("\t<incomingdirectory>" + IncomingDirectory + "</incomingdirectory>");
                if (Username.Length > 0) SW.WriteLine("\t<username>" + Username + "</username>");
                if (Password.Length > 0) SW.WriteLine("\t<password>" + Password + "</password>");
                SW.WriteLine("\t<max_threads>" + MaxThreads + "</max_threads>");
                SW.WriteLine("\t<timeout_connect>" + TimeoutConnect + "</timeout_connect>");
                SW.WriteLine("\t<timeout_read>" + TimeoutRead + "</timeout_read>");

                SW.WriteLine("</sharpTrade_config>");

                SW.Close();
            }
            catch (IOException) { return false; }

            return true;
        }




        // Class variables
        private String incomingdirectory;
        public String IncomingDirectory
        {
            get { return incomingdirectory; }
            set { incomingdirectory = value; }
        }

        private int timeout_connect;
        public int TimeoutConnect
        {
            get { return timeout_connect; }
            set { timeout_connect = value; }
        }

        private int timeout_read;
        public int TimeoutRead
        {
            get { return timeout_read; }
            set { timeout_read = value; }
        }


        private int max_threads;
        public int MaxThreads
        {
            get { return max_threads; }
            set { max_threads = value; }
        }


        private String username;
        public String Username
        {
            get { return username; }
            set { username = value; }
        }

        private String password;
        public String Password
        {
            get { return password; }
            set { password = value; }
        }
    }
}
