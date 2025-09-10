using G1ANT.Addon.Net.Models;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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

        protected async Task<MailFolder> GetMailFolderByName(string folder, MailFolder parentFolder = null)
        {
            var folderItems = folder.Split('/').ToList();
            if (folderItems.Count == 0 || string.IsNullOrEmpty(folderItems[0]))
                return parentFolder;

            folder = folderItems[0];
            folderItems.RemoveAt(0);

            MailFolder selFolder = null;
            if (parentFolder != null)
            {
                var userRequestBuilder = this.client.Users[this.userName];
                var folderRequest = userRequestBuilder.MailFolders[parentFolder.Id].ChildFolders.Request().Filter($"displayName eq '{folder}'");
                var resultFolder = await folderRequest.GetAsync();
                selFolder = resultFolder.CurrentPage.FirstOrDefault();
            }
            else
            {
                var userRequestBuilder = this.client.Users[this.userName];
                var folderRequest = userRequestBuilder.MailFolders.Request().Filter($"displayName eq '{folder}'");
                var resultFolder = await folderRequest.GetAsync();
                selFolder = resultFolder.CurrentPage.FirstOrDefault();
            }
            if (selFolder == null)
                return null;
            if (folderItems.Count > 0)
                return await GetMailFolderByName(string.Join("/", folderItems), selFolder);
            return selFolder;
        }

        public List<GraphSimplifiedMessage> GetMessages(string folder, int limit, int skip, bool onlyUnreaded = false, string idContains = null, string subjectContains = null)
        {
            var filters = new List<string>();
            if (onlyUnreaded)
                filters.Add("IsRead eq false");
            if (!string.IsNullOrEmpty(idContains))
                filters.Add($"contains(InternetMessageId, '{idContains}')");
            if (!string.IsNullOrEmpty(subjectContains))
                filters.Add($"contains(Subject, '{subjectContains}')");

            var userRequestBuilder = this.client.Users[this.userName];
            var resultFolder = Task.Run(async () => await GetMailFolderByName(folder));
            var selFolder = resultFolder.Result;
            if (selFolder == null)
                throw new ApplicationException($"Cannot find folder {folder}");

            var request = userRequestBuilder.MailFolders[selFolder.Id].Messages.Request().Filter(string.Join(" and ", filters)).Top(limit).Skip(skip);
            var result = Task.Run(async () => await request.GetAsync());
            return result.Result.Select(x => new GraphSimplifiedMessage(x, userRequestBuilder)).ToList();
        }

        public List<string> GetMailFolders(string folder = null)
        {
            var request = this.client.Users[this.userName].MailFolders.Request().Top(100);
            var result = Task.Run(async () => await request.GetAsync());
            return result.Result.CurrentPage.Select(x => x.DisplayName).ToList();
        }

        public void MoveMailTo(GraphSimplifiedMessage message, string folder)
        {
            var result = Task.Run(async () => await GetMailFolderByName(folder));
            if (result.Result == null)
                throw new ApplicationException($"Folder {folder} doesn't exist");

            var folderId = result.Result.Id;
            var moveRequest = this.client.Users[this.userName].Messages[message.MessageId].Move(folderId).Request();
            Task.Run(async () => await moveRequest.PostAsync());
        }

        public void SendMessage(GraphSimplifiedMessage message)
        {
            var request = this.client.Users[this.userName].SendMail(message.Message, true).Request();

            var result = Task.Run(async () => await request.PostAsync());
            result.Wait();
        }

        public static FileAttachment CreateAttachment(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            byte[] contentBytes = System.IO.File.ReadAllBytes(filePath);
            string contentType = MimeMapping.GetMimeMapping(fileName);

            return new FileAttachment
            {
                ODataType = "#microsoft.graph.fileAttachment",
                ContentBytes = contentBytes,
                ContentType = contentType,
                ContentId = Guid.NewGuid().ToString(),
                Name = fileName
            };
        }
    }
}
