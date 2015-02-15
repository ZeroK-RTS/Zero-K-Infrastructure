using System;

namespace ZkData
{
    public class UserLanguageNoteAttribute: Attribute
    {
        protected String note;

        public String Note { get { return note; } }

        public UserLanguageNoteAttribute(string note) {
            this.note = note;
        }
    }
}