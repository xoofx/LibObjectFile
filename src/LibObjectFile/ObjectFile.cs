// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile
{
    public abstract class ObjectFile
    {
        /// <summary>
        /// Verifies the integrity of this file.
        /// </summary>
        /// <returns>The result of the diagnostics</returns>
        public DiagnosticBag Verify()
        {
            var diagnostics = new DiagnosticBag();
            Verify(diagnostics);
            return diagnostics;
        }

        /// <summary>
        /// Verifies the integrity of this file.
        /// </summary>
        /// <param name="diagnostics">A DiagnosticBag instance to receive the diagnostics.</param>
        public abstract void Verify(DiagnosticBag diagnostics);

        /// <summary>
        /// Update and calculate the layout of this file.
        /// </summary>
        public void UpdateLayout()
        {
            var diagnostics = new DiagnosticBag();
            TryUpdateLayout(diagnostics);
            if (diagnostics.HasErrors)
            {
                throw new ObjectFileException("Unexpected error while updating the layout of this instance", diagnostics);
            }
        }

        /// <summary>
        /// Tries to update and calculate the layout of the sections, segments and <see cref="Layout"/>.
        /// </summary>
        /// <param name="diagnostics">A DiagnosticBag instance to receive the diagnostics.</param>
        /// <returns><c>true</c> if the calculation of the layout is successful. otherwise <c>false</c></returns>
        public abstract bool TryUpdateLayout(DiagnosticBag diagnostics);
    }
}