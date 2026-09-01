# NotiRelay UI System

The authoritative specification for NotiRelay's interface. Numeric values used by the
shipping XAML exist as named resources; pages reference the resource, never the literal.
Section 14 records the audited third-party control metrics and their relationship to the
owned token dictionary.

Governed by [ADR 0011](../adr/0011-own-the-content-design-system-and-keep-the-shell-native.md):
the shell is native Windows 11 Fluent, the content area follows this system, and no
`ControlTemplate` is rewritten.

## 0. Provenance

The system was derived by comparing three references against NotiRelay's own content:
Windows 11 Settings, WeFlow, and ElegantClipboard. Windows Settings supplied the row
grammar and the type-ramp discipline. WeFlow supplied the pane toggle's place in the
title bar, the page-level action row, and the preference for progressive disclosure
over showing every field at once. ElegantClipboard supplied the group card — one topic
per card, with the card owning its title and group-level control — and higher
information density inside a card.

Two things the references were **not** allowed to supply. WeFlow's navigation
selection treatment was tried and rejected: NotiRelay keeps the native NavigationView
selection. And neither reference has list pages, so the second density tier is
NotiRelay's own — Apps can hold hundreds of entries while Destinations holds three
groups, and a single density damages one of them.

## 1. Type ramp

Five levels. All are WinUI ramp values; 18 px, 32 px, and other off-ramp sizes are not
used.

| Role | Size / weight | Brush | Resource |
| --- | --- | --- | --- |
| Page title | 28 SemiBold | Primary | `PageTitleTextBlockStyle` |
| Card title | 16 SemiBold | Primary | `CardTitleTextBlockStyle` |
| Row title | 14 SemiBold | Primary | `RowTitleTextBlockStyle` |
| Body / value | 14 Regular | Primary | `BodyTextBlockStyle` |
| Description / metadata | 12 Regular | `TextFillColorSecondaryBrush` | `DescriptionTextBlockStyle` |

**Pages have no subtitle.** A 28 px title followed by a 14 px line restating it is
filler; the page title alone opens the page. Anything that genuinely needs saying
belongs to a card, not to the page.

The card title is 16, not 18. An earlier revision used 18 and set only `FontSize` on a
style derived from `BodyStrongTextBlockStyle`, leaving 18 px glyphs inside a 20 px line
box. 16 is on the ramp, needs no line-height override, and still outranks the 14 px row
title without competing with the 28 px page title.

Descriptions are 12 px secondary in both English and Simplified Chinese. Do not raise
the size to compensate for CJK; the secondary brush already carries the contrast the
ramp expects.

## 2. Spacing

A strict 4 px grid governs component spacing. Responsive page gutters are dedicated
layout tokens (`24`, `40`, and `56`) on the same grid rather than component-spacing
steps.

| Token | Value | Typical use |
| --- | --- | --- |
| `SpacingXS` | 4 | Title to description |
| `SpacingS` | 8 | Icon to text, chip gaps |
| `SpacingM` | 12 | Adjacent controls in a row |
| `SpacingL` | 16 | Card-to-card gap, icon-to-text gap |
| `SpacingXL` | 24 | Card content inset, page title row to first card |
| `SpacingXXL` | 32 | Stable page bottom padding |

## 3. Corner radius

Application-owned containers use 8 while native controls retain their theme-provided
control radius. Keyword chips are compact 28 px pills so a removable value reads as one
object rather than a miniature card.

| Element | Radius | Resource |
| --- | --- | --- |
| Application-owned group card | 8 | `CardCornerRadius` |
| Toolkit `SettingsCard` / `SettingsExpander` | Native control radius | `ControlCornerRadius` |
| Content area top-left | 8 | `CardCornerRadius` |
| Control inside a card | 4 | `ControlCornerRadius` |
| Keyword chip | 14 | `ChipCornerRadius` |

## 4. Density — two tiers

