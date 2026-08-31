# Validate Windows 11 x64 first and build all desktop architectures

The first public release requires Windows 11 build 22621 or later. The project
produces x86, x64, and ARM64 builds. x64 is the primary and fully runtime-validated
target. x86 receives a basic compatibility smoke test on x64 Windows. ARM64 remains
compile-only until it passes physical-device or Store-flight validation, and the
project must not claim physical ARM64 validation before that evidence exists.
