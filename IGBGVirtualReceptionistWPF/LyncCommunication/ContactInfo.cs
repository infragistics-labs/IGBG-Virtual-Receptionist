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

        public ContactAvailability Availability { get; private set; }

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
                // TODO: Temp fix for images.
                photoImage = MainWindow.dummyPic;
                //mStream = (Stream)contact.GetContactInformation(ContactInformationType.Photo);
                //if (mStream != null)
                //{
                //    photoImage = new BitmapImage();
                //    photoImage.StreamSource = mStream;
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine("ContactInfo error: " + ex);
            }

            return new ContactInfo(displayName, contact.Uri)
            {
                PhotoImage = photoImage,
                Availability = GetContactInfo<ContactAvailability>(contact, ContactInformationType.Availability),
                Activity = GetContactInfo<string>(contact, ContactInformationType.Activity),
                Title = GetContactInfo<string>(contact, ContactInformationType.Title),
                Department = GetContactInfo<string>(contact, ContactInformationType.Department),
                OutOfficeNote = GetContactInfo<string>(contact, ContactInformationType.OutOfficeNote),
                Description = GetContactInfo<string>(contact, ContactInformationType.Description),
                FirstName = GetContactInfo<string>(contact, ContactInformationType.FirstName),
                LastName = GetContactInfo<string>(contact, ContactInformationType.LastName),
                Capabilities = GetContactInfo<ContactCapabilities>(contact, ContactInformationType.Capabilities)
            };
        }

        private static T GetContactInfo<T>(Contact contact, ContactInformationType infoType)
        {
            try
            {
                return (T)contact.GetContactInformation(infoType);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ContactInfo: Could not get contact info - " + ex.Message);
            }

            return default(T);
        }
    }
}
