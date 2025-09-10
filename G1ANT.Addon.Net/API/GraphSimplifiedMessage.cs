using G1ANT.Addon.Net.Models;
using Google.Apis.Requests;
using MailKit;
using Microsoft.Graph;
using MimeKit;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static G1ANT.Language.RobotMessage32;

namespace G1ANT.Addon.Net.API
{
    public class GraphSimplifiedMessage : ISimplifiedMessage
    {
        private Message message;
        private IUserRequestBuilder requestBuilder;
        private IList<Attachment> attachements = null;

        public Message Message => message;

        public GraphSimplifiedMessage(Message message, IUserRequestBuilder requestBuilder)
        {
            this.message = message;
            this.requestBuilder = requestBuilder;
        }

        public bool IsUnread 
        { 
            get => message.IsRead != true; 
            set
            {
                if (!string.IsNullOrEmpty(message.Id))
                {
                    message.IsRead = value;
                    if (requestBuilder != null)
                        Task.Run(async () => await requestBuilder.Messages[message.Id].Request().Select(x => x.IsRead).UpdateAsync(message));
                }
            }
        }

        public List<object> Attachments
        {
            get
            {
                if (attachements == null)
                {
                    if (!string.IsNullOrEmpty(message.Id) && requestBuilder != null)
                    {
                        var task = Task.Run(async () => await requestBuilder.Messages[message.Id].Attachments.Request().GetAsync());
                        var result = task.Result;
                        attachements = result.CurrentPage ?? new List<Attachment>();
                    }
                    else
                        attachements = new List<Attachment>();

                }
                return attachements.Select(x => new AttachmentStructure(new GraphAttachmentModel(x))).ToList<object>();
            }
            set => throw new NotImplementedException(); 
        }

        public string MessageId => message.InternetMessageId;

        public UniqueId UniqueId => throw new NotImplementedException();

        public string Subject 
        { 
            get => message.Subject; 
            set => message.Subject = value;
        }

        public InternetAddressList To => new InternetAddressList(message.ToRecipients.Select(address => new MailboxAddress(address.EmailAddress.Name, address.EmailAddress.Address)));

        public InternetAddressList From
        {
            get
            {
                if (message.From != null)
                    return new InternetAddressList
                    {
                        new MailboxAddress(message.From?.EmailAddress?.Name, message.From?.EmailAddress?.Address)
                    };
                else
                    return null;
            }
        }

        public InternetAddressList Cc
        {
            get
            {
                if (message.CcRecipients != null && message.CcRecipients.Count() > 0)
                    return new InternetAddressList(message.CcRecipients.Select(address => new MailboxAddress(address.EmailAddress.Name, address.EmailAddress.Address)));
                else
                    return null;
            }
        }

        public InternetAddressList Bcc
        {
            get
            {
                if (message.BccRecipients != null && message.BccRecipients.Count() > 0)
                    return new InternetAddressList(message.BccRecipients.Select(address => new MailboxAddress(address.EmailAddress.Name, address.EmailAddress.Address)));
                else
                    return null;
            }
        }

        public InternetAddressList ReplyTo
        {
            get
            {
                if (message.ReplyTo != null && message.ReplyTo.Count() > 0)
                    return new InternetAddressList(message.ReplyTo.Select(address => new MailboxAddress(address.EmailAddress.Name, address.EmailAddress.Address)));
                else
                    return null;
            }
        }

        public bool IsReply => !string.IsNullOrEmpty(message.ConversationId);


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
            }
        }

        public DateTimeOffset? Date 
        { 
            get => message.SentDateTime; 
            set => message.SentDateTime = value; 
        }
        
        public string HtmlBody 
        { 
            get => message.Body.Content; 
            set => message.Body.Content = value; 
        }
        
        public string TextBody 
        {
            get
            {
                if (string.IsNullOrEmpty(message.Id) && requestBuilder != null)
                {
                    var options = new List<Option>
                    {
                        new HeaderOption("Prefer", "outlook.body-content-type='text'")
                    };
                    var task = Task.Run(async () => await requestBuilder.Messages[message.Id].Request(options).Select(x => x.Body).GetAsync());
                    var tmpMessage = task.Result;
                    if (tmpMessage != null)
                        return tmpMessage.Body.Content;
                }
                return message.Body.Content;
            }
            set => message.Body.Content = value; 
        }

        public ISimplifiedMessage CreateReply(bool replyToAll, string replyPrefix = "Re: ")
        {
            if (string.IsNullOrEmpty(message.Id) || requestBuilder == null)
                throw new ApplicationException("Cannot reply to a new email");

            string replySubject = "";
            if (string.IsNullOrEmpty(message.Subject) || !message.Subject.StartsWith(replyPrefix, StringComparison.OrdinalIgnoreCase))
                replySubject = $"{replyPrefix}{message.Subject}";
            else
                replySubject = message.Subject;

            var replyMessage = new Message
            {
                Subject = replySubject,
            };
            Task<Message> responseTask = null;
            if (replyToAll)
                responseTask = Task.Run(async () => await requestBuilder.Messages[message.Id].CreateReplyAll(replyMessage).Request().PostAsync());
            else
                responseTask = Task.Run(async () => await requestBuilder.Messages[message.Id].CreateReply(replyMessage).Request().PostAsync());

            return new GraphSimplifiedMessage(responseTask.Result, requestBuilder);
        }

        public void SaveToFile(string path)
        {
            throw new NotImplementedException();
        }
    }
}