| Tier | Row height | Applies to |
| --- | --- | --- |
| `RowHeightSetting` | 68 | Home, Destinations, Rules, Settings |
| `RowHeightList` | 60 | Apps list, Activity list |

**The two tiers differ in row height and in nothing else.** Left padding, icon column
width, and divider inset are identical across both. The earlier revision changed the
implementation, the padding, and the height together, which is why Apps and
Destinations read as two unrelated designs.

**A configuration row is 68 whether or not it has a description.** A single-line title
sitting in a 68 px row is normal Windows Settings observance; alternating 52 and 68
inside one card is not.

An icon column is optional per list. When present its layout width is fixed at 32 while
the glyph remains 20–24; when a list has no icons the column is absent, not empty.

## 5. Card and row grammar

One topic is one card. This is the only grouping mechanism — never express grouping
with spacing alone.

### Which control is a card

| Structure | Control |
| --- | --- |
| A collapsible group whose details are secondary | `SettingsExpander` — its header is the group title |
| An always-visible group of related rows | A plain `Border` on `GroupCardBorderStyle`, with its title and controls inside |
| One standalone setting | `SettingsCard` |
| A list of homogeneous items | A plain `Border` on `GroupCardBorderStyle`, rows inside |

Cards come from the toolkit; **rows in a list do not.** A list row is a lightweight
`Border`, because `SettingsCard` is built for one-setting-per-row with Header,
Description, HeaderIcon, and Content slots, and Activity's failed rows need a third
line the slots cannot hold. What is unified is the *metrics*, not the control — see
§14.

### Page header vs card header

**Single-card pages avoid duplicate titles.** Activity puts its summary and action on
the page title row. Apps keeps a plain page title and uses one compact command row at
the top of its card, because discovery state, filtering, and the list belong to the
same working surface:

```
Apps                                                            ← page title row
┌─ card ────────────────────────────────────────────────────────┐
│  共 6 个 · 已启用 3 个                         [↻ 发现应用]  │  ← compact command row
├───────────────────────────────────────────────────────────────┤
│  [search]                                  [ All | Enabled ]  │
├───────────────────────────────────────────────────────────────┤
│  [icon]  Row title                                   [toggle] │  ← 60
│          Row description                                      │
└───────────────────────────────────────────────────────────────┘
```

The Apps list preserves discovery order when an app is toggled. Enabling an app
must not reorder rows underneath the pointer; this keeps the interaction stable and
prevents an in-flight click from landing on a different row.

**Multi-card pages** give each card its own header. Use `SettingsExpander` only when
collapsing details removes optional or already-configured information. Always-visible
groups, such as Rules and Settings sections, use `GroupCardBorderStyle` and have no
chevron:

```
Destinations                                                      ← page title row
┌─ SettingsExpander ────────────────────────────────────────────┐
│  [icon]  Bark            已连接      [toggle]            [⌄]  │  ← 68
├───────────────────────────────────────────────────────────────┤
│  Row title                                    [row control]   │  ← 68
│  Row description                                              │
├───────────────────────────────────────────────────────────────┤
│  Clear                                        [Clear button]  │  ← destructive, own row
│  Clearing requires re-entering the device key                 │
└───────────────────────────────────────────────────────────────┘
```

Rules:

- The card header carries group-level controls. A destination's enable toggle belongs
  in its card header, not in a row.
- The card itself has no content padding. Its header and rows each own their inset.
- Group headers use `24,24,24,24`; setting rows use `24,12,24,12`; list rows use
  `24,8,24,8`; form rows and action footers use `24,16,24,16`.
- Rows inside a card are flush, separated by a full-width 1 px divider.
  There is no gap between rows.
- Cards are separated by `SpacingL` (16). No divider between cards.
- A row is either clickable or contains a control. Never both.
- A destination's Save, Cancel, Send test, and Clear configuration actions share one
  footer. Status occupies the left star column; the equal-sized action cluster is
  right aligned. Cancel restores that destination's last saved editor snapshot. Clear
  remains secondary, separated from ordinary actions, and requires confirmation.

