// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using LibObjectFile.Collections;

namespace LibObjectFile.Dwarf;

[DebuggerDisplay("Count = {Operations.Count,nq}")]
public class DwarfExpression : DwarfObject<DwarfObject>
{
    private readonly List<DwarfOperation> _operations;

    public DwarfExpression()
    {
        _operations = new List<DwarfOperation>();
    }

    public ReadOnlyList<DwarfOperation> Operations => _operations;

    internal List<DwarfOperation> InternalOperations => _operations;

    public ulong OperationLengthInBytes { get; internal set; }

    public void AddOperation(DwarfOperation operation)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        _operations.Add(this, operation);
    }

    public void RemoveOperation(DwarfOperation operation)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        _operations.Remove(this, operation);
    }

    public DwarfOperation RemoveOperationAt(int index)
    {
        return _operations.RemoveAt(this, index);
    }

    public override void Verify(DwarfVerifyContext context)
    {
        foreach (var op in _operations)
        {
            op.Verify(context);
        }
    }

    internal void ReadInternal(DwarfReader reader, bool inLocationSection = false)
    {
        Position = reader.Position;
        var size = inLocationSection ? reader.ReadU16() : reader.ReadULEB128();
        OperationLengthInBytes = size;
        var endPosition = reader.Position + size;

        while (reader.Position < endPosition)
        {
            var op = new DwarfOperation() {Position = reader.Position};
            op.Read(reader);
            AddOperation(op);
        }

        Size = reader.Position - Position;
    }

    internal void WriteInternal(DwarfWriter writer, bool inLocationSection = false)
    {
        Debug.Assert(Position == writer.Position);
        Debug.Assert(!inLocationSection || OperationLengthInBytes <= ushort.MaxValue);

        var startExpressionOffset = writer.Position;
        if (inLocationSection)
        {
            writer.WriteU16((ushort)OperationLengthInBytes);
        }
        else
        {
            writer.WriteULEB128(OperationLengthInBytes);
        }

        foreach (var op in Operations)
        {
            op.Write(writer);
        }

        Debug.Assert(writer.Position - startExpressionOffset == Size);
    }

    internal void UpdateLayout(DwarfLayoutContext layoutContext, bool inLocationSection)
    {
        var endOffset = Position;
        foreach (var op in _operations)
        {
            op.Position = endOffset;
            op.UpdateLayout(layoutContext);
            endOffset += op.Size;
        }

        OperationLengthInBytes = endOffset - Position;

        // We need to shift the expression which is prefixed by its size encoded in LEB128,
        // or fixed-size U2 in .debug_loc section
        var deltaLength = inLocationSection ? sizeof(ushort) : DwarfHelper.SizeOfULEB128(Size);
        foreach (var op in InternalOperations)
        {
            op.Position += deltaLength;
        }

        Size = OperationLengthInBytes + deltaLength;
    }

    public override void UpdateLayout(DwarfLayoutContext layoutContext)
    {
        UpdateLayout(layoutContext, inLocationSection: false);
    }

    public override void Read(DwarfReader reader)
    {
        ReadInternal(reader, inLocationSection: false);
    }

    public override void Write(DwarfWriter writer)
    {
        WriteInternal(writer, inLocationSection: false);
    }
}