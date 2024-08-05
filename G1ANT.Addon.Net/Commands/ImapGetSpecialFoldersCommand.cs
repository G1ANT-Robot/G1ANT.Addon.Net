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
    [Command(Name = "imap.getspecialfolders", Tooltip = "This command returns special folders.")]
    public class ImapGetSpecialFoldersCommand : Command
    {
        public class Arguments : CommandArguments
        {
            [Argument(Required = false, Tooltip = "Name of a dictionary variable where the returned folders will be stored")]
            public VariableStructure Result { get; set; } = new VariableStructure("result");
        }

        public ImapGetSpecialFoldersCommand(AbstractScripter scripter) : base(scripter)
        { }

        public void Execute(Arguments arguments)
        {
            var folders = ImapManager.Instance.GetSpecialFolders();
            var FoldersObj = folders.ToDictionary(x => x.Key, x => (object)x.Value);
            var result = new DictionaryStructure(FoldersObj, "", Scripter);
            Scripter.Variables.SetVariableValue(arguments.Result.Value, result);
        }
    }
}
