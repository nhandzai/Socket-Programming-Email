using System.Text.Json.Serialization;
using System.Text.Json;
namespace Config
{
    public class General
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string MailServer { get; set; }
        public int SmtpPort { get; set; }
        public int Pop3Port { get; set; }
        public int AutoLoad { get; set; }

        // Constructor
        public General(string username, string password, string mailServer, int smtpPort, int pop3Port, int autoLoad)
        {
            this.Username = username;
            this.Password = password;
            this.MailServer = mailServer;
            this.SmtpPort = smtpPort;
            this.Pop3Port = pop3Port;
            this.AutoLoad = autoLoad;

        }
    }

    public class Criteria
    {
        public string[] From { get; set; }
        public string[] Subject { get; set; }
        public string[] Body { get; set; }
    }

    public class Filter
    {
        public string Folder { get; set; }
        public Criteria Criteria { get; set; }
    }
    public class EmailConfiguration
    {
        public General General { get; set; }
        public Filter[] Filters { get; set; }
      

       
        public static EmailConfiguration FromJsonFile(string filePath)
        {
            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<EmailConfiguration>(jsonString);
        }
    }
    public class IOConfig
    {
        public void WriteConfig(string configFile)
        {
            EmailConfiguration emailConfig = new EmailConfiguration
            {
                General = new General(@"HoangKha <phamhoangkha21@gmail.com>", "123", "127.0.0.1", 2225, 3335, 10),
                Filters = new[]
               {
                   new Filter
        {
            Folder = "Project",
            Criteria = new Criteria
            {
                From = new[] { "ahihi@testing.com", "ahuu@testing.com" }
            }
        },
        new Filter
        {
            Folder = "Important",
            Criteria = new Criteria
            {
                Subject = new[] { "urgent", "ASAP" }
            }
        },
        new Filter
        {
            Folder = "Work",
            Criteria = new Criteria
            {
                Body = new[] { "report", "meeting" }
            }
        },
        new Filter
        {
            Folder = "Spam",
            Criteria = new Criteria
            {
                Subject = new[] { "virus", "hack", "crack" },
                Body = new[] { "virus", "hack", "crack" }
            }
        },

    },
             
            };

            // Chuyển đổi dữ liệu thành chuỗi JSON
            string jsonString = JsonSerializer.Serialize(emailConfig, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull

            });

            // Ghi chuỗi JSON vào một file
            File.AppendAllText("Myconfig.json", jsonString);


        }
        public EmailConfiguration ReadConfig(string configFile)
        {
            try
            {
                // Replace with the actual path to your JSON file
                string filePath = configFile;

                // Deserialize the JSON string into a C# object
                EmailConfiguration emailConfig = EmailConfiguration.FromJsonFile(filePath);
                return emailConfig;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

    }
}