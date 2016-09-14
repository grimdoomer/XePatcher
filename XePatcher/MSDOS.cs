using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XePatcher
{
    public class MSDOS
    {
        /// <summary>
        /// Checks the specified file or folder path for MS-DOS compatibility.
        /// </summary>
        /// <param name="path">Folder or file path to check.</param>
        /// <returns>True if the folder or file path is MS-DOS compatible.</returns>
        public static bool IsSafeFilePath(string path)
        {
            // Check to amke sure the file path has no strings.
            return !path.Contains(" ");
        }

        /// <summary>
        /// Creates a new MS-DOS compatible folder and copies the contents of the folder
        /// specified by <paramref name="folderToCopy"/> into the new folder.
        /// </summary>
        /// <param name="folderName">Name of the new MS-DOS folder.</param>
        /// <param name="folderToCopy">Folder who's contents will be copied to the new MS-DOS compatible folder.</param>
        /// <param name="overwrite">Boolean value indicating that old files or folders should be overwriten.</param>
        /// <returns>The folder path of the new MS-DOS compatible folder.</returns>
        public static string CreateMsDosSafeFolder(string folderName, string folderToCopy, bool overwrite)
        {
            // Get the install drive mount point and create our dst folder path.
            string drive = System.Environment.GetEnvironmentVariable("HOMEDRIVE");
            string targetFolder = string.Format("{0}\\{1}\\", drive, folderName);

            // Check if the folder exists.
            if (System.IO.Directory.Exists(targetFolder) == true)
            {
                // Check if we should overwrite any existing files.
                if (overwrite == true)
                {
                    // Delete the old folder.
                    System.IO.Directory.Delete(targetFolder, true);

                    // Create a new folder.
                    System.IO.Directory.CreateDirectory(targetFolder);
                }
            }
            else
            {
                // Create the folder.
                System.IO.Directory.CreateDirectory(targetFolder);
            }

            // Copy over the folder contents.
            CopyMsDosFolder(folderToCopy, targetFolder, overwrite);

            // Return the new MS-DOS safe folder path.
            return targetFolder;
        }

        private static void CopyMsDosFolder(string srcFolder, string dstFolder, bool overwrite)
        {
            // Get the directory info on the source folder.
            System.IO.DirectoryInfo info = new System.IO.DirectoryInfo(srcFolder);

            // Loop through all the folders and copy them over.
            System.IO.DirectoryInfo[] folders = info.GetDirectories();
            for (int i = 0; i < folders.Length; i++)
            {
                // Check that the target folder name is safe.
                string targetFolder = string.Format("{0}{1}", dstFolder, folders[i].Name);
                if (IsSafeFilePath(targetFolder) == false)
                    throw new Exception(string.Format("Folder path \"{0}\" is not MS-DOS safe!", targetFolder));

                // Copy over folder
                CopyMsDosFolder(folders[i].FullName, targetFolder, overwrite);
            }

            // Loop through all the files and copy them over.
            System.IO.FileInfo[] files = info.GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                // Check that the target folder name is safe.
                string targetFile = string.Format("{0}{1}", dstFolder, files[i].Name);
                if (IsSafeFilePath(targetFile) == false)
                    throw new Exception(string.Format("File path \"{0}\" is not MS-DOS safe!", targetFile));

                // Copy over file
                System.IO.File.Copy(files[i].FullName, targetFile, overwrite);
            }
        }
    }
}
