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

namespace G1ANT.Addon.Net
{
    [Command(Name = "msexchange.getmailfolders", Tooltip = "This command returns all account personal folders.")]
    public class ExchangeGetMailFoldersCommand : Command
    {
        public class Arguments : CommandArguments
        {
            [Argument(Tooltip = "Structure describes authentication method")]
            public Structure Authentication { get; set; }

            [Argument(Tooltip = "User account")]
            public TextStructure Account { get; set; }

            [Argument(Required = false, Tooltip = "Name of a list variable where the returned mail variables will be stored")]
            public VariableStructure Result { get; set; } = new VariableStructure("result");
        }

        public ExchangeGetMailFoldersCommand(AbstractScripter scripter) : base(scripter)
        { }

        public void Execute(Arguments arguments)
        {
            EwsGate.EnsureReady();
            Run(arguments);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void Run(Arguments arguments)
        {
            IAuthenticationModel authenticator = null;
            string userEmail = arguments.Account?.Value;
            if (arguments.Authentication != null)
            {
                if (arguments.Authentication.Object is IAuthenticationModel model)
                {
                    var exchangeAuthenticator = (ExchangeOAuthModel)model;
                    if(exchangeAuthenticator.Host == null)
                    {
                        throw new Exception($"Not defined 'Host' in argument {nameof(arguments.Authentication)}.");
                    }

                    authenticator = exchangeAuthenticator;
                    if (userEmail is null)
                        userEmail = authenticator.Name;
                }
                else
                    throw new ArgumentException($"Authentication argument is incorrect type, try 'exchangeoauth' structure");
            }
            else
            {
                throw new ArgumentException($"Authentication argument is required, try 'exchangeoauth' structure");
            }

            var client = new EwsApiManager(authenticator, userEmail);
            var folders = client.GetMailFolders();
            var result = new ListStructure(folders.ToList<object>(), "", Scripter);
            Scripter.Variables.SetVariableValue(arguments.Result.Value, result);
        }
    }
}
