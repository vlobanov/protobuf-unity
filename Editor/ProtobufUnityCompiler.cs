using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace E7.Protobuf
{
    internal class ProtobufUnityCompiler : AssetPostprocessor
    {
        /// <summary>
        /// Path to the file of all protobuf files in your Unity folder.
        /// </summary>
        static string[] AllProtoFiles
        {
            get
            {
                string[] protoFiles = Directory.GetFiles(SourceDir, "*.proto", SearchOption.AllDirectories);
                return protoFiles;
            }
        }

        private static string SourceDir {
            get {
                return Application.dataPath + "/proto";
            }
        }

        private static string TargetDir {
            get {
                return Application.dataPath + "/Scripts/proto";
            }
        }

        /// <summary>
        /// A parent folder of all protobuf files found in your Unity project collected together.
        /// This means all .proto files in Unity could import each other freely even if they are far apart.
        /// </summary>
        static string[] IncludePaths
        {
            get
            {
                string[] protoFiles = AllProtoFiles;

                string[] includePaths = new string[protoFiles.Length];
                for (int i = 0; i < protoFiles.Length; i++)
                {
                    string protoFolder = Path.GetDirectoryName(protoFiles[i]);
                    includePaths[i] = protoFolder;
                }
                return includePaths;
            }
        }

        static bool anyChanges = false;
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            anyChanges = false;
            if (ProtoPrefs.enabled == false)
            {
                return;
            }

            foreach (string str in importedAssets)
            {
                if (CompileProtobufAssetPath(str, IncludePaths) == true)
                {
                    anyChanges = true;
                }
            }

            /*
            for (int i = 0; i < movedAssets.Length; i++)
            {
                CompileProtobufAssetPath(movedAssets[i]);
            }
            */

            if (anyChanges)
            {
                UnityEngine.Debug.Log(nameof(ProtobufUnityCompiler));
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Called from Force Compilation button in the prefs.
        /// </summary>
        internal static void CompileAllInProject()
        {
            if (ProtoPrefs.logStandard)
            {
                UnityEngine.Debug.Log("Protobuf Unity : Compiling all .proto files in the project...");
            }

            foreach (string s in AllProtoFiles)
            {
                if (ProtoPrefs.logStandard)
                {
                    UnityEngine.Debug.Log("Protobuf Unity : Compiling " + s);
                }
                CompileProtobufSystemPath(s, IncludePaths);
            }
            UnityEngine.Debug.Log(nameof(ProtobufUnityCompiler));
            AssetDatabase.Refresh();
        }

        private static bool CompileProtobufAssetPath(string assetPath, string[] includePaths)
        {
            string protoFileSystemPath = Directory.GetParent(Application.dataPath) + Path.DirectorySeparatorChar.ToString() + assetPath;
            return CompileProtobufSystemPath(protoFileSystemPath, includePaths);
        }

        private static bool CompileProtobufSystemPath(string protoFileSystemPath, string[] includePaths)
        {
            //Do not compile changes coming from UPM package.
            if (protoFileSystemPath.Contains("Packages/com.e7.protobuf-unity")) return false;

            if (Path.GetExtension(protoFileSystemPath) == ".proto") {
                string outputPath = Path.GetDirectoryName(protoFileSystemPath).Replace(SourceDir, TargetDir);
                Directory.CreateDirectory(outputPath);

                string options = " --csharp_out \"{0}\" ";
                foreach (string s in includePaths)
                {
                    options += string.Format(" --proto_path \"{0}\" ", s);
                }

                //string combinedPath = string.Join(" ", optionFiles.Concat(new string[] { protoFileSystemPath }));

                string finalArguments = string.Format("\"{0}\"", protoFileSystemPath) + string.Format(options, outputPath);

                if (ProtoPrefs.logStandard)
                {
                    UnityEngine.Debug.Log("Protobuf Unity : Final arguments :\n" + finalArguments);
                }

                ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = ProtoPrefs.excPath, Arguments = finalArguments };

                Process proc = new Process() { StartInfo = startInfo };
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.Start();

                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (ProtoPrefs.logStandard)
                {
                    if (output != "")
                    {
                        UnityEngine.Debug.Log("Protobuf Unity : " + output);
                    }
                    UnityEngine.Debug.Log("Protobuf Unity : Compiled " + Path.GetFileName(protoFileSystemPath));
                }

                if (ProtoPrefs.logError && error != "")
                {
                    UnityEngine.Debug.LogError("Protobuf Unity : " + error);
                }
                return true;
            }
            return false;
        }
    }
}