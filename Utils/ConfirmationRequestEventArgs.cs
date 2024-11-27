namespace Thesis.Utils
{
    public class ConfirmationRequestEventArgs : EventArgs
    {
        public string Message { get; }
        public string Caption { get; }
        public Action<bool> Callback { get; }

        public ConfirmationRequestEventArgs(string message, string caption, Action<bool> callback)
        {
            this.Message = message;
            this.Caption = caption;
            this.Callback = callback;
        }
    }
}
