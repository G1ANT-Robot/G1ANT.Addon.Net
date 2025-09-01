using MailKit.Net.Imap;
using MailKit.Net.Smtp;

namespace G1ANT.Addon.Net.Models
{
    public interface IAuthenticationModel
    {
        string Name { get; }
        string GetToken();
        void Authenticate(ImapClient client);
        void Authenticate(SmtpClient client);
    }
}
