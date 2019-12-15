// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public class DwarfReader : DwarfReaderWriter
    {
        private readonly Dictionary<ulong, DwarfDIE> _registeredDIEPerCompilationUnit;
        private readonly Dictionary<ulong, DwarfDIE> _registeredDIEPerSection;
        private readonly List<DwarfDIEReference> _unresolvedDIECompilationUnitReference;
        private readonly List<DwarfDIEReference> _attributesWithUnresolvedDIESectionReference;
        
        private DiagnosticBag _diagnostics;
        private bool _is64Address;
        private ushort _version;
        private ulong _offsetOfCompilationUnitInSection;
        private DwarfFile _parent;

        internal DwarfReader(DwarfReaderContext context, DwarfFile file, DiagnosticBag diagnostics) : base(file, diagnostics)
        {
            IsReadOnly = context.IsInputReadOnly;
            Is64BitAddress = context.Is64BitAddress;
            _registeredDIEPerCompilationUnit = new Dictionary<ulong, DwarfDIE>();
            _registeredDIEPerSection = new Dictionary<ulong, DwarfDIE>();
            _unresolvedDIECompilationUnitReference = new List<DwarfDIEReference>();
            _attributesWithUnresolvedDIESectionReference = new List<DwarfDIEReference>();
            _offsetToDebugLine = new Dictionary<ulong, DwarfLine>();
        }

        public override bool IsReadOnly { get; }

        public DwarfUnitKind DefaultUnitKind { get; internal set; }

        internal int DIELevel { get; set; }

        internal DwarfAttributeDescriptor CurrentAttributeDescriptor { get; set; }

        internal readonly Dictionary<ulong, DwarfLine> _offsetToDebugLine;
        
        internal void RegisterDIE(DwarfDIE die)
        {
            _registeredDIEPerCompilationUnit.Add(die.Offset - CurrentUnit.Offset, die);
            _registeredDIEPerSection.Add(die.Offset, die);
        }

        internal void ClearResolveAttributeReferenceWithinCompilationUnit()
        {
            _registeredDIEPerCompilationUnit.Clear();
            _unresolvedDIECompilationUnitReference.Clear();
        }

        internal void ResolveAttributeReferenceWithinCompilationUnit()
        {
            // Resolve attribute reference within the CU
            foreach (var unresolvedAttrRef in _unresolvedDIECompilationUnitReference)
            {
                ResolveAttributeReferenceWithinCompilationUnit(unresolvedAttrRef, true);
            }
        }

        internal void ResolveAttributeReferenceWithinSection()
        {
            // Resolve attribute reference within the section
            foreach (var unresolvedAttrRef in _attributesWithUnresolvedDIESectionReference)
            {
                ResolveAttributeReferenceWithinSection(unresolvedAttrRef, true);
            }
        }
        
        internal void ResolveAttributeReferenceWithinCompilationUnit(DwarfDIEReference dieRef, bool errorIfNotFound)
        {
            if (_registeredDIEPerCompilationUnit.TryGetValue(dieRef.Offset, out var die))
            {
                dieRef.Resolved = die;
                dieRef.Resolver(ref dieRef);
            }
            else
            {
                if (errorIfNotFound)
                {
                    if (dieRef.Offset != 0)
                    {
                        _diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidReference, $"Unable to resolve DIE reference (0x{dieRef.Offset:x}, section 0x{(dieRef.Offset + _offsetOfCompilationUnitInSection):x}) for {dieRef.DwarfObject} at offset 0x{dieRef.Offset:x}");
                    }
                }
                else
                {
                    _unresolvedDIECompilationUnitReference.Add(dieRef);
                }
            }
        }

        internal  void ResolveAttributeReferenceWithinSection(DwarfDIEReference dieRef, bool errorIfNotFound)
        {
            if (_registeredDIEPerSection.TryGetValue(dieRef.Offset, out var die))
            {
                dieRef.Resolved = die;
                dieRef.Resolver(ref dieRef);
            }
            else
            {
                if (errorIfNotFound)
                {
                    if (dieRef.Offset != 0)
                    {
                        _diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidReference, $"Unable to resolve DIE reference (0x{dieRef.Offset:x}) for {dieRef.DwarfObject} at offset 0x{dieRef.Offset:x}");
                    }
                }
                else
                {
                    _attributesWithUnresolvedDIESectionReference.Add(dieRef);
                }
            }
        }

        internal struct DwarfDIEReference
        {
            public DwarfDIEReference(ulong offset, object dwarfObject, DwarfDIEReferenceResolver resolver) : this()
            {
                Offset = offset;
                DwarfObject = dwarfObject;
                Resolver = resolver;
            }

            public readonly ulong Offset;

            public readonly object DwarfObject;

            public readonly DwarfDIEReferenceResolver Resolver;

            public DwarfDIE Resolved;
        }

        internal delegate void DwarfDIEReferenceResolver(ref DwarfDIEReference reference);
    }
}