using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Microsoft.Lync.Model;

namespace IGBGVirtualReceptionist.LyncCommunication
{
    public class ContactInfo
    {
        public string DisplayName { get; private set; }

        public string SipUri { get; private set; }

        public BitmapImage PhotoImage { get; private set; }

        public string Availability { get; private set; }

        public string Activity { get; private set; }

        public string Title { get; private set; }

        public string Department { get; private set; }

        public string OutOfficeNote { get; private set; }

        public string Description { get; private set; }

        public string FirstName { get; private set; }

        public string LastName { get; private set; }

        public ContactCapabilities Capabilities { get; private set; }

        public ContactInfo(string displayName, string sipUri)
        {
            this.DisplayName = displayName;
            this.SipUri = sipUri;
        }

        public static ContactInfo GetContactInfo(Contact contact)
        {
            string displayName = (string)contact.GetContactInformation(ContactInformationType.DisplayName);

            Stream mStream = null;
            BitmapImage photoImage = null;
            try
            {
                mStream = (Stream)contact.GetContactInformation(ContactInformationType.Photo);
                if (mStream != null)
                {
                    photoImage = new BitmapImage();
                    photoImage.StreamSource = mStream;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ContactInfo error: " + ex);
            }

            return new ContactInfo(displayName, contact.Uri)
            {
                PhotoImage = photoImage,
                Availability = (string)contact.GetContactInformation(ContactInformationType.Availability),
                Activity = (string)contact.GetContactInformation(ContactInformationType.Activity),
                Title = (string)contact.GetContactInformation(ContactInformationType.Title),
                Department = (string)contact.GetContactInformation(ContactInformationType.Department),
                OutOfficeNote  = (string)contact.GetContactInformation(ContactInformationType.OutOfficeNote),
                Description  = (string)contact.GetContactInformation(ContactInformationType.Description),
                FirstName = (string)contact.GetContactInformation(ContactInformationType.FirstName),
                LastName = (string)contact.GetContactInformation(ContactInformationType.LastName),
                Capabilities = (ContactCapabilities)contact.GetContactInformation(ContactInformationType.Capabilities)
            };
        }
    }
}
