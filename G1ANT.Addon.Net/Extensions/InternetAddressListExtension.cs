using G1ANT.Language;
using MimeKit;
using System;

namespace G1ANT.Addon.Net.Extensions
{
    public static class InternetAddressListExtension
    {
        public static void SetMailboxesFromStructure(this InternetAddressList mails, Structure newMails, string nameOfEntity)
        {
            mails.Clear();
            if (newMails is TextStructure text)
            {
                var mailsArray = text.Value.Split(new char[] { ';', SpecialChars.ArraySeparator[0] });
                foreach (var mail in mailsArray)
                    if (!string.IsNullOrEmpty(mail))
                        mails.Add(MailboxAddress.Parse(mail));
            }
            else if (newMails is ListStructure list)
            {
                foreach (var mail in list.Value)
                    if (!string.IsNullOrEmpty(mail?.ToString()))
                        mails.Add(MailboxAddress.Parse(mail.ToString()));
            }
            else
                throw new ArgumentException($"'{nameOfEntity}' should be text or list structure");
        }

        public static InternetAddressList CreateMailboxesFromStructure(this Structure newMails, string nameOfEntity)
        {
            var mails = new InternetAddressList();
            if (newMails is TextStructure text)
            {
                var mailsArray = text.Value.Split(new char[] { ';', SpecialChars.ArraySeparator[0] });
                foreach (var mail in mailsArray)
                    if (!string.IsNullOrEmpty(mail))
                        mails.Add(MailboxAddress.Parse(mail));
            }
            else if (newMails is ListStructure list)
            {
                foreach (var mail in list.Value)
                    if (!string.IsNullOrEmpty(mail?.ToString()))
                        mails.Add(MailboxAddress.Parse(mail.ToString()));
            }
            else
                throw new ArgumentException($"'{nameOfEntity}' should be text or list structure");
            return mails;
        }
    }
}
