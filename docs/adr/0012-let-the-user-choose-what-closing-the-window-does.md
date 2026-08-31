# Let the user choose what closing the window does

Closing the main window always hid NotiRelay in the notification area. That is right
for a forwarding utility that must keep running, and wrong for a user who is done with
it: the only way to actually quit was the notification-area menu, which is not where
anyone looks first. The close button now honours a Close Action the user picks in
Settings — hide to the notification area, exit the application, or ask each time. The
default remains hide, so behaviour is unchanged for anyone who never opens Settings.

Ask-each-time shows a dialog with the two real outcomes plus Cancel, and a "remember my
choice" checkbox that writes the chosen outcome back to the Close Action setting. This
is the only path that changes the setting from outside Settings, and it changes it only
on explicit consent. Silent Start becomes its own setting rather than something implied
by sign-in startup, because starting hidden and starting at sign-in are different
wishes: a user may want a visible window at sign-in, or a hidden one on a manual launch.

Two alternatives were rejected. Keeping the unconditional hide leaves quitting
undiscoverable and makes the application feel like it will not let go. Making close
always exit defeats the product — a forwarding utility that stops forwarding when its
window closes is not doing its job, and the user would have to remember to minimise
instead. Because the window can now be closed for good, the notification-area menu's
forwarding entry stops being a read-only label and becomes a real toggle, so a control
point survives in every configuration.

This supersedes the sentence in
[ADR 0009](0009-use-a-per-user-single-instance-tray-lifecycle.md) that closing the main
window always hides it. Everything else in ADR 0009 — one instance per user, icon click
shows and activates, explicit exit from the menu, no silent sign-in startup, no
first-run prompt — still holds.
