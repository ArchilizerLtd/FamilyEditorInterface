using System.Collections.Generic;
using System.Windows.Input;
using Dialog.Service;
using FamilyEditorInterface.WPF;

namespace Dialog.Alerts
{
    public class OKDialogViewModel : DialogViewModelBase<DialogResults>
    {
        public ICommand OKCommand { get; private set; }

        public OKDialogViewModel(string title, string message) : base(title, message, null) { }
        public OKDialogViewModel(string title, string message, List<Message> note) : base(title, message, note)
        {
            OKCommand = new RelayCommand(OK);
        }

        private void OK(object window)
        {
            CloseDialogWithResult(window as IDialogWindow, DialogResults.Undefined);
        }
    }
}
