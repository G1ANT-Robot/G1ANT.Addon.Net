using G1ANT.Addon.Net.Models;
using MailKit;
using Microsoft.Exchange.WebServices.Data;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;

namespace G1ANT.Addon.Net.API
{
    internal class EwsSimplifiedMessage : ISimplifiedMessage
    {
        private EmailMessage message;
        private ExchangeService service;
        private IList<FileAttachment> attachments = null;

        private string _bodyText = null;
        private string _bodyHtml = null;

        private string _subject = null;

        public EmailMessage Message => message;

        public EwsSimplifiedMessage(EmailMessage message, ExchangeService service)
        {
            this.message = message;
            this.service = service;

            if (!message.IsNew)
            {
                var ps = new PropertySet(BasePropertySet.FirstClassProperties, ItemSchema.Body, ItemSchema.TextBody, ItemSchema.Attachments)
                {
                    RequestedBodyType = BodyType.HTML
                };
                message.Load(ps);

                _bodyText = message.TextBody.Text ?? string.Empty;
                _bodyHtml = message.Body?.Text ?? string.Empty;
                _subject = message.Subject ?? string.Empty;
            }
        }

        public bool IsUnread
        {
            get => !message.IsRead;
            set
            {
                message.IsRead = !value;
                if (!string.IsNullOrEmpty(message.Id?.UniqueId))
                {
                    message.Update(ConflictResolutionMode.AlwaysOverwrite);
                }
            }
        }
        public List<object> Attachments
        {
            get
            {
                if (attachments == null)
                {
                    attachments = new List<FileAttachment>();
                    if (message.Id != null && message.HasAttachments)
                    {
                        // Załadowanie właściwości załączników, jeśli jeszcze nie pobrano
                        message.Load(new PropertySet(ItemSchema.Attachments));
                        foreach (var attachment in message.Attachments)
                        {
                            if (attachment is FileAttachment fileAtt)
                            {
                                fileAtt.Load(); // Pobranie zawartości pliku
                                attachments.Add(fileAtt);
                            }
                        }
                    }
                }

                // Mapowanie załączników EWS na model oczekiwany przez G1ANT
                return attachments.Select(x => new AttachmentStructure(new EwsAttachmentModel(x))).ToList<object>();
            }
            set {
                message.Attachments.Clear();
                foreach (var attachment in value)
                {
                    message.Attachments.AddFileAttachment(attachment.ToString());
                }
                UpdateMessage();
            }
        }

        public string MessageId => message.InternetMessageId;

        public UniqueId UniqueId => throw new NotImplementedException();

        public string Subject
        {
            get => _subject;
            set
            {
                _subject = value;
                message.Subject = value;
                UpdateMessage();
            }
        }

        public InternetAddressList To
        {
            get => new InternetAddressList(message.ToRecipients.Select(address => new MailboxAddress(address.Name, address.Address)));
            set
            {
                message.ToRecipients.Clear();
                foreach (var x in value.Where(item => item is MailboxAddress).Cast<MailboxAddress>())
                {
                    message.ToRecipients.Add(x.Name, x.Address);
                }
                UpdateMessage();
            }
        }

        public InternetAddressList From
        {
            get
            {
                if (message.Sender != null || message.From != null)
                {
                    var sender = message.Sender ?? message.From;
                    return new InternetAddressList
                    {
                        new MailboxAddress(sender.Name, sender.Address)
                    };
                }
                return null;
            }
            set
            {
                if (value.Count != 1)
                    throw new ArgumentException("You can assign only one address to from property");
                if (value.First() is MailboxAddress newVal)
                {
                    message.Sender = new EmailAddress(newVal.Name, newVal.Address);
                }
                UpdateMessage();
            }
        }

        public InternetAddressList Cc
        {
            get
            {
                if (message.CcRecipients != null && message.CcRecipients.Count > 0)
                    return new InternetAddressList(message.CcRecipients.Select(address => new MailboxAddress(address.Name, address.Address)));
                return null;
            }
            set
            {
                message.CcRecipients.Clear();
                foreach (var x in value.Where(item => item is MailboxAddress).Cast<MailboxAddress>())
                {
                    message.CcRecipients.Add(x.Name, x.Address);
                }
                UpdateMessage();
            }
        }

        public InternetAddressList Bcc
        {
            get
            {
                if (message.BccRecipients != null && message.BccRecipients.Count > 0)
                    return new InternetAddressList(message.BccRecipients.Select(address => new MailboxAddress(address.Name, address.Address)));
                return null;
            }
            set
            {
                message.BccRecipients.Clear();
                foreach (var x in value.Where(item => item is MailboxAddress).Cast<MailboxAddress>())
                {
                    message.BccRecipients.Add(x.Name, x.Address);
                }
                UpdateMessage();
            }
        }

        public InternetAddressList ReplyTo
        {
            get
            {
                if (message.ReplyTo != null && message.ReplyTo.Count > 0)
                    return new InternetAddressList(message.ReplyTo.Select(address => new MailboxAddress(address.Name, address.Address)));
                return null;
            }
            set
            {
                message.ReplyTo.Clear();
                foreach (var x in value.Where(item => item is MailboxAddress).Cast<MailboxAddress>())
                {
                    message.ReplyTo.Add(x.Name, x.Address);
                }
                UpdateMessage();
            }
        }