## 6. Descriptions

**A description appears only when the title is not enough for the user to decide.**
Restating the title is prohibited. Stating the obvious is prohibited.

A description is justified by exactly one of four things:

1. A precondition the user must satisfy
2. A format or source the user cannot guess
3. A consequence that is not reversible
4. A privacy implication

Applied to current copy:

| Title / description | Verdict | Reason |
| --- | --- | --- |
| 主页 / 快速查看通知转发状态 | **delete** | restates |
| 最近发送 / 最近三个发送任务 | **delete** | the row count says it |
| 服务器地址 / Bark 服务端的基础地址 | **delete** | restates |
| 登录时启动 / Windows 登录后自动运行 | **delete** | restates |
| 通知转发 / 开启转发前，请至少启用一个应用 | keep | precondition; explains a disabled toggle |
| 静默启动 / 启动时不显示窗口，停留在通知区域 | keep | not obvious |
| 设备密钥 / Bark 应用首页显示的那串字符 | keep | source the user cannot guess |
| 存储的数据 / 清除…不影响配置 | keep | consequence |
| 关于 / 版本 · 项目与支持信息 | keep | selected for v1.2; identifies the installed product and support paths |

Two legal description positions, and only two: under the row title (default), or under
the control when it explains a value the user just produced. A description never sits
outside and above its card. Runtime operation status uses the owning row or footer's
fixed status slot; field validation stays immediately below its field.

## 7. Control inventory

| Situation | Control | Notes |
| --- | --- | --- |
| Binary state | `ToggleSwitch` | **`MinWidth="0"`, `OnContent`/`OffContent` cleared.** The default `MinWidth` is 154 and reserves ~110 px of empty space to the right of the knob. |
| Exclusive choice in a setting | `ComboBox` | At every cardinality, including three. Settings rows read as a list; a segmented control in one of them is a visual exception with no meaning. |
| Filtering a list | `Segmented` | Apps (All / Enabled). Activity filtering is deferred until its event model is designed. |
| Free text | `TextBox` | |
| Secret | `PasswordBox` with a fixed trailing reveal button | The button appears whenever a value exists, toggles hidden/visible state, and owns a permanently reserved 32 px slot so the editor bounds never change. Save, Cancel, Test, navigation, and restart restore the hidden state. |
| Multi-value set | `TextBox` + add button, values as chips in `ItemsRepeater` + `WrapLayout` | `ItemsWrapGrid` is not valid outside `ListViewBase` |
| Primary action in a card | `Button` with `AccentButtonStyle` | One per card |
| Page-level or card-level secondary action | Borderless button, 16 px `FontIcon` + text, `TitleBarIconButtonStyle` grammar | A bordered `Button` floating on a card reads as a stray box; text alone has no affordance |
| Destructive action | Plain `Button` in the owning action footer | Separated from ordinary actions; confirming `ContentDialog` where the action is not undoable |

## 8. Commit model

- Toggles, filters, and dropdown choices apply immediately.
- Text and secret fields require explicit Save. Save is disabled until the card
  validates. Unsaved, progress, success, and operation-error states use a fixed left
  slot in the owning one-line footer; equal-width short action buttons occupy a stable
  right slot so feedback never changes layout height or alignment.
- Leaving the page with unsaved edits still prompts.

Windows Settings uses the same split — toggles are immediate, proxy and Wi-Fi
property fields are saved explicitly — so this is a division of labor, not an
inconsistency.

## 9. Adaptive behavior

Breakpoints key off **content width** — the width actually available to the page —
never off window width. Window width is not a proxy, because the navigation pane is a
persisted user choice worth either 232 or 48 px.

| Tier | Content width | Page padding |
| --- | --- | --- |
| Compact | < 760 | `24,24,24,32` |
| Standard | 760–1039 | `40,24,40,32` |
| Wide | ≥ 1040 | `56,24,56,32` |

