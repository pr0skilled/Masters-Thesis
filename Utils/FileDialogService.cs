using Microsoft.Win32;

namespace Thesis.Utils
{
    public class FileDialogService : IFileDialogService
    {
        public string OpenFile(string filter, string initialDirectory)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = filter,
                InitialDirectory = initialDirectory
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        public string SaveFile(string defaultFileName, string filter, string initialDirectory)
        {
            var saveFileDialog = new SaveFileDialog
            {
                FileName = defaultFileName,
                Filter = filter,
                InitialDirectory = initialDirectory
            };

            return saveFileDialog.ShowDialog() == true ? saveFileDialog.FileName : null;
        }
    }

}
