using G1ANT.Addon.Net.Models;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace G1ANT.Addon.Net.API
{
    public class GraphApiManager
    {
        private GraphServiceClient client;
        private string userName;

        public GraphApiManager(IAuthenticationModel authenticator, string userName)
        {
            var authProvider = new DelegateAuthenticationProvider(async (request) =>
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authenticator.GetToken());
            });

            var graphClient = new GraphServiceClient(authProvider);
            this.client = graphClient;
            this.userName = userName;
        }

        public List<GraphSimplifiedMessage> GetMessages(string folder, int limit, int skip, bool onlyUnreaded = false)
        {
            var filters = new List<string>();
            if (onlyUnreaded)
                filters.Add("IsRead eq false");
            var userRequestBuilder = this.client.Users[this.userName];
            var request = userRequestBuilder.MailFolders[folder].Messages.Request().Filter(string.Join(" and ", filters)).Top(limit).Skip(skip);
            var result = Task.Run(async () => await request.GetAsync());
            return result.Result.Select(x => new GraphSimplifiedMessage(x, userRequestBuilder)).ToList();
        }

        public List<string> GetMailFolders()
        {
            var request = this.client.Users[this.userName].MailFolders.Request().Top(100);
            var result = Task.Run(async () => await request.GetAsync());
            return result.Result.CurrentPage.Select(x => x.DisplayName).ToList();
        }

        public void MoveMailTo(GraphSimplifiedMessage message, string folder)
        {
            var request = this.client.Users[this.userName].MailFolders.Request().Filter($"DisplayName eq '{folder}'");
            var result = Task.Run(async () => await request.GetAsync());
            if (result.Result.CurrentPage.Count == 0)
                throw new ApplicationException($"Folder {folder} doesn't exist");

            var folderId = result.Result.CurrentPage[0].Id;
            var moveRequest = this.client.Users[this.userName].Messages[message.MessageId].Move(folderId).Request();
            Task.Run(async () => await moveRequest.PostAsync());
        }

        public void SendMessage(GraphSimplifiedMessage message)
        {
            var request = this.client.Users[this.userName].SendMail(message.Message, true).Request();

            var result = Task.Run(async () => await request.PostAsync());
            result.Wait();
        }
    }
}