**`MaxContentWidth` is 1200 and the column is centered.** The outer column includes
the current page gutters, so the card surface itself may grow to 1200 rather than
remaining fixed while only its margins change. Only the horizontal gutter changes by
tier. Top padding remains 24 and bottom padding remains 32, so page titles do not move
vertically while the window is resized. Card internal padding does not jump at a page
breakpoint.

The page scroll host spans the entire NavigationView content pane so its vertical
scrollbar sits at the pane's trailing edge. The centred, width-bounded content column
is the scroll host's child, not its parent. The page reserves a safe inset between its
content and an overlaid vertical scrollbar. A page has one vertical scroll owner.
A nested list scroll surface is legal only when the page scroll host is disabled and
the page gives that list a bounded `*` viewport; two independently scrollable
vertical regions must never compete in the same narrow-window interaction path.

Interactive rows do not move controls to a second line. Inputs, buttons, combo boxes,
and toggles remain in their owning row at every supported window size. When a component
enters its compact width tier, fixed accessory and destination label columns become narrower;
descriptive text wraps or trims while the control remains right-aligned. This preserves
the Windows Settings scan line and avoids resize-dependent interaction grammar.

## 10. Shell

| Element | Specification |
| --- | --- |
| Title bar | 48 px, `AppWindowTitleBar.PreferredHeightOption = Tall` |
| Title bar content | 20×20 brand mark, `NotiRelay` at 14 SemiBold, then the pane toggle at 32×32 |
| Pane toggle | Borderless, transparent, 16 px `FontIcon` glyph `E700` — the same weight and size as a navigation item's icon. Registered as a passthrough non-client region via `InputNonClientPointerSource`. It is not a `NavigationViewItem`. |
| Caption buttons | Native. Never redrawn. |
| Pane widths | 232 expanded / 48 collapsed, persisted user choice |
| Navigation row height | 44 |
| Selection treatment | **Native, untouched.** The NavigationView's own indicator and background. Do not assign `Foreground` or `FontWeight` to a `NavigationViewItem`; the control's visual states overwrite it and the assignment never renders. |
| Content area | A distinct layer: override `NavigationViewContentBackground` to `LayerFillColorDefaultBrush`, keeping the native `8,0,0,0` corner. One resource, no template change. |
| Footer | A forwarding **status row** — icon plus `转发已开启` / `转发已关闭`, `SelectsOnInvoked="False"`, click navigates to Home — then Settings. The row contains no control. |
| Icons | Segoe Fluent Icons via `FontIcon`. `SymbolIcon` is not used; it resolves to Segoe MDL2 Assets and mixes stroke weights. |

### Forwarding control points

Forwarding can be switched in exactly two places:

1. The toggle on **Home**.
2. The **notification-area menu**, as a checkable item.

The navigation footer shows state and does not change it — a navigation item that is
also a control cannot describe itself to the keyboard or to a screen reader. The
notification-area item must be checkable rather than a read-only label, because the
window can now be closed for good (ADR 0012) and would otherwise leave no control
point at all.

## 11. State matrix

Every interactive component defines Default, PointerOver, Pressed, Focused, Disabled,
and Error.

**The focused state must not change layout size.** Use `FocusVisualMargin` and the
system focus visual; never animate or set `BorderThickness`, which reflows content by
a pixel on focus.

## 12. Accessibility

- A clickable row exposes the Invoke pattern. `SettingsCard.IsClickEnabled` provides
  this; a `UserControl` with a `KeyDown` handler does not.
- Decorative icons set `AutomationProperties.AccessibilityView="Raw"`.
- No fixed `Width` on inputs. Use `MinWidth` plus stretch so 200 % text scaling does
  not clip.
- Dark theme, high-contrast theme, text scaling, focus, and adaptive reflow are
  verified on the real-page matrix before a slice is considered done.

## 13. Prohibited

