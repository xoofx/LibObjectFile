// Fixture source for the Mach-O dylib tests. See helloworld.c for why this
// calls into libc rather than standing alone.
#include <stdio.h>

int helloworld_shared(int x)
{
    printf("shared %d\n", x);
    return x + 1;
}
