using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using System;
using System.Net;

namespace G1ANT.Addon.Net.Models
{
    public class SimpleAuthenticationModel : IAuthenticationModel
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public SimpleAuthenticationModel()
        { }

        public SimpleAuthenticationModel(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public string Name => Username;

        public string GetToken()
        {
            throw new NotImplementedException();
        }

        public void Authenticate(ImapClient client)
        {
            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
            {
                var credentials = new NetworkCredential(Username, Password);
                client.Authenticate(credentials);
            }
        }

        public void Authenticate(SmtpClient client)
        {
            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
            {
                var credentials = new NetworkCredential(Username, Password);
                client.Authenticate(credentials);
            }
        }
    }
}
