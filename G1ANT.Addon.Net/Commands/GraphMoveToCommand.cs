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
    [Command(Name = "msgraph.moveto", Tooltip = "This command uses the IMAP protocol to move an email to the new folder.")]
    public class GraphMoveToCommand : Command
    {
        public class Arguments : CommandArguments
        {
            [Argument(Tooltip = "Structure describes authentication method")]
            public Structure Authentication { get; set; }

            [Argument(Tooltip = "User account")]
            public TextStructure Account { get; set; }

            [Argument(Required = true, Tooltip = "Mail message to be moved")]
            public MailStructure Mail { get; set; }

            [Argument(Required = true, Tooltip = "Name of the destination folder")]
            public TextStructure Folder { get; set; }
        }

        public GraphMoveToCommand(AbstractScripter scripter) : base(scripter)
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
            client.MoveMailTo(simplifiedMessgae, arguments.Folder.Value);
        }
    }
}
