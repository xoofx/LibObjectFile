// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("Count = {Operations.Count,nq}")]
    public class DwarfExpression : DwarfContainer
    {
        private readonly List<DwarfOperation> _operations;

        public DwarfExpression()
        {
            _operations = new List<DwarfOperation>();
        }

        public IReadOnlyList<DwarfOperation> Operations => _operations;

        internal List<DwarfOperation> InternalOperations => _operations;

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
        
        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            var endOffset = Offset;
            foreach (var op in _operations)
            {
                op.Offset = endOffset;
                op.UpdateLayoutInternal(layoutContext);
                endOffset += op.Size;
            }
            Size = endOffset - Offset;
        }

        protected override void Read(DwarfReader reader)
        {
            var size = reader.ReadULEB128();

            Offset = reader.Offset;
            var endPosition = Offset + size;

            while (reader.Offset < endPosition)
            {
                var kind = new DwarfOperationKindEx(reader.ReadU8());
                var op = new DwarfOperation
                {
                    Offset = Offset,
                    Kind = kind
                };
                AddOperation(op);

                switch (kind.Value)
                {
                    case DwarfOperationKind.Addr:
                        op.Operand1.U64 = reader.ReadUInt();
                        break;
                    case DwarfOperationKind.Const1u:
                        op.Operand1.U64 = reader.ReadU8();
                        break;
                    case DwarfOperationKind.Const1s:
                        op.Operand1.I64 = reader.ReadI8();
                        break;
                    case DwarfOperationKind.Const2u:
                        op.Operand1.U64 = reader.ReadU16();
                        break;
                    case DwarfOperationKind.Const2s:
                        op.Operand1.I64 = reader.ReadI16();
                        break;

                    case DwarfOperationKind.Const4u:
                        op.Operand1.U64 = reader.ReadU32();
                        break;
                    case DwarfOperationKind.Const4s:
                        op.Operand1.I64 = reader.ReadU32();
                        break;

                    case DwarfOperationKind.Const8u:
                        op.Operand1.U64 = reader.ReadU64();
                        break;

                    case DwarfOperationKind.Const8s:
                        op.Operand1.I64 = reader.ReadI64();
                        break;

                    case DwarfOperationKind.Constu:
                        op.Operand1.U64 = reader.ReadULEB128();
                        break;

                    case DwarfOperationKind.Consts:
                        op.Operand1.I64 = reader.ReadILEB128();
                        break;

                    case DwarfOperationKind.Deref:
                    case DwarfOperationKind.Dup:
                    case DwarfOperationKind.Drop:
                    case DwarfOperationKind.Over:
                    case DwarfOperationKind.Swap:
                    case DwarfOperationKind.Rot:
                    case DwarfOperationKind.Xderef:
                    case DwarfOperationKind.Abs:
                    case DwarfOperationKind.And:
                    case DwarfOperationKind.Div:
                    case DwarfOperationKind.Minus:
                    case DwarfOperationKind.Mod:
                    case DwarfOperationKind.Mul:
                    case DwarfOperationKind.Neg:
                    case DwarfOperationKind.Not:
                    case DwarfOperationKind.Or:
                    case DwarfOperationKind.Plus:
                    case DwarfOperationKind.Shl:
                    case DwarfOperationKind.Shr:
                    case DwarfOperationKind.Shra:
                    case DwarfOperationKind.Xor:
                    case DwarfOperationKind.Eq:
                    case DwarfOperationKind.Ge:
                    case DwarfOperationKind.Gt:
                    case DwarfOperationKind.Le:
                    case DwarfOperationKind.Lt:
                    case DwarfOperationKind.Ne:
                    case DwarfOperationKind.Nop:
                    case DwarfOperationKind.PushObjectAddress:
                    case DwarfOperationKind.FormTlsAddress:
                    case DwarfOperationKind.CallFrameCfa:
                        break;

                    case DwarfOperationKind.Pick:
                        op.Operand1.U64 = reader.ReadU8();
                        break;

                    case DwarfOperationKind.PlusUconst:
                        op.Operand1.U64 = reader.ReadULEB128();
                        break;

                    case DwarfOperationKind.Bra:
                    case DwarfOperationKind.Skip:
                        // TODO: resolve branches to DwarfOperation
                        op.Operand1.I64 = reader.ReadI16();
                        break;

                    case DwarfOperationKind.Lit0:
                    case DwarfOperationKind.Lit1:
                    case DwarfOperationKind.Lit2:
                    case DwarfOperationKind.Lit3:
                    case DwarfOperationKind.Lit4:
                    case DwarfOperationKind.Lit5:
                    case DwarfOperationKind.Lit6:
                    case DwarfOperationKind.Lit7:
                    case DwarfOperationKind.Lit8:
                    case DwarfOperationKind.Lit9:
                    case DwarfOperationKind.Lit10:
                    case DwarfOperationKind.Lit11:
                    case DwarfOperationKind.Lit12:
                    case DwarfOperationKind.Lit13:
                    case DwarfOperationKind.Lit14:
                    case DwarfOperationKind.Lit15:
                    case DwarfOperationKind.Lit16:
                    case DwarfOperationKind.Lit17:
                    case DwarfOperationKind.Lit18:
                    case DwarfOperationKind.Lit19:
                    case DwarfOperationKind.Lit20:
                    case DwarfOperationKind.Lit21:
                    case DwarfOperationKind.Lit22:
                    case DwarfOperationKind.Lit23:
                    case DwarfOperationKind.Lit24:
                    case DwarfOperationKind.Lit25:
                    case DwarfOperationKind.Lit26:
                    case DwarfOperationKind.Lit27:
                    case DwarfOperationKind.Lit28:
                    case DwarfOperationKind.Lit29:
                    case DwarfOperationKind.Lit30:
                    case DwarfOperationKind.Lit31:
                        op.Operand1.U64 = (ulong)((byte)kind.Value - (byte)DwarfOperationKind.Lit0);
                        break;

                    case DwarfOperationKind.Reg0:
                    case DwarfOperationKind.Reg1:
                    case DwarfOperationKind.Reg2:
                    case DwarfOperationKind.Reg3:
                    case DwarfOperationKind.Reg4:
                    case DwarfOperationKind.Reg5:
                    case DwarfOperationKind.Reg6:
                    case DwarfOperationKind.Reg7:
                    case DwarfOperationKind.Reg8:
                    case DwarfOperationKind.Reg9:
                    case DwarfOperationKind.Reg10:
                    case DwarfOperationKind.Reg11:
                    case DwarfOperationKind.Reg12:
                    case DwarfOperationKind.Reg13:
                    case DwarfOperationKind.Reg14:
                    case DwarfOperationKind.Reg15:
                    case DwarfOperationKind.Reg16:
                    case DwarfOperationKind.Reg17:
                    case DwarfOperationKind.Reg18:
                    case DwarfOperationKind.Reg19:
                    case DwarfOperationKind.Reg20:
                    case DwarfOperationKind.Reg21:
                    case DwarfOperationKind.Reg22:
                    case DwarfOperationKind.Reg23:
                    case DwarfOperationKind.Reg24:
                    case DwarfOperationKind.Reg25:
                    case DwarfOperationKind.Reg26:
                    case DwarfOperationKind.Reg27:
                    case DwarfOperationKind.Reg28:
                    case DwarfOperationKind.Reg29:
                    case DwarfOperationKind.Reg30:
                    case DwarfOperationKind.Reg31:
                        op.Operand1.U64 = (ulong)kind.Value - (ulong)DwarfOperationKind.Reg0;
                        break;

                    case DwarfOperationKind.Breg0:
                    case DwarfOperationKind.Breg1:
                    case DwarfOperationKind.Breg2:
                    case DwarfOperationKind.Breg3:
                    case DwarfOperationKind.Breg4:
                    case DwarfOperationKind.Breg5:
                    case DwarfOperationKind.Breg6:
                    case DwarfOperationKind.Breg7:
                    case DwarfOperationKind.Breg8:
                    case DwarfOperationKind.Breg9:
                    case DwarfOperationKind.Breg10:
                    case DwarfOperationKind.Breg11:
                    case DwarfOperationKind.Breg12:
                    case DwarfOperationKind.Breg13:
                    case DwarfOperationKind.Breg14:
                    case DwarfOperationKind.Breg15:
                    case DwarfOperationKind.Breg16:
                    case DwarfOperationKind.Breg17:
                    case DwarfOperationKind.Breg18:
                    case DwarfOperationKind.Breg19:
                    case DwarfOperationKind.Breg20:
                    case DwarfOperationKind.Breg21:
                    case DwarfOperationKind.Breg22:
                    case DwarfOperationKind.Breg23:
                    case DwarfOperationKind.Breg24:
                    case DwarfOperationKind.Breg25:
                    case DwarfOperationKind.Breg26:
                    case DwarfOperationKind.Breg27:
                    case DwarfOperationKind.Breg28:
                    case DwarfOperationKind.Breg29:
                    case DwarfOperationKind.Breg30:
                    case DwarfOperationKind.Breg31:
                        op.Operand1.U64 = (ulong)kind.Value - (ulong)DwarfOperationKind.Breg0;
                        op.Operand2.I64 = reader.ReadILEB128();
                        break;

                    case DwarfOperationKind.Regx:
                        op.Operand1.U64 = reader.ReadULEB128();
                        break;

                    case DwarfOperationKind.Fbreg:
                        op.Operand1.I64 = reader.ReadILEB128();
                        break;

                    case DwarfOperationKind.Bregx:
                        op.Operand1.U64 = reader.ReadULEB128();
                        op.Operand2.I64 = reader.ReadILEB128();
                        break;

                    case DwarfOperationKind.Piece:
                        op.Operand1.U64 = reader.ReadULEB128();
                        break;

                    case DwarfOperationKind.DerefSize:
                        op.Operand1.U64 = reader.ReadU8();
                        break;

                    case DwarfOperationKind.XderefSize:
                        op.Operand1.U64 = reader.ReadU8();
                        break;

                    case DwarfOperationKind.Call2:
                        {
                            var offset = reader.ReadU16();
                            var dieRef = new DwarfReader.DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                            reader.ResolveAttributeReferenceWithinSection(dieRef, false);
                            break;
                        }

                    case DwarfOperationKind.Call4:
                        {
                            var offset = reader.ReadU32();
                            var dieRef = new DwarfReader.DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                            reader.ResolveAttributeReferenceWithinSection(dieRef, false);
                            break;
                        }

                    case DwarfOperationKind.CallRef:
                        {
                            var offset = reader.ReadUIntFromEncoding();
                            var dieRef = new DwarfReader.DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                            reader.ResolveAttributeReferenceWithinSection(dieRef, false);
                            break;
                        }

                    case DwarfOperationKind.BitPiece:
                        op.Operand1.U64 = reader.ReadULEB128();
                        op.Operand2.U64 = reader.ReadULEB128();
                        break;

                    case DwarfOperationKind.ImplicitValue:
                        {
                            var length = reader.ReadULEB128();
                            op.Operand0 = reader.ReadAsStream(length);
                            break;
                        }

                    case DwarfOperationKind.StackValue:
                        break;

                    case DwarfOperationKind.ImplicitPointer:
                    case DwarfOperationKind.GNUImplicitPointer:
                        {
                            ulong offset;
                            //  a reference to a debugging information entry that describes the dereferenced object’s value
                            if (reader.CurrentUnit.Version == 2)
                            {
                                offset = reader.ReadUInt();
                            }
                            else
                            {
                                offset = reader.ReadUIntFromEncoding();
                            }
                            //  a signed number that is treated as a byte offset from the start of that value
                            op.Operand1.I64 = reader.ReadILEB128();

                            if (offset != 0)
                            {
                                var dieRef = new DwarfReader.DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                                reader.ResolveAttributeReferenceWithinSection(dieRef, false);
                            }
                            break;
                        }

                    case DwarfOperationKind.Addrx:
                    case DwarfOperationKind.GNUAddrIndex:
                    case DwarfOperationKind.Constx:
                    case DwarfOperationKind.GNUConstIndex:
                        op.Operand1.U64 = reader.ReadULEB128();
                        break;

                    case DwarfOperationKind.EntryValue:
                    case DwarfOperationKind.GNUEntryValue:
                    {
                        var subExpression = new DwarfExpression();
                        subExpression.Read(reader);
                        op.Operand0 = subExpression;
                        break;
                    }

                    case DwarfOperationKind.ConstType:
                    case DwarfOperationKind.GNUConstType:
                        {
                            // The DW_OP_const_type operation takes three operands

                            // The first operand is an unsigned LEB128 integer that represents the offset
                            // of a debugging information entry in the current compilation unit, which
                            // must be a DW_TAG_base_type entry that provides the type of the constant provided
                            var offset = reader.ReadULEB128();
                            if (offset != 0)
                            {
                                var dieRef = new DwarfReader.DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                                reader.ResolveAttributeReferenceWithinCompilationUnit(dieRef, false);
                            }
                            op.Operand1.U64 = ReadEncodedValue(reader, kind, out var sizeOfEncodedValue);
                            // Encode size of encoded value in Operand1
                            op.Operand2.U64 = sizeOfEncodedValue;
                            break;
                        }

                    case DwarfOperationKind.RegvalType:
                    case DwarfOperationKind.GNURegvalType:
                        {
                            // The DW_OP_regval_type operation provides the contents of a given register
                            // interpreted as a value of a given type

                            // The first operand is an unsigned LEB128 number, which identifies a register
                            // whose contents is to be pushed onto the stack
                            op.Operand1.U64 = reader.ReadULEB128();

                            // The second operand is an unsigned LEB128 number that represents the offset
                            // of a debugging information entry in the current compilation unit
                            var offset = reader.ReadULEB128();
                            if (offset != 0)
                            {
                                var dieRef = new DwarfReader.DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                                reader.ResolveAttributeReferenceWithinCompilationUnit(dieRef, false);
                            }
                            break;
                        }

                    case DwarfOperationKind.DerefType:
                    case DwarfOperationKind.GNUDerefType:
                    case DwarfOperationKind.XderefType:
                        {
                            // The DW_OP_deref_type operation behaves like the DW_OP_deref_size operation:
                            // it pops the top stack entry and treats it as an address.

                            // This operand is a 1-byte unsigned integral constant whose value which is the
                            // same as the size of the base type referenced by the second operand
                            op.Operand1.U64 = reader.ReadU8();

                            // The second operand is an unsigned LEB128 number that represents the offset
                            // of a debugging information entry in the current compilation unit
                            var offset = reader.ReadULEB128();
                            if (offset != 0)
                            {
                                var dieRef = new DwarfReader.DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                                reader.ResolveAttributeReferenceWithinCompilationUnit(dieRef, false);
                            }
                            break;
                        }

                    case DwarfOperationKind.Convert:
                    case DwarfOperationKind.GNUConvert:
                    case DwarfOperationKind.Reinterpret:
                    case DwarfOperationKind.GNUReinterpret:
                        {
                            ulong offset = reader.ReadULEB128();
                            if (offset != 0)
                            {
                                var dieRef = new DwarfReader.DwarfDIEReference(offset, op, DwarfExpressionLocationDIEReferenceResolverInstance);
                                reader.ResolveAttributeReferenceWithinCompilationUnit(dieRef, false);
                            }
                            break;
                        }

                    case DwarfOperationKind.GNUPushTlsAddress:
                    case DwarfOperationKind.GNUUninit:
                        break;

                    case DwarfOperationKind.GNUEncodedAddr:
                        {
                            op.Operand1.U64 = ReadEncodedValue(reader, kind, out var sizeOfEncodedValue);
                            op.Operand2.U64 = sizeOfEncodedValue;
                            break;
                        }

                    case DwarfOperationKind.GNUParameterRef:
                        op.Operand1.U64 = reader.ReadU32();
                        break;

                    default:
                        throw new NotSupportedException($"The {nameof(DwarfOperationKind)} {kind} is not supported");
                }

                // Store the size of the current op
                op.Size = reader.Offset - op.Offset;
            }
        }

        private ulong ReadEncodedValue(DwarfReader reader, DwarfOperationKind kind, out byte size)
        {
            size = reader.ReadU8();
            switch (size)
            {
                case 0:
                    return reader.ReadUInt();
                case 1:
                    return reader.ReadU8();
                case 2:
                    return reader.ReadU16();
                case 4:
                    return reader.ReadU32();
                case 8:
                    return reader.ReadU64();
                default:
                    throw new InvalidOperationException($"Invalid Encoded address size {size} for {kind}");
            }
        }

        protected override void Write(DwarfWriter writer)
        {
            writer.WriteULEB128(Size);

            var startExpressionOffset = writer.Offset;
            Debug.Assert(startExpressionOffset == Offset);

            foreach (var op in Operations)
            {
                op.WriteInternal(writer);
            }

            Debug.Assert(writer.Offset - startExpressionOffset == Size);
        }

        private static readonly DwarfReader.DwarfDIEReferenceResolver DwarfExpressionLocationDIEReferenceResolverInstance = DwarfExpressionLocationDIEReferenceResolver;

        private static void DwarfExpressionLocationDIEReferenceResolver(ref DwarfReader.DwarfDIEReference dieRef)
        {
            var op = (DwarfOperation)dieRef.DwarfObject;
            op.Operand0 = dieRef.Resolved;
        }
    }
}