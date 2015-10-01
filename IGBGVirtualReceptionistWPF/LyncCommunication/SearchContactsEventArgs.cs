using System;
using System.Collections.Generic;
using System.Linq;

namespace IGBGVirtualReceptionist.LyncCommunication
{
    public class SearchContactsEventArgs : EventArgs
    {
        public IEnumerable<ContactInfo> FoundContacts { get; private set; }

        public SearchContactsEventArgs(IEnumerable<ContactInfo> foundContacts)
        {
            this.FoundContacts = foundContacts;
        }
    }
}
