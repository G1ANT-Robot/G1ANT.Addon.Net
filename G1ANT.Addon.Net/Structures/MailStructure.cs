using System;
using MailKit;
using MimeKit;
using G1ANT.Language;
using System.Collections.Generic;
using G1ANT.Addon.Net.Extensions;
using G1ANT.Addon.Net.API;

namespace G1ANT.Addon.Net
{
    [Structure(Name = "mail", Priority = 10, Default = 0, AutoCreate = false, Tooltip = "This structure stores current information about a mail message, which was downloaded with the `mail.imap` command")]
    public class MailStructure : StructureTyped<ISimplifiedMessage>
    {
        private static class IndexNames
        {
            public const string Id = "id";
            public const string From = "from";
            public const string To = "to";
            public const string Cc = "cc";
            public const string Bcc = "bcc";
            public const string ReplyTo = "replyto";
            public const string Subject = "subject";
            public const string Body = "content";
            public const string HtmlBody = "htmlcontent";
            public const string Date = "date";
            public const string Priority = "priority";
            public const string Attachments = "attachments";
            public const string IsReply = "isreply";
            public const string IsUnread = "isunread";
        }

        public MailStructure() : base(new MessageSummary(0))
        {
        }

        public MailStructure(ISimplifiedMessage value, string format = "") :
            base(value, format)
        {
            Init();
        }

        public MailStructure(object value, string format = "", AbstractScripter scripter = null)
            : base(value, format, scripter)
        {
            Init();
        }

        private void Init()
        {
            Indexes.Add(IndexNames.Id);
            Indexes.Add(IndexNames.Subject);
            Indexes.Add(IndexNames.From);
            Indexes.Add(IndexNames.To);
            Indexes.Add(IndexNames.Cc);
            Indexes.Add(IndexNames.Bcc);
            Indexes.Add(IndexNames.Date);
            Indexes.Add(IndexNames.Attachments);
            Indexes.Add(IndexNames.Priority);
            Indexes.Add(IndexNames.Body);
            Indexes.Add(IndexNames.HtmlBody);
            Indexes.Add(IndexNames.IsReply);
            Indexes.Add(IndexNames.IsUnread);
            Indexes.Add(IndexNames.ReplyTo);
        }

        public override Structure Get(string index = "")
        {
            if (string.IsNullOrWhiteSpace(index))
                return this;

            switch (index.ToLower())
            {
                case IndexNames.Id:
                    return new TextStructure(Value.MessageId, null, Scripter);
                case IndexNames.Subject:
                    return new TextStructure(Value.Subject, null, Scripter);
                case IndexNames.From:
                    return new TextStructure(Value.From, null, Scripter);
                case IndexNames.To:
                    return new TextStructure(Value.To, null, Scripter);
                case IndexNames.Cc:
                    return new TextStructure(Value.Cc, null, Scripter);
                case IndexNames.Bcc:
                    return new TextStructure(Value.Bcc, null, Scripter);
                case IndexNames.ReplyTo:
                    return new TextStructure(Value.ReplyTo, null, Scripter);
                case IndexNames.Date:
                    return new DateTimeStructure(Value.Date, "");
                case IndexNames.IsReply:
                    return new BooleanStructure(Value.IsReply);
                case IndexNames.Body:
                    return new TextStructure(Value.TextBody, null, Scripter);
                case IndexNames.HtmlBody:
                    return new TextStructure(Value.HtmlBody, null, Scripter);
                case IndexNames.Priority:
                    return new IntegerStructure(Value.Priority);
                case IndexNames.Attachments:
                    return new ListStructure(Value.Attachments, "", Scripter);
                case IndexNames.IsUnread:
                    return new BooleanStructure(Value.IsUnread, "", Scripter);
            }
            throw new ArgumentException($"Unknown index '{index}'", nameof(index));
        }

        public override void Set(Structure structure, string index = null)
        {
            if (structure?.Object == null)
            {
                throw new ArgumentNullException(nameof(structure));
            }

            switch (index.ToLower())
            {
                case IndexNames.Subject:
                    Value.Subject = structure.ToString();
                    break;
                case IndexNames.Date:
                    if (DateTimeOffset.TryParse(structure.ToString(), out DateTimeOffset dateTime))
                    {
                        Value.Date = dateTime;
                    }
                    break;
                case IndexNames.To:
                    Value.To.SetMailboxesFromStructure(structure, "To");
                    break;
                case IndexNames.From:
                    Value.From.SetMailboxesFromStructure(structure, "From");
                    break;
                case IndexNames.Cc:
                    Value.Cc.SetMailboxesFromStructure(structure, "Cc");
                    break;
                case IndexNames.ReplyTo:
                    Value.ReplyTo.SetMailboxesFromStructure(structure, "ReplyTo");
                    break;
                case IndexNames.Bcc:
                    Value.Bcc.SetMailboxesFromStructure(structure, "Bcc");
                    break;
                case IndexNames.Body:
                    Value.TextBody = structure.ToString();
                    break;
                case IndexNames.HtmlBody:
                    Value.HtmlBody = structure.ToString();
                    break;
                case IndexNames.Priority:
                    Value.Priority = structure.ToString();
                    break;
                case IndexNames.IsUnread:
                    Value.IsUnread = Convert.ToBoolean(structure.Object);
                    break;
                case IndexNames.Attachments:
                    if (structure is ListStructure list)
                        Value.Attachments = list.Value;
                    else
                        Value.Attachments = new List<object>(new object[] { structure.ToString() });
                    break;
                default:
                    throw new ArgumentException($"Unknown index '{index}'", nameof(index));
            }
        }

        public override string ToString(string format = "")
        {
            return Value.Subject;
        }

        protected override ISimplifiedMessage Parse(string value, string format = null)
        {
            throw new NotImplementedException();
        }

        public MailStructure CreateReply(bool replyToAll, string replyPrefix)
        {
            return new MailStructure(Value.CreateReply(replyToAll, replyPrefix));
        }
    }
}