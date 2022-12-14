using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace VisioAddIn
{
    public static class ShapeFinder
    {

        private const string SIDPrefix = "Abstract PASS SID Visio Shapes";
        private const string SBDPrefix = "Abstract PASS SBD Visio Shapes";
        private const string versionPattern = "\\s(v)(\\d*)\\.(\\d*)\\.(\\d*)(\\S)*";
        private const string ending = ".vssm";
        private static string sidName;
        private static string sbdName;

        /// <summary>
        /// Returns the name of the newest SID-Shapes in the Shapes-folder
        /// </summary>
        /// <returns>The name of the SID-file</returns>
        public static String getSIDName()
        {
            if (sidName == null)
                sidName = getShapes(SIDPrefix);
            return sidName;
        }

        /// <summary>
        /// Returns the name of the newest SBD-Shapes in the Shapes-folder
        /// </summary>
        /// <returns>The name of the SBD-file</returns>
        public static String getSBDName()
        {
            if (sbdName == null)
                sbdName = getShapes(SBDPrefix);
            return sbdName;
        }

        /// <summary>
        /// Searches inside the Shapes-Folder for the potentially newest file with the specified prefix
        /// </summary>
        /// <param name="prefix">The prefix of the file that should be returned</param>
        /// <returns>The name of the file with the specified prefix that is supposed to be the newest file with this prefix</returns>
        private static String getShapes(String prefix)
        {
            String path = Globals.ThisAddIn.Application.MyShapesPath;
            DirectoryInfo myShapes = new DirectoryInfo(path);
            String regex = prefix + versionPattern + ending;
            List<FileInfo> possibleFiles = getMatchingFiles(myShapes, regex);
            if (possibleFiles == null || possibleFiles.Count == 0)
            {
                return prefix + " v.x.x.x.x" + ending;
            }
            FileInfo newestFile = possibleFiles.First();
            foreach (FileInfo fileInfo in possibleFiles)
            {
                String name = getNewest(newestFile.Name, fileInfo.Name);
                if (!newestFile.Name.Equals(name))
                {
                    newestFile = fileInfo;
                }
            }
            return newestFile.Name;
        }


        /// <summary>
        /// This helping method decides which of the both filenames indicates a newer file version.
        /// Therefore, the version pattern of the filename in the form "v(\d.)*(\d)*" plus an unspecified char sequence is used.
        /// Example version: v0.8.1-R.
        ///If all the numbers match, but one filename contains more numbers, this file is prioritized.
        /// If all the numbers match and the names got the same amount of numbers, the length of the suffix char-sequence will decide which file may be newer.
        /// </summary>
        /// <param name="nameFirst">The name of the first file being tested</param>
        /// <param name="nameSecond">The name of the second file being tested</param>
        /// <returns>The name of the file that is evaluated to be newer, based on the version pattern inside the name</returns>
        private static String getNewest(String nameFirst, String nameSecond)
        {
            if (nameFirst.Equals(nameSecond))
            {
                return nameFirst;
            }
            // Get the version by pattern. Remove the file ending suffix
            String versionFirst = Regex.Match(nameFirst, versionPattern).ToString();
            String versionSecond = Regex.Match(nameSecond, versionPattern).ToString();
            versionFirst = versionFirst.Replace(ending, "");
            versionSecond = versionSecond.Replace(ending, "");
            String endPattern = "[^\\.\\d]$";
            String endFirst = Regex.Match(versionFirst, endPattern).ToString();
            String endSecond = Regex.Match(versionSecond, endPattern).ToString();
            versionFirst = Regex.Replace(versionFirst, endPattern, "").ToString();
            versionSecond = Regex.Replace(versionSecond, endPattern, "").ToString();

            // Get all the version numbers
            Regex numbers = new Regex(@"\d+");
            MatchCollection numbersFirst = numbers.Matches(versionFirst);
            MatchCollection numbersSecond = numbers.Matches(versionSecond);

            // Iterate through numbers, return a file if the version number is bigger than the corresponding number of the other file
            for (int i = 0; i < numbersFirst.Count; i++)
            {
                if (numbersSecond.Count <= i)
                {
                    return nameFirst;
                }
                int first = int.Parse(numbersFirst[i].Value);
                int second = int.Parse(numbersSecond[i].Value);

                if (first > second)
                {
                    return nameFirst;
                }
                else if (first < second)
                {
                    return nameSecond;
                }
            }

            if (numbersSecond.Count > numbersFirst.Count)
            {
                return nameSecond;
            }

            // All numbers are identical and both names have the same amount of numbers -> the ending sequence decides
            if (endFirst.Length > endSecond.Length)
            {
                return nameFirst;
            }
            else if (endFirst.Length < endSecond.Length)
            {
                return nameSecond;
            }
            return nameFirst;
        }

        /// <summary>
        /// Gets all the files from the root folder for which the name matches the regex
        /// </summary>
        /// <param name="root">the starting folder</param>
        /// <param name="nameRegex">The matching regex</param>
        /// <returns></returns>
        private static List<FileInfo> getMatchingFiles(System.IO.DirectoryInfo root, String nameRegex)
        {
            System.IO.FileInfo[] files = null;

            try
            {
                files = root.GetFiles("*.*");
            }
            // This is thrown if even one of the files requires permissions greater
            // than the application provides.
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }

            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            if (files != null)
            {
                // Checks for each file whether it matches the specified name regex
                Regex rgx = new Regex(nameRegex);
                List<FileInfo> fileObjects = new List<FileInfo>();
                foreach (System.IO.FileInfo fi in files)
                {
                    if (rgx.IsMatch(fi.Name))
                    {
                        fileObjects.Add(fi);
                    }
                }

                // iterates recursive to get all possible files
                foreach (System.IO.DirectoryInfo directory in root.GetDirectories())
                {
                    fileObjects.AddRange(getMatchingFiles(directory, nameRegex));
                }

                // Matching files are being returned
                return fileObjects;
            }

            return null;
        }


        

    }

}
