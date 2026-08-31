# Own the content design system and keep the shell native

NotiRelay's main window is entirely configuration, so its interface quality is
decided by content-area grammar rather than by shell chrome. The window frame,
caption buttons, backdrop, and NavigationView stay native Windows 11 Fluent, while
card grouping, row composition, description placement, spacing, density, and control
choice follow an application-owned design system recorded in
`docs/design/ui-system.md`. That system is expressed as named XAML resources and
native control composition; no `ControlTemplate` is rewritten.

Two alternatives were rejected. Reproducing Windows Settings exactly yields correct
metrics but not the grouping and directness the project wants. Building a full custom
design system — custom caption buttons, custom control templates, a bespoke palette —
would force a single-maintainer utility to hand-maintain dark theme, high-contrast
theme, focus visuals, and accessibility. Custom caption buttons are rejected outright
because they break Windows 11 snap-layout hover, the system menu, and assistive
technology.

The consequences are a dependency on the Windows Community Toolkit controls
(`SettingsCard`, `SettingsExpander`, `Segmented`) that retires the hand-written
`SettingsRow`, plus a real-page verification matrix covering Home, Apps,
Destinations, Rules, Activity, and Settings. External design tooling produces no
artifact that ships: an HTML sketch may be used to agree on layout, layering, and
density before markup is written, but theme resources, adaptive reflow, text scaling,
focus visuals, and content density are verified on the pages that actually ship.

The first attempt at this system was reviewed and rejected, which sharpened the
decision in two places. Adopting the toolkit is not enough on its own: the toolkit's
own metric resources must be overridden in `Styles/AppStyles.xaml` so that toolkit
cards and hand-written list rows resolve their padding, row height, and corner radius
from one set of tokens. Leaving toolkit defaults in place reproduces the competing
card grammars this decision exists to remove, and building first-party replacements
instead was rejected a second time for the same maintenance reason as above. Second,
"the shell stays native" is literal — the NavigationView's own selection treatment is
kept, and code that assigns `Foreground` to a `NavigationViewItem` is removed rather
than replaced, because the control's visual states overwrite it and the attempt never
rendered.
