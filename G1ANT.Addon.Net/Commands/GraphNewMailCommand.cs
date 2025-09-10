/**
*    Copyright(C) G1ANT Ltd, All rights reserved
*    Solution G1ANT.Addon, Project G1ANT.Addon.Net
*    www.g1ant.com
*
*    Licensed under the G1ANT license.
*    See License.txt file in the project root for full license information.
*
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using G1ANT.Addon.Net.API;
using G1ANT.Addon.Net.Models;
using G1ANT.Language;
using G1ANT.Language.Models;
using MailKit;
using MailKit.Search;
using Microsoft.Graph;
using MimeKit;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace G1ANT.Addon.Net
{
    [Command(Name = "msgraph.newmail", Tooltip = "This command creates new empty mail message which can be modified and send using graph.send command.")]
    public class GraphNewMailCommand : Command
    {
        public class Arguments : CommandArguments
        {
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

        public GraphNewMailCommand(AbstractScripter scripter) : base(scripter)
        { }

        public void Execute(Arguments arguments)
        {
            var attachments = new MessageAttachmentsCollectionPage();
            if (arguments.Attachments != null)
            {
                foreach (var path in arguments.Attachments?.Value)
                    attachments.Add(GraphApiManager.CreateAttachment(path.ToString()));
            }
            var message = new Message
            {
                Subject = arguments.Subject.Value,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = arguments.HtmlBody.Value,
                },
                ToRecipients = CreateRecipients(arguments.To),
                CcRecipients = CreateRecipients(arguments.Cc),
                BccRecipients = CreateRecipients(arguments.Bcc),
                Attachments = attachments,
            };
            Scripter.Variables.SetVariableValue(arguments.Result.Value, new MailStructure(new GraphSimplifiedMessage(message, null)));
        }

        private List<Recipient> CreateRecipients(Structure structure)
        {
            if (structure is ListStructure recipientList)
            {
                return recipientList.Value.Select(
                    x => new Recipient { EmailAddress = new EmailAddress { Address = x.ToString() } }
                ).ToList();
            }
            else if (structure is TextStructure recipientText)
            {
                if (string.IsNullOrEmpty(recipientText.Value))
                    return new List<Recipient>();
                return new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress { Address = recipientText.Value }
                        }
                    };
            }
            else
                throw new ApplicationException("Incompatible recipient value");
        }
    }
}
