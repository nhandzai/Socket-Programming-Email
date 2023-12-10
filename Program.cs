using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using MyStmp;
using MyPop3;
using MyMime;
using Config;
class Program
{
    private static bool isRunning = true;
    public static void noop(EmailConfiguration config)
    {
        try
        {

            using (TcpClient client = new TcpClient(config.General.MailServer, config.General.Pop3Port))
            using (NetworkStream networkStream = client.GetStream())
            using (StreamReader reader = new StreamReader(networkStream, Encoding.ASCII))
            using (StreamWriter writer = new StreamWriter(networkStream, Encoding.ASCII) { AutoFlush = true })
            {

                writer.WriteLine("NOOP");
                reader.ReadLine();

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    static void Main()
    {

        try
        {
            IOConfig ioConfig = new IOConfig();
            if (!File.Exists("Myconfig.json")){
                ioConfig.WriteConfig("Myconfig.json");
            }
            EmailConfiguration config = ioConfig.ReadConfig(@"Myconfig.json");

            new Thread(() =>
             {
                 while (isRunning)
                 {
                     int time = config.General.AutoLoad;
                     Receive(config);

                     Thread.Sleep(TimeSpan.FromSeconds(time));
                 }
             }).Start();
            new Thread(() =>
          {
              while (isRunning)
              {
                  noop(config);
                  Thread.Sleep(10000);
              }
          }).Start();

            Menu(config);
            isRunning = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

    }
    static void Send(EmailConfiguration config)
    {
        string to = "", cc = "", bcc = "";
        string subject = "", content = "";
        Console.WriteLine("Enter email information:");
        Console.Write("To: ");
        to = Console.ReadLine();
        Console.Write("CC: ");
        cc = Console.ReadLine();
        Console.Write("BCC: ");
        bcc = Console.ReadLine();
        List<string> lto = new List<string>();
        List<string> lcc = new List<string>();
        List<string> lbcc = new List<string>();
        List<string> pathFile = new List<string>();
        string[] parts = cc.Trim().Split(',');
        foreach (var part in parts)
        {
            if (part != "")
                lcc.Add(part);
        }
        parts = bcc.Trim().Split(',');
        foreach (var part in parts)
        {
            if (part != "")
                lbcc.Add(part);
        }
        parts = to.Trim().Split(',');
        foreach (var part in parts)
        {
            if (part != "")
                lto.Add(part);
        }
        Console.Write("Subject:");
        subject = Console.ReadLine();
        Console.Write("Content:");
        content = Console.ReadLine();
        int choice;
        do
        {
            Console.Write("Attach file (1. Yes, 2. No): ");

            if (int.TryParse(Console.ReadLine(), out choice))
            {
                if (choice != 1 && choice != 2)
                {
                    Console.WriteLine("The option you entered does not match!");
                }
            }
            else
            {
                Console.WriteLine("The option you entered does not match!");
            }

        } while (choice != 1 && choice != 2);
        if (choice == 1)
        {
             int num;
            while(true){
            Console.Write("Enter the number of attachments: ");
             if (int.TryParse(Console.ReadLine(), out num)) {break;}
             else{
                 Console.WriteLine();
                 Console.WriteLine("The option you entered does not match, Re-enter");
             }
            }
            for (int i = 1; i <= num; i++)
            {

                while (true)
                {
                    Console.Write($"Enter path {i}: ");
                    string attachmentPath = Console.ReadLine();
                    if (!Path.IsPathRooted(attachmentPath))
                    {
                        Console.WriteLine($"path does not exist or is invalid! Re-enter the path.");

                    }
                    else
                    {
                        FileInfo fileInfo = new FileInfo(attachmentPath);
                        if (fileInfo.Length <= 3 * 1024 * 1024)
                        {
                            pathFile.Add(attachmentPath);
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Capacity exceeded limit. Unable to send file.");
                            Console.WriteLine("Please re-select the file within the required capacity limit (3MB)");
                        }
                    }
                }
            }
        }
        if (to == "" && cc == "" && bcc == "")
        {
            Console.WriteLine("The message you sent lacks a recipient");
            return;

        }
        MimeMessage mimeMessage = CreateMimeMessage(config.General.Username, lto, lcc, subject, content, pathFile);
        StmpSendEmail emailSender = new StmpSendEmail();
        emailSender.SendEmail(subject, config.General.Username, lto, lcc, lbcc, mimeMessage, config);
    }
    static void Receive(EmailConfiguration config)
    {
        Pop3EmailReader emailReader = new Pop3EmailReader();
        emailReader.ReadAllMessages(config);
    }
    static void ReadMail(EmailConfiguration config)
    {

        Console.WriteLine("List of received emails");
        Console.WriteLine("1. Inbox");
        Console.WriteLine("2. Project");
        Console.WriteLine("3. Important");
        Console.WriteLine("4. Work");
        Console.WriteLine("5. Spam");
        Console.Write("Enter your choice: ");
        int choice;

        if (int.TryParse(Console.ReadLine(), out choice))
        {
            switch (choice)
            {
                case 1:
                    ReadEmail("Inbox");
                    break;

                case 2:
                    ReadEmail("Project");
                    break;

                case 3:
                    ReadEmail("Important");
                    break;

                case 4:
                    ReadEmail("Work");
                    break;

                case 5:
                    ReadEmail("Spam");
                    break;


                default:
                    Console.WriteLine("The option you entered does not match!");
                    break;
            }
        }
        else
        {
            Console.WriteLine("The option you entered does not match!");
        }
    }

    static void ReadEmail(string folder)
    {
        List<Tuple<int, int>> tuples;
        DatabaseMime.InitializeDatabase();
        Console.WriteLine($"Email list in {folder} folder");
        tuples = DatabaseMime.GetEmails(folder);
        int id = 0;
        Console.WriteLine();
        while (true)
        {
            Console.Write("Enter the email you want to read(Enter '0' to  exit): ");
            int read;
            if (int.TryParse(Console.ReadLine(), out read))
            {
                if (read == 0)
                {
                    return;
                }
                if (read <= tuples.Count() && read > 0)
                {
                    DatabaseMime.UpdateData(read);

                    foreach (var tuple in tuples)
                    {
                        if (tuple.Item1 == read)
                        {
                            id = tuple.Item2;
                            break;

                        }
                    }
                    DatabaseMime.PrintEmailDetails(id);
                    Console.WriteLine();

                }
                else
                {
                    Console.WriteLine("The option you entered does not match!");
                }
            }
            else Console.WriteLine("The option you entered does not match!");

            while (DatabaseMime.CountAttachmentsForMessage(id) > 0)
            {
                Console.Write("Save File(1.Yes/2.No): ");
                int save;
                if (int.TryParse(Console.ReadLine(), out save))
                {
                    if (save == 2)
                    {
                        return;
                    }
                    if (save == 1)
                    {
                        Console.WriteLine("Enter path ");
                        string path = "";
                        path = Console.ReadLine();
                        DatabaseMime.SaveAttachmentsFromDatabase(id, path);
                        Console.WriteLine("saved file");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("The option you entered does not match!");
                    }
                }
                else Console.WriteLine("The option you entered does not match!");

            }

            Console.WriteLine("Press enter to exit)");
            Console.ReadLine();
            return;
        }
    }
    static void Menu(EmailConfiguration config)
    {
        while (true)
        {
            Console.WriteLine("Email Menu: ");
            Console.WriteLine("1. Send Email");
            Console.WriteLine("2. List of received Emails");
            Console.WriteLine("0. Exit");
            Console.Write("Enter your choice: ");

            int choice;
            if (int.TryParse(Console.ReadLine(), out choice))
            {
                switch (choice)
                {
                    case 1:
                        Send(config);
                        break;

                    case 2:
                        ReadMail(config);
                        break;

                    case 0:
                        Console.WriteLine("Exit.");
                        return;
                    default:
                        Console.WriteLine("Your selection is out of range!");
                        break;
                }
            }
            else
            {
                Console.WriteLine("The option you entered does not match!");
            }
        }
    }
    static MimeMessage CreateMimeMessage(string from, List<string> to, List<string> cc, string subject, string body, List<string> pathFile)
    {
        MimeMessage mimeMessage = new MimeMessage();
        mimeMessage.From = from;
        foreach (string i in to)
        {
            mimeMessage.To.Addresses.Add(i);
        }
        foreach (string i in cc)
        {
            mimeMessage.CC.Addresses.Add(i);
        }
        mimeMessage.Subject = subject;
        mimeMessage.TextBody(body);
        foreach (string i in pathFile)
        {
            mimeMessage.AttachFile(i);
        }
        return mimeMessage;
    }
}