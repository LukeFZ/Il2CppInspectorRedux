/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using Il2CppInspector.Next;
using Il2CppInspector.Next.BinaryMetadata;
using System.Buffers;
using System.Text;
using VersionedSerialization;

namespace Il2CppInspector
{
    partial class Il2CppBinary
    {
        // Boyer-Moore-Horspool
        public IEnumerable<uint> FindAllBytes(byte[] blob, byte[] signature, uint requiredAlignment = 1)
        {
            var badBytes = ArrayPool<uint>.Shared.Rent(256);

            var signatureLength = (uint) signature.Length;

            for (uint i = 0; i < 256; i++)
            {
                badBytes[(int)i] = signatureLength;
            }

            var lastSignatureIndex = signatureLength - 1;

            for (uint i = 0; i < lastSignatureIndex; i++)
            {
                badBytes[signature[(int)i]] = lastSignatureIndex - i;
            }

            var blobLength = blob.Length;

            var currentIndex = 0u;

            while (currentIndex <= blobLength - signatureLength)
            {
                for (uint i = lastSignatureIndex; blob[currentIndex + i] == signature[(int)i]; i--)
                {
                    if (i == 0)
                    {
                        yield return currentIndex;
                        break;
                    }
                }

                currentIndex += badBytes[blob[currentIndex + lastSignatureIndex]];

                var alignment = currentIndex % requiredAlignment;
                if (alignment != 0)
                    currentIndex += requiredAlignment - alignment;
            }

            ArrayPool<uint>.Shared.Return(badBytes);
        }

        // Find strings
        private IEnumerable<uint> FindAllStrings(byte[] blob, string str) => FindAllBytes(blob, Encoding.ASCII.GetBytes(str), 1);

        // Find 32-bit words
        private IEnumerable<uint> FindAllDWords(byte[] blob, uint word) => FindAllBytes(blob, BitConverter.GetBytes(word), 4);

        // Find 64-bit words
        private IEnumerable<uint> FindAllQWords(byte[] blob, ulong word) => FindAllBytes(blob, BitConverter.GetBytes(word), 8);

        // Find words for the current binary size
        private IEnumerable<uint> FindAllWords(byte[] blob, ulong word)
            => Image.Bits switch {
                32 => FindAllDWords(blob, (uint) word),
                64 => FindAllQWords(blob, word),
                _ => throw new InvalidOperationException("Invalid architecture bit size")
            };

        // Find all valid virtual address pointers to a virtual address
        private IEnumerable<ulong> FindAllMappedWords(byte[] blob, ulong va)
        {
            var fileOffsets = FindAllWords(blob, va);
            foreach (var offset in fileOffsets)
                if (Image.TryMapFileOffsetToVA(offset, out va))
                    yield return va;
        }

        // Find all valid virtual address pointers to a set of virtual addresses
        private IEnumerable<ulong> FindAllMappedWords(byte[] blob, IEnumerable<ulong> va) => va.SelectMany(a => FindAllMappedWords(blob, a));

        // Find all valid pointer chains to a set of virtual addresses with the specified number of indirections
        private IEnumerable<ulong> FindAllPointerChains(byte[] blob, ulong va, int indirections)
        {
            foreach (var vas in FindAllMappedWords(blob, va))
            {
                if (indirections == 1)
                {
                    yield return vas;
                }
                else
                {
                    foreach (var foundPointer in FindAllPointerChains(blob, vas, indirections - 1))
                    {
                        yield return foundPointer;
                    }
                }
            }

            //IEnumerable<ulong> vas = va;
            //for (int i = 0; i < indirections; i++)
            //    vas = FindAllMappedWords(blob, vas);
            //return vas;
        }

