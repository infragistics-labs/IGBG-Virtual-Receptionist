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

        public ContactInfo(string displayName, string sipUri, BitmapImage photoImage)
        {
            this.DisplayName = displayName;
            this.SipUri = sipUri;
            this.PhotoImage = photoImage;
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

            return new ContactInfo(displayName, contact.Uri, photoImage);
        }

    }

}
