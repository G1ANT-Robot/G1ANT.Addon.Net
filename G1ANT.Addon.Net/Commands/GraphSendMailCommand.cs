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
    [Command(Name = "msgraph.sendmail", Tooltip = "Send mail messages created by newmail or reply commands.")]
    public class GraphSendMailCommand : Command
    {
        public class Arguments : CommandArguments
        {
            [Argument(Tooltip = "Structure describes authentication method")]
            public Structure Authentication { get; set; }

            [Argument(Tooltip = "User account")]
            public TextStructure Account { get; set; }

            [Argument(Required = true, Tooltip = "Message to reply to")]
            public MailStructure Mail { get; set; }
        }

        public GraphSendMailCommand(AbstractScripter scripter) : base(scripter)
        { }

        public void Execute(Arguments arguments)
        {
            var simplifiedMessgae = arguments.Mail.Value as GraphSimplifiedMessage;
            if (simplifiedMessgae == null)
                throw new ArgumentException("Message type is incompatible");

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
            client.SendMessage(simplifiedMessgae);
        }
    }
}
