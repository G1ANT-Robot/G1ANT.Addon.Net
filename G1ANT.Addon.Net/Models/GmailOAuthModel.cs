using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using System;
using Google.Apis.Auth.OAuth2;
using G1ANT.Language;
using System.IO;
using Google.Apis.Util.Store;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Flows;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Security;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Google.Apis.Util;

namespace G1ANT.Addon.Net.Models
{
    public class GmailOAuthModel : IAuthenticationModel
    {
        public string Username { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Token { get; set; }
        public string Scope { get; set; } = "imap,smtp";
        public string CacheFolder { get; set; }

        private readonly Dictionary<string, string> _mappedScopes = new Dictionary<string, string>()
        {
            { "imap",  "https://mail.google.com/" },
            { "smtp",  "https://mail.google.com/" },
        };

        public GmailOAuthModel()
        {
            if (AbstractSettingsContainer.Instance != null)
                CacheFolder = Path.Combine(AbstractSettingsContainer.Instance.UserDocsAddonFolder.FullName, "gmail-client-secrets");
        }

        public string Name => Username;

        public void Authenticate(ImapClient client)
        {
            var token = Task.Run(async () => await GetAccessToken()).Result;
            client.Authenticate(new SaslMechanismOAuth2(Username, token));
        }

        public void Authenticate(SmtpClient client)
        {
            var token = Task.Run(async () => await GetAccessToken()).Result;
            client.Authenticate(new SaslMechanismOAuth2(Username, token));
        }

        private ClientSecrets GetClientSecrets()
        {
            return new ClientSecrets()
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret
            };
        }

        public string GetToken()
        {
            var token = Task.Run(async () => await GetAccessToken()).Result;
            return token;
        }

        private async Task<UserCredential> GetUserCredentials()
        {
            var token = new TokenResponse
            {
                RefreshToken = Token
            };
            var credentials = new UserCredential(
                new GoogleAuthorizationCodeFlow(
                    new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = GetClientSecrets(),
                        DataStore = GetDataStore(),
                    }),
                Username,
                token);
            if (credentials.Token.IsExpired(SystemClock.Default))
                await credentials.RefreshTokenAsync(new CancellationTokenSource(10000).Token);
            return credentials;
        }

        private async Task<string> GetAccessToken()
        {
            var credentials = await GetUserCredentials();
            return credentials.Token.AccessToken;
        }

        private string[] GetScopes()
        {
            var scopes = Scope.Split(',');
            var result = _mappedScopes.Where(x => scopes.Contains(x.Key)).Select(x => x.Value).Distinct();
            if (!result.Any())
                throw new ArgumentNullException($"Scope is not defined, allowed scopes: {string.Join(",", _mappedScopes.Select(x => x.Key))}");
            return result.Concat(new[] { "https://www.googleapis.com/auth/userinfo.email" }).ToArray();
        }

        private async Task AcquireTokenInteractive()
        {
            var datastore = GetDataStore();
            var authorizer = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GetClientSecrets(),
                    GetScopes(),
                    Username, 
                    CancellationToken.None,
                    datastore);

            var oauthSerivce = new Oauth2Service(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = authorizer
                });
            var userInfo = await oauthSerivce.Userinfo.Get().ExecuteAsync();

            if (userInfo.Email.ToLower() != Username.ToLower())
            {
                await datastore.DeleteAsync<object>(Username);
                throw new ArgumentException($"Logged user is not {Username}.");
            }

            Token = authorizer.Token.RefreshToken;
        }

        public void RequestTokenInteractive()
        {
            if (string.IsNullOrEmpty(ClientId))
                throw new ArgumentNullException(nameof(ClientId));
            if (string.IsNullOrEmpty(ClientSecret))
                throw new ArgumentNullException(nameof(ClientSecret));
            if (string.IsNullOrEmpty(Username))
                throw new ArgumentNullException(nameof(Username));

            Task.Run(async () => await AcquireTokenInteractive()).Wait();
            if (string.IsNullOrEmpty(Token))
                throw new NullReferenceException($"Cannot retrieve Token for user {Username}");
        }

        private IDataStore GetDataStore()
        {
            return CacheFolder != null ? new FileDataStore(CacheFolder, true) : null;
        }
    }
}