        // Scan the image for the needed data structures
        private (ulong, ulong) ImageScan(Metadata metadata) {
            Image.Position = 0;
            var imageBytes = Image.ReadBytes((int) Image.Length);

            var ptrSize = (uint)Image.Bits / 8;
            var methodPointersCount = metadata.Methods.Count(m => (uint)m.MethodIndex != 0xffff_ffff);
            var metadataUsageCount = metadata.Version < MetadataVersions.V270 && metadata.MetadataUsagePairs.Length > 0
                ? (int)metadata.MetadataUsagePairs.Max(x => x.DestinationIndex) + 1
                : 0;
            ulong codeRegistration = 0;
            IEnumerable<ulong> vas;

            // Find CodeRegistration
            // >= 24.2
            if (metadata.Version >= MetadataVersions.V242) {

                // < 27: mscorlib.dll is always the first CodeGenModule
                // >= 27: mscorlib.dll is always the last CodeGenModule (Assembly-CSharp.dll is always the first but non-Unity builds don't have this DLL)
                //        NOTE: winrt.dll + other DLLs can come after mscorlib.dll so we can't use its location to get an accurate module count
                ulong FindCodeRegistration()
                {
                    var imagesCount = Metadata.Images.Length;

                    foreach (var offset in FindAllStrings(imageBytes, "mscorlib.dll\0"))
                    {
                        if (!Image.TryMapFileOffsetToVA(offset, out var va))
                            continue;

                        // Unwind from string pointer -> CodeGenModule -> CodeGenModules + x
                        foreach (var potentialCodeGenModules in FindAllPointerChains(imageBytes, va, 2))
                        {
                            if (metadata.Version >= MetadataVersions.V270)
                            {
                                for (int i = imagesCount - 1; i >= 0; i--)
                                {
                                    foreach (var potentialCodeRegistrationPtr in FindAllPointerChains(imageBytes,
                                                 potentialCodeGenModules - (ulong) i * ptrSize, 1))
                                    {
                                        var expectedImageCountPtr = potentialCodeRegistrationPtr - ptrSize;
                                        if (!Image.TryMapVATR(expectedImageCountPtr, out _))
                                            continue;

                                        var expectedImageCount = Image.ReadMappedWord(expectedImageCountPtr);
                                        if (expectedImageCount == imagesCount)
                                            return potentialCodeRegistrationPtr;
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < imagesCount; i++)
                                {
                                    foreach (var potentialCodeRegistrationPtr in FindAllPointerChains(imageBytes,
                                                 potentialCodeGenModules - (ulong)i * ptrSize, 1))
                                    {
                                        return potentialCodeRegistrationPtr;
                                    }
                                }
                            }
                        }
                    }

                    return 0;
                }

                /*
                // We'll work back one pointer width at a time trying to find the first CodeGenModule
                // Let's hope there aren't more than 200 DLLs in any given application :)
                var maxCodeGenModules = 200;

                for (int backtrack = 0; backtrack < maxCodeGenModules && (codeRegVas?.Count() ?? 0) != 1; backtrack++) {
                    // Unwind from CodeGenModules + x -> CodeRegistration + y
                    codeRegVas = FindAllMappedWords(imageBytes, vas);

                    // The previous word must be the number of CodeGenModules
                    if (codeRegVas.Count() == 1) {
                        var codeGenModuleCount = Image.ReadMappedWord(codeRegVas.First() - ptrSize);

                        // Basic validity check
                        if (codeGenModuleCount <= 0 || codeGenModuleCount > maxCodeGenModules)
                            codeRegVas = Enumerable.Empty<ulong>();
                    }

                    // Move to the previous CodeGenModule if the above fails
                    vas = vas.Select(va => va - ptrSize);
                }

                if (!codeRegVas.Any())
                    return (0, 0);

                if (codeRegVas.Count() > 1)
                    throw new InvalidOperationException("More than one valid pointer chain found during data heuristics");
                */

                var codeRegVa = FindCodeRegistration();

                if (codeRegVa != 0)
                {
                    try
                    {
                        var codeGenEndPtr = codeRegVa + ptrSize;
                        // pCodeGenModules is the last field in CodeRegistration so we subtract the size of one pointer from the struct size
                        codeRegistration = codeGenEndPtr - (ulong)Il2CppCodeRegistration.Size(Image.Version, Image.Bits == 32);

                        // In v24.3, windowsRuntimeFactoryTable collides with codeGenModules. So far no samples have had windowsRuntimeFactoryCount > 0;
                        // if this changes we'll have to get smarter about disambiguating these two.
                        var cr = Image.ReadMappedVersionedObject<Il2CppCodeRegistration>(codeRegistration);

                        if (Image.Version == MetadataVersions.V242 && cr.InteropDataCount == 0)
                        {
                            Image.Version = MetadataVersions.V243;
                            codeRegistration = codeGenEndPtr - (ulong)Il2CppCodeRegistration.Size(Image.Version, Image.Bits == 32);
                        }

                        if (Image.Version == MetadataVersions.V270 && cr.ReversePInvokeWrapperCount > 0x30000)
                        {
                            // If reversePInvokeWrapperCount is a pointer, then it's because we're actually on 27.1 and there's a genericAdjustorThunks pointer interfering.
                            // We need to bump version to 27.1 and back up one more pointer.
                            Image.Version = MetadataVersions.V271;
                            codeRegistration = codeGenEndPtr - (ulong)Il2CppCodeRegistration.Size(Image.Version, Image.Bits == 32);
                            cr = Image.ReadMappedVersionedObject<Il2CppCodeRegistration>(codeRegistration);
                        }

                        // genericAdjustorThunks was inserted before invokerPointersCount in 24.5 and 27.1
                        // pointer expected if we need to bump version
                        if (Image.Version == MetadataVersions.V244 &&
                            (cr.InvokerPointersCount > 0x50000 || cr.ReversePInvokeWrapperCount > cr.ReversePInvokeWrappers))
                        {
                            Image.Version = MetadataVersions.V245;
                            codeRegistration = codeGenEndPtr - (ulong)Il2CppCodeRegistration.Size(Image.Version, Image.Bits == 32);
                            cr = Image.ReadMappedVersionedObject<Il2CppCodeRegistration>(codeRegistration);
                        }

                        if ((Image.Version == MetadataVersions.V290 || Image.Version == MetadataVersions.V310) &&
                            cr.GenericMethodPointersCount >= cr.GenericMethodPointers)
                        {
                            Image.Version = new StructVersion(Image.Version.Major, 0, MetadataVersions.Tag2022);
                            codeRegistration = codeGenEndPtr - (ulong)Il2CppCodeRegistration.Size(Image.Version, Image.Bits == 32);
                        }
                    }
                    catch
                    {
                        codeRegistration = 0;
                    }
                }
            }

            // Find CodeRegistration
            // <= 24.1
            else
            {
                // The first item in CodeRegistration is the total number of method pointers
                vas = FindAllMappedWords(imageBytes, (ulong)methodPointersCount);

                // The count of method pointers will be followed some bytes later by
                // the count of custom attribute generators; the distance between them
                // depends on the il2cpp version so we just use ReadMappedObject to simplify the math
                foreach (var va in vas) {
                    var cr = Image.ReadMappedVersionedObject<Il2CppCodeRegistration>(va);

                    if (cr.CustomAttributeCount == metadata.AttributeTypeRanges.Length)
                        codeRegistration = va;
                }

            }

            // Find MetadataRegistration
            // >= 19
            var metadataRegistration = 0ul;

            // Find TypeDefinitionsSizesCount (4th last field) then work back to the start of the struct
            // This saves us from guessing where metadataUsagesCount is later
            var mrSize = (ulong)Il2CppMetadataRegistration.Size(Image.Version, Image.Bits == 32);
            var typesLength = (ulong)metadata.Types.Length;

            vas = FindAllMappedWords(imageBytes, typesLength).Select(a => a - mrSize + ptrSize * 4);

            // >= 19
            // Luke: Previously, a check comparing MetadataUsagesCount was used here, 
            // but I know of at least one binary where this will break detection.
            // Testing showed that we can just use the same heuristic used for v27+
            // on older versions as well, so we'll just use it for all cases.

            // On some 32-bit (arm) binaries, just checking fields with the type count is not sufficient
            // for uniquely identifying the metareg. We assume that all binaries have at least one generic class + inst
            // and use this for an additional heuristic, though this is kinda jank and might not be enough for all binaries
            if (Image.Version >= MetadataVersions.V190)
            {
                foreach (var va in vas)
                {
                    try
                    {
                        var mr = Image.ReadMappedVersionedObject<Il2CppMetadataRegistration>(va);
                        if (mr.TypeDefinitionsSizesCount == metadata.Types.Length
                            && mr.FieldOffsetsCount == metadata.Types.Length
                            && mr is { GenericInstsCount: > 0, GenericClassesCount: > 0 })
                        {
                            metadataRegistration = va;
                            break;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            if (codeRegistration == 0 || metadataRegistration == 0)
            {
                if (!Image.TryGetSections(out var sectionList))
                    return (0, 0);

                var sections = sectionList.ToList();
                var exec = sections.Where(x => x.IsExec).ToList();
                var data = sections.Where(x => x.IsData && !x.IsBSS).ToList();
                var bss = sections.Where(x => x.IsBSS).ToList();

                if (Image is NsoReader)
                {
                    data = data
                        .OrderByDescending(x => string.Equals(x.Name, "data", StringComparison.OrdinalIgnoreCase))
                        .ThenByDescending(x => string.Equals(x.Name, "rodata", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!exec.Any() || !data.Any())
                    return (0, 0);

                var pointerInExec = false;
                var sectionCodeRegistrationNeedsNormalization = false;

                ulong ReadPointer()
                    => Image.Bits == 32 ? Image.ReadUInt32() : Image.ReadUInt64();

                bool CheckFileOffsetInSections(ulong fileOffset, List<Section> candidates)
                    => candidates.Any(x => fileOffset >= x.ImageStart && fileOffset <= x.ImageEnd);

                bool CheckVirtualAddressInSections(IEnumerable<ulong> addresses, List<Section> candidates)
                    => addresses.All(address => candidates.Any(x => address >= x.VirtualStart && address <= x.VirtualEnd));

                IEnumerable<ulong> FindReferences(ulong value)
                {
                    foreach (var section in data)
                    {
                        var end = Math.Min((long)section.ImageEnd + 1, Image.Length) - ptrSize;
                        for (var position = (long)section.ImageStart; position <= end; position += ptrSize)
                        {
                            Image.Position = position;
                            if (ReadPointer() == value)
                                yield return section.VirtualStart + (ulong)(position - section.ImageStart);
                        }
                    }
                }

                ulong FindCodeRegistration(List<Section> searchSections)
                {
                    if (metadata.Version < MetadataVersions.V242)
                    {
                        foreach (var section in data)
                        {
                            var end = Math.Min((long)section.ImageEnd + 1, Image.Length) - ptrSize;
                            for (var position = (long)section.ImageStart; position <= end; position += ptrSize)
                            {
                                Image.Position = position;
                                if (Image.ReadWord() != methodPointersCount)
                                    continue;

                                try
                                {
                                    var listPointer = Image.MapVATR(ReadPointer());
                                    if (!CheckFileOffsetInSections(listPointer, data))
                                        continue;

                                    var pointers = Image.ReadArray<ulong>((long)listPointer, methodPointersCount);
                                    if (CheckVirtualAddressInSections(pointers, exec))
                                        return Image.MapFileOffsetToVA((uint)position);
                                }
                                catch
                                {
                                    // ignored
                                }
                            }
                        }

                        return 0;
                    }

                    foreach (var section in searchSections)
                    {
                        Image.Position = section.ImageStart;
                        var sectionBytes = Image.ReadBytes((int)(section.ImageEnd - section.ImageStart + 1));

                        foreach (var index in FindAllStrings(sectionBytes.ToArray(), "mscorlib.dll\0"))
                        {
                            var dllVa = section.VirtualStart + index;

                            foreach (var refVa in FindReferences(dllVa))
                            {
                                foreach (var refVa2 in FindReferences(refVa))
                                {
                                    if (metadata.Version >= MetadataVersions.V270)
                                    {
                                        for (var i = metadata.Images.Length - 1; i >= 0; i--)
                                        {
                                            foreach (var refVa3 in FindReferences(refVa2 - (ulong)i * ptrSize))
                                            {
                                                var imageCountPointer = refVa3 - ptrSize;
                                                if (!Image.TryMapVATR(imageCountPointer, out _))
                                                    continue;

                                                if (Image.ReadMappedWord(imageCountPointer) != metadata.Images.Length)
                                                    continue;

                                                return refVa3;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        for (var i = 0; i < metadata.Images.Length; i++)
                                        {
                                            foreach (var refVa3 in FindReferences(refVa2 - (ulong)i * ptrSize))
                                                return refVa3;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return 0;
                }

                ulong FindMetadataRegistration()
                {
                    foreach (var section in data)
                    {
                        var end = Math.Min((long)section.ImageEnd + 1, Image.Length) - ptrSize;
                        for (var position = (long)section.ImageStart; position <= end; position += ptrSize)
                        {
                            Image.Position = position;
                            if (Image.ReadWord() != metadata.Types.Length)
                                continue;

                            if (Image.Version >= MetadataVersions.V270)
                            {
                                Image.Position += ptrSize;
                                if (Image.ReadWord() != metadata.Types.Length)
                                    continue;

                                try
                                {
                                    var listPointer = Image.MapVATR(ReadPointer());
                                    if (!CheckFileOffsetInSections(listPointer, data))
                                        continue;

                                    var pointers = Image.ReadArray<ulong>((long)listPointer, metadata.Types.Length);
                                    var valid = pointerInExec
                                        ? CheckVirtualAddressInSections(pointers, exec)
                                        : CheckVirtualAddressInSections(pointers, data);

                                    if (valid)
                                        return Image.MapFileOffsetToVA((uint)position) - ptrSize * 10;
                                }
                                catch
                                {
                                    // ignored
                                }
                            }
                            else
                            {
                                if (metadataUsageCount == 0)
                                    continue;

                                try
                                {
                                    Image.Position += ptrSize * 2;
                                    var listPointer = Image.MapVATR(ReadPointer());
                                    if (!CheckFileOffsetInSections(listPointer, data))
                                        continue;

                                    var pointers = Image.ReadArray<ulong>((long)listPointer, metadataUsageCount);
                                    if (CheckVirtualAddressInSections(pointers, bss))
                                        return Image.MapFileOffsetToVA((uint)position) - ptrSize * 12;
                                }
                                catch
                                {
                                    // ignored
                                }
                            }
                        }
                    }

                    return 0;
                }

                if (Image is IElfReader || Image is NsoReader)
                {
                    codeRegistration = FindCodeRegistration(exec);
                    if (codeRegistration == 0)
                    {
                        codeRegistration = FindCodeRegistration(data);
                    }
                    else
                    {
                        pointerInExec = true;
                    }
                }
                else
                {
                    codeRegistration = FindCodeRegistration(data);
                    if (codeRegistration == 0)
                    {
                        codeRegistration = FindCodeRegistration(exec);
                        pointerInExec = true;
                    }
                }

                if (codeRegistration == 0)
                    return (0, 0);

                sectionCodeRegistrationNeedsNormalization = metadata.Version >= MetadataVersions.V242;

                metadataRegistration = FindMetadataRegistration();
                if (metadataRegistration == 0)
                    return (0, 0);

                if (sectionCodeRegistrationNeedsNormalization)
                {
                    try
                    {
                        var codeGenEndPtr = codeRegistration + ptrSize;
                        codeRegistration = codeGenEndPtr - (ulong)Il2CppCodeRegistration.Size(Image.Version, Image.Bits == 32);

                        var cr = Image.ReadMappedVersionedObject<Il2CppCodeRegistration>(codeRegistration);

                        if (Image.Version == MetadataVersions.V242 && cr.InteropDataCount == 0)
                        {
                            Image.Version = MetadataVersions.V243;
                            codeRegistration = codeGenEndPtr - (ulong)Il2CppCodeRegistration.Size(Image.Version, Image.Bits == 32);
                        }

                        if (Image.Version == MetadataVersions.V270 && cr.ReversePInvokeWrapperCount > 0x30000)
                        {
                            Image.Version = MetadataVersions.V271;
                            codeRegistration = codeGenEndPtr - (ulong)Il2CppCodeRegistration.Size(Image.Version, Image.Bits == 32);
                            cr = Image.ReadMappedVersionedObject<Il2CppCodeRegistration>(codeRegistration);
                        }

                        if (Image.Version == MetadataVersions.V244 &&
                            (cr.InvokerPointersCount > 0x50000 || cr.ReversePInvokeWrapperCount > cr.ReversePInvokeWrappers))
                        {
                            Image.Version = MetadataVersions.V245;
                            codeRegistration = codeGenEndPtr - (ulong)Il2CppCodeRegistration.Size(Image.Version, Image.Bits == 32);
                            cr = Image.ReadMappedVersionedObject<Il2CppCodeRegistration>(codeRegistration);
                        }

                        if ((Image.Version == MetadataVersions.V290 || Image.Version == MetadataVersions.V310) &&
                            cr.GenericMethodPointersCount >= cr.GenericMethodPointers)
                        {
                            Image.Version = new StructVersion(Image.Version.Major, 0, MetadataVersions.Tag2022);
                            codeRegistration = codeGenEndPtr - (ulong)Il2CppCodeRegistration.Size(Image.Version, Image.Bits == 32);
                        }
                    }
                    catch
                    {
                        codeRegistration = 0;
                    }

                    if (codeRegistration == 0)
                        return (0, 0);
                }

                Console.WriteLine("Required structures acquired from section heuristics");
            }

            return (codeRegistration, metadataRegistration);
        }
    }
}
