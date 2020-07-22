using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace Diannex.NET
{
    public class Binary
    {
        public bool InternalTranslation;
        public List<string> StringTable; // [ID]: <string>
        public List<string> TranslationTable;
        public List<Instruction> Instructions;

        public List<(int, int)> Scenes; // TODO: Promote to object
        public List<(int, int)> Functions; // TODO: Promote to object
        public List<(int, int, int)> Definitions; // TODO: Promote to object

        public Binary()
        {
            InternalTranslation = true;
            StringTable = new List<string>();
            TranslationTable = new List<string>();
            Instructions = new List<Instruction>();

            Scenes = new List<(int, int)>();
            Functions = new List<(int, int)>();
            Definitions = new List<(int, int, int)>();
        }

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
                if (ver != 1)
                {
                    throw new BinaryReaderException(path, "Binary file not for this version of Diannex.");
                }

                var flags = br.ReadByte();
                bool compressed, internalTranslationFile;
                // (compressed | (internalTranslationFile << 1))
                compressed = Convert.ToBoolean(flags & 0x01);
                internalTranslationFile = Convert.ToBoolean(flags >> 1);

                b.InternalTranslation = internalTranslationFile;

                // TODO: Read in actual blocks to save memory?
                if (compressed)
                {
                    uint decompSize = br.ReadUInt32();
                    uint compSize = br.ReadUInt32();
                    block = new byte[decompSize];
                    _ = br.ReadBytes(2); // DeflateStream doesn't handle the zlib header, so we're gonna skip it
                    byte[] compressedData = br.ReadBytes((int)compSize - 2);
                    using (DeflateStream decompStream = new DeflateStream(new MemoryStream(compressedData), CompressionMode.Decompress))
                    {
                        
                        decompStream.CopyTo(new MemoryStream(block), (int)decompSize);
                    }
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
                    var bytecodeIndex = br.ReadInt32();
                    b.Scenes.Add(((int)symbol, bytecodeIndex));
                }

                uint functionCount = br.ReadUInt32();
                for (uint i = 0; i < functionCount; i++)
                {
                    var symbol = br.ReadUInt32();
                    var bytecodeIndex = br.ReadInt32();
                    b.Functions.Add(((int)symbol, bytecodeIndex));
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
                for (uint i = 0; i < bytecodeCount; i++)
                {
                    Instruction inst;
                    Opcode opcode = (Opcode)br.ReadByte();
                    switch (opcode)
                    {
                        case Opcode.FreeLocal:
                        case Opcode.PushInt:
                        case Opcode.PushString:
                        case Opcode.PushBinaryString:
                        case Opcode.SetVarGlobal:
                        case Opcode.SetVarLocal:
                        case Opcode.PushVarGlobal:
                        case Opcode.PushVarLocal:
                        case Opcode.Jump:
                        case Opcode.JumpTruthy:
                        case Opcode.JumpFalsey:
                        case Opcode.ChoiceAdd:
                        case Opcode.ChoiceAddTruthy:
                        case Opcode.ChooseAdd:
                        case Opcode.ChooseAddTruthy:
                            inst = new Instruction(opcode, br.ReadInt32());
                            break;
                        case Opcode.PushInterpolatedString:
                        case Opcode.PushBinaryInterpolatedString:
                        case Opcode.Call:
                        case Opcode.CallExternal:
                            inst = new Instruction(opcode, br.ReadInt32(), br.ReadInt32());
                            break;
                        case Opcode.PushDouble:
                            inst = new Instruction(opcode, br.ReadDouble());
                            break;
                        default:
                            inst = new Instruction(opcode);
                            break;
                    }
                    b.Instructions.Add(inst);
                }

                uint internalStringCount = br.ReadUInt32();
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
            }
            return b;
        }

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
