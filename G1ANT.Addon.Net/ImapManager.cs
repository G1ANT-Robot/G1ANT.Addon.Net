﻿using MailKit;
using MailKit.Net.Imap;
using System;
using System.Net;

namespace G1ANT.Addon.Net
{
    public sealed class ImapManager
    {
        private NetworkCredential credentials;
        private ImapClient client;
        private Uri uri;

        public static ImapManager Instance { get; } = new ImapManager();

        private void ConnectClient(ImapClient client)
        {
            client.Connect(uri);
            if (credentials != null)
                client.Authenticate(credentials);
            client.Inbox.Open(FolderAccess.ReadWrite);
            client.Inbox.Subscribe();
        }

        public void DisconnectClient()
        {
            client.Disconnect(true);
        }

        public ImapClient CreateImapClient(NetworkCredential credentials, Uri uri, int timeout)
        {
            var client = new ImapClient { Timeout = timeout };
            this.credentials = credentials;
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
    }
}
