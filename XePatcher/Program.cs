using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace XePatcher
{
    class Program
    {
        public enum ProcessorType
        {
            none,
            x86,
            ppc
        }

        #region Xbox 360 (xex)

        static int ByteFlip32(int Val)
        {
            return (int)(((Val & 0xFF000000) >> 24) | ((Val & 0xFF0000) >> 8) | ((Val & 0xFF00) << 8) | ((Val & 0xFF) << 24));
        }

        static bool CompileXexPatches(string PatchFile)
        {
            // Get Patch Name
            string ExePath = Process.GetCurrentProcess().MainModule.FileName;
            string BinFolder = ExePath.Substring(0, ExePath.LastIndexOf("\\") + 1) + "bin\\";
            string BinaryPath = PatchFile.ToLower().Replace(".s", ".elf");

            // Setup Process
            Process compiler = new Process();
            compiler.StartInfo.FileName = BinFolder + "xenon-as.exe";
            compiler.StartInfo.Arguments = "-be -many " + "\"" + PatchFile + "\"" + " -o " + "\"" + BinaryPath + "\"";
            compiler.StartInfo.UseShellExecute = false;
            compiler.StartInfo.RedirectStandardOutput = true;
            compiler.StartInfo.RedirectStandardError = true;
            compiler.Start();

            // Compile
            Console.Write("Compiling Patches...");
            string Output = compiler.StandardError.ReadToEnd();
            compiler.WaitForExit();
            Console.WriteLine(Output.Contains("Error") ? "\r\nError Compiling Patches: " + Output : " Success");

            // Copy
            if (!Output.Contains("Error"))
            {
                // Reset Process
                compiler = new Process();
                compiler.StartInfo.FileName = BinFolder + "xenon-objcopy.exe";
                compiler.StartInfo.Arguments = "\"" + BinaryPath + "\"" + " -O binary " + "\"" + BinaryPath.Replace(".elf", ".bin") + "\"";
                compiler.StartInfo.UseShellExecute = false;
                compiler.StartInfo.RedirectStandardOutput = true;
                compiler.StartInfo.RedirectStandardError = true;
                compiler.Start();

                // Compile
                Console.Write("Converting Patches...");
                Output = compiler.StandardError.ReadToEnd();
                compiler.WaitForExit();
                Console.WriteLine(Output.Contains("Error") ? "\r\nError Converting Patches: " + Output : " Success");

                // Delete elf File
                File.Delete(BinaryPath);
            }

            return Output.Contains("Error") ? false : true;
        }

        static void InitXex(string Xex, string Output)
        {
            // Get XexTool Path
            string ExePath = Process.GetCurrentProcess().MainModule.FileName;
            string XexTool = ExePath.Substring(0, ExePath.LastIndexOf("\\")) + "\\XexTool.exe";

            // Run Xex through XexTool
            Process compiler = new Process();
            compiler.StartInfo.FileName = XexTool;
            compiler.StartInfo.Arguments = "-e d -c b -o \"" + Output + "\" \"" + Xex + "\"";
            compiler.StartInfo.UseShellExecute = false;
            compiler.StartInfo.RedirectStandardOutput = true;
            compiler.StartInfo.RedirectStandardError = true;
            compiler.Start();

            // Compile
            Console.Write("Extracting basefile...");
            compiler.WaitForExit();
            Console.WriteLine(" Done");
        }

        static void FinishXex(string Xex, bool Encrypt, bool Compress)
        {
            // Get XexTool Path
            string ExePath = Process.GetCurrentProcess().MainModule.FileName;
            string XexTool = ExePath.Substring(0, ExePath.LastIndexOf("\\")) + "\\XexTool.exe";

            // Run Xex through XexTool
            Process compiler = new Process();
            compiler.StartInfo.FileName = XexTool;
            compiler.StartInfo.Arguments = (Encrypt ? "-e e" : "") + (Compress ? "-c c" : "") + "-r a \"" + Xex + "\"";
            compiler.StartInfo.UseShellExecute = false;
            compiler.StartInfo.RedirectStandardOutput = true;
            compiler.StartInfo.RedirectStandardError = true;
            compiler.Start();

            // Compile
            Console.Write("Repairing Xex...");
            compiler.WaitForExit();
            Console.WriteLine(" Done");
        }

        static void PatchXex(string[] args)
        {
            // Runtime Data
            bool Continue = false;
            int PathIndex = -1;

            // Compile Patches
            string Output = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-p")
                {
                    // Check It Exists
                    if (File.Exists(args[i + 1]))
                    {
                        Continue = CompileXexPatches(args[i + 1]);
                        PathIndex = i + 1;
                    }
                    else
                        Console.WriteLine("Could Not Find: " + args[i + 1]);
                }
                else if (args[i] == "-o")
                    Output = args[i + 1];
            }

            // Check For Error
            if (!Continue)
                return;

            // Patch to Xex
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-xex")
                {
                    // Check if we have an alternative output file
                    if (Output == "")
                        Output = args[i + 1];

                    // Check It Exists
                    if (File.Exists(args[i + 1]))
                    {
                        // Run it through Xex Tool to decrypt/decompress it
                        InitXex(args[i + 1], Output);

                        // Open Patch File
                        BinaryReader br = new BinaryReader(new FileStream(args[PathIndex].ToLower().Replace(".s", ".bin"), FileMode.Open, FileAccess.Read, FileShare.Read));

                        // Open Xex and Read Info
                        FileStream Xexfs = new FileStream(Output, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                        BinaryReader Xexbr = new BinaryReader(Xexfs);
                        BinaryWriter Xexbw = new BinaryWriter(Xexfs);

                        // Read out Loading Info
                        Xexbr.BaseStream.Position = 8;
                        int PeOffset = ByteFlip32(Xexbr.ReadInt32());
                        Xexbr.BaseStream.Position = 16;
                        Xexbr.BaseStream.Position = ByteFlip32(Xexbr.ReadInt32()) + 272;
                        uint LoadAddress = (uint)ByteFlip32(Xexbr.ReadInt32());

                        // Load Basefile Into MemoryStream
                        Xexbr.BaseStream.Position = PeOffset;
                        MemoryStream ms = new MemoryStream(Xexbr.ReadBytes((int)Xexbr.BaseStream.Length - PeOffset));
                        BinaryWriter bw = new BinaryWriter(ms);

                        // Begin Patching
                        Console.Write("Patching Xex...");
                        for (; ; )
                        {
                            // Test Address
                            uint Address = (uint)ByteFlip32(br.ReadInt32());
                            if (br.BaseStream.Position == br.BaseStream.Length)
                                break;

                            // Read Patch
                            int Count = ByteFlip32(br.ReadInt32());
                            byte[] Patch = br.ReadBytes(Count);

                            // Apply To Xex
                            try
                            {
                                bw.BaseStream.Position = Address - LoadAddress;
                                bw.Write(Patch);
                            }
                            catch
                            { Console.WriteLine("Failed To Patch: " + Address.ToString("X")); }
                        }

                        // Write New Basefile
                        Xexbw.BaseStream.Position = PeOffset;
                        Xexbw.Write(ms.ToArray());
                        Console.WriteLine(" Done");

                        // Close all Streams
                        bw.Close();
                        ms.Close();
                        br.Close();
                        Xexbr.Close();
                        Xexbw.Close();
                        Xexfs.Close();

                        // Find Optional Args
                        bool Encrypt = false, Compress = false;
                        for (int x = 0; x < args.Length; x++)
                        {
                            if (args[x] == "-e")
                                Encrypt = true;
                            if (args[x] == "-c")
                                Compress = true;
                        }

                        // Repair Xex
                        FinishXex(Output, Encrypt, Compress);

                    }
                    else
                        Console.WriteLine("Could Not Find: " + args[i + 1]);

                    // Done
                    break;
                }
            }
        }

        #endregion

        #region x86 Assembly

        static bool CompileX86Patches(string patchFile)
        {
            #region Depricated

            //// Get a some environment variables that we will need later.
            //string drive = System.Environment.GetEnvironmentVariable("HOMEDRIVE");
            //string userFolder = System.Environment.GetEnvironmentVariable("USERPROFILE");

            //// Get the path of this executable and the x86 folder, and check that they are ms-dos safe.
            //string exePath = Process.GetCurrentProcess().MainModule.FileName;
            //string x86Folder = string.Format("{0}\\bin\\x86\\", exePath.Substring(0, exePath.LastIndexOf("\\")));
            //if (MSDOS.IsSafeFilePath(x86Folder) == false)
            //{
            //    // We need to copy over the x86 folder to a MS-DOS safe folder.
            //    try
            //    {
            //        // Create a MS-DOS safe folder and copy the contents of the x86 folder over.
            //        x86Folder = MSDOS.CreateMsDosSafeFolder("XePatcher_x86", x86Folder, true);

            //        // Check that the new folder exists and all the files we need are in it.
            //        if (Directory.Exists(x86Folder) == true)
            //        {
            //            // Check for child files.
            //            if (File.Exists(string.Format("{0}ml.exe", x86Folder)) == false ||
            //                File.Exists(string.Format("{0}link.exe", x86Folder)) == false ||
            //                File.Exists(string.Format("{0}MAKE.BAT", x86Folder)) == false)
            //            {
            //                // Print error and return.
            //                Console.WriteLine("XePatcher is not located in a MS-DOS safe folder and " +
            //                    "all attempts to create one have failed! Please run XePatcher from a " +
            //                    "MS-DOS safe folder (no spaces must exist in the folder path).\n");
            //                return false;
            //            }
            //        }
            //        else
            //        {
            //            // Print error and return.
            //            Console.WriteLine("XePatcher is not located in a MS-DOS safe folder and " +
            //                "all attempts to create one have failed! Please run XePatcher from a " +
            //                "MS-DOS safe folder (no spaces must exist in the folder path).\n");
            //            return false;
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        // Print error and return.
            //        Console.WriteLine("XePatcher has encoutered an error while trying to create a " +
            //            "MS-DOS safe folder. Please run XePatcher from a " +
            //            "MS-DOS safe folder (no spaces must exist in the folder path).\nException: {0}\n", e.ToString());
            //        return false;
            //    }
            //}

            #endregion

            // Get the path of ml.exe
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            string x86Folder = string.Format("{0}\\bin\\x86\\", exePath.Substring(0, exePath.LastIndexOf("\\")));
            string mlExe = string.Format("{0}ml.exe", x86Folder);

            // Get the paths for the obj and bin files we will be creating.
            string objFile = string.Format("{0}.obj", patchFile.Substring(0, patchFile.IndexOf('.')));
            string binFile = string.Format("{0}.bin", patchFile.Substring(0, patchFile.IndexOf('.')));

            // If there are any old files delete them now.
            if (File.Exists(binFile) == true)
                File.Delete(binFile);
            if (File.Exists(objFile) == true)
                File.Delete(objFile);

            // Run the assembler in a background cmd line instance.
            bool result = false;
            using (Process compiler = new Process())
            {
                // Setup the cmd line instance.
                compiler.StartInfo.FileName = mlExe;
                compiler.StartInfo.WorkingDirectory = x86Folder;
                compiler.StartInfo.Arguments = string.Format("/AT /Fo \"{0}\" /c \"{1}\"", objFile, patchFile);
                compiler.StartInfo.UseShellExecute = false;
                compiler.StartInfo.RedirectStandardOutput = true;
                compiler.StartInfo.RedirectStandardError = true;
                compiler.Start();

                // Wait for the assembler to exit.
                Console.Write("Compiling Patches...");
                compiler.WaitForExit();

                // Capture the output and check for any errors.
                string Output = compiler.StandardOutput.ReadToEnd();
                result = Output.Contains("error");
                Console.WriteLine(Output.Contains("error") ? "\r\nError Compiling Patches: " + Output : " Success");
            }

            // Check for any errors, if there are non process the COFF file.
            if (!result == true)
            {
                // Create a new COFF object and parse the obj file.
                COFF coff_file = new COFF(objFile);

                // Get the bytes for the .text section since thats all we care about.
                byte[] textData = coff_file.GetTextSectionData();
                if (textData != null)
                {
                    // Write .text data to a bin file.
                    File.WriteAllBytes(binFile, textData);
                }
                else
                {
                    // Write error and return.
                    Console.WriteLine("obj file is currupt or of the wrong format!");
                    return false;
                }

                // Delete the obj file since we do not need it any more.
                if (File.Exists(objFile) == true)
                    File.Delete(objFile);
            }

            return !result;
        }

        #endregion

        #region Binary file patching

        public static void PatchBinaryFile(string binFile, string outFile, string patchFile, bool isBigEndian)
        {
            // Out file/memory io helpers.
            IO.EndianReader patch_reader = null;
            IO.EndianWriter binary_writer = null;

            // Open the patch file.
            patch_reader = new IO.EndianReader((isBigEndian == true ? IO.Endianness.Big : IO.Endianness.Little),
                new FileStream(patchFile, FileMode.Open, FileAccess.Read, FileShare.Read));

            // Check if we should create a new output file.
            if (outFile != null)
            {
                // Copy the bin file to the outFile path.
                File.Copy(binFile, outFile, true);

                // Open the new out file.
                binary_writer = new IO.EndianWriter((isBigEndian == true ? IO.Endianness.Big : IO.Endianness.Little),
                    new FileStream(outFile, FileMode.Open, FileAccess.Write, FileShare.Write));
            }
            else
                binary_writer = new IO.EndianWriter((isBigEndian == true ? IO.Endianness.Big : IO.Endianness.Little),
                    new FileStream(binFile, FileMode.Open, FileAccess.Write, FileShare.Write));

            // Check to make sure we have a valid reader and writer.
            Debug.Assert(patch_reader != null);
            Debug.Assert(binary_writer != null);

            // Loop through the patch file and patch the binary file.
            Console.Write("Patching File...");
            while (patch_reader.BaseStream.Position < patch_reader.BaseStream.Length)
            {
                // Read the patch address and make sure it is valid.
                long Address = patch_reader.ReadInt32() & 0xFFFFFFFF;
                if ((int)Address == -1)
                    break;

                // Read in the patch data.
                int Count = patch_reader.ReadInt32();
                byte[] Patch = patch_reader.ReadBytes(Count);

                // Apply patch to the binary file.
                try
                {
                    binary_writer.BaseStream.Position = Address;
                    binary_writer.Write(Patch);
                }
                catch
                { Console.WriteLine("Failed To Patch: " + Address.ToString("X")); }
            }

            // Done, close the file streams.
            Console.WriteLine(" Done");
            patch_reader.Close();
            binary_writer.Close();
        }

        //public static void PatchBootloader(string[] args)
        //{
        //    // Runtime Data
        //    bool Continue = false;
        //    int PathIndex = -1;

        //    // Compile Patches
        //    for (int i = 0; i < args.Length; i++)
        //    {
        //        if (args[i] == "-p")
        //        {
        //            // Check It Exists
        //            if (File.Exists(args[i + 1]))
        //            {
        //                Continue = CompileXexPatches(args[i + 1]);
        //                PathIndex = i + 1;
        //            }
        //            else
        //                Console.WriteLine("Could Not Find: " + args[i + 1]);

        //            // Done
        //            break;
        //        }
        //    }

        //    // Check For Error
        //    if (!Continue)
        //        return;

        //    // Patch to Xex
        //    for (int i = 0; i < args.Length; i++)
        //    {
        //        if (args[i] == "-b")
        //        {
        //            // Check It Exists
        //            if (File.Exists(args[i + 1]))
        //            {
        //                // Open Patch File
        //                BinaryReader br = new BinaryReader(new FileStream(args[PathIndex].ToLower().Replace(".s", ".bin"), FileMode.Open, FileAccess.Read, FileShare.Read));

        //                // Open file
        //                BinaryWriter bw = new BinaryWriter(new FileStream(args[i + 1], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));

        //                // Begin Patching
        //                Console.Write("Patching File...");
        //                for (; ; )
        //                {
        //                    // Test Address
        //                    long Address = ByteFlip32(br.ReadInt32()) & 0xFFFFFFFF;
        //                    if ((int)Address == -1 && br.BaseStream.Position == br.BaseStream.Length)
        //                        break;

        //                    // Read Patch
        //                    int Count = ByteFlip32(br.ReadInt32());
        //                    byte[] Patch = br.ReadBytes(Count);

        //                    // Apply To Xex
        //                    try
        //                    {
        //                        // Subtract kernel address
        //                        if (Address >= 0x80000000)
        //                            Address -= 0x80000000;

        //                        bw.BaseStream.Position = Address;
        //                        bw.Write(Patch);
        //                    }
        //                    catch
        //                    { Console.WriteLine("Failed To Patch: " + Address.ToString("X")); }
        //                }

        //                // Done
        //                Console.WriteLine(" Done");
        //                br.Close();
        //                bw.Close();
        //            }
        //            else
        //                Console.WriteLine("Could Not Find: " + args[i + 1]);

        //            // Done
        //            break;
        //        }
        //    }
        //}

        #endregion

        static void PrintUse()
        {
            Console.WriteLine("Use: XePatcher.exe -p <patch_file> -xbe/-xex/-b <file> <options>");
            Console.WriteLine("   -p <file> Patch file to compile");
            Console.WriteLine("   -xbe <file> Xbox 1 Executable");
            Console.WriteLine("   -xex <file> Xbox 360 Executable");
            Console.WriteLine("   -bin <file> Binary file to apply patches to, must use -proc for binary files");
            Console.WriteLine("   -proc <type> Processor type, either x86 or ppc");

            Console.WriteLine("Optional:");
            Console.WriteLine("   -c Compress Xex Image");
            Console.WriteLine("   -e Encrypt Xex Image");
            Console.WriteLine("   -o <file> Output file, input executable/binary file will remain untouched");
        }

        static void Main(string[] args)
        {
            // Print the application logo.
            Console.WriteLine("XePatcher v2.5 by TheFallen93");
            Console.WriteLine();

            // Temp Args
            //args = new string[]
            //{
            //    "-proc",
            //    "x86",
            //    "-p",
            //    "X:\\Halo 2\\Xbox\\Trainer\\sample_x86.asm",
            //    "-bin",
            //    "C:\\Program Files (x86)\\Microsoft Games\\Halo 2 Map Editor\\H2Guerilla.exe",
            //    "-o",
            //    "C:\\Program Files (x86)\\Microsoft Games\\Halo 2 Map Editor\\H2Guerilla_h4x.exe"
            //};

            // Check for the minimum number of required parameters.
            if (args.Length < 2)
                goto print_use;

            // Create a CommandLine object to parse the command line for us.
            CommandLine cmd = new CommandLine(args);

            // Check for certain keys and options.
            string patch_file = cmd.GetKeyValue("-p", false);
            string xbe_file = cmd.GetKeyValue("-xbe", false);
            string xex_file = cmd.GetKeyValue("-xex", false);
            string binary_file = cmd.GetKeyValue("-bin", false);
            string output_file = cmd.GetKeyValue("-o", false);
            string proc_type = cmd.GetKeyValue("-proc", false);
            bool compress = cmd.FindOption("-c", false);
            bool encrypt = cmd.FindOption("-e", false);

            // Check that we have a valid patch file.
            if (patch_file == null || File.Exists(patch_file) == false)
            {
                // Print error and ussage.
                Console.WriteLine("patch file not specified or file does not exist!\n");
                goto print_use;
            }

            // Check that any executable/binary files specified are valid.
            string[] files = { xbe_file, xex_file, binary_file };
            for (int i = 0; i < files.Length; i++)
            {
                // Check the file exists.
                if (files[i] != null && File.Exists(files[i]) == false)
                {
                    // Print error and ussage.
                    Console.WriteLine("could not find {0}!", files[i]);
                    goto print_use;
                }
            }

            // Check for a processor type and parse it if it exists.
            ProcessorType procType = ProcessorType.none;
            if (proc_type != null)
            {
                // Parse processor type.
                if (proc_type.Equals("x86") == true)
                    procType = ProcessorType.x86;
                else if (proc_type.Equals("ppc") == true)
                    procType = ProcessorType.ppc;

                // If the processor type is none then the argument for processor type is invalid.
                if (procType == ProcessorType.none)
                {
                    // Print error and ussage.
                    Console.WriteLine("invalid processor type {0}!", proc_type);
                    goto print_use;
                }
            }

            // If a binary file was specified make sure there is a processor type set.
            if (binary_file != null && procType == ProcessorType.none)
            {
                // Print error and ussage.
                Console.WriteLine("processor type must be set when using a binary file!");
                goto print_use;
            }

            // If only a patch file was specified make sure there is a processor type set.
            if (xbe_file == null && xex_file == null && binary_file == null)
            {
                // Check processor type
                if (procType == ProcessorType.none)
                {
                    // Print error and ussage.
                    Console.WriteLine("processor type must be set when using a lone patch file!");
                    goto print_use;
                }
            }

            // Check what type of opperation to perform.
            if (xex_file != null && patch_file != null)
            {

            }
            else if (xbe_file != null && patch_file != null)
            {

            }
            else if (binary_file != null && patch_file != null && procType != ProcessorType.none)
            {
                // Compile the patch file accordingly.
                bool result = false;
                if (procType == ProcessorType.x86)
                {
                    // Try to compile the patch
                    result = CompileX86Patches(patch_file);
                }
                else if (procType == ProcessorType.ppc)
                {
                    throw new NotImplementedException();
                }

                // Check that the patches compiled successfully.
                string patch_bin = string.Format("{0}.bin", patch_file.Substring(0, patch_file.IndexOf('.')));
                if (result == false || File.Exists(patch_bin) == false)
                {
                    // Failed to compile patches, exit the application.
                    goto application_exit;
                }

                // Patch the file accordingly.
                PatchBinaryFile(binary_file, output_file, patch_bin, (procType == ProcessorType.ppc));
            }

        application_exit:
            return;

        print_use:
            PrintUse();
            return;
        }
    }
}
