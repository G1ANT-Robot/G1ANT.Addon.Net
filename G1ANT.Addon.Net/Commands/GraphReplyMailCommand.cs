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
    [Command(Name = "msgraph.replymail", Tooltip = "Send mail messages created by newmail or reply commands.")]
    public class GraphReplyMailCommand : Command
    {
        public class Arguments : CommandArguments
        {
            [Argument(Required = true, Tooltip = "Message to reply to")]
            public MailStructure Mail { get; set; }

            [Argument(Required = false, Tooltip = "Reply to all recipients or only to the sender")]
            public BooleanStructure ReplyToAll { get; set; } = new BooleanStructure(false);

            [Argument(Required = false, Tooltip = "Prefix added to the subject of reply message")]
            public TextStructure SubjectPrefix { get; set; } = new TextStructure("Re: ");

            [Argument(Required = false, Tooltip = "Name of a variable where the reply message will be stored")]
            public VariableStructure Result { get; set; } = new VariableStructure("result");
        }

        public GraphReplyMailCommand(AbstractScripter scripter) : base(scripter)
        { }

        public void Execute(Arguments arguments)
        {
            var replyMail = arguments.Mail.CreateReply(arguments.ReplyToAll.Value, arguments.SubjectPrefix.Value);
            Scripter.Variables.SetVariableValue(arguments.Result.Value, replyMail);
        }
    }
}
