using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
namespace MyMime
{
    public class MimeMessage
    {
        public MimeMessage()
        {
            Headers = new HeaderList();
            Body = new TextPart("text/plain");
            Attachments = new List<MimePart>();
            ContentType = null;
            From = null;
            To = new AddressList();
            CC = new AddressList();
        }

        public HeaderList Headers { get; set; }
        public TextPart Body { get; set; }
        public List<MimePart> Attachments { get; set; }
        public string ContentType { get; set; }
        public string From { get; set; }
        public AddressList To { get; set; }
        public AddressList CC { get; set; }
        public string Subject { get; set; }
        public void TextBody(string text)
        {
            var textpart = new TextPart("text/plain")
            {
                ContentType = { Parameters = { Charset = "UTF8", TransferEncoding = "7bit", Format = "flowed", } },
                Text = text
            };
            Body = textpart;
        }
        public void AttachFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(filePath).ToLower();
            var mimeType = MimeTypeMapper.GetMimeType(extension);
            var attachment = new MimePart(mimeType)
            {
                ContentType = { Parameters = { Filename = fileName, Disposition = "attachment", TransferEncoding = "Base64" } },
                Content = new MemoryStream(File.ReadAllBytes(filePath))
            };
            if (extension == ".txt")
            {
                attachment.ContentType.Parameters.Charset = "UTF8";
            }

