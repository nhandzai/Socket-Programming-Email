// MyStmp.cs
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using MyMime;
using System.Text.Json;
using Config;

namespace MyStmp
{
    public class Stmp
    {
        public static void SendCommand(StreamWriter writer, string command)
        {
            //Console.WriteLine($"Client: {command}");
            writer.WriteLine(command);
        }

        public static void ReadResponse(NetworkStream networkStream)
        {
            StreamReader reader = new StreamReader(networkStream, Encoding.ASCII);
            string response = reader.ReadLine();
            //Console.WriteLine($"Server: {response}");
        }
    }
    public class StmpSendEmail
    {
        public void SendEmail(string subject, string from, List<string> to, List<string> cc, List<string> bcc, MimeMessage mimeMessage, EmailConfiguration config)
        {
            using (TcpClient tcpClient = new TcpClient(config.General.MailServer, config.General.SmtpPort))
            using (NetworkStream networkStream = tcpClient.GetStream())
            using (StreamWriter writer = new StreamWriter(networkStream, Encoding.ASCII) { AutoFlush = true })
            {
                Stmp.ReadResponse(networkStream);
                Stmp.SendCommand(writer, $"HELO {config.General.MailServer}");
                Stmp.ReadResponse(networkStream);

                string mailAddress = from.Substring(from.IndexOf('<') + 1, from.IndexOf('>') - from.IndexOf('<') - 1).Trim();
                Stmp.SendCommand(writer, $"MAIL FROM: <{mailAddress}>");
                Stmp.ReadResponse(networkStream);
                if (to.Count > 0)
                {
                    foreach (string i in to)
                    {
                        string tempCC = i;
                        Stmp.SendCommand(writer, $"RCPT TO: <{tempCC}>");
                        Stmp.ReadResponse(networkStream);
                    }
                }
                if (to.Count > 0)
                {
                    foreach (string i in cc)
                    {
                        string tempCC = i;
                        Stmp.SendCommand(writer, $"RCPT TO: <{tempCC}>");
                        Stmp.ReadResponse(networkStream);
                    }
                }
                if (to.Count > 0)
                {
                    foreach (string i in bcc)
                    {
                        string tempCC = i;
                        Stmp.SendCommand(writer, $"RCPT TO: <{tempCC}>");
                        Stmp.ReadResponse(networkStream);
                    }
                }

                Stmp.SendCommand(writer, "DATA");
                Stmp.ReadResponse(networkStream);

                mimeMessage.WriteTo(networkStream);

                Stmp.SendCommand(writer, ".");
                Stmp.ReadResponse(networkStream);

                Stmp.SendCommand(writer, "QUIT");
                Stmp.ReadResponse(networkStream);

                Console.WriteLine("Email sent successfully!");
            }
        }
    }
}