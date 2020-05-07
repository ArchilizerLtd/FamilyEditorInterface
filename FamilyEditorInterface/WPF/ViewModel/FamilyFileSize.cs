using Autodesk.Revit.DB;
using System.IO;

namespace FamilyEditorInterface.WPF.ViewModel
{
    public class FamilyFileSize
    {
        /// <summary>
        /// Returns family file size by storing the family in the Temp folder
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="family"></param>
        /// <returns></returns>
        public static long GetFileSize(Document famDoc)
        {
            string path = Path.GetTempPath();
            string name = famDoc.Title + ".rfa";
            string fPath = path + name;

            var options = new SaveAsOptions();
            options.OverwriteExistingFile = true;

            famDoc.SaveAs(fPath,options);
            var size = new FileInfo(fPath).Length;
            famDoc.Close(true);

            return size;
        }
    }
}