            Attachments.Add(attachment);
        }
        public void WriteTo(Stream stream)
        {
            using (var filtered = new FilteredStream(stream))
            {
                filtered.Add(EncoderFilter.Create(Encoding.UTF8));

                var options = FormatOptions.Default.Clone();
                options.NewLineFormat = NewLineFormat.Dos;
                options.Boundary = "------------" + Guid.NewGuid().ToString("N");

                using (var filteredStream = new FilteredStream(filtered))
                {
                    filteredStream.Add(EncoderFilter.Create(Encoding.UTF8));

                    MimeWriter writer = MimeWriter.Create(filteredStream, options);

                    Headers.Headers.Add(new Header { Field = "Message-ID", Value = $"<{Guid.NewGuid()}@gmail.com>" });

                    Headers.Headers.Add(new Header { Field = "Date", Value = DateTime.Now.ToString("R") });

                    Headers.Headers.Add(new Header { Field = "MIME-Version", Value = "1.0" });

                    Headers.Headers.Add(new Header { Field = "User-Agent", Value = "Mozila Thunderbird" });

                    Headers.Headers.Add(new Header { Field = "Content-Language", Value = "en-US" });
                      if (To.Addresses.Count != 0)
                    {
                        int a = 0;
                        String Address = null;
                        foreach (string x in To.Addresses)
                        {
                            if (a == 0)
                            {
                                Address += x;
                            }
                            else Address += $",{x}";
                            a++;
                        }
                        Headers.Headers.Add(new Header { Field = "To", Value = Address });
                    }
                    if (CC.Addresses.Count != 0)
                    {
                        int a = 0;
                        String Address = null;
                        foreach (string x in CC.Addresses)
                        {
                            if (a == 0)
                            {
                                Address += x;
                            }
                            else Address += $",{x}";
                            a++;
                        }
                        Headers.Headers.Add(new Header { Field = "Cc", Value = Address });
                    }
                    if (!string.IsNullOrEmpty(From))
                    {
                        Headers.Headers.Add(new Header { Field = "From", Value = From });
                    }
                    if (!string.IsNullOrEmpty(Subject))
                    {
                        Headers.Headers.Add(new Header { Field = "Subject", Value = Subject });
                    }
                    if (Attachments.Count != 0)
                    {
                        ContentType = "multipart/mixed";
                    }
                    writer.WriteMessage(Headers, Body, Attachments, ContentType, options);

                    writer.Flush();
                }
            }
        }
    }
    public class AddressList
    {
        public List<string> Addresses { get; set; }

        public AddressList()
        {
            Addresses = new List<string>();
        }
    }
    public class TextPart : MimePart
    {
        public TextPart(string mimeType) : base(mimeType) { }
        public TextPart() : base() { }
        public void trans(string a, string b)
        {
            ContentType.MediaType = a;
            ContentType.Parameters.Charset = b;

        }

        public string Text { get; set; }


    }

    public class MimePart
    {
        public MimePart(string mimeType)
        {
            ContentType = new ContentType(mimeType);
            Content = new MemoryStream();
        }
        public MimePart()
        {
            ContentType = new ContentType();
            Content = new MemoryStream();
        }
        public ContentType ContentType { get; set; }
        public Stream Content { get; set; }

        public void WriteTo(Stream stream)
        {
            using (var filtered = new FilteredStream(stream))
            {
                filtered.Add(EncoderFilter.Create(Encoding.UTF8));
                WriteContent(filtered, Encoding.UTF8);
                filtered.Flush();
            }
        }

        protected virtual void WriteContent(Stream stream, Encoding encoding)
        {
            Content.Position = 0;
            Content.CopyTo(stream);
        }
    }

    public class ContentType
    {
        public ContentType(string mimeType)
        {
            MediaType = mimeType;
            Parameters = new Parameters();
        }
        public ContentType()
        {
            MediaType = null;
            Parameters = new Parameters();
        }

        public string MediaType { get; set; }
        public Parameters Parameters { get; set; } = new Parameters();
    }

    public class HeaderList
    {
        public List<Header> Headers { get; set; }

        public HeaderList()
        {
            Headers = new List<Header>();
        }
    }

    public class Header
    {
        public string Field { get; set; }
        public string Value { get; set; }
    }

    public class Parameters
    {
        public string Filename { get; set; }
        public string Disposition { get; set; }
        public string TransferEncoding { get; set; }
        public string Charset { get; set; }
        public string Format { get; set; }
    }

    public class EncoderFilter
    {
        public static EncoderFilter Create(Encoding encoding)
        {
            return new EncoderFilter(encoding);
        }

        private EncoderFilter(Encoding encoding)
        {
            Encoding = encoding;
        }

        public Encoding Encoding { get; private set; }
    }

    public class FilteredStream : Stream
    {
        private Stream InnerStream;
        private List<EncoderFilter> Filters;

        public FilteredStream(Stream innerStream)
        {
            InnerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
            Filters = new List<EncoderFilter>();
        }

        public void Add(EncoderFilter filter)
        {
            Filters.Add(filter ?? throw new ArgumentNullException(nameof(filter)));
        }

        public override bool CanRead => InnerStream.CanRead;

        public override bool CanSeek => InnerStream.CanSeek;

        public override bool CanWrite => InnerStream.CanWrite;

        public override long Length => InnerStream.Length;

        public override long Position
        {
            get => InnerStream.Position;
            set => InnerStream.Position = value;
        }

        public override void Flush()
        {
            InnerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return InnerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return InnerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            InnerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            foreach (var filter in Filters)
            {
                buffer = filter.Encoding.GetBytes(filter.Encoding.GetString(buffer, offset, count));
            }

            InnerStream.Write(buffer, offset, buffer.Length);
        }
    }


    public class FormatOptions
    {
        public NewLineFormat NewLineFormat { get; set; }
        public string Boundary { get; set; }
        public string MessageId { get; set; }

        public static FormatOptions Default
        {
            get
            {
                return new FormatOptions
                {
                    NewLineFormat = NewLineFormat.Dos,
                    Boundary = null 
                };
            }
        }

        public FormatOptions Clone()
        {
            return (FormatOptions)MemberwiseClone();
        }
    }

    public enum NewLineFormat
    {
        Dos,
        Unix,
        Mac
    }

    public class MimeWriter
    {
        private readonly Stream _stream;
        private readonly FormatOptions _options;

        private const int BufferSize = 4096;

        private MimeWriter(Stream stream, FormatOptions options)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public static MimeWriter Create(Stream stream, FormatOptions options)
        {
            return new MimeWriter(stream, options);
        }

        public void WriteMessage(HeaderList headers, MimePart body, List<MimePart> attachments, string ContentType, FormatOptions Options)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));
            if (body == null)
                throw new ArgumentNullException(nameof(body));
            if (ContentType != null)
            {
                Write($"Content-Type: {ContentType}");
                Write($"; boundary=\"{Options.Boundary}\"");
                WriteLine();
            }
            WriteHeaders(headers, ContentType);
            if (ContentType != null)
                WriteBoundary();
            WriteLine();
            WriteBody(body);

            foreach (var attachment in attachments)
            {
                WriteAttachment(attachment);
            }
            if (ContentType != null)
                Write("--" + Options.Boundary + "--");
            WriteLine();

        }

        public void Flush()
        {
            _stream.Flush();
        }

        private void WriteHeaders(HeaderList headers, string ContentType)
        {

            foreach (var header in headers.Headers)
            {
                WriteHeader(header);
            }
            if (ContentType != null)
            {
                WriteLine();
                Write("This is a multi-part message in MIME format.  ");
                WriteLine();
            }
        }

        private void WriteHeader(Header header)
        {
            Write($"{header.Field}: {header.Value}");
            WriteLine();
        }

        private void WriteBody(MimePart body)
        {
            WriteContentType(body.ContentType);
            WriteLine();

            WriteContentDisposition(body.ContentType);


            WriteContentTransferEncoding(body.ContentType);
            WriteLine();
            WriteLine();
            if (body is TextPart textPart)
            {
                WriteContent(textPart);
            }
            else if (body is MimePart mimePart)
            {
                WriteContent(mimePart);
            }
            WriteLine();
        }

        private void WriteAttachment(MimePart attachment)
        {
            WriteBoundary();
            WriteLine();
            WriteBody(attachment);
            WriteLine();
        }

        private void Write(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            _stream.Write(bytes, 0, bytes.Length);
        }

        private void WriteLine()
        {
            var newLineBytes = GetNewLineBytes();
            _stream.Write(newLineBytes, 0, newLineBytes.Length);
        }

        private void WriteContentType(ContentType contentType)
        {
            Write($"Content-Type: {contentType.MediaType}");
            WriteParameters(contentType.Parameters);
        }

        private void WriteContentTransferEncoding(ContentType contentType)
        {
            var encodingParameter = contentType.Parameters.TransferEncoding;
            if (encodingParameter != null && !string.IsNullOrEmpty(encodingParameter))
            {
                Write($"Content-Transfer-Encoding: {encodingParameter}");
            }
        }

        private void WriteContentDisposition(ContentType contentType)
        {
            var encodingParameter = contentType.Parameters.Disposition;
            if (encodingParameter != null)
            {
                Write($"Content-Disposition: {encodingParameter}");
            }
            var FilenameParameter = contentType.Parameters.Filename;
            if (FilenameParameter != null)
            {
                Write($"; filename=\"{FilenameParameter}\"");
                WriteLine();
            }
        }


        private void WriteContent(MimePart body)
        {

            using (var base64Stream = new CryptoStream(_stream, new ToBase64Transform(), CryptoStreamMode.Write))
            {
                body.Content.Position = 0;
                var buffer = new byte[72];
                int bytesRead;

                while ((bytesRead = body.Content.Read(buffer, 0, buffer.Length)) > 0)
                {
                    base64Stream.Write(buffer, 0, bytesRead);
                    WriteLine(); // Add a new line after every 72 bytes
                }
            }
        }
        private void WriteContent(TextPart text)
        {
            using (var writer = new StreamWriter(_stream))
            {
                writer.Write(text.Text);
                writer.Flush();
            }
            WriteLine();


        }



        private void WriteParameters(Parameters parameters)
        {
            if (parameters.Charset != null)
            {
                Write($"; charset={parameters.Charset}");
            }
            if (parameters.Filename != null)
            {
                Write($"; name=\"{parameters.Filename}\"");
            }
            if (parameters.Format != null)
            {
                Write($"; format={parameters.Format}");
            }
        }

        private void WriteBoundary()
        {
            Write($"--{_options.Boundary}");
        }

        private byte[] GetNewLineBytes()
        {
            switch (_options.NewLineFormat)
            {
                case NewLineFormat.Dos:
                    return Encoding.UTF8.GetBytes("\r\n");
                case NewLineFormat.Unix:
                    return Encoding.UTF8.GetBytes("\n");
                case NewLineFormat.Mac:
                    return Encoding.UTF8.GetBytes("\r");
                default:
                    throw new InvalidOperationException("Unsupported NewLineFormat");
            }
        }
    }
    public static class MimeTypeMapper
    {
        private static readonly IDictionary<string, string> _mappings =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
            {".xls", "application/vnd.ms-excel"},
            {".xlsb", "application/vnd.ms-excel.sheet.binary.macroEnabled.12"},
            {".xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12"},
            {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
            {".xlt", "application/vnd.ms-excel"},
            {".xltm", "application/vnd.ms-excel.template.macroEnabled.12"},
            {".zip", "application/x-zip-compressed"},
            {".png", "image/png"},
            {".txt", "text/plain"},
            {".doc", "application/msword"},
            {".pdf", "application/pdf"},
            };

        public static string GetMimeType(string extension)
        {
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            if (!extension.StartsWith("."))
            {
                extension = ("." + extension).ToLower();
            }

            string mime;
            return _mappings.TryGetValue(extension, out mime) ? mime : "application/octet-stream";
        }

        public static ContentType GetContentType(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            string extension = Path.GetExtension(filePath);
            string mimeType = GetMimeType(extension);
            return new ContentType(mimeType);
        }
    }
    public class DatabaseMime
    {
        public const string DatabaseFileName = "MimeMessages.db";
        public const string ConnectionString = "Data Source=" + DatabaseFileName;

        public static void InitializeDatabase()
        {
            if (!File.Exists(DatabaseFileName))
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Emails(
                                MessageId TEXT PRIMARY KEY,
                                Folder TEXT,
                                FromAr TEXT,
                                ToAr TEXT,
                                CCAddresses TEXT,
                                Subject TEXT,
                                Body TEXT,
                                Status TEXT
                            );

                            CREATE TABLE IF NOT EXISTS Attachments(
                                MessageId TEXT,
                                FileName TEXT,
                                Content BLOB,
                                FOREIGN KEY(MessageId) REFERENCES Emails(MessageId)
                            )";

                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }
            }
        }

        public static void InsertData(MimeMessage mimeMessage, int ret, string folder)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                // Insert sample email data
                using (var insertEmailCommand = connection.CreateCommand())
                {
                    insertEmailCommand.CommandText = @"
                          INSERT INTO Emails (MessageId, Folder, FromAr, ToAr,CCAddresses, Subject, Body, Status)
                VALUES (@MessageId, @Folder, @FromAr, @ToAr,@CCAddresses, @Subject, @Body, 'not seen')";
                    int a = 0;
                    String CCAddress = "";
                    foreach (string x in mimeMessage.CC.Addresses)
                    {
                        if (a == 0)
                        {
                            CCAddress += x;
                        }
                        else CCAddress += $",{x}";
                        a++;
                    }
                    String ToAddress = "";
                    foreach (string x in mimeMessage.To.Addresses)
                    {
                        if (a == 0)
                        {
                            ToAddress += x;
                        }
                        else ToAddress += $",{x}";
                        a++;
                    }
                    insertEmailCommand.Parameters.AddWithValue("@MessageId", ret);
                    insertEmailCommand.Parameters.AddWithValue("@Folder", folder); 
                    insertEmailCommand.Parameters.AddWithValue("@FromAr", mimeMessage.From);
                    insertEmailCommand.Parameters.AddWithValue("@ToAr", ToAddress);
                    insertEmailCommand.Parameters.AddWithValue("@CCAddresses", CCAddress);
                    insertEmailCommand.Parameters.AddWithValue("@Subject", mimeMessage.Subject);
                    insertEmailCommand.Parameters.AddWithValue("@Body", mimeMessage.Body.Text);
                    insertEmailCommand.ExecuteNonQuery();
                }

                foreach (var attachment in mimeMessage.Attachments)
                {
                    using (var insertAttachmentCommand = connection.CreateCommand())
                    {
                        insertAttachmentCommand.CommandText = @"
                        INSERT INTO Attachments (MessageId, FileName,  Content)
                        VALUES (@MessageId, @FileName, @Content)";

                        insertAttachmentCommand.Parameters.AddWithValue("@MessageId", ret);
                        insertAttachmentCommand.Parameters.AddWithValue("@FileName", attachment.ContentType.Parameters.Filename);

                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            attachment.Content.Position = 0;
                            attachment.Content.CopyTo(memoryStream);
                            byte[] contentBytes = memoryStream.ToArray();
                            insertAttachmentCommand.Parameters.AddWithValue("@Content", contentBytes);
                        }

                        insertAttachmentCommand.ExecuteNonQuery();
                    }
                }
                connection.Close();

            }
        }

        public static void PrintEmailDetails(int messageId)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                // Query data from the Emails table with a specific MessageId
                using (var selectEmailCommand = connection.CreateCommand())
                {
                    selectEmailCommand.CommandText = @"
                SELECT *
                FROM Emails
                WHERE MessageId = @MessageId";
                    selectEmailCommand.Parameters.AddWithValue("@MessageId", messageId);

                    using (var reader = selectEmailCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Console.WriteLine($"From: {reader["FromAr"]}");
                            string ToAddress = reader["ToAr"].ToString();
                            if (!string.IsNullOrEmpty(ToAddress))
                            {
                                Console.WriteLine($"To: {ToAddress}");
                            }
                            string ccAddresses = reader["CCAddresses"].ToString();
                            if (!string.IsNullOrEmpty(ccAddresses))
                            {
                                Console.WriteLine($"Cc: {ccAddresses}");
                            }
                            Console.WriteLine($"Subject: {reader["Subject"]}");
                            Console.WriteLine($"Content: {reader["Body"]}");

                        }
                        else
                        {
                            Console.WriteLine($"No email found with MessageId: {messageId}");
                            connection.Close();
                            return;
                        }
                    }
                }

                // Query data from the Attachments table with a specific MessageId
                using (var selectAttachmentCommand = connection.CreateCommand())
                {
                    selectAttachmentCommand.CommandText = @"
                SELECT FileName
                FROM Attachments
                WHERE MessageId = @MessageId";
                    selectAttachmentCommand.Parameters.AddWithValue("@MessageId", messageId);

                    using (var reader = selectAttachmentCommand.ExecuteReader())
                    {
                          if(DatabaseMime.CountAttachmentsForMessage(messageId) > 0){
                        Console.WriteLine("\nList of attachments:");
                        int attachmentOrder = 1;
                        while (reader.Read())
                        {
                            Console.WriteLine($"{attachmentOrder}. FileName: {reader["FileName"]}");
                            attachmentOrder++;
                        }
                          }

                    }
                }

                connection.Close();
            }
        }

        public static List<Tuple<int, int>> GetEmails(string Folder)
        {
            List<Tuple<int, int>> IDEmails = new List<Tuple<int, int>>();

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();


                using (var selectEmailsCommand = connection.CreateCommand())
                {
                    selectEmailsCommand.CommandText = @"SELECT *FROM Emails WHERE Folder = @Folder";
                    selectEmailsCommand.Parameters.AddWithValue("@Folder", Folder);
                    using (var reader = selectEmailsCommand.ExecuteReader())
                    {
                        int stt = 1;

                        while (reader.Read())
                        {
                            int messageId = Convert.ToInt32(reader["MessageId"]);
                            IDEmails.Add(new Tuple<int, int>(stt, messageId));
                            Console.WriteLine($"{stt}. From: {reader["FromAr"]},Subject: {reader["Subject"]} - {reader["Status"]}");
                            stt++;
                        }

                    }
                }

                connection.Close();
            }

            return IDEmails;
        }
        public static void UpdateData(int Id)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                // Update status of the email
                using (var updateEmailCommand = connection.CreateCommand())
                {
                    updateEmailCommand.CommandText = "UPDATE Emails SET Status = 'seen' WHERE MessageId = @Id";
                    updateEmailCommand.Parameters.AddWithValue("@Id", Id);
                    updateEmailCommand.ExecuteNonQuery();
                }

                connection.Close();
            }
        }
        public static int CountAttachmentsForMessage(int messageId)
        {
            int attachmentCount = 0;

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                using (var countAttachmentsCommand = connection.CreateCommand())
                {
                    countAttachmentsCommand.CommandText = "SELECT COUNT(*) FROM Attachments WHERE MessageId = @MessageId";
                    countAttachmentsCommand.Parameters.AddWithValue("@MessageId", messageId);

                    attachmentCount = Convert.ToInt32(countAttachmentsCommand.ExecuteScalar());
                }

                connection.Close();
            }

            return attachmentCount;
        }

        public static int CountEmail()
        {
            int attachmentCount = 0;

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                using (var countAttachmentsCommand = connection.CreateCommand())
                {
                    countAttachmentsCommand.CommandText = "SELECT COUNT(*) FROM Emails";

                    attachmentCount = Convert.ToInt32(countAttachmentsCommand.ExecuteScalar());
                }

                connection.Close();
            }

            return attachmentCount;
        }
        public static void SaveAttachmentsFromDatabase(int messageId, string path)
        {
            try
            {
                // Get all attachments from database with specific MessageId
                List<AttachmentData> attachments = GetAttachmentsByMessageId(messageId);

                // Check if the attachment exists
                if (attachments.Count > 0)
                {
                    // Call the SaveAttachmentFromDatabase function to save each attachment
                    SaveAttachmentsFromDatabase(attachments, messageId, path);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static List<AttachmentData> GetAttachmentsByMessageId(int messageId)
        {

            List<AttachmentData> attachments = new List<AttachmentData>();

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                // Query data from the Attachments table with a specific MessageId
                using (var selectAttachmentsCommand = connection.CreateCommand())
                {
                    selectAttachmentsCommand.CommandText = "SELECT * FROM Attachments WHERE MessageId = @MessageId";
                    selectAttachmentsCommand.Parameters.AddWithValue("@MessageId", messageId);

                    using (var reader = selectAttachmentsCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Read data from the database and add it to the attachment list
                            AttachmentData attachment = new AttachmentData
                            {
                                FileName = reader["FileName"].ToString(),
                                Content = (byte[])reader["Content"]
                            };

                            attachments.Add(attachment);
                        }
                    }
                }

                connection.Close();
            }

            return attachments;
        }

        private static void SaveAttachmentsFromDatabase(List<AttachmentData> attachments, int messageId, string path)
        {
            try
            {
                string saveFolder = path;
                if (!Directory.Exists(saveFolder))
                {
                    Directory.CreateDirectory(saveFolder);
                }

                foreach (var attachment in attachments)
                {
                    string fileName = Path.Combine(saveFolder, attachment.FileName);

                    using (var fileStream = File.Create(fileName))
                    {
                        // Writes the attachment content to a file on disk
                        fileStream.Write(attachment.Content, 0, attachment.Content.Length);
                    }

                    Console.WriteLine($"saved: {fileName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        public class AttachmentData
        {
            public string FileName { get; set; }
            public byte[] Content { get; set; }
        }
    }
}