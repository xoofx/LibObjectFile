// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

public class PEVisitorContext : VisitorContextBase<PEFile>
{
    internal PEVisitorContext(PEFile peFile, DiagnosticBag diagnostics) : base(peFile, diagnostics)
    {
    }
}

public sealed class PELayoutContext : PEVisitorContext
{
    internal PELayoutContext(PEFile peFile, DiagnosticBag diagnostics, bool updateSizeOnly = false) : base(peFile, diagnostics)
    {
        UpdateSizeOnly = updateSizeOnly;
    }

    public bool UpdateSizeOnly { get; }
    
    internal PESection? CurrentSection { get; set; }
}


public sealed class PEVerifyContext : PEVisitorContext
{
    internal PEVerifyContext(PEFile peFile, DiagnosticBag diagnostics) : base(peFile, diagnostics)
    {
    }

    public void VerifyObject(PEObjectBase? peObject, PEObjectBase currentObject, string objectKindText, bool allowNull)
    {
        if (peObject is null)
        {
            if (allowNull) return;

            Diagnostics.Error(DiagnosticId.PE_ERR_VerifyContextInvalidObject, $"Error while processing {currentObject}. The parent object for {objectKindText} is null.");
            return;
        }

        var peFile = peObject.GetPEFile();
        if (peFile is null)
        {
            Diagnostics.Error(DiagnosticId.PE_ERR_VerifyContextInvalidObject, $"Error while processing {currentObject}. The parent of the object {peObject} from {objectKindText} is null. This object is not attached to the PE file.");
        }
        else if (peFile != File)
        {
            Diagnostics.Error(DiagnosticId.PE_ERR_VerifyContextInvalidObject, $"Error while processing {currentObject}. The parent object {peObject} for {objectKindText} is invalid. The object is attached to another PE File than the current PE file being processed.");
        }
    }
}