using G1ANT.Language.Services;
using Microsoft.Graph;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace G1ANT.Addon.Net.Models
{
    public class GraphAttachmentModel : IAttachmentModel
    {
        private Attachment attachment;
        private ITempFileService tempFileService = null;
        private IGetMd5HashService getMd5HashService = null;

        private const string AttachmentFilePrefix = "g1ant.attachment.";

        public GraphAttachmentModel(Attachment attachment, ITempFileService iTempFileService = null, IGetMd5HashService iGetMd5HashService = null)
        {
            this.attachment = attachment;
            tempFileService = iTempFileService ?? new TempFileService();
            getMd5HashService = iGetMd5HashService ?? new GetMd5HashService();
        }

        public string Name => attachment.Name;

        public long Size => attachment.Size ?? 0;

        public string Type => attachment.ContentType;

        public string SaveAndGetPath()
        {
            if (attachment is FileAttachment fileAttachment && fileAttachment.Size != null)
            {
                var attachmentNameHash = GetAttachmentNameHash(Name, attachment.Id);
                var filePath = GetAttachmentTempFileNamePath($"{attachmentNameHash}.{Name}");
                if (!System.IO.File.Exists(filePath))
                {
                    using (var stream = System.IO.File.Create(filePath))
                        stream.Write(fileAttachment.ContentBytes, 0, fileAttachment.ContentBytes.Length);
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
