using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
//using Newtonsoft.Json;
using System.Text.Json;
using MyMime;
using Config;

namespace MyPop3
{
    public class Pop3EmailReader
    {
        public void ReadAllMessages(EmailConfiguration config)
        {
            try
            {
                using (TcpClient client = new TcpClient(config.General.MailServer, config.General.Pop3Port))
                using (NetworkStream networkStream = client.GetStream())
                using (StreamReader reader = new StreamReader(networkStream, Encoding.ASCII))
                using (StreamWriter writer = new StreamWriter(networkStream, Encoding.ASCII) { AutoFlush = true })
                {
                    // Read the server greeting
                    reader.ReadLine();

                    // Send USER and PASS commands
                    string from = config.General.Username;
                    string mailAddress = from.Substring(from.IndexOf('<') + 1, from.IndexOf('>') - from.IndexOf('<') - 1).Trim();

                    SendCommand(writer, $"USER {mailAddress}");
                    ReadResponse(reader);
                    SendCommand(writer, $"PASS {config.General.Password}");


                    SendCommand(writer, "LIST");
                    ReadAllResponse(networkStream);
                    // Send the STAT command
                    SendCommand(writer, "STAT");
                    string statResponse = ReadResponse(reader);
                    string[] statParts = statResponse.Split(" ");
                    String line;
                    DatabaseMime.InitializeDatabase();

                    if (statParts.Length >= 1 && int.TryParse(statParts[1], out int totalMessages))
                    {
                        // Retrieve each email
                        for (int messageNumber = DatabaseMime.CountEmail() + 1; messageNumber <= totalMessages; messageNumber++)
                        {
                            MimeMessage mimeMessage = new MimeMessage();
                            FormatOptions option = new FormatOptions();
                            SendCommand(writer, $"RETR {messageNumber}");
                            ReadResponse(reader);
                            bool isRunning = true;
                           
                            while ((line = reader.ReadLine()) != ".")
                            {

                                if (line.StartsWith("From:"))
                                {
                                    mimeMessage.From = line.Substring("From:".Length + 1).Trim();
                                }
                                if (line.StartsWith("To:"))
                                {
                                    mimeMessage.From = line.Substring("To:".Length + 1).Trim();
                                }
                                else if (line.StartsWith("Cc:"))
                                {
                                    string tmp = line.Substring("Cc:".Length + 1).Trim();
                                    string[] parts = tmp.Split(',');
                                    foreach (var part in parts)
                                    {
                                        mimeMessage.CC.Addresses.Add(part);
                                    }
                                }
                                else if (line.StartsWith("Subject:"))
                                {
                                    mimeMessage.Subject = line.Substring("Subject:".Length + 1).Trim();
                                    break;
                                }
                                else if (line.StartsWith("Content-Type:"))
                                {
                                    string tmp = line.Trim();
                                    string[] parts = tmp.Split(';');
                                    foreach (var part in parts)
                                    {
                                        if (part.Trim().StartsWith("boundary="))
                                        {
                                            option.Boundary = part.Substring("boundary=".Length + 1).Trim().Trim('\"');
                                        }
                                        if (part.Trim().StartsWith("Content-Type:"))
                                        {
                                            mimeMessage.ContentType = part.Substring("Content-Type:".Length + 1).Trim();
                                        }
                                    }
                                }
                            }
                            int index = 0;
                            
                            while (line != ".")
                            {


                                var attachFile = new MimePart();
                                var textPart = new TextPart();
                                StringBuilder body_ = new StringBuilder();
                                int choice = 0;

                                using (MemoryStream memoryStream = new MemoryStream())
                                {
                                    writer.Flush();
                                    while ((line = reader.ReadLine()) != ".")
                                    {

                                        if (line.Trim() == "This is a multi-part message in MIME format.") { }
                                        else if(line.StartsWith($"To:")){}
                                        else if (line.StartsWith($"--{option.Boundary}"))
                                        {
                                            if (index == 0) index++;
                                            else break;
                                        }
                                        else if (line.StartsWith($" filename="))
                                        {
                                            attachFile.ContentType.Parameters.Filename = line.Substring(" filename=".Length + 1).Trim().Trim('\"');
                                        }
                                        else if (line.StartsWith($" name=")){}
                                        else if (line.StartsWith("Content-Type:"))
                                        {
                                            string tmp = line.Trim();
                                            string[] parts = tmp.Split(';');
                                            foreach (var part in parts)
                                            {
                                                if (part.Trim().StartsWith("name="))
                                                {
                                                    attachFile.ContentType.Parameters.Filename = part.Substring("name=".Length + 1).Trim().Trim('\"');

                                                }
                                                if (part.Trim().StartsWith("charset="))
                                                {
                                                    attachFile.ContentType.Parameters.Charset = part.Substring("filename=".Length + 1).Trim();

                                                }
                                                if (part.Trim().StartsWith("format="))
                                                {
                                                    textPart.ContentType.Parameters.Format = part.Substring("format=".Length + 1).Trim();
                                                    choice = 1;
                                                    textPart.trans(attachFile.ContentType.MediaType, attachFile.ContentType.Parameters.Charset);
                                                }
                                                if (part.Trim().StartsWith("Content-Type:"))
                                                {
                                                    attachFile.ContentType.MediaType = part.Substring("Content-Type:".Length + 1).Trim();

                                                }
                                            }
                                        }
                                        else if (line.StartsWith("Content-Disposition:"))
                                        {
                                            string tmp = line.Trim();
                                            string[] parts = tmp.Split(';');

                                            foreach (var part in parts)
                                            {
                                                if (part.Trim().StartsWith("Content-Disposition:"))
                                                {
                                                    attachFile.ContentType.Parameters.Disposition = part.Substring("Content-Disposition:".Length + 1).Trim();
                                                }
                                            }

                                        }
                                        else if (line.StartsWith("Content-Transfer-Encoding:"))
                                        {
                                            if (choice == 1)
                                            {
                                                textPart.ContentType.Parameters.TransferEncoding = line.Substring("Content-Tranfer-Encoding:".Length + 1).Trim();
                                            }
                                            if (choice == 0)
                                            {
                                                attachFile.ContentType.Parameters.TransferEncoding = line.Substring("Content-Tranfer-Encoding:".Length + 1).Trim();
                                            }
                                        }
                                        else if (line != "" && choice == 0)
                                        {
                                            byte[] lineBytes = Convert.FromBase64String(line);
                                            memoryStream.Write(lineBytes, 0, lineBytes.Length);
                                        }
                                        else if (line != "" && choice == 1)
                                        {
                                            body_.AppendLine(line);
                                        }
                                    }
                                    if (choice == 0)
                                    {
                                        memoryStream.Seek(0, SeekOrigin.Begin);
                                        memoryStream.CopyTo(attachFile.Content);
                                    }

                                    if (choice == 1)
                                    {
                                        textPart.Text = body_.ToString();
                                    }
                                }

                                if (choice == 0 && attachFile.ContentType.MediaType != null)
                                {

                                    mimeMessage.Attachments.Add(attachFile);
                                }
                                if (choice == 1) mimeMessage.Body = textPart;
                            }

                            DatabaseMime.InsertData(mimeMessage, messageNumber, checkFolder(mimeMessage, config));

                            
                        }   
                    }
                    SendCommand(writer, "QUIT");
                    ReadResponse(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error1: {ex.Message}");
            }
        }
        
        private static String checkFolder(MimeMessage mimeMessage, EmailConfiguration config)
        {


            foreach (var x in config.Filters)
            {


                if (x.Criteria.From != null)
                {

                    foreach (var y in x.Criteria.From)
                    {
                        if (mimeMessage.From.Trim() == y.Trim())
                        {

                            return x.Folder;
                        }
                    }
                }
                if (x.Criteria.Subject != null)
                {

                    foreach (var z in x.Criteria.Subject)
                    {

                        if (mimeMessage.Subject.Trim().ToLower().Contains(z.Trim().ToLower()))
                        {

                            return x.Folder;
                        }
                    }
                }
                if (x.Criteria.Body != null)
                {

                    foreach (var t in x.Criteria.Body)
                    {

                        if (mimeMessage.Body.Text.Trim().ToLower().Contains(t.Trim().ToLower()))
                        {

                            return x.Folder;
                        }
                    }
                }

            }

            return "Inbox";
        }
        private void SendCommand(StreamWriter writer, string command)
        {
            //Console.WriteLine($"Client: {command}");
            writer.WriteLine(command);
        }

        private string ReadResponse(StreamReader reader)
        {
            string response = reader.ReadLine();
            //Console.WriteLine($"Server: {response}");
            return response;
        }
        public static void ReadAllResponse(NetworkStream networkStream)
        {
            StreamReader reader = new StreamReader(networkStream, Encoding.ASCII);
            while (true)
            {
                string response = reader.ReadLine();
                if (response == ".")
                {
                    break;
                }
            }
        }
    }
}