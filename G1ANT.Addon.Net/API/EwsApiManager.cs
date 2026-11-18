using G1ANT.Addon.Net.Listeners;
using G1ANT.Addon.Net.Models;
using G1ANT.Language;
using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace G1ANT.Addon.Net.API
{
    internal class EwsApiManager
    {
        private ExchangeService _service;
        private string _userEmail;

        public EwsApiManager(IAuthenticationModel authenticator, string userEmail)
        {
            if (authenticator == null)
                throw new ArgumentNullException(nameof(authenticator), "Wymagany argument authentication (exchangeoauth).");

            if ((authenticator is ExchangeOAuthModel) == false)
            {
                throw new InvalidCastException($"Autentykator musi być zgodny z exchangeoauth");
            }

            var exchangeAuthenticator = (ExchangeOAuthModel)authenticator;

            this._service = new ExchangeService(ExchangeVersion.Exchange2013_SP1);

            // Konfiguracja poświadczeń domenowych (NTLM / Windows Credentials)

            if (string.IsNullOrEmpty(exchangeAuthenticator.Username))
            {
                this._service.Credentials = new WebCredentials(CredentialCache.DefaultNetworkCredentials);
            }
            else
            {
                if (!string.IsNullOrEmpty(exchangeAuthenticator.Domain))
                    this._service.Credentials = new WebCredentials(exchangeAuthenticator.Username, exchangeAuthenticator.Password, exchangeAuthenticator.Domain);
                else
                    this._service.Credentials = new WebCredentials(exchangeAuthenticator.Username, exchangeAuthenticator.Password);
            }

            if (exchangeAuthenticator.Host.StartsWith("http"))
                this._service.Url = new Uri($"{exchangeAuthenticator.Host}/EWS/Exchange.asmx");
            else
                this._service.Url = new Uri($"https://{exchangeAuthenticator.Host}/EWS/Exchange.asmx");

            if (exchangeAuthenticator.Logger)
            {
                this._service.TraceEnabled = true;
                this._service.TraceFlags = TraceFlags.All;

                var logPath = Path.Combine(AbstractSettingsContainer.Instance.UserDocsAddonFolder.FullName, "exchangetracer.log");

                var fileStream = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                this._service.TraceListener = new EwsListener(new StreamWriter(fileStream) { AutoFlush = true });
            }

            this._userEmail = userEmail;

            ServicePointManager.ServerCertificateValidationCallback = (s, cert, chain, errors) =>
                errors == SslPolicyErrors.None || (s is HttpWebRequest r && r.RequestUri.Host == exchangeAuthenticator.Host);
        }



        /// <summary>
        /// Rekurencyjne wyszukiwanie folderu po ścieżce (np. "Inbox/Projekty/2026")
        /// </summary>
        protected Folder GetFolderByName(string folderPath, FolderId parentFolderId = null)
        {
            var folderItems = folderPath.Split('/').Where(x => !string.IsNullOrEmpty(x)).ToList();
            if (folderItems.Count == 0)
                return null;

            string currentFolderName = folderItems[0];
            folderItems.RemoveAt(0);

            // Domyślnie szukamy w katalogu głównym skrzynki (MsgFolderRoot) jeśli brak rodzica
            FolderId currentParentId = parentFolderId ?? new FolderId(WellKnownFolderName.MsgFolderRoot, new Mailbox(this._userEmail));

            FolderView folderView = new FolderView(100);
            FindFoldersResults findResults = this._service.FindFolders(currentParentId, folderView);

            Folder matchedFolder = findResults.Folders.FirstOrDefault(f => f.DisplayName.Equals(currentFolderName, StringComparison.OrdinalIgnoreCase));

            if (matchedFolder == null)
                return null;

            if (folderItems.Count > 0)
            {
                return GetFolderByName(string.Join("/", folderItems), matchedFolder.Id);
            }

            return matchedFolder;
        }

        protected Folder ResolveFolderPath(string path)
        {
            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            FolderId currentId = new FolderId(WellKnownFolderName.MsgFolderRoot, new Mailbox(this._userEmail));
            Folder current = null;

            foreach (string part in parts)
            {
                var view = new FolderView(2)
                {
                    Traversal = FolderTraversal.Shallow,
                    PropertySet = new PropertySet(BasePropertySet.IdOnly, FolderSchema.DisplayName)
                };

                var filter = new SearchFilter.IsEqualTo(FolderSchema.DisplayName, part);
                var found = this._service.FindFolders(currentId, filter, view);

                if (found.Folders.Count == 0)
                    throw new ApplicationException($"Folder '{part}' (ścieżka: {path}) nie istnieje");

                current = found.Folders[0];
                currentId = current.Id;
            }

            return current;
        }

        public List<EwsSimplifiedMessage> GetMessages(string folderpath, int limit, int skip, bool onlyUnreaded = false, string idContains = null, string subjectContains = null)
        {
            Folder targetFolder = ResolveFolderPath(folderpath);
            if (targetFolder == null)
                throw new ApplicationException($"Cannot find folder {folderpath}");

            // Budowanie filtrów wyszukiwania (SearchFilter w EWS)
            var filters = new List<SearchFilter>();
            if (onlyUnreaded)
                filters.Add(new SearchFilter.IsEqualTo(EmailMessageSchema.IsRead, false));
            if (!string.IsNullOrEmpty(idContains))
                filters.Add(new SearchFilter.ContainsSubstring(EmailMessageSchema.InternetMessageId, idContains));
            if (!string.IsNullOrEmpty(subjectContains))
                filters.Add(new SearchFilter.ContainsSubstring(ItemSchema.Subject, subjectContains));

            SearchFilter searchFilter = null;
            if (filters.Count > 0)
            {
                searchFilter = filters.Count == 1
                    ? filters[0]
                    : new SearchFilter.SearchFilterCollection(LogicalOperator.And, filters.ToArray());
            }

            // Paginacja w EWS (ItemView przyjmuje rozmiar strony oraz offset/skip)
            ItemView view = new ItemView(limit, skip, OffsetBasePoint.Beginning);
            view.PropertySet = new PropertySet(BasePropertySet.FirstClassProperties);

            FindItemsResults<Item> findResults = searchFilter != null
                ? this._service.FindItems(targetFolder.Id, searchFilter, view)
                : this._service.FindItems(targetFolder.Id, view);

            // Zwracamy listę przekonwertowaną na EwsSimplifiedMessage, przekazując instancję 'service'
            return findResults.Items
                .OfType<EmailMessage>()
                .Select(x => new EwsSimplifiedMessage(x, this._service))
                .ToList();
        }

        public List<string> GetMailFolders(string folder = null)
        {
            FolderId parentFolderId = string.IsNullOrEmpty(folder)
                ? new FolderId(WellKnownFolderName.MsgFolderRoot, new Mailbox(this._userEmail))
                : GetFolderByName(folder)?.Id;

            if (parentFolderId == null && !string.IsNullOrEmpty(folder))
                throw new ApplicationException($"Folder {folder} doesn't exist");

            FolderView folderView = new FolderView(100)
            {
                Traversal = FolderTraversal.Deep,
                PropertySet = new PropertySet(BasePropertySet.IdOnly, FolderSchema.DisplayName, FolderSchema.ParentFolderId, FolderSchema.FolderClass)
            };
            //FindFoldersResults findResults = this._service.FindFolders(parentFolderId, folderView);
            //return findResults.Folders.Select(x => x.DisplayName).ToList();

            //filtrujemy tylko skrzynki pocztowe (IPF.Note) i ich podfoldery, aby uniknąć folderów systemowych, kontaktów, kalendarzy itp.
            SearchFilter mailFilter = new SearchFilter.SearchFilterCollection(
                LogicalOperator.Or,
                new SearchFilter.IsEqualTo(FolderSchema.FolderClass, "IPF.Note"),
                new SearchFilter.ContainsSubstring(FolderSchema.FolderClass, "IPF.Note.", ContainmentMode.Prefixed, ComparisonMode.IgnoreCase)
                );

            var allFolders = new List<Folder>();
            FindFoldersResults results;
            do
            {
                results = this._service.FindFolders(parentFolderId, mailFilter, folderView);
                allFolders.AddRange(results.Folders);
                folderView.Offset += results.Folders.Count;
            }
            while (results.MoreAvailable);

            var byId = allFolders.ToDictionary(f => f.Id.UniqueId);

            string BuildPath(Folder f)
            {
                var parts = new Stack<string>();
                Folder current = f;
                while (current != null)
                {
                    parts.Push(current.DisplayName);
                    byId.TryGetValue(current.ParentFolderId.UniqueId, out current);
                }
                return string.Join("/", parts);
            }

            return allFolders.Select(BuildPath).ToList();
        }

        public EwsSimplifiedMessage CreateMessage(string subject, string htmlbody, List<object> attachments, List<EmailAddress> to, List<EmailAddress> cc, List<EmailAddress> bcc)
        {
            var emailMessage = new EmailMessage(this._service)
            {
                Subject = subject,
                Body = new MessageBody(BodyType.HTML, htmlbody)
            };

            if (attachments != null)
            {
                foreach (var path in attachments)
                {
                    string filePath = path.ToString();
                    if (File.Exists(filePath))
                    {
                        emailMessage.Attachments.AddFileAttachment(filePath);
                    }
                }
            }

            if (to != null && to.Count() > 0)
            {
                emailMessage.ToRecipients.AddRange(to);
            }

            if (cc != null && cc.Count() > 0)
            {
                emailMessage.CcRecipients.AddRange(cc);
            }

            if (bcc != null && bcc.Count() > 0)
            {
                emailMessage.BccRecipients.AddRange(bcc);
            }

            return new EwsSimplifiedMessage(emailMessage, this._service);
        }

        public void MoveMailTo(EwsSimplifiedMessage message, string folderpath)
        {
            Folder targetFolder = ResolveFolderPath(folderpath);
            if (targetFolder == null)
                throw new ApplicationException($"Folder {folderpath} doesn't exist");

            // Przeniesienie wiadomości do nowego folderu w EWS przy użyciu wewnętrznego obiektu EmailMessage
            message.Message.Move(targetFolder.Id);
        }

        public void SendMessage(EwsSimplifiedMessage message)
        {
            if (message?.Message == null)
                throw new ArgumentNullException(nameof(message), "Obiekt wiadomości nie może być pusty.");

            // Sprawdzamy czy wiadomość ma już ID na serwerze (np. jest wcześniej utworzonym szkicem)
            if (!string.IsNullOrEmpty(message.Message.Id?.UniqueId))
            {
                // Wysłanie istniejącego szkicu z serwera
                message.Message.Send();
            }
            else
            {
                // Wysłanie nowej wiadomości i zapisanie kopii w folderze Elementy Wysłane (Sent Items)
                message.Message.SendAndSaveCopy();
            }
        }

        public static void AddAttachment(EmailMessage message, string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Nie znaleziono pliku: {filePath}");

            string fileName = Path.GetFileName(filePath);
            message.Attachments.AddFileAttachment(fileName, filePath);
        }
    }
}
