// RUN: %dafny /verifyAllModules /allocated:1 /compile:3 "%s" > "%t"
// RUN: %diff "%s.expect" "%t"
include "../../dafny0/RangeCompilation.dfy"
