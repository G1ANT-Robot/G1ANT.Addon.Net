using G1ANT.Addon.Net.API;
using G1ANT.Addon.Net.Models;
using G1ANT.Language;
using MimeKit;
using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Windows.Forms;

namespace G1ANT.Addon.Net.Commands
{
    [Command(Name = "msexchange.save", Tooltip = "Save Mail structure to selecteed file")]
    public class ExchangeMailSaveCommand : Command
    {
        public class Arguments : CommandArguments
        {
            [Argument(Tooltip = "Path to the file where message will be stored")]
            public TextStructure Path { get; set; }

            [Argument(Tooltip = "Mail structure to be saved")]
            public MailStructure Mail { get; set; }
        }

        public ExchangeMailSaveCommand(AbstractScripter scripter) : base(scripter)
        { }

        public void Execute(Arguments arguments)
        {
            EwsGate.EnsureReady();
            Run(arguments);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void Run(Arguments arguments)
        {
            var exchangeMail = arguments.Mail.Value as EwsSimplifiedMessage;
            exchangeMail.SaveToFile(arguments.Path?.Value);
        }
    }
}
