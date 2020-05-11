using System.Collections.Generic;

namespace Dialog.Service
{
    /// <summary>
    /// https://www.youtube.com/watch?v=KbzuK5i_Sks
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DialogViewModelBase<T>
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public List<Message> Note { get; set; }
        public T DialogResult { get; set; }

        public DialogViewModelBase() : this(string.Empty, string.Empty, null) { }
        public DialogViewModelBase(string title) : this(title, string.Empty, null) { }
        public DialogViewModelBase(string title, List<Message> note) : this(title, string.Empty, note) { }
        public DialogViewModelBase(string title, string message) : this(title, message, null) { }
        public DialogViewModelBase(string title, string message, List<Message> note)
        {
            Title = title;
            Message = message;
            Note = note;
        }

        public void CloseDialogWithResult(IDialogWindow dialog, T result)
        {
            DialogResult = result;

            if (dialog != null)
                dialog.DialogResult = true;
        }
    }
    public class Message
    {
        public string Body { get; set; }
        public string Bold { get; set; }

        public Message(string bold, string body)
        {
            Bold = bold;
            Body = body;
        }
    }
}
