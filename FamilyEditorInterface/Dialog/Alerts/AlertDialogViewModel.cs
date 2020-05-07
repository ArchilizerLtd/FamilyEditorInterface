using System.Windows.Input;
using Dialog.Service;
using FamilyEditorInterface.WPF;

namespace Dialog.Alerts
{
    public class AlertDialogViewModel : DialogViewModelBase<DialogResults>
    {
        public ICommand OKCommand { get; private set; }

        public AlertDialogViewModel(string title, string message) : base(title, message)
        {
            OKCommand = new RelayCommand(OK);
        }

        private void OK(object window)
        {
            CloseDialogWithResult(window as IDialogWindow, DialogResults.Undefined);
        }
    }
}
