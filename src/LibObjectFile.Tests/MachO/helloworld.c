// Fixture source for the Mach-O tests. It calls into libc on purpose: the
// resulting lazy-binding stubs, indirect symbol table and bind opcodes are
// structures the reader has to handle, and a freestanding binary has none.
#include <stdio.h>

int helloworld_twice(int x) { return x * 2; }

int helloworld_data = 21;

int main(void)
{
    printf("hello world %d\n", helloworld_twice(helloworld_data));
    return 0;
}
