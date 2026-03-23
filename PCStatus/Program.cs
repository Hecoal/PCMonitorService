using System.Security.Cryptography.X509Certificates;
using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.Data.SqlClient;
using System.Xml.Linq;

namespace PCStatus
{
    internal class Program
    {

       /*
        * Function to read the XML file and read each XML Property
        */
        static (string Buliding, string Line) ReadConfigurationFromXml()
        {
            //Make sure that the .xml is in the same location as the .exe, and must have the name 'config.xml'
            string xmlPath = "config.xml";

            if (!File.Exists(xmlPath))
            {
                throw new FileNotFoundException($"XML file not found at path: {xmlPath}");
            }

            XDocument document = XDocument.Load(xmlPath);

            XElement? root = document.Root;
            if (root == null)
            {
                throw new Exception("The XML file does not contain a root element.");
            }

            //Here add every element from the XML!!!
            //For example, here i manage only 'Building' and 'Line', but you can add as much as you want!
            string buliding = root.Element("buliding")?.Value?.Trim()
                ?? throw new Exception("The 'buliding' node was not found in the XML file.");

            string line = root.Element("line")?.Value?.Trim()
                ?? throw new Exception("The 'line' node was not found in the XML file.");

            return (buliding, line);
        }
        /*
         * The following function creates the connection string for MSSQL for gloabally use.
         */
        public static string ConnectionSql()
        {
            /*
             * As per development pruposes, I'm using the second connection string, because i don't want to use passwords
             * for real cases, just uncomment the first connectionString' and comment the second one.
             */

            //string connectionString = "Server=HECOAL;Database=Development_DB;User Id=your_username;Password=your_password;";
             string connectionString = "Server = HECOAL; Database = Development_DB; Trusted_Connection=True; TrustServerCertificate = True";

            return connectionString;
        }
        /*
         * The following function inserts a NEW pc to the database. Just in case theres not a registry made before
         * This function runs ONLY the first time!
         */
        public static bool InsertPCToDB()
        {
            //We call the XML Function, to save info directly to SQL. Dani, Make sure to add every element from the XML
            try
            {
                var (buliding, line) = ReadConfigurationFromXml();
                //We create a new SQL Connection Instance
                using (SqlConnection connection = new SqlConnection(ConnectionSql()))
                {
                    //We open the sql connection Just don't forget to close it each time you open it!!!
                    connection.Open();

                    //First we check if the PC is already on the db
                    //We pass the sql query. Make sure to add the rest of the XML elements, in this case 2,3

                    //                                     0          1          2      3
                    string query = "INSERT INTO PCStatus (pc_Name, ip_address, line, building) VALUES (@pc_Name, @ip_address, @line, @building)";

                    //We pass each parameter
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@pc_Name", GetPCName());
                        command.Parameters.AddWithValue("@ip_address", GetIPAddress());
                        command.Parameters.AddWithValue("@line", line);
                        command.Parameters.AddWithValue("@building", buliding);

                        //We try to run the query
                        try
                        {
                            int affected_rows = command.ExecuteNonQuery();

                            //Everything ran successfully!
                            if (affected_rows > 0)
                            {
                                //[Test line] Uncomment for testing pruposes
                                //Console.WriteLine("Data added successfully");
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
            }
            catch (Exception e) 
            {
                Console.WriteLine($"Error: {e.Message}");
            }
            return false;
        }
        /*
         * The following function updates an already existing registry of a pc in the database.
         * We can call this function each time we want
         */
        public static bool UpdatePCToDB()
        {
            //We call the XML Function, to save info directly to SQL. Dani, Make sure to add every element from the XML
            try
            {
                var (buliding, line) = ReadConfigurationFromXml();
                //We create a new SQL Connection Instance
                using (SqlConnection connection = new SqlConnection(ConnectionSql()))
                {
                    //We open the sql connection Just don't forget to close it each time you open it!!!
                    connection.Open();

                    //We pass the sql query, to update the current pc_name, and update XML info
                    string query = "UPDATE PCStatus " +
                        "SET ip_address= @ip_address, " +
                        "last_ping=@last_ping, " +
                        "line=@line, " +
                        "building=@building " +
                        "WHERE pc_Name = @pc_Name;";

                    //We pass each parameter
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@pc_Name", GetPCName());
                        command.Parameters.AddWithValue("@ip_address", GetIPAddress());
                        command.Parameters.AddWithValue("@line", line);
                        command.Parameters.AddWithValue("@building", buliding);

                        //Time zone is set as UTC, but you can change it to 'DateTime.Now'
                        command.Parameters.AddWithValue("@last_ping", DateTime.UtcNow);

                        try
                        {
                            int affected_rows = command.ExecuteNonQuery();

                            //We check if the command ran successfully
                            if (affected_rows > 0)
                            {
                                //[TESTING LINE] Uncomment the following line to test the result
                                //Console.WriteLine("UPDATE !Data added successfully");
                                return true;
                                
                            }else if (affected_rows == 0)
                            {
                                return InsertPCToDB();
                            }
                        }             
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
            }
            catch (Exception e) 
            {
                Console.WriteLine(e.ToString());                
            }
            return false;
        }
        /*
        * The following function, saves the PC Name in the variable 'pcName'
        * And returns it as a String
        * We don't need aditional parameters
        */
        public static string GetPCName()
        {

            string pcName = Environment.MachineName;

            /*
             * Uncomment the following line to test the PCName
             */

            //Console.WriteLine("PC Name: " + pcName);
            
            return pcName;
        }

        /*
        * The following function, saves the IP Adress on the 'ip' variable
        * And returns it as a String
        * We don't need aditional parameters
        */
        public static string GetIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    /*
                     * Uncomment the following line to test ip_address
                     */

                    //Console.WriteLine("IP Address: " + ip.ToString());

                    return ip.ToString();
                }
            }

            /*
             * Instead of null, you can return any string value, when it is not possible to fetch the IP
             */
            return null;
        }
        /*
         * Runs UpdatePCToDB function every x minutes
         */
        static async Task RunUpdateLoopAsync()
        {
            //Here you can swap the '1' to any minute you want, dani!!
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

            while (await timer.WaitForNextTickAsync())
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Running UpdatePCToDB...");
                UpdatePCToDB();
            }
        }
        /*
         * Main function, here we check if the pc registry already exists, 
         * if it doesn't we create a new one, and if it does, we update it.
         */
        static async Task Main(string[] args)
        {
            UpdatePCToDB();

            //Function that runs the update every x minutes, just to keep the registry alive and updated
            await RunUpdateLoopAsync();
        }
    }
}
