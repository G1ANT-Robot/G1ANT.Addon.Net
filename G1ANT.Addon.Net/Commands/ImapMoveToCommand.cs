﻿/**
*    Copyright(C) G1ANT Ltd, All rights reserved
*    Solution G1ANT.Addon, Project G1ANT.Addon.Net
*    www.g1ant.com
*
*    Licensed under the G1ANT license.
*    See License.txt file in the project root for full license information.
*
*/
using System;
using G1ANT.Language;
using MailKit;

namespace G1ANT.Addon.Net.Commands
{
    [Command(Name = "imap.moveto", Tooltip = "This command uses the IMAP protocol to move an email to the new folder")]
    public class ImapMoveToCommand : Command
    {
        public class Arguments : CommandArguments
        {
            [Argument(Required = true, Tooltip = "Mail message to be moved")]
            public MailStructure Mail { get; set; }

            [Argument(Required = true, Tooltip = "Name of the destination folder")]
            public TextStructure Folder { get; set; }
        }

        public ImapMoveToCommand(AbstractScripter scripter) : base(scripter)
        { }

        public void Execute(Arguments arguments)
        {
            ImapManager.Instance.MoveMailTo(arguments.Mail.Value, arguments.Folder.Value);
        }
    }
}
