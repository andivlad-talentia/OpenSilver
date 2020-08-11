﻿

/*===================================================================================
* 
*   Copyright (c) Userware (OpenSilver.net, CSHTML5.com)
*      
*   This file is part of both the OpenSilver Compiler (https://opensilver.net), which
*   is licensed under the MIT license (https://opensource.org/licenses/MIT), and the
*   CSHTML5 Compiler (http://cshtml5.com), which is dual-licensed (MIT + commercial).
*   
*   As stated in the MIT license, "the above copyright notice and this permission
*   notice shall be included in all copies or substantial portions of the Software."
*  
\*====================================================================================*/



using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DotNetForHtml5.Compiler
{
    internal static class WrappingOutputFiles_BridgeVersion
    {
        public static void CreateAndCopySupportFiles(
            string pathOfAssemblyThatContainsEntryPoint,
            List<string> librariesFolders,
            string outputRootPath,
            string outputAppFilesPath,
            string outputLibrariesPath,
            string outputResourcesPath,
            string[] listOfResXGeneratedFiles,
            string assemblyNameWithoutExtension
            )
        {
            StringBuilder codeForReferencingAdditionalLibraries = new StringBuilder();

            // Determine the application title:
            string title = assemblyNameWithoutExtension;

            // Determine the output path:
            string outputPathAbsolute = PathsHelper.GetOutputPathAbsolute(pathOfAssemblyThatContainsEntryPoint, outputRootPath);

            // Combine the output path and the relative "Libraries" folder path, while also ensuring that there is no forward slash, and that the path ends with a backslash:
            string absoluteOutputLibrariesPath = PathsHelper.CombinePathsWhileEnsuringEndingBackslashAndMore(outputPathAbsolute, outputLibrariesPath); // Note: when it was hard-coded, it was Path.Combine(outputPath, @"Libraries\");

            // Create the destination folders hierarchy if it does not already exist:
            if (!Directory.Exists(absoluteOutputLibrariesPath))
                Directory.CreateDirectory(absoluteOutputLibrariesPath);

            // Copy the content of the "Libraries" files:
            foreach (string librariesFolder in librariesFolders)
            {
                foreach (var file in Directory.GetFiles(librariesFolder))
                {
                    string fileName = Path.GetFileName(file);
                    string outputFileWithFullPath = Path.Combine(absoluteOutputLibrariesPath, fileName);
                    File.Copy(file, Path.Combine(absoluteOutputLibrariesPath, fileName), true);
                }
            }

            // Read the application files from the "index.html" and "index.min.html" files generated by Bridge.NET (if found):
            // Note: the idea here is that we copy all the <script> tabs from the "index.html" file generated by Bridge to ours.
            bool skipIndexGeneration;
            bool skipIndexMinGeneration;
            string indexFullPath = Path.Combine(outputPathAbsolute, "index.html");
            string indexMinFullPath = Path.Combine(outputPathAbsolute, "index.min.html");
            string scriptForApplicationFiles_readFromIndex = ReadApplicationFilesFromBridgeGeneratedOutput(indexFullPath, out skipIndexGeneration);
            string scriptForApplicationFiles_readFromIndexMin = ReadApplicationFilesFromBridgeGeneratedOutput(indexMinFullPath, out skipIndexMinGeneration);

            // Create and save the "index.html" file:
            if (!skipIndexGeneration)
            {
                //------------
                // index.html
                //------------
                CreateAndSaveIndexHtml(
                    indexFullPath,
                    outputPathAbsolute,
                    outputRootPath,
                    outputAppFilesPath,
                    outputLibrariesPath,
                    outputResourcesPath,
                    listOfResXGeneratedFiles,
                    assemblyNameWithoutExtension,
                    title,
                    scriptForApplicationFiles_readFromIndex
                    );
            }

            // Create and save the "index.min.html" file:
            if (!skipIndexMinGeneration)
            {
                //----------------
                // index.min.html
                //----------------
                CreateAndSaveIndexHtml(
                    indexMinFullPath,
                    outputPathAbsolute,
                    outputRootPath,
                    outputAppFilesPath,
                    outputLibrariesPath,
                    outputResourcesPath,
                    listOfResXGeneratedFiles,
                    assemblyNameWithoutExtension,
                    title,
                    scriptForApplicationFiles_readFromIndexMin
                    );
            }
        }

        static string ReadApplicationFilesFromBridgeGeneratedOutput(
            string indexFileFullPath,
            out bool skipIndexGeneration)
        {
            // Note: the idea here is that we copy all the <script> tabs from the "index.html" file generated by Bridge to ours.

            if (File.Exists(indexFileFullPath))
            {
                string fileContent = File.ReadAllText(indexFileFullPath);

                // Verify that we are reading the Bridge.NET-generated file and not ours:
                bool isOriginalBridgeGeneratedFile = !fileContent.Contains("cshtml5.css");
                if (isOriginalBridgeGeneratedFile)
                {
                    int startIndex = fileContent.IndexOf("<script");
                    int endIndex = fileContent.LastIndexOf("</script>");
                    if (startIndex > -1 && endIndex > startIndex)
                    {
                        skipIndexGeneration = false;
                        string result = fileContent.Substring(startIndex, endIndex - startIndex + 9); // Note: we add "9" in order to include the full "</script>" closing tag.
                        return result;
                    }
                    else
                    {
                        throw new Exception("No <script> tab was found in the file 'index.html' generated by Bridge.NET");
                    }
                }
                else
                {
                    // It is possible that the user has disabled the generation of index.html in the "bridge.json" configuration file (typically in order to not ovveride a custom made one).
                    // In this case we simply ignore and skip the generation of the "index.html" file.
                    skipIndexGeneration = true;
                    return null;
                }
            }
            else
            {
                // It is possible that the user has disabled the generation of index.html in the "bridge.json" configuration file (typically in order to not ovveride a custom made one).
                // In this case we simply ignore and skip the generation of the "index.html" file.
                skipIndexGeneration = true;
                return null;
            }
        }

        static void CreateAndSaveIndexHtml(
            string outputFileNameWithPath,
            string outputPathAbsolute,
            string outputRootPath,
            string outputAppFilesPath,
            string outputLibrariesPath,
            string outputResourcesPath,
            string[] listOfResXGeneratedFiles,
            string assemblyNameWithoutExtension,
            string title,
            string scriptsForApplicationFiles
            )
        {
            // Read the "index.html" template:
            string indexHtmlFileTemplate = WrapperHelpers.ReadTextFileFromEmbeddedResource("index_BridgeVersion.html");

            // Replace the placeholders:
            string indexHtmlFile = indexHtmlFileTemplate.Replace("[LIBRARIES_PATH_GOES_HERE]", PathsHelper.EnsureNoBackslashAndEnsureItEndsWithAForwardSlash(outputLibrariesPath));
            indexHtmlFile = indexHtmlFile.Replace("[TITLE_GOES_HERE]", title);
            indexHtmlFile = indexHtmlFile.Replace("[SCRIPTS_FOR_APPLICATION_FILES_GO_HERE]", scriptsForApplicationFiles);

            // Read the "App.Config" file for future use by the ClientBase.
            string relativePathToAppConfigFolder = PathsHelper.CombinePathsWhileEnsuringEndingBackslashAndMore(outputResourcesPath, assemblyNameWithoutExtension);
            string relativePathToAppConfig = Path.Combine(relativePathToAppConfigFolder, "app.config.g.js");
            if (File.Exists(Path.Combine(outputPathAbsolute, relativePathToAppConfig)))
            {
                string scriptToReadAppConfig = "        <script src=\"" + relativePathToAppConfig.Replace('\\', '/') + "\"></script>";
                indexHtmlFile = indexHtmlFile.Replace("[SCRIPT_TO_READ_APPCONFIG_GOES_HERE]", scriptToReadAppConfig);
            }
            else
            {
                indexHtmlFile = indexHtmlFile.Replace("[SCRIPT_TO_READ_APPCONFIG_GOES_HERE]", string.Empty);
            }

            // Read the "ServiceReferences.ClientConfig" file for future use by the ClientBase.
            string relativePathToServiceReferencesClientConfig = Path.Combine(relativePathToAppConfigFolder, "servicereferences.clientconfig.g.js");
            if (File.Exists(Path.Combine(outputPathAbsolute, relativePathToServiceReferencesClientConfig)))
            {
                string scriptToReadServiceReferencesClientConfig = "        <script src=\"" + relativePathToServiceReferencesClientConfig.Replace('\\', '/') + "\"></script>";
                indexHtmlFile = indexHtmlFile.Replace("[SCRIPT_TO_READ_SERVICEREFERENCESCLIENTCONFIG_GOES_HERE]", scriptToReadServiceReferencesClientConfig);
            }
            else
            {
                indexHtmlFile = indexHtmlFile.Replace("[SCRIPT_TO_READ_SERVICEREFERENCESCLIENTCONFIG_GOES_HERE]", string.Empty);
            }

            // Add the scripts for reading the ResX files:
            if(listOfResXGeneratedFiles != null && listOfResXGeneratedFiles.Length > 0)
            {
                string resultingStringForLoadingResXFiles = String.Empty;
                foreach(string str in listOfResXGeneratedFiles)
                {
                    int indexForRelativePath = str.IndexOf(outputResourcesPath);
                    if (indexForRelativePath == -1)
                        throw new Exception(string.Format("The file path '{0}' does not contain the substring '{1}'.", str, outputResourcesPath));
                    string relativePath = str.Substring(indexForRelativePath).Replace('\\', '/');
                    resultingStringForLoadingResXFiles += Environment.NewLine + "        <script src=\"" + relativePath + "\"></script>";
                }
                indexHtmlFile = indexHtmlFile.Replace("[SCRIPTS_FOR_RESX_GO_HERE]", resultingStringForLoadingResXFiles);
            }
            else
            {
                indexHtmlFile = indexHtmlFile.Replace("[SCRIPTS_FOR_RESX_GO_HERE]", string.Empty);
            }

            // Add the version number to the files so that they are cached by the web browser only until a new version is available:
            indexHtmlFile = indexHtmlFile.Replace(".js\"", ".js?" + String.Format("?{0:yyyyMMddHHmm}", DateTime.UtcNow) + "\"");
            indexHtmlFile = indexHtmlFile.Replace(".css\"", ".css?" + String.Format("?{0:yyyyMMddHHmm}", DateTime.UtcNow) + "\"");

            // Save the "index.html" to the final folder:
            File.WriteAllText(outputFileNameWithPath, indexHtmlFile);
        }
    }
}
