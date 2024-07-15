using MailKit;
using MailKit.Net.Imap;
using System;
using System.Collections.Generic;
using G1ANT.Addon.Net.Models;
using System.Linq;
using Org.BouncyCastle.Crypto;
using System.Windows.Forms;

namespace G1ANT.Addon.Net
{
    public sealed class ImapManager
    {
        private IAuthenticationModel authenticator;
        private ImapClient client;
        private Uri uri;

        public static ImapManager Instance { get; } = new ImapManager();

        private void ConnectClient(ImapClient client)
        {
            client.Connect(uri);
            authenticator?.Authenticate(client);
            client.Inbox.Open(FolderAccess.ReadWrite);
            client.Inbox.Subscribe();
        }

        public void DisconnectClient()
        {
            client.Disconnect(true);
        }

        public ImapClient CreateImapClient(IAuthenticationModel authenticator, Uri uri, int timeout)
        {
            var client = new ImapClient { Timeout = timeout };
            this.authenticator = authenticator;
            this.client = client;
            this.uri = uri;
            ConnectClient(this.client);
            return this.client;
        }

        public ImapClient GetClient()
        {
            return client;
        }

        public void Reconnect()
        {
            if (!client.IsConnected)
                ConnectClient(client);
        }

        public List<string> GetPersonalFolders()
        {
            ValidateConnection();
            return client.GetFolders(client.PersonalNamespaces.FirstOrDefault()).Select(x => x.FullName).ToList();
        }

        public Dictionary<string, string> GetSpecialFolders()
        {
            ValidateConnection();
            var result = new Dictionary<string, string>();
            foreach (var eFolder in (SpecialFolder[]) Enum.GetValues(typeof(SpecialFolder)))
            {
                var folder = client.GetFolder(eFolder);
                result.Add(eFolder.ToString(), folder.FullName);
            }
            return result;
        }

        public void MoveMailTo(SimplifiedMessageSummary mail, string folderName)
        {
            ValidateConnection();

            var originFolder = client.GetFolder(mail.Folder.FullName);
            var destinationFolder = client.GetFolder(folderName);

            ValidateFolders(originFolder, destinationFolder);

            destinationFolder.Open(FolderAccess.ReadWrite);
            originFolder.Open(FolderAccess.ReadWrite);
            originFolder.MoveTo(mail.UniqueId, destinationFolder);
        }

        public void AppendMailTo(SimplifiedMessageSummary mail, string folderName)
        {
            ValidateConnection();

            var destinationFolder = client.GetFolder(folderName);
            if (destinationFolder == null)
                throw new NullReferenceException($"Destination folder {folderName} does not exist.");

            destinationFolder.Append(mail.FullMessage, MessageFlags.Seen);
        }

        private void ValidateConnection()
        {
            if (!client.IsConnected || !client.IsAuthenticated)
                throw new Exception("Could not connect or authenticate on the server");
        }

        private void ValidateFolders(IMailFolder origin, IMailFolder destination)
        {
            if (origin == null)
            {
                throw new NullReferenceException($"Source folder {origin.Name} does not exist.");
            }
            if (destination == null)
            {
                throw new NullReferenceException($"Destination folder {destination.Name} does not exist.");
            }
        }
    }
}