- Redrawing caption buttons
- Rewriting any `ControlTemplate`
- Literal spacing, font size, or corner radius values in page XAML
- Assigning `Foreground` or `FontWeight` to a `NavigationViewItem`
- Using unaudited Toolkit metric resources (see §14)
- Page subtitles
- A description that restates its title
- Window width as an adaptive trigger
- `ListView` or `ItemsRepeater` inside a `StackPanel` inside a `ScrollViewer` —
  available height becomes infinite, virtualization is lost, and every item realizes
- `ItemsWrapGrid` outside a `ListViewBase` host
- A runtime status string used as a row's static description

## 14. Toolkit metric overrides

The keys below were audited against the Windows Community Toolkit 8.2.251219 source at
tag `v8.2.251219` (`a6b4dc451c0e54dd29f58743894a956100e7f713`). The NuGet package ships compiled XBF,
so the tagged source is the readable source of truth.

| Toolkit resource | Toolkit default | NotiRelay mapping |
| --- | --- | --- |
| `SettingsCardBorderThickness` | `1` | `ThinBorderThickness` (`1`) |
| `SettingsCardPadding` | `16,16,16,16` | Reviewed Toolkit internal default; page-owned rows use their own named padding tokens |
| `SettingsCardMinHeight` | `68` | `RowHeightSetting` (`68`) |
| `SettingsCardDescriptionFontSize` | `12` | Reviewed default; matches the 12 px description ramp |
| `SettingsCardHeaderIconMaxSize` | `20` | `StandardIconSize` (`20`) |
| `SettingsCardHeaderIconMargin` | `2,0,20,0` | Reviewed Toolkit internal default |
| `SettingsCardWrapThreshold` | `476` | `RowReflowBreakpoint` (`720`) |
| `SettingsCardWrapNoIconThreshold` | `286` | Reviewed Toolkit internal default |
| `SettingsExpanderHeaderPadding` | `16,16,4,16` | Reviewed Toolkit internal default |
| `SettingsExpanderItemPadding` | `58,8,44,8` | Reviewed Toolkit internal default; destination body supplies its own zero-padded card and named form-row insets |
| `SettingsExpanderContentMinHeight` | `16` | Reviewed Toolkit internal default |
| `SettingsExpanderChevronButtonWidth` / `Height` | `32` / `32` | `TitleBarInteractiveSize` (`32`) for both dimensions |
| `SegmentedItemSpacing` | `1` | Reviewed Toolkit internal default |
| `ButtonItemSpacing` | `2` | Reviewed Toolkit internal default |

Toolkit 8.2.251219 uses the global `ControlCornerRadius` directly for `SettingsCard` and
`SettingsExpander`; it exposes no dedicated card-corner resource. NotiRelay therefore
does not override `ControlCornerRadius`, because that would restyle unrelated native
controls. Application-owned `GroupCardBorderStyle` surfaces continue to use
`CardCornerRadius` (`8`). The Toolkit controls retain their native reviewed corner and
internal padding while outer spacing, destination form rows, list rows, and action
footers remain owned by this system. This preserves the no-template-rewrite boundary.

## 15. Page specifications

Specifications marked as v1.2 targets are accepted milestone designs rather than
claims about the completed v1.1 implementation. Their code and validation land in
the corresponding v1.2 Development Slice.

**Home (v1.2 target)** — the visible page name is `主页` / `Home` everywhere. A forwarding card
whose single row carries state, the precondition description, and the toggle. Its
summary separates configuration from delivery health: enabled Apps and Destinations
are configuration counts; In queue is the live non-terminal count; Sent (24h) and
Failed (24h) use an explicit rolling window based on terminal time. Active and
Retry Scheduled remain
Activity-level details rather than separate Home metrics. A second card lists the
three most recent sends with a secondary action to Activity. The four shortcut rows
that duplicated the navigation pane stay deleted.

**Apps** — a plain page title followed by one card. The card begins with a compact
68 px functional header: a vertically centred title `Application list` / `应用列表`
and total/enabled or discovery-status description at the left, plus a borderless
icon-and-text Discover apps action at the right. Search plus an All/Enabled `Segmented`
occupy the next row, followed by the list at list density. The empty state alone keeps
a 180 px minimum; a populated list grows from its natural row height to a bounded nested
scroll surface when the collection is long.

