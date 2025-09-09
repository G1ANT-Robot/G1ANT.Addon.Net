using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using G1ANT.Addon.Net.API;
using G1ANT.Language;

namespace G1ANT.Addon.Net.Models
{
    public class OfficeOAuthModel : IAuthenticationModel
    {
        public string Username { get; set; }
        public string ClientId { get; set; }
        public string TenantId { get; set; }
        private string Token { get; set; }
        public string Scope { get; set; } = "imap,smtp";
        public string CacheFolder { get; set; } = AbstractSettingsContainer.Instance?.UserDocsAddonFolder.FullName;

        private readonly Dictionary<string, string> _mappedScopes = new Dictionary<string, string>()
        {
            { "imap",  "https://outlook.office.com/IMAP.AccessAsUser.All" },
            { "smtp",  "https://outlook.office.com/SMTP.Send" },
            { "ms-graph-api",  "https://graph.microsoft.com/.default" },
            //{ "pop",  "https://outlook.office.com/POP.AccessAsUser.All" },
        };

        private IPublicClientApplication publicClientApplication;
        private TokenCacheHelper tokenCacheHelper;

        public OfficeOAuthModel()
        {
        }

        public string Name => Username;

        private IPublicClientApplication BuildPublicClientApplication()
        {
            if (publicClientApplication == null)
            {
                publicClientApplication = PublicClientApplicationBuilder.CreateWithApplicationOptions(
                    new PublicClientApplicationOptions
                    {
                        ClientId = ClientId,
                        TenantId = TenantId,
                        RedirectUri = "https://login.microsoftonline.com/common/oauth2/nativeclient"
                    }).Build();
                tokenCacheHelper = new TokenCacheHelper(CacheFolder);
                tokenCacheHelper.EnableSerialization(publicClientApplication.UserTokenCache);
            }
            return publicClientApplication;
        }

        private string[] GetScopes()
        {
            var scopes = Scope.Split(',');
            var result = _mappedScopes.Where(x => scopes.Contains(x.Key)).Select(x => x.Value).ToArray();
            if (result.Count() == 0)
                throw new ArgumentNullException($"Scope is not defined, allowed scopes: {string.Join(",", _mappedScopes.Select(x => x.Key))}");
            return result;
        }

        private async Task<AuthenticationResult> GetCachedOAuthToken()
        {
            var app = BuildPublicClientApplication();
            var scopes = GetScopes();
            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts?.FirstOrDefault(x => x.Username.Equals(Username, StringComparison.CurrentCultureIgnoreCase));
            if (firstAccount == null)
                throw new NullReferenceException("Cannot find account in cache.");
            return await app.AcquireTokenSilent(scopes, firstAccount).ExecuteAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenInteractive()
        {
            var app = BuildPublicClientApplication();
            var scopes = GetScopes();
            return await app.AcquireTokenInteractive(scopes).WithLoginHint(Username).ExecuteAsync();
        }

        public string GetToken()
        {
            var task = Task.Run(async () => await GetCachedOAuthToken());
            Token = task.Result.AccessToken;
            if (string.IsNullOrEmpty(Token))
                throw new NullReferenceException($"Cannot retrieve Token for user {Username}");
            return Token;
        }

        public void RequestTokenInteractive()
        {
            if (string.IsNullOrEmpty(TenantId))
                throw new ArgumentNullException(nameof(TenantId));
            if (string.IsNullOrEmpty(ClientId))
                throw new ArgumentNullException(nameof(ClientId));

            var task = Task.Run(async () => await AcquireTokenInteractive());
            Token = task.Result.AccessToken;
            Username = task.Result.Account?.Username ?? Username;
            if (string.IsNullOrEmpty(Token))
                throw new NullReferenceException($"Cannot retrieve Token for user {Username}");
        }

        public void Authenticate(ImapClient client)
        {
            string token = GetToken();
            client.Authenticate(new SaslMechanismOAuth2(Username, token));
        }

        public void Authenticate(SmtpClient client)
        {
            string token = GetToken();
            client.Authenticate(new SaslMechanismOAuth2(Username, token));
        }
    }
}
