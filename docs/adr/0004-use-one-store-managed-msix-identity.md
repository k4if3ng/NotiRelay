# Use one Store-managed MSIX identity

NotiRelay uses one packaged MSIX product identity, with Microsoft Store as the
only official binary-distribution, production-signing, and update channel. GitHub
hosts source code and project collaboration artifacts, not installable binaries.
Direct MSIX, setup, standalone EXE, GitHub Release assets, and independently signed
WinGet packages do not form additional release channels. WinGet may discover the
same Store product through the `msstore` source without carrying a second package
identity. Repository and self-signed identities remain development-only.
