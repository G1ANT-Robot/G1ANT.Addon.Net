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
using System.Linq;
using G1ANT.Addon.Net.API;
using G1ANT.Addon.Net.Models;
using G1ANT.Language;
using G1ANT.Language.Models;
using MailKit;
using MailKit.Search;
using Microsoft.Graph;
using MimeKit;

namespace G1ANT.Addon.Net
{
    [Command(Name = "msgraph.getmails", Tooltip = "This command uses the Graph API to check an email inbox and allows the user to analyze their messages received within a specified time span, with the option to consider only unread messages and/or mark all of the checked ones as read")]
    public class GraphGetMailsCommand : Command
    {
        public class Arguments : CommandArguments
        {
            [Argument(Tooltip = "Structure describes authentication method")]
            public Structure Authentication { get; set; }

            [Argument(Tooltip = "User account")]
            public TextStructure Account { get; set; }

            [Argument(Tooltip = "Folder to fetch emails from")]
            public TextStructure Folder { get; set; } = new TextStructure("INBOX");

            [Argument(Tooltip = "Sets the page size of results")]
            public IntegerStructure Count { get; set; } = new IntegerStructure(20);

            [Argument(Tooltip = "Skips items in a result set")]
            public IntegerStructure Skip { get; set; } = new IntegerStructure(0);

            [Argument(Required = false, Tooltip = "Messages which id contains text")]
            public TextStructure IdContains { get; set; }

            [Argument(Required = false, Tooltip = "Messages which subject contains text")]
            public TextStructure SubjectContains { get; set; }

            [Argument(Required = false, Tooltip = "If set to `true`, only unread messages will be checked")]
            public BooleanStructure OnlyUnreadMessages { get; set; } = new BooleanStructure(false);

            [Argument(Required = false, Tooltip = "Name of a list variable where the returned mail variables will be stored")]
            public VariableStructure Result { get; set; } = new VariableStructure("result");
        }

        public GraphGetMailsCommand(AbstractScripter scripter) : base(scripter)
        { }

        public void Execute(Arguments arguments)
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
                    throw new ArgumentException($"Authentication argument is incorrect type, try 'officeoauth' structure");
            }
            var client = new GraphApiManager(authenticator, userEmail);

            var messages = client.GetMessages(
                folder: arguments.Folder.Value,
                limit: arguments.Count.Value,
                skip: arguments.Skip.Value,
                onlyUnreaded: arguments.OnlyUnreadMessages.Value,
                idContains: arguments.IdContains?.Value,
                subjectContains: arguments.SubjectContains?.Value
            );

            var messageList = new ListStructure();
            foreach (var message in messages)
            {
                var structure = new MailStructure(message, null, null);
                messageList.AddItem(structure);
            }

            Scripter.Variables.SetVariableValue(arguments.Result.Value, messageList);

        }
    }
}
