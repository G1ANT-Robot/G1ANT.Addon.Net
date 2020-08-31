﻿using MailKit;
using MimeKit;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using G1ANT.Addon.Net.Models;

namespace G1ANT.Addon.Net
{
    public class SimplifiedMessageSummary
    {
        public IMailFolder Folder;

        private MessageSummary messageSummary = null;
        private MimeMessage fullMessage = null;

        public SimplifiedMessageSummary()
        {
            FullMessage = new MimeMessage();
        }

        public SimplifiedMessageSummary(MessageSummary summary, IMailFolder folder)
        {
            messageSummary = summary;
            Folder = folder;
        }

        protected SimplifiedMessageSummary(MimeMessage message)
        {
            FullMessage = message;
        }

        public MimeMessage FullMessage
        {
            get
            {
                if (fullMessage == null && messageSummary != null)
                    fullMessage = Folder.GetMessage(messageSummary.UniqueId);
                return fullMessage;
            }
            set
            {
                fullMessage = value;
            }
        }

        public List<object> Attachments
        {
            get => CreateAttachmentStructuresFromAttachments(FullMessage.Attachments);
            set
            {
                var bodyBuilder = new BodyBuilder();
                if (bodyBuilder.Attachments != null)
                {
                    bodyBuilder.HtmlBody = HtmlBody;
                    bodyBuilder.TextBody = TextBody;
                    foreach (var attachment in value)
                        bodyBuilder.Attachments.Add(attachment.ToString());
                    FullMessage.Body = bodyBuilder.ToMessageBody();
                }
            }
        }

        private IEnumerable<MimeEntity> GetAttachmentsWithNamesSet(IEnumerable<MimeEntity> attachments)
        {
            return attachments.Where(x => !string.IsNullOrEmpty(x.ContentDisposition?.FileName));
        }

        private List<object> CreateAttachmentStructuresFromAttachments(IEnumerable<MimeEntity> attachments)
        {
            return GetAttachmentsWithNamesSet(attachments)
                .Select(a => new AttachmentStructure(new AttachmentModel(a)))
                .ToList<object>();
        }

        public string MessageId
        {
            get
            {
                if (fullMessage != null)
                    return fullMessage.MessageId;
                else if (messageSummary != null)
                    return messageSummary.Envelope.MessageId;
                return string.Empty;
            }
        }

        public UniqueId UniqueId
        {
            get
            {
                if (messageSummary != null)
                    return messageSummary.UniqueId;
                return UniqueId.Invalid;
            }
        }

        public string Subject
        {
            get
            {
                if (fullMessage != null)
                    return fullMessage.Subject;
                else if (messageSummary != null)
                    return messageSummary.NormalizedSubject;
                return string.Empty;
            }
            set
            {
                if (fullMessage != null)
                    fullMessage.Subject = value;
                else if (messageSummary != null)
                    messageSummary.Envelope.Subject = value;
            }
        }

        public InternetAddressList To
        {
            get
            {
                if (fullMessage != null)
                    return fullMessage.To;
                else if (messageSummary != null)
                    return messageSummary.Envelope.To;
                return null;
            }
        }

        public InternetAddressList From
        {
            get
            {
                if (fullMessage != null)
                    return fullMessage.From;
                else if (messageSummary != null)
                    return messageSummary.Envelope.From;
                return null;
            }
        }

        public MailboxAddress Sender
        {
            set
            {
                if (fullMessage != null)
                    fullMessage.Sender = value;
                else
                    throw new NotSupportedException("Cannot set Sender for received message");
            }
        }

        public InternetAddressList Cc
        {
            get
            {
                if (fullMessage != null)
                    return fullMessage.Cc;
                else if (messageSummary != null)
                    return messageSummary.Envelope.Cc;
                return null;
            }
        }

        public InternetAddressList Bcc
        {
            get
            {
                if (fullMessage != null)
                    return fullMessage.Bcc;
                else if (messageSummary != null)
                    return messageSummary.Envelope.Bcc;
                return null;
            }
        }

