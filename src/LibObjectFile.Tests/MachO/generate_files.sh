#!/bin/sh
# Regenerates the Mach-O test fixtures. Needs an OSXCross toolchain on PATH
# (o64-clang for x86_64, oa64-clang for arm64) so the fixtures come out of the
# real cctools ld64 rather than an approximation, plus LLVM for yaml2obj and
# ld64.lld. The toolchain is only needed to regenerate, not to run the tests,
# since the fixtures are committed.
#
# OSXCross: https://github.com/tpoechtrager/osxcross
# It builds the cctools/ld64 port and needs a macOS SDK packaged per its README;
# Apple does not permit redistributing the SDK, which is why the fixtures are
# committed rather than built during the test run.
#
# LLVM and current cctools both dropped 32-bit Mach-O linking, so the two i386
# fixtures are synthesized with yaml2obj rather than linked. unixthread_i386
# models a pre-10.8 executable, which uses LC_UNIXTHREAD for the entry point;
# dyldinfo_i386 models a 10.7-era one, which pairs 32-bit segments with LC_MAIN
# and LC_DYLD_INFO_ONLY. No linker still emits either combination.
#
# The committed fixtures were produced with LLVM 22.1.8 and the OSXCross
# MacOSX14 SDK. Another version will not necessarily lay them out the same way,
# so regenerating with one is expected to move offsets and will show up as a
# diff in the byte-exact and snapshot tests. Check such a diff rather than
# accepting it: the fixtures exist to pin what a real toolchain emits.
set -e

# Locate the SDK rather than assuming a version, so a toolchain built against a
# different one still works.
SDK="$(dirname "$(command -v oa64-clang)")/../SDK"
SDK="$(ls -d "$SDK"/MacOSX*.sdk 2>/dev/null | sort -V | tail -1)"
if [ -z "$SDK" ]; then
    echo "No macOS SDK found next to oa64-clang; see the OSXCross README." >&2
    exit 1
fi

o64-clang helloworld.c -o helloworld_x86_64
oa64-clang helloworld.c -o helloworld_arm64

# arm64 gets an ad-hoc signature from the linker, x86_64 stays unsigned. The
# signing tests need both a "sign from scratch" and a "replace existing" input.
o64-clang -dynamiclib -install_name /usr/local/lib/libhelloworld.dylib \
    libhelloworld.c -o libhelloworld_x86_64.dylib

o64-clang -c helloworld.c -o helloworld_x86_64.o

lipo -create helloworld_x86_64 helloworld_arm64 -output helloworld_fat

# cctools ld64 predates chained fixups, so this one fixture comes from lld,
# which emits LC_DYLD_CHAINED_FIXUPS when the deployment target is macOS 12+.
# That is a separate read path from the LC_DYLD_INFO_ONLY opcode streams above.
oa64-clang -mmacosx-version-min=12.0 -c helloworld.c -o chained.o
ld64.lld -arch arm64 -platform_version macos 12.0 12.0 -syslibroot "$SDK" \
    -lSystem -e _main -fixup_chains -o chainedfixups_arm64 chained.o
rm -f chained.o

yaml2obj unixthread_i386.yaml -o unixthread_i386
yaml2obj dyldinfo_i386.yaml -o dyldinfo_i386

# A reference for the editing tests: what Apple's own tool produces for the same
# edit. Comparing against it catches an encoding that is merely self-consistent.
cp unixthread_i386 unixthread_i386_rpath
install_name_tool -add_rpath '@executable_path/../Frameworks' unixthread_i386_rpath
