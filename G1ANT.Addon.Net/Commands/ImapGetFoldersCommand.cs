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
using System.Linq;

namespace G1ANT.Addon.Net.Commands
{
    [Command(Name = "imap.getfolders", Tooltip = "This command returns all account personal folders.")]
    public class ImapGetFoldersCommand : Command
    {
        public class Arguments : CommandArguments
        {
            [Argument(Required = false, Tooltip = "Name of a list variable where the returned folders will be stored")]
            public VariableStructure Result { get; set; } = new VariableStructure("result");

            [Argument(Required = false, Tooltip = "Folder name")]
            public TextStructure Folder { get; set; }
        }

        public ImapGetFoldersCommand(AbstractScripter scripter) : base(scripter)
        { }

        public void Execute(Arguments arguments)
        {
            var folders = ImapManager.Instance.GetPersonalFolders(arguments.Folder?.Value);
            var result = new ListStructure(folders.ToList<object>(), "", Scripter);
            Scripter.Variables.SetVariableValue(arguments.Result.Value, result);
        }
    }
}
