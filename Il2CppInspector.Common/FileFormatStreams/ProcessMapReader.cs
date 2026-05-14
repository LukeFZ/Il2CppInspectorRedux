/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using NoisyCowStudios.Bin2Object;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Il2CppInspector
{
    // This is a wrapper for a Linux memory dump
    // The supplied file is a text file containing the output of "cat /proc/["self"|process-id]/maps"
    // We re-construct libil2cpp.so from the *.bin files and return it as the first image
    public class ProcessMapReader : FileFormatStream<ProcessMapReader>
    {
        private BinaryObjectStream il2cpp;
        private record MemoryRange(ulong Start, ulong End);
        private record MemoryMapping(ulong Start, ulong End, string Permissions, string Path);
        private record MemoryFile(ulong Start, ulong End, string Name);

        public override string DefaultFilename => "maps.txt";

        protected override bool Init() {

            // Maps.txt is extremely unlikely to be larger than this, so don't waste time loading many megabytes of binary data for no reason
            if (Length > 8 * 1024 * 1024)
                return false;

            // Get the entire stream as a string
            var text = System.Text.Encoding.ASCII.GetString(ToArray());

            // Line format is: https://stackoverflow.com/questions/1401359/understanding-linux-proc-id-maps
            // xxxxxxxx-yyyyyyyy ffff zzzzzzzz aa:bb c [whitespace] [image path]
            // Where x = the start address
            // Where y = the end address
            // Where f = permission flags (rwxp or -)
            // Where z = offset in file that the region was mapped from (we ignore this and build a file based on the memory dump)
            // Where aa:bb = device ID
            // Where c = inode

            var rgxProc = new Regex(@"^(?<start>[0-9A-Fa-f]+)-(?<end>[0-9A-Fa-f]+)\s+(?<perms>[rwxsp\-]{4})\s+(?<offset>[0-9A-Fa-f]+)\s+[0-9A-Fa-f]+:[0-9A-Fa-f]+\s+\d+\s+(?<path>\S+)\r?$", RegexOptions.Multiline);

            // Determine where libil2cpp.so was mapped into memory
            var il2cppMemory = rgxProc.Matches(text)
                                    .Where(m => m.Groups["path"].Value.EndsWith("libil2cpp.so"))
                                    .Select(m => new MemoryMapping(
                                        Convert.ToUInt64(m.Groups["start"].Value, 16),
                                        Convert.ToUInt64(m.Groups["end"].Value, 16),
                                        m.Groups["perms"].Value,
                                        m.Groups["path"].Value))
                                    .OrderBy(m => m.Start).ToList();

            if (il2cppMemory.Count == 0)
                return false;

            // Get file path
            // This error should never occur with the bundled CLI and GUI; only when used as a library by a 3rd party tool
            if (LoadOptions == null || !(LoadOptions.BinaryFilePath is string mapsPath))
                throw new InvalidOperationException("To load a Linux process map, you must specify the maps file path in LoadOptions");

            var mapsFileName = Path.GetFileName(mapsPath);
            var mapsFileNameLower = mapsFileName.ToLowerInvariant();
            if (!mapsFileNameLower.EndsWith("-maps.txt") && mapsFileNameLower != "maps.txt")
                throw new InvalidOperationException("To load a Linux process map, the map file must be named maps.txt or *-maps.txt");

            var mapsDir = Path.GetDirectoryName(mapsPath) ?? ".";
            var mapsPrefix = mapsFileNameLower.EndsWith("-maps.txt") ? mapsFileName[..^9] : Path.GetFileNameWithoutExtension(mapsFileName);

            // Get memory dump filenames and mappings
            var rgxFile = new Regex(@"^\S+?-(?<start>[0-9A-Fa-f]+)-(?<end>[0-9A-Fa-f]+)\.bin$");

            var files = Directory.GetFiles(mapsDir, mapsPrefix + "-*.bin")
                                    .Select(f => new { Name = f, Match = rgxFile.Match(Path.GetFileName(f)) })
                                    .Where(f => f.Match.Success)
                                    .Select(f => new MemoryFile(
                                        Convert.ToUInt64(f.Match.Groups["start"].Value, 16),
                                        Convert.ToUInt64(f.Match.Groups["end"].Value, 16),
                                        f.Name))
                                    .OrderBy(m => m.Start).ToList();

            if (files.Count == 0) {
                var layoutInspectorDump = Path.Combine(mapsDir, "libil2cpp.so.bin");
                if (File.Exists(layoutInspectorDump))
                    files.Add(inferRangeForSingleDump(il2cppMemory, layoutInspectorDump));
            }

            // Find which file(s) are needed for each chunk of libil2cpp.so
            var chunks = il2cppMemory.Select(m => new {
                                    Memory = m,
                                    Files = files.Where(f => f.Start < m.End && f.End > m.Start).ToList()
            }).Where(c => c.Files.Count != 0).ToList();

            if (chunks.Count == 0)
                return false;

            // Set image base address for ELF loader
            // ELF loader will rebase the image and mark it as modified for saving
            LoadOptions.ImageBase = chunks.First().Memory.Start;

            // Merge the files, copying each chunk from one or more files to the specified offset in the merged file
            il2cpp = new BinaryObjectStream();

            foreach (var chunk in chunks) {
                var memoryNext = chunk.Memory.Start;
                il2cpp.Position = checked((long) (chunk.Memory.Start - LoadOptions.ImageBase));

                foreach (var file in chunk.Files) {
                    var copyStart = Math.Max(memoryNext, file.Start);
                    if (copyStart >= chunk.Memory.End)
                        break;

                    if (copyStart > memoryNext) {
                        il2cpp.Position += checked((long) (copyStart - memoryNext));
                        memoryNext = copyStart;
                    }

                    var fileStart = copyStart - file.Start;

                    using var source = File.Open(file.Name, FileMode.Open, FileAccess.Read, FileShare.Read);
                    if ((ulong) source.Length <= fileStart)
                        continue;

                    // Get the entire remaining chunk, or to the end of the file if it doesn't contain the end of the chunk
                    var length = checked((int) Math.Min(chunk.Memory.End - memoryNext, (ulong) source.Length - fileStart));

                    AnsiConsole.WriteLine($"Writing {length:x8} bytes from {Path.GetFileName(file.Name)} +{fileStart:x8} ({memoryNext:x8}) to target {il2cpp.Position:x8}");

                    // Can't use Stream.CopyTo as it doesn't support length parameter
                    var buffer = new byte[length];
                    source.Position = checked((long) fileStart);
                    source.ReadExactly(buffer);
                    il2cpp.Write(buffer);

                    memoryNext += (ulong) length;
                }
            }
            RestoreElfHeader(mapsDir, chunks.Select(c => c.Memory).ToList());
            return true;
        }

        private void RestoreElfHeader(string mapsDir, List<MemoryMapping> mappings) {

            if (HasElfMagic(il2cpp))
                return;

            var originalLib = Path.Combine(mapsDir, "libil2cpp.so");
            if (TryCopyElfHeader(originalLib))
                return;

            if (TryWriteSyntheticElfHeader(mappings))
                return;

            AnsiConsole.WriteLine("Linux process map dump does not contain an ELF header and no replacement header could be inferred");
        }

        private static bool HasElfMagic(Stream stream) {

            if (stream.Length < 4)
                return false;

            var position = stream.Position;
            var magic = new byte[4];

            try {
                stream.Position = 0;
                stream.ReadExactly(magic);
            }
            finally {
                stream.Position = position;
            }

            return magic.SequenceEqual(new byte[] { 0x7f, 0x45, 0x4c, 0x46 });
        }

        private bool TryCopyElfHeader(string sourceFile) {

            if (!File.Exists(sourceFile))
                return false;

            using var source = File.Open(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var ident = new byte[0x40];

            if (source.Length < ident.Length)
                return false;

            source.ReadExactly(ident);

            if (!ident.Take(4).SequenceEqual(new byte[] { 0x7f, 0x45, 0x4c, 0x46 }))
                return false;

            var headerSize = ident[4] switch {
                1 => BitConverter.ToUInt16(ident, 0x28),
                2 => BitConverter.ToUInt16(ident, 0x34),
                _ => 0
            };

            if (headerSize == 0 || headerSize > source.Length || headerSize > il2cpp.Length)
                return false;

            var header = new byte[headerSize];
            Array.Copy(ident, header, headerSize);

            il2cpp.Write(0, header);
            IsModified = true;
            AnsiConsole.WriteLine($"Restored ELF header from {Path.GetFileName(sourceFile)}");
            return true;
        }

        private bool TryWriteSyntheticElfHeader(List<MemoryMapping> mappings) {

            if (mappings.Count == 0 || !mappings.Any(m => m.Path.Contains("/arm64/")))
                return false;

            const ushort headerSize = 0x40;
            const ushort programHeaderSize = 0x38;

            if (il2cpp.Length < headerSize + programHeaderSize * mappings.Count)
                return false;

            il2cpp.Position = 0;
            il2cpp.Write(new byte[] {
                0x7f, 0x45, 0x4c, 0x46, // ELF magic
                0x02,                   // 64-bit
                0x01,                   // little endian
                0x01,                   // original ELF version
                0x00, 0x00,             // System V ABI
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            });
            il2cpp.Write((ushort) 3);              // ET_DYN
            il2cpp.Write((ushort) Elf.EM_AARCH64);
            il2cpp.Write((uint) 1);
            il2cpp.Write(0ul);                     // e_entry
            il2cpp.Write((ulong) headerSize);      // e_phoff
            il2cpp.Write(0ul);                     // e_shoff
            il2cpp.Write((uint) 0);                // e_flags
            il2cpp.Write(headerSize);
            il2cpp.Write(programHeaderSize);
            il2cpp.Write(checked((ushort) mappings.Count));
            il2cpp.Write((ushort) 0);              // e_shentsize
            il2cpp.Write((ushort) 0);              // e_shnum
            il2cpp.Write((ushort) 0);              // e_shtrndx

            foreach (var (mapping, index) in mappings.Select((m, i) => (m, i))) {
                var offset = mapping.Start - LoadOptions.ImageBase;

                il2cpp.Position = headerSize + programHeaderSize * index;
                il2cpp.Write((uint) Elf.PT_LOAD);
                il2cpp.Write(GetElfSegmentFlags(mapping.Permissions));
                il2cpp.Write(offset);
                il2cpp.Write(offset);
                il2cpp.Write(offset);
                il2cpp.Write(mapping.End - mapping.Start);
                il2cpp.Write(mapping.End - mapping.Start);
                il2cpp.Write(0x1000ul);
            }

            IsModified = true;
            AnsiConsole.WriteLine("Synthesized ELF64/AArch64 header from process maps");
            return true;
        }

        private static uint GetElfSegmentFlags(string permissions) {

            var flags = 0u;

            if (permissions.Contains('r'))
                flags |= (uint) Elf.PF_R;
            if (permissions.Contains('w'))
                flags |= (uint) Elf.PF_W;
            if (permissions.Contains('x'))
                flags |= (uint) Elf.PF_X;

            return flags;
        }

        private static MemoryFile inferRangeForSingleDump(List<MemoryMapping> il2cppMemory, string fileName) {

            var fileLength = (ulong) new FileInfo(fileName).Length;
            var candidates = new List<MemoryRange>();

            for (var i = 0; i < il2cppMemory.Count; i++) {
                for (var j = i; j < il2cppMemory.Count; j++) {
                    var start = il2cppMemory[i].Start;
                    var end = il2cppMemory[j].End;
                    if (end > start && end - start == fileLength)
                        candidates.Add(new MemoryRange(start, end));
                }
            }

            if (candidates.Count == 1)
                return new MemoryFile(candidates[0].Start, candidates[0].End, fileName);

            throw new InvalidOperationException($"Could not infer memory range for {Path.GetFileName(fileName)} from libil2cpp.so maps");
        }

        public override IFileFormatStream this[uint index] {
            get {
                // Get merged stream as ELF file
                return (IFileFormatStream) ElfReader32.Load(il2cpp, LoadOptions, OnStatusUpdate) ?? ElfReader64.Load(il2cpp, LoadOptions, OnStatusUpdate);
            }
        }
    }
}
