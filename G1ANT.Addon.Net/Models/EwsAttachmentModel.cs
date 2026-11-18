using G1ANT.Language.Services;
using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace G1ANT.Addon.Net.Models
{
    public class EwsAttachmentModel : IAttachmentModel
    {
        private FileAttachment attachment;
        private ITempFileService tempFileService = null;
        private IGetMd5HashService getMd5HashService = null;

        private const string AttachmentFilePrefix = "g1ant.attachment.";

        public EwsAttachmentModel(FileAttachment attachment, ITempFileService iTempFileService = null, IGetMd5HashService iGetMd5HashService = null)
        {
            this.attachment = attachment;
            tempFileService = iTempFileService ?? new TempFileService();
            getMd5HashService = iGetMd5HashService ?? new GetMd5HashService();
        }

        public string Name => attachment.Name;

        public long Size => attachment.Size;

        public string Type => attachment.ContentType;

        public string SaveAndGetPath()
        {
            if (attachment != null && attachment.Size > 0)
            {
                // Upewniamy się, że zawartość pliku została pobrana z serwera EWS
                if (attachment.Content == null)
                {
                    attachment.Load();
                }

                var attachmentNameHash = GetAttachmentNameHash(Name, attachment.Id);
                var filePath = GetAttachmentTempFileNamePath($"{attachmentNameHash}.{Name}");

                if (!System.IO.File.Exists(filePath))
                {
                    using (var stream = System.IO.File.Create(filePath))
                    {
                        stream.Write(attachment.Content, 0, attachment.Content.Length);
                    }
                }
                return filePath;
            }
            return "";
        }

        private string GetAttachmentTempFileNamePath(string attachmentNameHash)
        {
            return tempFileService.GetTempPath(AttachmentFilePrefix, attachmentNameHash, "");
        }

        private string GetAttachmentNameHash(string fileName, string contentId)
        {
            if (string.IsNullOrEmpty(contentId))
                contentId = Guid.NewGuid().ToString();
            return getMd5HashService.GetMd5Hash($"{contentId}-{fileName}");
        }
    }
}