**Destinations** — one page, three `SettingsExpander` cards. Header is icon, name,
status, enable toggle, chevron. **Configured and healthy destinations collapse;
unconfigured or failing ones expand.** Body fields use a 280 px shared label column,
16 px gap, and a fluid editor column inside a centred 760 px maximum form workspace;
below the 640 px form-content breakpoint, the label column contracts to 180 px while
labels and 32 px-high inputs stay on one row. Secret editors reserve a fixed 32 px
trailing reveal slot, so
showing or hiding a secret never changes the editor width. The footer shares the form
workspace and stays on one line: a fixed-status slot at the left and a fixed
right-aligned group of equal-sized Save, Cancel, Test, and Clear buttons. Cancel
restores the last saved values for that destination; Clear remains secondary and
confirmed.

**Rules** — two always-visible, non-collapsible group cards, Include and Exclude, each
with an independently persisted group-level enable toggle. The body uses a centred
524 px maximum content block. Its fluid keyword editor and Add button share one row;
feedback, empty-state copy, and the content-sized wrapping chip flow align to the
editor's left edge inside that block. The block shrinks with the card instead of forcing
a fixed input width, and each removable chip is capped independently. Enter or Add trims
the value, rejects empty and case-insensitive duplicates, persists immediately, and
renders an individually
removable chip. Disabling a group preserves its chips but ignores them at runtime.
Exclude wins when both groups match. The precedence explanation belongs to the Exclude
card's description.

**Activity (v1.2 target)** — the card begins with a vertically centred functional header: `Send
history` / `发送记录`, its live summary, and the Refresh command. The page-level
scroll host owns vertical scrolling at every supported window size; the Activity card
does not add a competing vertical scrollbar. The empty state keeps a 180 px minimum,
and a populated list grows at natural row height. A row shows Source Application to
Destination, a bounded two-line Activity Preview when enabled, enqueue time, attempt
count, and trailing status. The status is centred against the complete record; an error
uses the final bounded line and a tooltip. Preview text trims rather than widening the
row, is excluded from diagnostics, and has an accessible name that does not disclose
more content than is visibly retained. Grouping and filtering remain deferred; do not
add a provisional All/Failed split.

**Settings (v1.2 target)** — four always-visible, non-collapsible group cards whose section title is
inside the card: Startup and close (Start at sign-in, Silent start, Close action),
System (notification access and a language `ComboBox`), Privacy and data, and About.
Privacy provides the concrete clear-records action, the Activity Preview preference,
and sanitized diagnostic export; it explains local retention without repeating the
card title. About uses Settings-style rows for the installed version, author
`凯风（Kaifeng）`, GitHub account `@k4if3ng`, repository, Issues, Star, and MIT
License. External rows expose normal link semantics and an external-link affordance.
The version is read from installed package or assembly metadata, never hard-coded in
XAML or resources. Accessory controls remain on the same row at every supported width,
but alignment is type-specific: ComboBoxes and action buttons are 160 px at both tiers,
and native internal alignment is preserved (`ComboBox` values left aligned, button
labels centred). Decorative row icons use a restrained 16 px secondary colour; startup
and close behaviour use semantic system/window glyphs rather than a Play triangle or
prominent X. Close action offers Minimize to tray, Exit, and Ask every time. The ask
dialog has Minimize to tray as primary, Exit as secondary, Cancel, and an optional
Remember my choice check box. Silent Start is independent from Start at sign-in and
also applies to manual launch.

## 16. Verification surface

The shipping Home, Apps, Destinations, Rules, Activity, and Settings pages are the
verification matrix. Together they exercise every real token and component in default,
pointer-over, pressed, focused, disabled, error, empty, long-content, compact,
standard, and wide states. Validate light, dark, and high-contrast themes plus text
scaling on those pages. No separate verification-only page is shipped.