        public bool IsReply
        {
            get
            {
                if (messageSummary != null)
                    return messageSummary.IsReply;
                return false;
            }
        }

        public string Priority
        {
            get
            {
                if (fullMessage?.Headers != null)
                    return fullMessage.Headers[HeaderId.Priority];
                else if (messageSummary != null && messageSummary.Headers != null)
                    return messageSummary.Headers[HeaderId.Priority];
                return "0";
            }
            set
            {
                if (fullMessage != null && fullMessage.Headers != null)
                    fullMessage.Headers[HeaderId.Priority] = value;
                else if (messageSummary != null && messageSummary.Headers != null)
                    messageSummary.Headers[HeaderId.Priority] = value;
            }
        }

        public DateTimeOffset? Date
        {
            get
            {
                if (fullMessage != null)
                    return fullMessage.Date;
                else if (messageSummary != null)
                    return messageSummary.Envelope.Date;
                return null;
            }
            set
            {
                if (fullMessage != null && value.HasValue)
                    fullMessage.Date = value.Value;
                else if (messageSummary != null)
                    messageSummary.Envelope.Date = value;
            }
        }

        private void FillBodyBuilderAttachments(BodyBuilder bodyBuilder)
        {
            if (bodyBuilder.Attachments != null)
                foreach (var att in FullMessage.Attachments)
                    bodyBuilder.Attachments.Add(att);
        }

        public string HtmlBody
        {
            get => FullMessage.HtmlBody;
            set
            {
                var bodyBuilder = new BodyBuilder();
                FillBodyBuilderAttachments(bodyBuilder);
                bodyBuilder.HtmlBody = value;
                bodyBuilder.TextBody = TextBody;
                FullMessage.Body = bodyBuilder.ToMessageBody();
            }
        }

        public string TextBody
        {
            get => FullMessage.TextBody;
            set
            {
                var bodyBuilder = new BodyBuilder();
                FillBodyBuilderAttachments(bodyBuilder);
                bodyBuilder.HtmlBody = HtmlBody;
                bodyBuilder.TextBody = value;
                FullMessage.Body = bodyBuilder.ToMessageBody();
            }
        }

        public SimplifiedMessageSummary CreateReply(bool replyToAll, string replyPrefix = "Re: ")
        {
            var message = FullMessage;
            var reply = new MimeMessage();

            if (message.ReplyTo.Count > 0)
                reply.To.AddRange(message.ReplyTo);
            else if (message.From.Count > 0)
                reply.To.AddRange(message.From);
            else if (message.Sender != null)
                reply.To.Add(message.Sender);

            if (replyToAll)
            {
                reply.To.AddRange(message.To);
                reply.Cc.AddRange(message.Cc);
            }

            if (!message.Subject.StartsWith(replyPrefix, StringComparison.OrdinalIgnoreCase))
                reply.Subject = $"{replyPrefix}{message.Subject}";
            else
                reply.Subject = message.Subject;

            if (!string.IsNullOrEmpty(message.MessageId))
            {
                reply.InReplyTo = message.MessageId;
                foreach (var id in message.References)
                    reply.References.Add(id);
                reply.References.Add(message.MessageId);
            }

            if (message.TextBody != null)
            {
                reply.Body = new TextPart("plain")
                {
                    Text = GetQuotedText(message)
                };
            }
            return new SimplifiedMessageSummary(reply);
        }

        private string GetQuotedText(MimeMessage message)
        {
            using (var quoted = new StringWriter())
            {
                var sender = message.Sender ?? message.From.Mailboxes.FirstOrDefault();

                quoted.WriteLine();
                quoted.WriteLine("On {0}, {1} wrote:", message.Date.ToString("f"), !string.IsNullOrEmpty(sender.Name) ? sender.Name : sender.Address);
                using (var reader = new StringReader(message.TextBody))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        quoted.Write("> ");
                        quoted.WriteLine(line);
                    }
                }
                return quoted.ToString();
            }
        }
    }
}
