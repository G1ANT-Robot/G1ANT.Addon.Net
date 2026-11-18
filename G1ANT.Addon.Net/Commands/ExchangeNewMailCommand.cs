/**
*    Copyright(C) G1ANT Ltd, All rights reserved
*    Solution G1ANT.Addon, Project G1ANT.Addon.Net
*    www.g1ant.com
*
*    Licensed under the G1ANT license.
*    See License.txt file in the project root for full license information.
*
*/

using G1ANT.Addon.Net.API;
using G1ANT.Addon.Net.Models;
using G1ANT.Language;
using G1ANT.Language.Models;
using MailKit;
using MailKit.Search;
using Microsoft.Exchange.WebServices.Data;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace G1ANT.Addon.Net
{
    [Command(Name = "msexchange.newmail", Tooltip = "This command creates new empty mail message which can be modified and send using graph.send command.")]
    public class ExchangeNewMailCommand : Command
    {
        public class Arguments : CommandArguments
        {
            [Argument(Tooltip = "Structure describes authentication method")]
            public Structure Authentication { get; set; }

            [Argument(Tooltip = "User account")]
            public TextStructure Account { get; set; }

            [Argument(Tooltip = "Recipient's email address as text or list of addressess")]
            public Structure To { get; set; } = new TextStructure(string.Empty);

            [Argument(Tooltip = "Carbon copy address as text or list of addressess")]
            public Structure Cc { get; set; } = new TextStructure(string.Empty);

            [Argument(Tooltip = "Blind carbon copy address as text or list of addressess")]
            public Structure Bcc { get; set; } = new TextStructure(string.Empty);

            [Argument(Tooltip = "Message subject")]
            public TextStructure Subject { get; set; } = new TextStructure(string.Empty);

            [Argument(Tooltip = "Message html body, i.e. the main content of an email")]
            public TextStructure HtmlBody { get; set; } = new TextStructure(string.Empty);

            [Argument(Tooltip = "List of full paths to all files to be attached")]
            public ListStructure Attachments { get; set; }

            [Argument(Required = false, Tooltip = "Name of a variable where the new message will be stored")]
            public VariableStructure Result { get; set; } = new VariableStructure("result");
        }

        public ExchangeNewMailCommand(AbstractScripter scripter) : base(scripter)
        { }

        public void Execute(Arguments arguments)
        {
            EwsGate.EnsureReady();
            Run(arguments);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void Run(Arguments arguments)
        {
            IAuthenticationModel authenticator = null;
            string userEmail = arguments.Account?.Value;
            if (arguments.Authentication != null)
            {
                if (arguments.Authentication.Object is IAuthenticationModel model)
                {
                    authenticator = model;
                    if (userEmail is null)
                        userEmail = authenticator.Name;
                }
                else
                    throw new ArgumentException($"Authentication argument is incorrect type, try 'exchangeoauth' structure");
            }

            var client = new EwsApiManager(authenticator, userEmail);

            var sm = client.CreateMessage(
                arguments.Subject.Value,
                arguments.HtmlBody.Value,
                arguments.Attachments?.Value,
                CreateRecipients(arguments.To),
                CreateRecipients(arguments.Cc),
                CreateRecipients(arguments.Bcc)
                );

            Scripter.Variables.SetVariableValue(arguments.Result.Value, new MailStructure(sm));
        }

        private List<EmailAddress> CreateRecipients(Structure structure)
        {
            if (structure is ListStructure recipientList)
            {
                return recipientList.Value.Select(x => new EmailAddress { Address = x.ToString() }).ToList();
            }
            else if (structure is TextStructure recipientText)
            {
                if (string.IsNullOrEmpty(recipientText.Value))
                    return new List<EmailAddress>();

                return new List<EmailAddress> { new EmailAddress { Address = recipientText.Value } };
            }
            else
                throw new ApplicationException("Incompatible recipient value");
        }
    }
}
