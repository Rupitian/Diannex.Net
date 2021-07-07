using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace Diannex.NET
{
    /// <summary>
    /// A processed representation of the compiled Diannex code.
    /// </summary>
    public class Binary
    {
        /// <summary>
        /// True if either an internal translation file is loaded, or when an external translation file is loaded.
        /// </summary>
        /// <remarks>
        /// NOTE: If you try to execute code with dialogue when this is false you <b>WILL</b> crash.
        /// </remarks>
        public bool TranslationLoaded;
        /// <summary>
        /// A list of internal strings used in the Diannex code, e.g. function/scene names.
        /// </summary>
        public List<string> StringTable; // [ID]: <string>
        /// <summary>
        /// A list of dialogue and other translatable strings that's displayed to the user.
        /// </summary>
        public List<string> TranslationTable;

        internal byte[] Instructions;
        internal uint[] ExternalFunctions;

        internal List<(int, List<int>)> Scenes;
        internal List<(int, List<int>)> Functions;
        internal List<(int, int, int)> Definitions;

        /// <summary>
        /// <inheritdoc cref="Binary"/>
        /// </summary>
        public Binary()
        {
            TranslationLoaded = false;
            StringTable = new List<string>();
            TranslationTable = new List<string>();

            Scenes = new List<(int, List<int>)>();
            Functions = new List<(int, List<int>)>();
            Definitions = new List<(int, int, int)>();
        }

        /// <summary>
        /// Loads a Diannex binary from a file.
        /// </summary>
        /// <param name="path">Path to the binary file.</param>
        /// <returns>The processed binary.</returns>
        /// <exception cref="BinaryReaderException"/>
        public static Binary ReadFromFile(string path)
        {
            Binary b = new Binary();
            byte[] block;
            using (BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read)))
            {
                var sig = br.ReadBytes(3);
                if (sig[0] != 'D' || sig[1] != 'N' || sig[2] != 'X')
                {
                    throw new BinaryReaderException(path, "Invalid signature!");
                }

                var ver = br.ReadByte();
                if (ver != 3)
                {
                    throw new BinaryReaderException(path, "Binary file not for this version of Diannex.");
                }

                var flags = br.ReadByte();
                bool compressed = Convert.ToBoolean(flags & 0x01);
                bool internalTranslationFile = Convert.ToBoolean(flags >> 1);

                Console.WriteLine($"Binary File: {path}\nSignature: {sig[0]}{sig[1]}{sig[2]}\nVersion: {ver}\nCompressed?: {compressed}\nInternal Translation File?: {internalTranslationFile}");

                b.TranslationLoaded = internalTranslationFile;

                if (compressed)
                {
                    uint decompSize = br.ReadUInt32();
                    uint compSize = br.ReadUInt32();
                    block = new byte[decompSize];
                    _ = br.ReadBytes(2); // DeflateStream doesn't handle the zlib header, so we're gonna skip it
                    byte[] compressedData = br.ReadBytes((int)compSize - 2);
                    using DeflateStream decompStream = new DeflateStream(new MemoryStream(compressedData), CompressionMode.Decompress);
                    decompStream.CopyTo(new MemoryStream(block), (int)decompSize);
                }
                else
                {
                    uint size = br.ReadUInt32();
                    block = br.ReadBytes((int)size);
                }
            }

            using (BinaryReader br = new BinaryReader(new MemoryStream(block)))
            {
                uint sceneCount = br.ReadUInt32();
                for (uint i = 0; i < sceneCount; i++)
                {
                    var symbol = br.ReadUInt32();
                    var count = br.ReadUInt16();
                    var bytecodeIndices = new List<int>();
                    for (int x = 0; x < count; x++)
                    {
                        bytecodeIndices.Add(br.ReadInt32());
                    }
                    b.Scenes.Add(((int)symbol, bytecodeIndices));
                }

                uint functionCount = br.ReadUInt32();
                for (uint i = 0; i < functionCount; i++)
                {
                    var symbol = br.ReadUInt32();
                    var count = br.ReadUInt16();
                    var bytecodeIndices = new List<int>();
                    for (int x = 0; x < count; x++)
                    {
                        bytecodeIndices.Add(br.ReadInt32());
                    }
                    b.Functions.Add(((int)symbol, bytecodeIndices));
                }

                uint definitionCount = br.ReadUInt32();
                for (uint i = 0; i < definitionCount; i++)
                {
                    var symbol = br.ReadUInt32();
                    var stringRef = br.ReadUInt32();
                    var bytecodeIndex = br.ReadInt32();
                    b.Definitions.Add(((int)symbol, (int)stringRef, bytecodeIndex));
                }

                uint bytecodeCount = br.ReadUInt32();
                b.Instructions = br.ReadBytes((int)bytecodeCount);

                uint internalStringCount = br.ReadUInt32();
                Console.WriteLine("Internal String Count: {0}", internalStringCount);
                for (uint i = 0; i < internalStringCount; i++)
                {
                    List<char> strBytes = new List<char>();
                    byte byteCurrent;
                    while ((byteCurrent = br.ReadByte()) != 0)
                    {
                        strBytes.Add((char)byteCurrent);
                    }
                    b.StringTable.Add(new string(strBytes.ToArray()));
                }

                uint translationStringCount = br.ReadUInt32();
                Console.WriteLine("Translation String Count: {0}", translationStringCount);
                for (uint i = 0; i < translationStringCount; i++)
                {
                    List<char> strBytes = new List<char>();
                    byte byteCurrent;
                    while ((byteCurrent = br.ReadByte()) != 0)
                    {
                        strBytes.Add((char)byteCurrent);
                    }
                    b.TranslationTable.Add(new string(strBytes.ToArray()));
                }

                uint externalFunctionCount = br.ReadUInt32();
                Console.WriteLine("External Function Count: {0}", externalFunctionCount);
                b.ExternalFunctions = new uint[externalFunctionCount];
                for (uint i = 0; i < externalFunctionCount; i++)
                {
                    b.ExternalFunctions[i] = br.ReadUInt32();
                }
            }
            return b;
        }

        /// <summary>
        /// Thrown whenever an error ocurrs in processing of a Diannex binary file.
        /// </summary>
        public class BinaryReaderException : Exception
        {
            public string File;

            public BinaryReaderException(string file, string message) : base(message)
            {
                File = file;
            }
        }
    }
}
