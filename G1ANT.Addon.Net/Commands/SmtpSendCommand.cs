/**
*    Copyright(C) G1ANT Ltd, All rights reserved
*    Solution G1ANT.Addon, Project G1ANT.Addon.Net
*    www.g1ant.com
*
*    Licensed under the G1ANT license.
*    See License.txt file in the project root for full license information.
*
*/
using G1ANT.Language;
using MimeKit;
using System;
using System.Linq;

namespace G1ANT.Addon.Net.Commands
{
    [Command(Name = "smtp.send", Tooltip = "This command uses the SMTP protocol to send messages created by smt.newmessage or smtp.reply commands")]
    public class SmtpSendCommand : Command
    {
        public class Arguments : CommandArguments
        {
            [Argument(Required = true, Tooltip = "Message to reply to")]
            public MailStructure Mail { get; set; }
        }

        public SmtpSendCommand(AbstractScripter scripter) : base(scripter)
        { }

        public void Execute(Arguments arguments)
        {
            var simplifiedMessgae = arguments.Mail.Value as SimplifiedMessageSummary;
            if (simplifiedMessgae == null)
                throw new ArgumentException("Message type is incompatible");


            if (simplifiedMessgae?.FullMessage == null)
                throw new Exception("Mail is not defined or is empty");

            var client = SmtpManager.Instance.GetClient();
            if (client == null)
                throw new Exception("Smtp client has not be initialized, use smtp.open to estaplish connection");

            if (client.IsConnected && client.IsAuthenticated)
            {
                var message = simplifiedMessgae.FullMessage;
                RemoveEmptyMailboxes(message);
                client.Send(message);      
            }
            else
            {
                throw new Exception("Could not connect or authenticate on the server");
            }
        }

        private void RemoveEmptyMailboxes(MimeMessage message)
        {
            RemoveEmptyMailboxes(message.To);
            RemoveEmptyMailboxes(message.Cc);
            RemoveEmptyMailboxes(message.Bcc);
        }

        private void RemoveEmptyMailboxes(InternetAddressList addressess)
        {
            var toRemove = addressess.Cast<MailboxAddress>().Where(x => string.IsNullOrWhiteSpace(x.Address)).ToList();
            foreach (var r in toRemove)
                addressess.Remove(r);
        }
    }
}
