using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace G1ANT.Addon.Net.Models
{
    public class ExchangeOAuthModel : IAuthenticationModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }
        public string Host { get; set; }
        public bool Logger { get; set; }

        public string Name => Username;

        public void Authenticate(ImapClient client)
        {
            throw new NotImplementedException();
        }

        public void Authenticate(SmtpClient client)
        {
            throw new NotImplementedException();
        }

        public string GetToken()
        {
            throw new NotImplementedException();
        }
    }
}
