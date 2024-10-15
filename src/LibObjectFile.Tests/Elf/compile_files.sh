#!/bin/sh
gcc helloworld.cpp -o helloworld
gcc helloworld.cpp -c -o helloworld.o
ar rcs libhelloworld.a helloworld.o
gcc helloworld.cpp -gdwarf-4 -o helloworld_debug
gcc lib_a.cpp lib_b.cpp -gdwarf-4 -shared -o lib_debug.so
gcc small.cpp -gdwarf-4 -c -o small_debug.o
gcc multiple_functions.cpp -gdwarf-4 -c -o multiple_functions_debug.o
