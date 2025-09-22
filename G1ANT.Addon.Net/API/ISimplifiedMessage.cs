using MailKit;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace G1ANT.Addon.Net.API
{
    public interface ISimplifiedMessage
    {
        bool IsUnread { get; set; }
        List<object> Attachments { get; set; }
        string MessageId { get; }
        UniqueId UniqueId { get; }
        string Subject { get; set; }
        InternetAddressList To { get; set; }
        InternetAddressList From { get; set; }
        InternetAddressList Cc { get; set; }
        InternetAddressList Bcc { get; set; }
        InternetAddressList ReplyTo { get; set; }
        bool IsReply { get; }
        string Priority { get; set; }
        DateTimeOffset? Date {  get; set; }
        string HtmlBody { get; set; }
        string TextBody { get; set; }

        ISimplifiedMessage CreateReply(bool replyToAll, string replyPrefix = "Re: ");
        void SaveToFile(string path);
    }
}
