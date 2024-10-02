// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

partial class PEFile
{
    /// <summary>
    /// Relocates the PE file to a new image base.
    /// </summary>
    /// <param name="newImageBase">The new image base.</param>
    public void Relocate(ulong newImageBase)
    {
        var diagnostics = new DiagnosticBag();
        Relocate(newImageBase, diagnostics);
        if (diagnostics.HasErrors)
        {
            throw new ObjectFileException("Unable to relocate this PE File", diagnostics);
        }
    }

    /// <summary>
    /// Relocates the PE file to a new image base.
    /// </summary>
    /// <param name="newImageBase">The new image base.</param>
    /// <param name="diagnostics">The diagnostic bag to collect errors and warnings.</param>
    public void Relocate(ulong newImageBase, DiagnosticBag diagnostics)
    {
        if (IsPE32 && newImageBase > uint.MaxValue)
        {
            diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationInvalid, $"The new image base {newImageBase} is out of range for a PE32 file");
            return;
        }

        long delta = (long)(newImageBase - OptionalHeader.ImageBase);
        if (delta == 0)
        {
            // Nothing to do
            return;
        }

        // If we don't have a base relocation directory, we can just update the ImageBase
        var baseRelocationDirectory = Directories.BaseRelocation;
        if (baseRelocationDirectory is null)
        {
            OptionalHeader.ImageBase = newImageBase;
            return;
        }

        for (var i = 0; i < baseRelocationDirectory.Content.Count; i++)
        {
            var content = baseRelocationDirectory.Content[i];
            var baseRelocationBlock = content as PEBaseRelocationBlock;

            if (baseRelocationBlock is null)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_VerifyContextInvalidObject, $"Invalid content found {content} at index #{i} in the {nameof(PEBaseRelocationDirectory)}");
                return;
            }

            var section = baseRelocationBlock.PageLink.Container;
            if (section is null)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_VerifyContextInvalidObject,
                    $"The {nameof(PEBaseRelocationBlock.PageLink)} in the base relocation block {baseRelocationBlock} at index #{i} in the {nameof(PEBaseRelocationDirectory)} is null and missing a link to an actual section");
                return;
            }

            var pageBaseRva = baseRelocationBlock.PageLink.RVA();

            var relocations = CollectionsMarshal.AsSpan(baseRelocationBlock.Relocations);
            try
            {
                for (int j = 0; j < relocations.Length; j++)
                {
                    var relocation = relocations[j];

                    var rva = pageBaseRva + relocation.OffsetInPage;
                    if (!section.TryFindByRVA(rva, out var sectionData) || sectionData is not PESectionData)
                    {
                        if (sectionData is null)
                        {
                            diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationInvalid,
                                $"Unable to find the section data for the rva {rva} in the base relocation block {baseRelocationBlock} at index #{i} in the {nameof(PEBaseRelocationDirectory)}");
                            return;
                        }

                        diagnostics.Warning(DiagnosticId.PE_WRN_BaseRelocationInVirtualMemory,
                            $"Invalid RVA {rva} found in virtual memory from base relocation block {baseRelocationBlock} at index #{i} in the {nameof(PEBaseRelocationDirectory)}");
                        continue;
                    }

                    var offsetInSectionData = rva - sectionData.RVA;

                    if (relocation.IsZero) continue;

                    switch (relocation.Type)
                    {
                        case PEBaseRelocationType.Absolute:
                            break;
                        case PEBaseRelocationType.High:
                            if (sectionData.CanReadWriteAt(offsetInSectionData, sizeof(ushort)))
                            {
                                sectionData.WriteAt(offsetInSectionData, (short)(sectionData.ReadAt<short>(offsetInSectionData) + (short)(delta >> 16)));
                            }
                            else
                            {
                                goto WarningOutOfBound;
                            }

                            break;
                        case PEBaseRelocationType.Low:
                            if (sectionData.CanReadWriteAt(offsetInSectionData, sizeof(ushort)))
                            {
                                sectionData.WriteAt(offsetInSectionData, (short)(sectionData.ReadAt<short>(offsetInSectionData) + (short)(delta)));
                            }
                            else
                            {
                                goto WarningOutOfBound;
                            }

                            break;
                        case PEBaseRelocationType.HighLow:
                            if (sectionData.CanReadWriteAt(offsetInSectionData, sizeof(uint)))
                            {
                                sectionData.WriteAt(offsetInSectionData, sectionData.ReadAt<int>(offsetInSectionData) + (int)(delta));
                            }
                            else
                            {
                                goto WarningOutOfBound;
                            }

                            break;
                        case PEBaseRelocationType.HighAdj:

                            if (sectionData.CanReadWriteAt(offsetInSectionData, sizeof(ushort)))
                            {
                                sectionData.WriteAt(offsetInSectionData, (short)(sectionData.ReadAt<short>(offsetInSectionData) + (short)(delta >> 16)));
                                j++;
                                if (j >= relocations.Length)
                                {
                                    diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationInvalid, $"Invalid HighAdj relocation index #{j - 1} in {nameof(PEBaseRelocationBlock)} {baseRelocationBlock}. Expecting a relocation after.");
                                    return;
                                }

                                var nextOffset = offsetInSectionData + 2;
                                sectionData.WriteAt(nextOffset, (short)(sectionData.ReadAt<short>(nextOffset) + (short)(delta)));
                            }
                            else
                            {
                                goto WarningOutOfBound;
                            }

                            break;
                        case PEBaseRelocationType.Dir64:
                            if (sectionData.CanReadWriteAt(offsetInSectionData, sizeof(ulong)))
                            {
                                sectionData.WriteAt(offsetInSectionData, sectionData.ReadAt<ulong>(offsetInSectionData) + (ulong)delta);
                            }
                            else
                            {
                                goto WarningOutOfBound;
                            }

                            break;
                        default:
                            diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationInvalid, $"Unsupported relocation type {relocation.Type} #{j} in {nameof(PEBaseRelocationBlock)} {baseRelocationBlock}.");
                            return;
                    }

                    continue;
                    WarningOutOfBound:
                    diagnostics.Warning(DiagnosticId.PE_WRN_BaseRelocationInVirtualMemory,
                        $"Cannot process base relocation block {baseRelocationBlock} at index #{i} in the {nameof(PEBaseRelocationDirectory)}. The linked address is out of bound.");
                    continue;

                }
            }
            catch (BaseRelocationNotSupportedException)
            {
                diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationInvalid, $"Failed to relocate base relocation block {baseRelocationBlock} at index #{i}. The target container {section} does not support relocation");
                return;
            }
        }

        OptionalHeader.ImageBase = newImageBase;
    }
}
