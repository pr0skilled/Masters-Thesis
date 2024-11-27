namespace Thesis.Utils
{
    public interface IFileDialogService
    {
        string OpenFile(string filter, string initialDirectory);
        string SaveFile(string defaultFileName, string filter, string initialDirectory);
    }

}
