using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Dialog.Service;
using FamilyEditorInterface.WPF;

namespace Dialog.Alerts
{
    public class NotifyDialogViewModel : DialogViewModelBase<DialogResults>
    {
        public ICommand OKCommand { get; private set; }

        public NotifyDialogViewModel(string title, string message) : base(title, message, null) { }
        public NotifyDialogViewModel(string title, string message, List<Message> note) : base(title, message, note)
        {
            OKCommand = new RelayCommand(OK);
        }

        private void OK(object window)
        {
            CloseDialogWithResult(window as IDialogWindow, DialogResults.Undefined);
        }
    }
}
