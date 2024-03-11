using G1ANT.Language;
using MimeKit;
using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Windows.Forms;

namespace G1ANT.Addon.Net.Commands
{
    [Command(Name = "mail.save", Tooltip = "Save Mail structure to selecteed file")]
    public class MailSaveCommand : Command
    {
        public class Arguments : CommandArguments
        {
            [Argument(Tooltip = "Path to the file where message will be stored")]
            public TextStructure Path { get; set; }

            [Argument(Tooltip = "Mail structure to be saved")]
            public MailStructure Mail { get; set; }
        }

        public MailSaveCommand(AbstractScripter scripter) : base(scripter)
        { }

        public void Execute(Arguments arguments)
        {
            arguments.Mail?.Value?.SaveToFile(arguments.Path?.Value);
        }
    }
}
