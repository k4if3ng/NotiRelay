# Use a tray-resident host for the first release

NotiRelay will keep one foreground process resident behind a notification-area icon for the first release so notification capture, delivery, retry, and status remain observable and straightforward to debug. Core capture and delivery services must not depend on WinUI types, preserving the option to add a Windows background-task host later without redesigning the pipeline.
