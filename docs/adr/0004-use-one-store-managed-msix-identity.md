# Use one Store-managed MSIX identity

NotiRelay will use a single packaged MSIX product identity with Microsoft Store as the canonical signing and update channel. WinGet distribution will refer to the same Store product through the `msstore` source instead of publishing a second independently signed package identity.