        public bool IsReply => !string.IsNullOrEmpty(message.ConversationId?.UniqueId);

        public string Priority
        {
            get
            {
                switch (message.Importance)
                {
                    case Importance.High:
                        return "1";
                    case Importance.Normal:
                        return "2";
                    default:
                        return "3";
                }
            }
            set
            {
                switch (value)
                {
                    case "1":
                        message.Importance = Importance.High;
                        break;
                    case "2":
                        message.Importance = Importance.Normal;
                        break;
                    default:
                        message.Importance = Importance.Low;
                        break;
                }
                UpdateMessage();
            }
        }

        public DateTimeOffset? Date
        {
            get => message.DateTimeSent != default ? new DateTimeOffset(message.DateTimeSent) : (DateTimeOffset?)null;
            set
            {
                // EWS nie pozwala na ręczną modyfikację daty wysłania/otrzymania na serwerze (właściwość jest read-only).
                // Dla nowych wiadomości data jest ustawiana automatycznie przez Exchange w momencie wysyłki.
            }
        }

        public string HtmlBody
        {
            get => _bodyHtml ?? message.Body?.Text ?? string.Empty;
            set
            {
                _bodyHtml = value;
                message.Body = new MessageBody(BodyType.HTML, value);
                UpdateMessage();
            }
        }

        public string TextBody
        {
            get => _bodyText ?? message.TextBody?.Text ?? string.Empty;
            set
            {
                // Ustawienie treści jako czysty tekst (Text) i automatyczna aktualizacja na serwerze
                message.Body = new MessageBody(BodyType.Text, value);
                UpdateMessage();
            }
        }

        public ISimplifiedMessage CreateReply(bool replyToAll, string replyPrefix = "Re: ")
        {
            if (string.IsNullOrEmpty(message.Id?.UniqueId))
                throw new ApplicationException("Cannot reply to a new email");

            // W EWS metoda CreateReply / CreateReplyResponse zwraca obiekt ResponseMessage
            ResponseMessage responseMessage = message.CreateReply(replyToAll);
            EmailMessage replyEmail = responseMessage.Save(); // Zapisanie jako szkic odpowiedzi

            // Ustawienie prefiksu i tematu, jeśli jest wymagany
            if (!string.IsNullOrEmpty(message.Subject))
            {
                if (!message.Subject.StartsWith(replyPrefix, StringComparison.OrdinalIgnoreCase))
                    replyEmail.Subject = $"{replyPrefix}{message.Subject}";
                else
                    replyEmail.Subject = message.Subject;

                replyEmail.Update(ConflictResolutionMode.AlwaysOverwrite);
            }

            return new EwsSimplifiedMessage(replyEmail, service);
        }

        private MessageBody GetBody(BodyType type)
        {
            // Body już załadowane (lub ustawione lokalnie) w odpowiednim formacie
            if (message.TryGetProperty(ItemSchema.Body, out MessageBody body) && body != null && body.BodyType == type)
            {
                return body;
            }

            // Wiadomość istnieje na serwerze, więc dociągamy body w żądanym formacie
            if (!string.IsNullOrEmpty(message.Id?.UniqueId))
            {
                var ps = new PropertySet(BasePropertySet.IdOnly, ItemSchema.Body)
                {
                    RequestedBodyType = type
                };
                message.Load(ps);

                if (message.TryGetProperty(ItemSchema.Body, out body))
                    return body;
            }

            return null; // nowa wiadomość bez ustawionego body
        }

        private void UpdateMessage()
        {
            if (!string.IsNullOrEmpty(message.Id?.UniqueId))
            {
                // Zapisanie zmian na serwerze Exchange dla istniejącej wiadomości
                message.Update(ConflictResolutionMode.AlwaysOverwrite);
            }
        }

        public void SaveToFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            string sourcePath = path;

            if (path.EndsWith("\\") || path.EndsWith("/"))
            {
                var filename = string.Concat(message.InternetMessageId.Split(Path.GetInvalidFileNameChars()));
                sourcePath = $"{path}{filename}.eml";
            }

            var targetpath = SanitizePath(sourcePath);

            var fi = new FileInfo(targetpath);

            if (fi.Directory != null && !fi.Directory.Exists)
                fi.Directory.Create(); // Tworzenie katalogu, jeśli nie istnieje

            var hasextension = !string.IsNullOrEmpty(fi.Extension);

            var fullpath = hasextension ? fi.FullName : $"{fi.FullName}.eml";

            // Pobranie i zapisanie jako plik EML (Natywne wsparcie EWS przez MimeContent)
            message.Load(new PropertySet(ItemSchema.MimeContent));
            if (message.MimeContent != null)
            {
                File.WriteAllBytes(fullpath, message.MimeContent.Content);
            }
            else
            {
                throw new ApplicationException("Nie udało się pobrać zawartości MimeContent wiadomości.");
            }
        }

        private string SanitizePath(string fullPath)
        {
            string dir = Path.GetDirectoryName(fullPath) ?? "";
            string name = Path.GetFileName(fullPath);

            dir = string.Concat(dir.Split(Path.GetInvalidPathChars()));
            name = string.Concat(name.Split(Path.GetInvalidFileNameChars()));

            return Path.Combine(dir, name);
        }
    }
}
