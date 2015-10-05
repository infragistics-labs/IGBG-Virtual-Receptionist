using System;
using System.Linq;
using Microsoft.Lync.Model.Conversation;

namespace IGBGVirtualReceptionist.LyncCommunication
{
    public class ConversationEventArgs : EventArgs
    {
        public Conversation Conversation { get; private set; }
        public ContactInfo ContactInfo { get; private set; }
        public ConversationType ConversationType { get; private set; }

        public ConversationEventArgs(Conversation conversation, ContactInfo contactInfo, ConversationType conversationType)
        {
            this.Conversation = conversation;
            this.ContactInfo = contactInfo;
            this.ConversationType = conversationType;
        }
    }
}
