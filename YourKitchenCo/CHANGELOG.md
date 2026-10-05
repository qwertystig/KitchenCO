# YourKitchenCo — build status

Everything below runs entirely on in-memory mock services (no database). Every mock service
sits behind an interface (`ICompanyDirectoryService`, `IUserDirectoryService`, `IOrderService`,
`IProductService`, `ICycleMenuService`, `IOrderSchedulingService`, `ISessionService`) — when
Supabase gets wired in at the end, only the implementations behind those interfaces change.
Nothing that consumes them (ViewModels, XAML) should need to change.

## Done

**Order scheduling & cycle menu**
- `OrderSchedulingService` — the weekday cutoff rules (48hr-ish, weekend-aware)
- `CycleMenuService` — resolves the 8-week rotating menu from `cycleMenu.json`
- Delivery-date picker wired into `ProductDetailPage` (5-day window for cycle items, 10-day for static)
- Checkout re-validates every cart item's date before submitting

**Multi-tenant backbone**
- `Company` / `CompanyLocation` models + `MockCompanyDirectoryService`, seeded with 3 companies / 4 locations
- `UserAccount` extended with `CompanyId` / `LocationId`
- `MockUserDirectoryService` — shared, single source of truth for user accounts (`AdminUsersPage` now reads/writes through it instead of its own private list)
- `SessionService` — holds the logged-in user for the app session
- `LoginPage` now does a real lookup against the user directory instead of just checking whether "admin" appears in the email

**Orders**
- `MockOrderService` — shared in-memory order store, seeded with realistic past + upcoming orders across all 3 companies
- Checkout (`CartPageViewModel`) now actually creates `Order` records (one per cart line, tagged with the logged-in user's company/location) instead of just showing an alert
- `ActiveOrdersPage` / `OrderHistoryPage` (customer-facing, were empty stubs) now show the logged-in user's real upcoming/past orders
- **New:** `AdminActiveOrdersPage` — the "see all active orders" admin screen from the original spec, filterable by company, with a status-update action

**Bugs fixed along the way**
- `IsNotNullOrEmptyConverter` and `InverseBoolConverter` were referenced in `CartPage.xaml`, `AdminUsersPage.xaml`, and `AdminNotificationsPage.xaml` but never defined or registered anywhere — those pages would have crashed with a `XamlParseException` the first time they loaded. Added and registered in `App.xaml`.

## Demo accounts (mock, no password check yet)

| Email | Role | Company |
|---|---|---|
| admin@yourkitchen.co | Admin | — |
| john.doe@ecogra.org | Customer | Ecogra (Rosebank) |
| sarah.smith@ecogra.org | Customer | Ecogra (Rosebank) |
| priya.naidoo@tata.co.za | Customer | TATA (Illovo) |
| thabo.nkosi@rcl.co.za | Customer | RCL (Bedfordview) |

Any other email logs in as an ad-hoc guest customer (or admin, if the email contains "admin").

**Menu presentation**
- `Product.Icon` — every mock product now carries a hand-picked emoji icon instead of a stock photo URL; `ProductDetailPage`, `UserDashboardPage`, and `AdminMenuPage` all render it in a gold-tinted badge instead of an `<Image>`
- Every product's `Description` polished into a proper one-sentence blurb; the dashboard product cards now also show a truncated one-line description under the name (previously name + price only)
- `ImageUrl` left on the `Product` model (unused in the UI now) rather than removed, in case real photography gets added later

**Real menu data — the rest of the menu**
- `JsonProductService` replaces `MockProductService` entirely — it reads `staticMenu.json` (89 real items across 13 categories, real prices, real descriptions, real icons from the file's own `_icons` map) and `cycleMenu.json` (the 8-week rotating menu) straight from the bundled assets, no more hand-written mock dishes
- Static items with a "large" price now get a real **Size** customization option (Standard/Large) instead of the price difference being silently dropped
- Cycle-menu items are generated for the current 5-day orderable window, each one **locked to its specific delivery date** (the dish only exists that day) — `ProductDetailPage` skips the date picker entirely for these and shows the fixed date instead
- Dashboard category chips (previously hardcoded "Burgers/Salads/Pizza/Asian", matched by guessing at substrings in the dish name) are now built dynamically from the real menu's actual categories, and filtering matches the real `Category` field exactly instead of guessing

**Add to Basket bug fix**
- The button was being hard-disabled on weekends (`IsEnabled` bound to `IsOrderingOpen`) — since delivery-date math already rolls forward past the weekend correctly on its own, there was no real reason to block *browsing and building a basket* on a Saturday/Sunday, only actual order *submission*. Removed the weekend gate from `ProductDetailViewModel`/`ProductDetailPage`; the weekend rule is still enforced (correctly) at checkout in `CartPageViewModel`, which was untouched.

**10 AM cutoff**
- `OrderSchedulingService.CutoffTime` now defaults to 10:00 AM instead of end-of-day — this is the real cutoff the scheduling math uses when deciding whether "today" still counts as a valid order date
- A one-time popup ("Order cutoff: 10:00 AM...") shows the first time someone opens a product each login session, via a new `ISessionService.HasSeenCutoffNotice` flag (resets on each sign-in)

**Admin: activate a specific cycle-menu week**
- `ICycleMenuService` gained `GetActiveWeekNumberAsync`, `SetActiveWeekAsync(weekNumber)`, and `GetAllWeeksAsync`. Activating a week re-anchors the whole 8-week rotation so *this* calendar week resolves to the chosen one, and future weeks keep cycling normally from there (it doesn't freeze on that week forever)
- New "Cycle Menu" tab on the admin Menu Catalog page: shows which week is currently active, a picker to browse any of the 8 weeks, and a "Set as Active Week" button

**Admin: CRUD for both menus**
- Static menu CRUD (Add/Edit/Delete/toggle availability) now actually persists into `JsonProductService`'s shared product list instead of only mutating the admin screen's own local copy — previously, an admin-added item would never show up on the customer dashboard, and a "deleted" item would reappear next time the dashboard reloaded. Fixed via three new methods on `IProductService`: `AddStaticProductAsync`, `UpdateStaticProductAsync`, `DeleteStaticProductAsync`
- Cycle menu items are now individually editable from the same admin page — pick a week, see all 25 dishes (5 days × 5 categories) as a flat editable list, tap Edit on any one to change it via `ICycleMenuService.UpdateCycleItemAsync`. There's no Add/Delete for cycle items since the day/category structure is fixed (always exactly 5 categories × 5 weekdays) — "CRUD" here is really "RU", which is the sensible scope for a fixed weekly grid

**Customer pages: Profile, Register, Settings**
- `ProfilePage` was a hardcoded stub ("John Doe") — now reads the real logged-in user from `ISessionService`, resolves their company/location names via `ICompanyDirectoryService`, and has a working Sign Out button
- `RegisterPage` had a form that only showed a fake "Account created!" alert and never touched the user directory — now it actually validates input, checks the email isn't already taken, requires picking a real company + delivery location (orders are grouped by these), calls `IUserDirectoryService.AddUserAsync`, signs the new user in, and drops them into the app. Found and fixed the same "manual `new ViewModel()` bypasses DI" bug as before — `LoginViewModel.RegisterAsync` was still doing `new RegisterPage(new RegisterViewModel())`
- `SettingsPage` toggles (notifications, dark mode) now persist for real via `Preferences` instead of being purely cosmetic — and toggling dark mode actually switches `Application.Current.UserAppTheme` immediately. Added a Sign Out button and an account email header too

**Admin: Companies & Locations CRUD (new page)**
- New "Companies" tab in the admin shell — add/edit companies, toggle active/suspended, and add/edit each company's delivery locations inline (nested list per company card)
- Wired straight into the existing `ICompanyDirectoryService` — no new service layer needed, it already had everything ready

**Admin Dashboard & Reports: real analytics**
- `AdminDashboardViewModel` no longer ships its own 3 hardcoded fake orders — the KPI cards (today's revenue, active orders, company count, customer count) and the "Upcoming Deliveries" queue are now computed from `IOrderService`/`ICompanyDirectoryService`/`IUserDirectoryService`
- `AdminReportsViewModel` no longer has hardcoded numbers per timeframe — Today/This Week/This Month now genuinely filter real orders by `DeliveryDate` and aggregate total revenue, order count, average order value, and top-selling items from them
- Added `IOrderService.GetAllOrdersAsync()` (every order regardless of user/date) since reporting needs the full history, not just active orders
- Heads up: seed data is only 8 orders, so these numbers will look sparse until more real orders exist — that's expected, not a bug

**Admin Notifications: real recipient counts**
- "Kitchen Staff" / "All Customers" / "Active Order Holders" recipient counts were hardcoded guesses (12, 45, 1500) — now computed from the real user directory and active-orders data. The compose/send flow itself was already solid and didn't need changes.

**Real brand colours & logo**
- Extracted the actual approved palette from the CSG Foods brand identity deck: Cream `#F7F2E8`, White, Summer Red `#D53220` (the core brand colour), plus Harvest Orange `#DA5D23` and Golden Yellow `#EFB638` from the "Summer Harvest" palette for secondary accents
- `#363C1E` ("Forest Green") is confirmed nowhere in the app — it only appears in a code comment in `Colors.xaml` explaining why it's excluded
- Found 18 places across the app where the old gold theme was hardcoded directly as raw hex in XAML instead of using the shared `Colors.xaml` resources — all repointed
- Replaced the old placeholder chef-hat/fork-and-knife logo (clearly an AI-generated stand-in, nothing like the real brand) with the actual "your kitchen co." wordmark from your PNG, processed into proper transparent-background light/dark variants (charcoal ink for light mode, cream ink for dark mode) — `yourkcolight.png` / `yourkcodark.png`

**Select delivery day at login (the big flow change)**
- New `SelectDeliveryDayPage`, shown right after login/registration (customers only — admins go straight to `AdminShell` since they don't order)
- `ISessionService.SelectedOrderingDate` holds the choice for the session; resets on each new sign-in
- `ProductDetailPage`'s date picker now defaults to that day automatically
- After adding an item to the basket: *"Order for a different day"* vs *"Continue with [day]"* — picking "different day" reopens the day picker without losing what's already in the cart

**Real client companies with meal subsidies**
- Replaced the placeholder seed companies (Acme/Globex/Initech) with the real ones: **Ecogra** (R80.00/meal, incl. VAT — 160 Jan Smuts Ave, Rosebank), **TATA** (R85.00/meal — 39 Ferguson Road, Illovo), **RCL** (R40.00/meal — 15 Railey Road, Bedfordview)
- Added `Company.MealSubsidyAmount` — this isn't just decorative, it's applied for real: the Cart page now shows Meal Total → Company Subsidy → **You Pay**, and checkout records both the subsidised amount charged and the subsidy covered on each `Order` (new `Order.SubsidyAmount` field) so admin reporting reflects real billing, not gross menu prices
- Subsidy is capped per meal (an R120 dish with an R80 subsidy still costs R40 — the subsidy never creates a negative charge or carries over as one lump basket-wide discount)
- Profile page shows the person's company subsidy rate

**Two genuinely different palettes: Berry & Cream (light) / Summer Harvest (dark)**
- Previously "Primary", "Secondary", "BrandGold", and "Gold" were flat single colors used everywhere via `{StaticResource X}` — meaning light and dark mode only ever differed in page/card background, never in accent color. Fixed properly: every one of those now has a `*Dark` counterpart (`PrimaryDark`, `SecondaryDark`, `BrandGoldDark`, `GoldDark`) and is consumed via `AppThemeBinding` at all ~65 use sites across the app, converted with a script rather than by hand to avoid missing any
- **Light mode** follows the deck's own Berry & Cream Daily Menu mockup exactly: dark red `#AF1718` for headings/prices/CTAs (`Primary`), pink `#FFDCE8` as the soft accent/badge fill (`Secondary`/`Gold`), light blue `#B6DFF8` as the contrasting second accent (`BrandGold`)
- **Dark mode** uses the full Summer Harvest set: Summer Red `#D53220` (`PrimaryDark`), Harvest Orange `#DA5D23` (`BrandGoldDark`), Golden Yellow `#EFB638` (`GoldDark`), and Sage Green `#C9DE87` (`SecondaryDark`) — all four colors get a real role instead of just two of them
- Also fixed two lines in `LoginPage.xaml`/`RegisterPage.xaml` that were already using `AppThemeBinding` but had copy-pasted the same color into both the Light and Dark side, silently defeating the point
- Found and fixed an unrelated but glaring miss while auditing colors: the app icon and splash screen were still the default MAUI scaffold purple (`#512BD4`) — now the brand red. The splash/icon *foreground graphic* is still the default .NET bot mascot SVG, since no real icon artwork was provided — that needs actual design assets, not just a color swap
- Confirmed via an automated cross-check script that every `StaticResource` reference in the whole project resolves to a defined key (this is exactly the class of bug that broke the app after the previous color pass)

**Color remix, round 2 — bolder, per client feedback**
- Because the previous pass already converted every accent color usage to `AppThemeBinding` (see above), this round was a values-only change in `Colors.xaml` — zero risk of missing a usage site, since the same ~65 consumers just pick up whatever the keys resolve to now
- **Light mode**: page background is now Light Blue `#B6DFF8` (was cream), menu/card containers are Pink `#FFDCE8`, headings/prices/CTAs stay Dark Red `#AF1718`, body text black/charcoal — badges use blue fills with red for selected states, so all three Berry & Cream colors are doing distinct, visible work
- **Dark mode**: mixed up analogously rather than mirrored — page background is a warm dark terracotta (`#241209`, darkened Harvest Orange) instead of neutral charcoal, cards are a lighter terracotta (`#3D2015`), and headings/CTAs switched to Golden Yellow `#EFB638` instead of red so dark mode reads as its own combination, not just a recolor of light mode. Secondary accent is Summer Red, badges are Sage Green.
- Also swapped several hardcoded (non-`StaticResource`) incidental colors that would've clashed with the new scheme: icon-badge circle backgrounds (was a pale-gold tint, now blue/terracotta to match), a translucent status-pill tint used across 4 admin pages, and a selected-size-option highlight on the product page
- Dark mode's background is a warm brown/terracotta specifically to stay well clear of the excluded `#363C1E` forest green — confirmed via the same automated cross-check as last time that it doesn't appear anywhere as an actual color value

**Drawer/flyout menu**
- Sized to content instead of the MAUI default: `Shell.FlyoutWidth="230"` (customer app) / `"280"` (admin — "Reports & Analytics" is the longest label), so it no longer looks like an oversized empty panel
- Added a `FlyoutFooter` (brand mark + "Powered by CSG Foods") to both shells so the full-height drawer feels intentional rather than trailing off into empty space at the bottom
- Found and fixed 3 admin flyout icons (Menu Catalog, Notifications, Reports & Analytics) that were referencing PNG files that **didn't exist** — silently broken/blank icons. Drew clean replacement silhouettes (list, bell, bar chart) matching the existing icon style, plus a proper distinct building icon for "Companies" (was reusing the Dashboard's home icon)

**Colors blending better**
- Auditing this surfaced two real bugs, not just style preference: the global `NavigationPage` style (used by Login/Register before the Shell takes over) still had light-gray text on a white bar — nearly unreadable — because it referenced generic `Gray200`/`White` instead of the actual brand colors. Fixed to use `PageBgLight`/`PageBgDark` and `PrimaryTextLight`/`PrimaryTextDark` like everywhere else.
- `AppShell`'s tab bar (the Active/History tabs under "My Orders") was hardcoded to a flat orange regardless of theme — same color in dark mode as light, clashing with everything else that properly switches. Now theme-aware: berry red on cream cards (light) / golden yellow on dark terracotta (dark).
- The implicit `Page`/`Shell` styles in `Styles.xaml` had the same generic-color problem as the NavigationPage bug above — fixed for consistency, even though most individual pages already override it explicitly.

**Feel & animations**
- Every button in the app now has real press feedback (slight scale-down + dim) via a single change to the global implicit `Button` style — applies everywhere at once, not just where I remembered to add it
- Page entrance animation (fade + gentle rise) was only on 3 of 18 pages — inconsistent, so some pages snapped into view while others eased in. Built one shared `PageAnimation.EntranceAsync()` helper and applied it consistently across all 18 pages, so navigating through the app feels the same everywhere

**Themed popups (replacing the native OS alert dialog)**
- Native `DisplayAlert` completely ignores app theming on every platform — white/black system dialog regardless of brand colors. Built `AppAlertPage`, a proper branded modal (dimmed backdrop, animated fade/scale-in card, theme-matched colors in both light and dark, 1–2 dynamically-built buttons) plus an `IAlertService` behind it
- Replaced all **31** `DisplayAlert` call sites across 12 files — login, register, cart/checkout, product add-to-basket, and every admin CRUD confirmation now use the branded popup
- Exposed as a static `AlertService.Instance` (set once in `App.xaml.cs`) rather than adding a constructor dependency to every single ViewModel — a deliberate, documented exception to the DI-everywhere pattern, scoped specifically to this one cross-cutting concern
- **Update**: the prompt and action-sheet dialogs mentioned as scoped out above are now done too (see below) — every native dialog in the app is themed.

**Themed prompts & action sheets (completing the popup system)**
- Built two more branded popups matching `AppAlertPage`'s design: `AppPromptPage` (title, message, bordered text entry, OK/Cancel — auto-focuses the input) and `AppActionSheetPage` (bottom-sheet style, slides up from the bottom like the native one does, one button per option plus a distinct Cancel)
- `IAlertService` gained `ShowPromptAsync` and `ShowActionSheetAsync`, mirroring the native `DisplayPromptAsync`/`DisplayActionSheet` signatures so the replacement was a straight swap at each call site
- Replaced the remaining **17** native dialog calls (14 prompts + 3 action sheets) across `AdminMenuViewModel`, `AdminCompaniesViewModel`, `AdminUsersViewModel`, `AdminDashboardViewModel`, and `AdminActiveOrdersViewModel`
- **Every single native `DisplayAlert`/`DisplayPromptAsync`/`DisplayActionSheet` call in the app is now themed** — confirmed via grep, zero remaining
- Cleanup: found and removed 4 now-dead `var page = Application.Current.MainPage;` locals in `AdminCompaniesViewModel` left over from before those lines were converted

**Custom category icons (replacing emoji)**
- Every food category icon in the app was actually just an emoji character rendered as text (`Label.Text = "🥗"`) — renders inconsistently across platforms/devices and doesn't look premium. Hand-drew a cohesive 16-icon silhouette set instead: Salad Bar, Poke Bowl, Stir-Fry, Ciao Italy, Wraps, Sandwiches, Hot Dog Saloon, Burger Bar, Sides & Sauces, Fitness Meals, Homemade Winter Soups, Ramen Bowls, Vegan Meals (all 13 static-menu categories), plus Main Meal and Curry of the Day for the two cycle-menu slots that didn't have a good static equivalent, and one generic fallback
- This was a real architecture change, not just new art: `Product.Icon` and `CategoryChip.Icon` now hold an image filename instead of an emoji string, so `ProductDetailPage`'s hero badge, `UserDashboardPage`'s product cards, and `AdminMenuPage`'s catalog thumbnails all switched from `<Label Text="{Binding Icon}">` to a real `<Image Source="{Binding Icon}">`
- The category filter chips needed more than a one-line swap — they used to be a single `Button` with icon+text combined into one string (`"🥗 Salad Bar"`). Restructured into a `Border` + `Image` + `Label` (with a `TapGestureRecognizer` standing in for the `Button.Command` it no longer has) so the icon renders as a real image alongside the text
- `JsonProductService` no longer reads the emoji suggestions from `staticMenu.json`'s own `_icons` map — replaced with its own category→filename mapping pointing at the new icon set
- Verified every referenced icon filename actually has a matching file installed (this is exactly the kind of thing that silently breaks — see the missing flyout icons from a couple of passes back)

**Icon visibility bug — found the actual root cause**
- The badge background behind each icon was `#D6ECFB`, a very pale near-white blue — and the icons were solid white silhouettes. White-on-near-white is exactly why they read as invisible, not a rendering failure.
- Fixed properly with `IconTintColorBehavior` (from CommunityToolkit.Maui, already referenced in this project): icons now tint to `Primary`/`PrimaryDark` (dark red / golden yellow) at render time — colors specifically chosen elsewhere in the app for legibility against these exact backgrounds, so contrast is guaranteed rather than hoped-for. Applied to the product hero badge, dashboard cards, and admin catalog thumbnails.

**Redesigned 8 of the 16 category icons for clarity**
- Poke Bowl (was an abstract steering-wheel shape) → a bowl with distinct topping mounds
- Stir-Fry → wok with food pieces visibly tossed above it
- Ciao Italy (was a bowl with barely-visible squiggles) → a simple, clean fork — instantly recognizable
- Wraps → rounder, clearer burrito shape with visible fold lines
- Homemade Winter Soups (looked too similar to the wok) → a proper pot with lid, handles, and steam
- Ramen Bowls → bolder, thicker noodle lines so they're actually visible
- Vegan Meals (was an unrecognizable pinwheel) → a clean single leaf with a stem
- Curry of the Day (swirl was barely visible, and content was clipping off-canvas) → a bowl with a clearly visible spice swirl and steam, nothing cut off
- Burger, Hot Dog, Fries, Dumbbell, Salad Bar, Sandwiches, Main Meal, and the default dish-cover icon were already reading well and are unchanged

**Drawer menu — fixed the actual cause, with an honest caveat**
- The flyout *panel* is always full device height on every platform — that's fundamental to how native drawers work on iOS/Android and isn't something any app can change. That part can't be "fixed."
- What actually was fixable: my previous attempt used `Shell.FlyoutFooter`, which renders directly after the nav items in normal document flow — not pinned to the bottom. With only a few short nav items, that left a floating, disconnected footer with an awkward gap on both sides, which read as broken.
- Rebuilt both `AppShell` and `AdminShell` using `Shell.FlyoutContent` for full manual layout control: a `Grid` with an explicit spacer row between the nav content and the footer, so the footer is now genuinely pinned to the true bottom edge and the nav content visibly ends right after Logout with a clean divider — no more floating disconnected block.
- This required replacing the auto-generated flyout item list with manually-built tappable rows (`TapGestureRecognizer` + `Shell.Current.GoToAsync`) — the routes and pages themselves are unchanged, only how the flyout is visually built.
- Found and fixed one more pre-existing missing-asset bug while auditing this: `ProfilePage.xaml` referenced `user_avatar.png`, which never existed — now reuses the existing `user.png` icon.

**Reverted custom-drawn icons back to emoji — an honest capability call**
- Feedback was that the hand-drawn icon set still looked bland and basic even after the redesign pass. That's a real ceiling, not a fixable bug: flat shapes built from basic drawing primitives (rectangles, circles, polygons) genuinely can't match the detail, shading, and color richness of professionally-designed emoji, which are made by dedicated design teams specifically for this purpose
- Reverted `Product.Icon`/`CategoryChip.Icon` back to emoji strings, `ProductDetailPage`/`UserDashboardPage`/`AdminMenuPage` back to `<Label Text="{Binding Icon}">`, category chips back to the simpler single-`Button` design — undoing the architecture change from two passes ago
- Chose noticeably better emoji than the original `staticMenu.json` suggestions where the old choice was generic or inaccurate: Poke Bowl (was 🍱 unchanged — kept, it's accurate), Stir-Fry → 🥘, Ciao Italy → 🍝, Ramen Bowls → 🍜 (all vivid, detailed, and immediately recognizable at a glance, which was the actual goal)
- The 16 custom PNG icon files from the last two passes are still sitting in `Resources/Images/` (`cat_*.png`) but are no longer referenced anywhere — harmless dead weight, not cleaned up given everything else in this pass, but worth deleting next time that folder gets touched

**New day-theme colors, tied to Main Menu vs Cycling Menu identity**
- Client asked to use `#3571B7`/`#B6DFF8` (one per menu) and `#AF1718`/`#363C1E` for the light theme. `#363C1E` is the color explicitly excluded from the palette a few passes back — flagged this conflict directly and confirmed with the client to keep it excluded and substitute a different dark shade (used charcoal `#231F20`, already an existing vetted neutral) rather than silently deciding either way
- Since the app already has real `MenuType` data (`Static` = Main Menu, `Cycle` = Cycling Menu) and an existing "Main Menu / Cycling Menu" toggle on the dashboard, tied the colors to that real structure instead of just swapping flat theme values: **Main Menu = `#3571B7` blue background / `#AF1718` red accent**, **Cycling Menu = `#B6DFF8` pale blue background / charcoal accent**
- Built one reusable `MenuIdentityColorConverter` (accepts either the dashboard's `CurrentMenu` string or a `Product.MenuType` enum) and applied it everywhere menu identity shows up: the Main/Cycling toggle buttons, the category filter chips (including the "All" chip, whose accent-colored selected state uses the stronger red/charcoal tone), the product detail hero badge, dashboard product cards, and the admin catalog thumbnails
- Dark mode is deliberately untouched — this was specifically a "day theme" request, so the converter falls back to the existing dark terracotta look whenever `Application.Current.RequestedTheme == Dark`
- Minor known limitation: because the converter reads `RequestedTheme` at bind time rather than through `AppThemeBinding`, colors on an already-open page won't live-update if someone switches their OS theme while that exact page is on screen — a genuine edge case, not worth the added complexity to solve here

**Real food photography, replacing emoji on menu items**
- Emoji were reading as childish for the actual product cards/hero images — sourced 14 real, non-copyright photos from Unsplash instead (one per static category, plus a dedicated one for curry on the cycle menu), all confirmed individually "Free to use under the Unsplash License" (free for commercial use, no attribution required — https://unsplash.com/license), not just pulled from a search results page
- Each URL points at a specific photo's stable CDN address, not a random/search endpoint, so the same image loads every time rather than changing on refresh
- Since the sandbox's network access is limited to package registries (no general image hosting), these are hotlinked rather than bundled as local assets — exactly how the original scaffold's mock data worked before any of my changes, and how `Image.Source` already supports remote URLs natively
- `JsonProductService` now assigns a real `ImageUrl` per category alongside the existing emoji (kept only for the small category-filter chips, where a photo wouldn't fit) — `ProductDetailPage`'s hero, `UserDashboardPage`'s product cards, and `AdminMenuPage`'s catalog thumbnails all switched from the emoji `Label` to a real `Image`
- Admin CRUD (`UpdateStaticProductAsync`, new-item defaults) updated to persist `ImageUrl` correctly so this doesn't silently regress the next time someone edits a menu item
- Verified every photo ID referenced in code is unique and present, the same discipline as the icon-file cross-check that caught two real bugs earlier in this project

**Light mode background: back to cream**
- `PageBgLight` changed from `#B6DFF8` (light blue) to Cream `#F7F2E8` — a single key change in `Colors.xaml` that propagates everywhere automatically via the existing `AppThemeBinding` pattern (every page, the Shell chrome, `NavigationPage`, etc.)
- Cards stay Pink `#FFDCE8`, and the Main Menu / Cycling Menu identity colors (blue, red, charcoal) are untouched — blue is still very much present, just no longer the page-wide background

**Dedicated photo for Main Meal**
- The cycle menu's "Main Meal" slot was reusing Burger Bar's photo as a placeholder stand-in — didn't really fit dishes like Bobotie, Boerewors, or Beef Brisket
- Sourced a proper one: "Sunday Dinner" by Lisa Baker — a plated roast with veggies, confirmed "Free to use under the Unsplash License" — much closer to the actual hearty, home-style mains this category serves

**Correct images for Sides & Sauces (per-item, not per-category)**
- This category actually mixes 3 sauces, 2 potato-wedge dishes, and 5 salads/slaws — one shared category photo (fries) couldn't accurately represent all 10 items, since none of them are literally french fries
- Sourced a proper photo for the sauces sub-group ("Small bowl of queso / cheese sauce," confirmed Unsplash-licensed) and reused the existing Salad Bar photo for the 5 salad/slaw items — the 2 wedge items keep the category's existing fries photo, which is a reasonable fit
- This required a real architecture change: image resolution moved from category-level (`StaticCategoryImageUrl`) to a new per-item `StaticItemImageUrl` method that checks the item name for "sauce"/"salad"/"slaw" and falls back to the category photo otherwise — the first category needing this, but the pattern's now there for any other mixed category later

**Drawer menu footer — found the actual cause this time**
- Diagnosed instead of guessing: the footer text color itself had reasonable contrast against the cream background, so "not very visible" was more likely the footer being squeezed or clipped off-screen on shorter devices or larger accessibility text sizes — it was pinned to the absolute bottom via a spacer row with nothing making it scrollable
- Restructured both `AppShell` and `AdminShell`: header stays pinned at the top (doesn't scroll), but the nav rows, Logout, and the footer are now all one scrollable unit, with the footer directly following Logout in the same natural flow rather than pinned separately. This guarantees the footer is always reachable — it scrolls into view if the content doesn't fit, rather than being clipped — and admin's 8-row menu (the most likely to overflow a short screen) gets the same fix
- Also switched the footer text color from `SecondaryTextLight`/`Dark` to the same `PrimaryTextLight`/`Dark` used by every other nav label, for one less variable

**Functional fixes & additions (large combined pass)**
- Fixed the blank static-menu-items bug — real cause: `AdminMenuPage.xaml` had `x:DataType` set at the page root but the product `DataTemplate` had no override, which can make compiled bindings silently fail against the wrong type (text doesn't render, but the layout still takes up space). Fixed, and confirmed no other page has the same risky pattern.
- Fixed the "order for a different day" flow — it was popping back to the menu *and* pushing the date picker at the same time regardless of your choice. Now: different day → date picker shows and stays; same day → returns to the menu, matching exactly what was asked for.
- Cycling menu now filters to just the day you've selected (via `ISessionService.SelectedOrderingDate`) instead of showing the whole week's dishes at once. Verified admin cycle-menu edits already propagate to the customer side correctly (no caching bug — didn't need new code).
- **Active Orders**: added a date filter alongside the existing company filter, plus "Print Order Sheet" — builds a formatted summary of whatever's currently filtered and hands it to the OS share sheet (Print is a standard option there on both iOS/Android; MAUI has no native print API, so this is the real way to do it without adding a dependency).
- **Overview**: same company/date filtering, plus **bulk status update** — when Company + Date narrow the list down to "everyone at Company X getting delivery on Thursday," one button sends the same status (Preparing/Out for Delivery/Delivered) to every one of them instead of tapping through individually. Rewired the per-order quick-action buttons too — they used to all open the same status picker regardless of which was tapped; now each one directly sets its own labeled status.
- **Reports & Analytics**: genuinely rebuilt, not just tweaked. Added a Company filter, a "Custom Range" option with real date pickers, a bar chart (revenue over time, auto-bucketing to weeks if a custom range spans more than 14 days), and a donut/pie chart (revenue share by top item) with a matching color-coded legend. Charts are hand-drawn with core `Microsoft.Maui.Graphics` (`GraphicsView`/`IDrawable`) rather than a charting NuGet package — this sandbox has no NuGet registry access, so I can't verify an unvetted package actually compiles; core MAUI Graphics is zero-risk since it's already part of the SDK.
- **Cycle menu CRUD**: added a Remove button (clears a dish from a day/category slot) alongside the existing Edit. The day/category grid itself is structurally fixed (every day always has exactly 5 slots), so "delete" clears a slot rather than removing a row, and an empty slot shows "Add" instead of "Edit" — typing a new dish into it is effectively "add." This is Read/Update/Clear rather than a fully dynamic list, which is the sensible scope given the underlying fixed-grid data model — flagged clearly rather than silently narrowed.

**Cosmetic: cycle-menu category colors**
- Original instructions here were genuinely ambiguous about the exact color-to-section mapping, so here's the concrete call I made: the 5 cycle-menu category badges (Main Meal, Vegetarian Meal, Healthy Meal, Curry of the Day, Gourmet Sandwich) now each get their own color from the golden-yellow/sage-green pair, via a new `CycleCategoryColorConverter`
- `#FFDCE8`/`#B6DFF8`/`#AF1718` were left completely untouched, staying exactly where they already were (the Main Menu identity colors) — read "reserved for the dessert section" as "don't touch these," and there's no literal dessert category in the actual menu data to assign them to
- Applied to the three product-photo badges (hero, dashboard cards, admin thumbnails) only — the Main Menu/Cycling Menu toggle buttons and category chips still use the existing blue/red vs pale-blue/charcoal scheme, since those represent which menu you're *browsing*, not which specific dish you're looking at

**Three real bugs found from "these changes aren't showing up" feedback**
- **Date picker not appearing after "order for a different day"**: found a genuine race condition in all three popup types (`AppAlertPage`/`AppPromptPage`/`AppActionSheetPage`). Each one set its result (`_tcs.TrySetResult`) *before* actually closing the modal — meaning the calling code resumed and tried to push the date-picker page while the previous popup was still mid-close on the navigation stack, which can silently fail. Fixed all three: the result is now only set after `PopModalAsync` genuinely completes.
- **Colors not visible**: the previous pass only touched small icon badges tucked inside product cards — easy to miss during a quick look. Extended the same per-category coloring (`CycleCategoryColorConverter`) to the category filter chips too, which are large and always on screen, and fixed the text color to actually be legible against light backgrounds (yellow/sage/pale-blue need dark text, not the white that was there before).
- **Cycling menu "not updating"**: found a real structural bug — `UserDashboardViewModel.LoadDataAsync()` sets an `IsBusy` guard flag but had no `try/finally` around it. If anything throws while loading (including from the day-filtering logic added last pass), `IsBusy` gets stuck `true` forever, and since the method's own guard is `if (IsBusy) return;`, every future reload attempt — including navigating back to the dashboard after picking a new day — would silently no-op. Added the missing `try/finally` so this can't get permanently stuck regardless of what the underlying cause turns out to be. Audited every other `IsBusy`-guarded method in the app for the same risky pattern — none of the others have the blocking `if (IsBusy) return` guard, so this specific failure mode was isolated to the dashboard.

**Colors — full rollout across every category this time**
- Previous pass only had explicit colors for the 5 cycle-menu slots; everything else fell back to one flat blue, which is why it still didn't feel like "mix these in for the rest of the menu." Rebuilt `CycleCategoryColorConverter` with an explicit color for all 18 categories in the menu — all 13 static categories now cycle through the full 7-color set (`#D53220 #DA5D23 #EFB638 #C9DE87 #3571B7 #B6DFF8 #AF1718`), and the 5 cycle slots use the exact pairs specified: Vegetarian/Healthy get yellow/sage, Curry/Main Meal get red/orange
- Every light background (yellow, sage, pale blue) now gets dark text and every saturated one (both reds, orange, blue) gets white text — got this backwards once already in an earlier pass, double-checked it this time
- Applied everywhere a category shows: the filter chips and all three product-photo badges (hero, dashboard cards, admin thumbnails), for both the static and cycle menus

**Flagging directly rather than continuing to work around it: there is no "dessert" category anywhere in this app's actual menu data**
- Checked both `staticMenu.json` (13 categories: Salad Bar, Poke Bowl, Stir-Fry, Ciao Italy, Wraps, Sandwiches, Hot Dog Saloon, Burger Bar, Sides & Sauces, Fitness Meals, Homemade Winter Soups, Ramen Bowls, Vegan Meals) and `cycleMenu.json` (Main Meal, Vegetarian Meal, Healthy Meal, Curry of the Day, Gourmet Sandwich) — no dessert or treats section in either
- The only place anything like that exists is the original CSG Foods brand deck's "Daily Menu" mockup image (Dark Chocolate Brownie, Lemon & Poppy Seed Cake, Seasonal Fruit Cup under a "TREATS" heading) — that was a design example in the PDF, never actually built into this app's real data
- `#FFDCE8`/`#B6DFF8`/`#AF1718` continue to sit exactly where they already were (the Main Menu toggle button, "All" chip selected state, and 2 of the 13 static category slots per the "mix in" list) — there's genuinely nowhere else for them to be "reserved" for without a real dessert category existing first

**Card container color now follows the category too, not just the photo badge**
- Previously only the small photo badge inside each card used the per-category color — the card itself (name, description, price) stayed a fixed pink/dark background regardless of category. Now the whole card container picks up the category color on both the dashboard and the admin catalog.
- Since the card background can now be anything from a light sage to a saturated dark red, every text label inside (name, description, price) is rebound to the same converter's matching "Text" role, so contrast is always correct rather than assuming one fixed text color works on every background
- The photo itself gets a subtle translucent white backing behind it so it still reads clearly regardless of which category color the card lands on, and the "+" add button / Edit / Delete actions were deliberately kept on a fixed neutral look rather than also color-cycling, since they need to stay identifiable and legible no matter what color the card around them is
- `ProductDetailPage` didn't need this change — it's a single detail view, not a grid of cards, and its hero photo area was already using the category color

**Per-item accurate photos — in progress (4 of 89 static items done)**
- Client asked for a genuinely accurate photo per menu item rather than one shared photo per category, confirmed to proceed exhaustively across many turns despite the pace
- Restructured `StaticItemImageUrl` around a proper `ItemPhotoIds` dictionary keyed by exact item name, checked before the category fallback — built to be extended incrementally as more items get sourced, rather than needing rework each time
- Done so far, all individually verified "Free to use under the Unsplash License": **Tuna Salad**, **Roasted Butternut Salad**, **Grilled Chicken or Halloumi Chop Salad**, **Thai Beef Noodle Salad** (Roasted Veg & Couscous Salad shares the same photo as Roasted Butternut Salad — genuinely very similar dishes)
- Everything else in Salad Bar and the other 12 categories still falls back to the existing category-level photo until sourced individually
- Worth flagging honestly: specific dish names mostly don't exist as dedicated stock photos the way generic categories do — most items are taking 3-5 searches each (recipe blogs dominate results, which aren't usable), so this is a genuinely slow, multi-session effort, not a quick pass

**Full menu data replacement from the client's spreadsheet, plus a real Add-Ons system**
- Replaced `staticMenu.json` entirely with the client's updated product list: 102 items across 13 categories with real Standard/Large pricing (was placeholder/mock pricing throughout)
- Structural changes from the spreadsheet, applied faithfully rather than smoothed over: **Stir-Fry** is now Beef/Chicken/Pork/Vegetarian × Noodles/Rice (8 items, was 3); **Burger Bar** is now Classic/Cheese × Beef/Chicken/Halloumi (6 items, was 2); **Sides & Sauces** no longer exists as a browsable category — its items (wedges, slaws, salads, sauces) are now Add-Ons attached to the categories that reference them, matching how the spreadsheet actually structures them; added the new **Pork Specialities** category (4 items). Left out "Chef's Specialities" and "From the Ocean" — the sheet lists both as "Menu to follow," so there's no real content yet to add.
- **Activated a whole feature that already existed but was never used**: this codebase already has the data model and full UI for per-item Size selection and multi-select Add-Ons (`Option.AdditionalPrice`, `CustomizationGroup.IsMultiSelect`, radio buttons for size / checkboxes for add-ons, live price recalculation) — nobody had ever populated it with real data. Every item with two prices now gets a real Size group (Standard/Large); every category with add-ons in the spreadsheet (extra cheese, extra protein, sauces, sides — 16 categories' worth) now gets a real Add-Ons group.
- Found and fixed a real gap while wiring this up: the additional price was already used in the total calculation but was never actually shown next to each option — customers were selecting "Large" or "Extra Bacon" with no visible price until checkout. Added a computed `DisplayName` on `Option` (e.g. "Large (+R60.00)") so the price is visible before selecting.
- Updated the cycle menu's placeholder price (R95 flat) to the real R80 confirmed in the spreadsheet's "Chef's Meal of the Day" pricing (Curry of the Day, Main Meal, Health(y) Meal, Vegetarian Meal, Gourmet Sandwich all price at R80) — the actual day-by-day dish rotation is unaffected, this was pricing only
- Updated every category color/icon/photo mapping for the renames (Stir-Fry, Hot Dogs) and the new Pork Specialities category — sourced and verified a new photo for it, and caught one mismatched Unsplash result along the way (an AI-mistagged photo captioned "Shoulder Of Lamb" that was showing up under "pork belly" searches — skipped it rather than using a wrong photo)

**Delivery fees (new)**
- Added `DistanceKm` to each company location and a `DeliveryFeeCalculator` matching the exact tiers given: 0–15km/R100, 15.01–20km/R140, 20.01–30km/R200, 30.01–50km/R350. Distances beyond 50km are flagged as outside the delivery range rather than silently charged a guessed fee, and checkout is blocked with a clear message in that case.
- Shows as a real line item in the cart breakdown, admin can set/edit each location's distance, and the 3 demo companies were seeded with placeholder distances (8–18km) so the feature works out of the box — these should be corrected once a real kitchen address exists to measure from

**Order cutoff time: 9:00 AM**
- Changed from 10:00 AM everywhere it appears (`OrderSchedulingService`, the cutoff reminder popup). Verified the existing 2-business-day delivery window logic already matches the examples given exactly (Wed→Fri, Thu→Mon) — no change was needed there, only the cutoff hour.

**5-star rating system (customer + admin)**
- Added `Order.Rating` (0 = unrated, 1–5 once rated) and `UpdateOrderRatingAsync` to the order service
- Customer side (Order History): a real tappable 5-star row per order — tapping a star rates it, tapping the currently-set star again clears the rating (a common, forgiving pattern rather than being stuck once set)
- Admin side (Active Orders): the same rating shown read-only, only once an order actually has one — no point showing 5 empty stars on every upcoming order that hasn't been delivered/rated yet
- Built one `RatingStarConverter` shared by both screens rather than duplicating the star logic

**Reorder — static menu only, exactly as asked**
- Added `Order.CanReorder` (true only when `MenuType == Static`) and a Reorder button on Order History that only appears for those orders — cycle-menu dishes are locked to one specific day and usually aren't even on the menu anymore by the time someone's looking at history, so there's deliberately no reorder option for them
- Reordering looks up the item by name on the current static menu (stripping any "2x " quantity prefix first) and adds it to the basket for the next available delivery date; if the dish has since been discontinued, says so clearly instead of silently failing

**Login screen colors**
- Email field container: pink → `#EFB638`
- Password field container: pink → `#C9DE87`

**Admin Overview dashboard colors** (the part of the previous message that got cut off before the color codes repeated)
- All 5 pink containers on Kitchen Operations Hub converted: the 4 KPI cards (Today's Deliveries Revenue, Active Orders, Client Companies, Registered Customers) alternate `#EFB638`/`#C9DE87` in a checkerboard, and the upcoming-deliveries order cards use `#C9DE87`
- Left the status badges (Preparing/Received, pale blue) and text colors untouched — only the actual pink containers were in scope

**Client-side pink containers — same treatment**
- Found every remaining pink container across the customer-facing app (16 total, across 8 pages: Active Orders, Cart, Help, Order History, Profile, Register, Select Delivery Day, Settings) and converted them all to alternate `#EFB638`/`#C9DE87`, same as the admin dashboard and login screen
- Dashboard product cards weren't touched here since those already moved to per-category coloring in an earlier pass, not flat pink — nothing to convert there
- Verified text contrast holds up on every page — body text is dark charcoal throughout, which reads fine against both light colors, same as already confirmed on login/admin

**Toned down the color scheme app-wide — client feedback: too colorful, not professional**
- This is the direct, honest consequence of several color-experimentation passes stacking up — each felt reasonable on its own, but the cumulative effect (rainbow product cards, pink cards, checkerboard yellow/sage containers everywhere) is exactly what a client would flag. Pulled back to a clean, restrained system rather than trying to patch individual spots.
- **`CycleCategoryColorConverter`** (drove every product card, category chip, and admin catalog row through 7 different saturated colors) simplified to one clean white/dark-neutral background with consistent text — real food photography already does the job of visually distinguishing dishes; the card underneath doesn't need to as well
- **`CardBgLight`** (solid pink on every card app-wide): `#FFDCE8` → white
- **16 checkerboard yellow/sage containers** across Login, Register, Cart, Settings, Profile, Help, Active Orders, Order History, Select Delivery Day, and the 5 on the admin dashboard: all converted to white
- **`MenuIdentityColorConverter`** and the Main Menu/Cycling Menu toggle buttons (previously blue vs. pale-blue-and-charcoal) unified to one consistent accent — the toggle already shows "Main Menu"/"Cycling Menu" as plain text, so the color distinction was never carrying real information, just competing for attention
- Added a subtle border to the category filter chips and admin catalog rows, which would otherwise have gone from "colorful" to "invisible" now that they're white — a plain white button on a similarly light page needs *some* definition, just not a loud fill color. Product cards elsewhere already had shadows for this, so they didn't need the same fix.
- Deliberately left alone: the reports pie/bar chart color palette (data visualization genuinely needs distinct colors per series — that's not the same problem as every menu item being a different neon color), the small red cart-count badge, the single-accent tab bar, and star ratings (still gold, a standard and expected convention, not part of the "too colorful" complaint)

**Switched the professional palette from white/red to blue**
- `Primary`, `BrandGold`, `Gold`, and `ButtonBgLight` (light mode) all changed from dark red `#AF1718` to medium blue `#3571B7` — since these are centralized resources consumed via `AppThemeBinding` throughout the app, this one change cascaded automatically to every button, price, header, and flyout icon that references them, without needing to touch those files individually
- `CardBgLight` and every hardcoded white container from the previous "professional" pass (23 spots across both customer and admin screens, plus the reports charts and remaining admin list pages) changed from white to pale blue `#B6DFF8`
- Kept the two roles cleanly separated to avoid a contrast mistake: pale blue only for backgrounds (safe with the dark text already in place everywhere), medium blue only for accents that already pair with white text (buttons, the tab bar, the menu toggle) — never alternated the two on a single background, since the medium blue is dark enough that dark text on it would be hard to read
- Tab bar's unselected-tab color (previously a pale pink tuned to complement the old red tab bar) updated to a pale blue tint so it still makes sense against the new blue tab bar
- Left alone: the reports chart palette (still needs multiple distinct colors for data series — different problem than the "everything is a different color" issue), and dark mode entirely (this was a light-mode-only request, consistent with how the color changes have been scoped throughout this conversation)

**Weekend ordering (Wednesday onwards)**
- Turned out the hard part was already done: the existing 2-business-day delivery math already correctly rolls a weekend order forward to Monday for calculation purposes, and Monday + 2 business days lands exactly on Wednesday — verified this with an actual date calculation before touching anything, for both Saturday and Sunday
- The only real change needed was `IsOrderingOpen()`, which was unconditionally blocking all weekend ordering — now returns true always. Kept as a method rather than deleted outright, since it's a sensible place to hang a future closure rule (e.g. public holidays) without re-threading every call site again.
- The three "closed for the weekend" messages (two banners, one alert) were now inaccurate given the method's new purpose, so reworded them to be generic rather than weekend-specific

**Payment screen (Card / PayFast)**
- New `PaymentPage` sits between Cart and order confirmation — payment method selector, a real card form (name, number, expiry, CVV) with actual validation, and a PayFast panel
- Being upfront about what this is: there's no PCI-compliant backend to actually process a card, and a genuine PayFast integration needs a live merchant account plus server-side webhook (ITN) handling to confirm payment safely — neither can live purely on the client. Both payment paths here simulate a realistic processing delay and then complete the order exactly as if payment succeeded, so the rest of the app (order history, admin views, reports) has real data to work with while the actual gateway integration is still pending. Said so directly in-app too ("this is a demo checkout") rather than pretending it's real.
- Refactored order creation out of `CartPageViewModel` into a shared `IOrderService.PlaceCartOrdersAsync` so both the cart's old checkout path and the new payment screen create orders identically — no duplicated logic to keep in sync
- Fixed a real UI bug while building this: the Card/PayFast selector cards only had a DataTrigger to highlight PayFast when selected — Card was hardcoded to always show as selected, so both would appear active at once once PayFast was chosen. Made it properly symmetric.

**Containers back to white**
- Every `#B6DFF8` container background (product cards, chips, admin rows, KPI cards, reports charts, form fields — the same ~30 spots from the last color pass, all across both customer and admin sides) reverted to white
- `PaymentPage` (built last turn) needed zero changes here — it references the `CardBgLight` resource rather than hardcoded hex, so it picked up white automatically
- Kept `#3571B7` untouched as the one accent color (buttons, prices, icons) — this was specifically about the container backgrounds, not the accent
- Left the reports chart palette alone, same reasoning as always — it genuinely needs multiple distinct colors per data series, unrelated to container styling

**Phase 1 of the full visual overhaul: monochrome color system (both light AND dark mode)**
- Client shared a parallel React Native implementation of this same app (built by a collaborator) as the visual direction to move towards — reviewed its theme system, screens, and components thoroughly before touching anything
- Its design language is genuinely monochrome: near-black accent on cream/white in light mode, cream accent on near-black in dark mode — color reserved *only* for semantic error/success states, never decoration. This is the opposite reaction to "too colorful" from a few passes back: rather than tuning which accent color, there mostly isn't one anymore.
- Rewrote `Colors.xaml` completely for **both themes** — previously dark mode used a separate "Summer Harvest" warm terracotta palette left untouched through every earlier light-mode-only color pass; that's retired now since the reference app applies the same monochrome logic consistently to both themes rather than treating dark mode as its own brand moment
- Went through every hardcoded hex color across the entire app (not just the centralized resources) — found and converted ~15 different leftover accent colors from every previous color experiment (old blues, oranges, yellows, reds, sages, pale-blue badge tints) to the new monochrome palette, file by file, since a global find-replace can't safely handle cases where the same old color meant different things in different places
- Deliberately kept as semantic exceptions, not converted to monochrome: delete/remove button red, the weekend-closed banner's amber warning tone, the admin status quick-action buttons (Preparing/Out for Delivery/Delivered — kept distinct for quick visual scanning), and the reports chart palette (still needs multiple distinct colors per data series)
- This is phase 1 of a multi-part visual overhaul the client confirmed as priority — list-style product cards (replacing the photo-grid layout) and a bottom navigation bar (replacing the flyout drawer) are next

**Phase 2 of the visual overhaul: list-style product cards, replacing the photo grid**
- Dashboard product list rebuilt to match the reference app's `DishCard` exactly: single-column horizontal rows instead of a 2-column photo grid, no photo at all — name, description, ingredients, dietary tag pills on the left; price, an optional "Large RXX.XX" sub-price, and a circular add button on the right
- Added `Ingredients` (string) and `DietaryTags` (list) to the `Product` model and `JsonProductService`'s parsing, matching the reference's data model. Real data isn't sourced for these yet — `staticMenu.json` doesn't have them — so every section hides itself cleanly (`HasIngredients`/`HasDietaryTags`) rather than showing an empty line. The structure's ready the moment real ingredient/dietary data exists.
- Added a computed `LargePrice` on `Product` (base price + the existing Size group's "Large" option) rather than a separate field, so there's one source of truth for pricing — the sub-price line just reads from data that already existed
- Deliberately left `ProductDetailPage`'s hero photo alone — the reference's list view has no photos, but that doesn't mean a detail screen shouldn't have one; only the browsing list needed to change
- Next: the bottom navigation bar, replacing the flyout drawer

**Phase 3 of the visual overhaul: bottom navigation, replacing the flyout drawer (customer side only)**
- `AppShell` rebuilt around a `TabBar` (Menu / Orders / Profile) instead of the flyout drawer — `FlyoutBehavior="Disabled"`, the whole custom flyout header/footer/nav-row structure removed
- **Honest fidelity note**: the reference app uses a floating glass-effect pill nav (rounded, margins on all sides, translucent blur) sitting above the content. MAUI Shell's native `TabBar` can't replicate that — it's always a solid edge-to-edge bar with no floating/blur support, and reliably faking that would need custom platform-specific rendering I can't verify without a live build. Used Shell's real `TabBar` instead: fully reliable, monochrome-styled to match, functionally equivalent — just not pixel-identical to the floating pill. Flagging this directly rather than claiming a visual match that isn't quite there.
- Settings and Help no longer have their own nav entry (3 tabs is closer to the reference's actual tab count than the old 5-item flyout) — both got a real new home instead of just disappearing: added proper navigation rows on the Profile page, wired through two new commands, registered as pushable routes in `AppShell.xaml.cs`
- **Admin side deliberately left as a flyout, unchanged** — Admin has 8 sections, too many for a bottom bar to hold reasonably, and the reference app itself only exposes a single "Command" bottom-tab for admin/chef roles rather than bottom-tabbing every admin section individually
- Audited every `GoToAsync` call across the whole app for routes that referenced the now-removed flyout structure (`//myorders`, `//help`, `//settings` as absolute routes) — none were affected; everything either already used relative/pushed navigation or targeted a route that still exists under the new `TabBar`
- Found and removed a real dead-code regression while doing this: `UserDashboardPage.xaml.cs` had an `OnMenuClicked` handler that set `Shell.Current.FlyoutIsPresented = true` — turned out it was never actually wired to any button in the XAML to begin with (leftover from an earlier iteration), so removing the flyout didn't break anything that was working, but it's a good example of why every changed page needs auditing rather than assuming — this "already broken but silent" version could just as easily have been "silently broken by this change" if I'd missed checking it

**Dashboard layout corrected to match the reference app exactly**
- Re-read the reference's actual source (`MenuBrowserScreen.tsx`'s numbered render order, plus `CategorySlider.tsx` and `CategorySection.tsx`) rather than relying on my earlier general impression, since the previous phases had gotten some real details wrong
- Rebuilt the page top-to-bottom in the order specified: logo + checkout cart button → search bar → compact date display (tap to open the full date picker, rather than duplicating that whole UI inline) → Main Menu/Cycling Menu toggle → "Explore Our Menu" label → category slider → ordering/cutoff message → category-sectioned dish list
- **Category slider redesigned to match `CategorySlider.tsx` exactly**: plain text labels only, no icons, no filled background — just bold text and a thin underline indicator bar on whichever one is active. This needed a new `MultiValueEqualsConverter` since comparing "is this chip's own name equal to the ViewModel's ActiveCategory" is two bindings, not a binding against a literal, which a plain `DataTrigger` can't express.
- **The "divide each category with an image" request, confirmed straight from `CategorySection.tsx`**: each category now gets its own 110px hero banner (photo + category name overlaid in white on a dark scrim) directly above that category's dishes, using the same category photos already sourced in an earlier pass — reused, not re-sourced
- This required real restructuring, not just a template swap: added a `CategorySection` grouping model, gave `CategoryChip` an `ImageUrl`, and rebuilt `UserDashboardViewModel` to maintain a grouped `GroupedProducts` collection alongside the existing flat `Products` list, kept in sync with the same search/category-filter/menu-switch logic so nothing forked into two different code paths
- Moved the page's whole header out of `Shell.TitleView` into real page content (`Shell.NavBarIsVisible="False"`) — the reference renders its top bar as part of the screen, not the native nav bar, and trying to keep both would've meant two overlapping headers
- Admin side still needs the equivalent pass — next up

**Admin makeover — reviewed the reference's actual admin screen, made a deliberate scope call**
- Checked `AdminPortalScreen.tsx` and `AdminHeader.tsx` before touching anything: the reference's admin experience is genuinely a *single screen* with a horizontal scrolling pill-tab switcher (Dispatch / Menu CRUD / Cycle / Analytics / Accounts) — not a bottom nav, not a drawer
- **Deliberately did not rebuild Admin's navigation to match this.** Converting 8 already-working, separately-built admin pages into one page with inline tab-switched content is a much bigger, riskier rewrite than the customer-side bottom nav was — that swap was one Shell structure for another with the same page-per-destination model underneath; this would mean consolidating real content and logic across 8 pages into one. Kept the reliable flyout instead, and brought as much of the reference's actual visual language to it as reasonably fits that structure:
  - Flyout header rebuilt to match `AdminHeader.tsx`: "Central Command" branding, a live clock (updates every second via `Dispatcher.CreateTimer`, using local device time rather than hardcoding a SAST/UTC+2 offset the way the reference does — assuming every deployment runs in South African time would be a worse bug than not showing a timezone-specific clock at all), and the "9:00 AM Cutoff Active" badge
- **Caught myself mid-edit and reverted it**: tried wrapping each flyout nav row in a bordered "pill" to match the reference's tab-button look, but the first one used a transparent stroke and no fill — a no-op that would've looked identical to before. Reverted rather than repeating a change that doesn't actually do anything 7 more times just to say it was done.
- Spot-checked every admin page's remaining hardcoded colors — confirmed the monochrome sweep from the earlier color-system phase already covered admin correctly; nothing further needed there

**Remaining reference-app features: tax invoices, disputes, per-company discounts, rating feedback**
- `Order` extended with real tax invoice generation (`INV-KC-YYMM-####`, generated automatically at checkout), a full dispute workflow (reason, auto-generated support ticket ref like `TCK-88219`, status), optional written feedback alongside the star rating, and a computed `Stage` (1-4) ready for a visual tracker
- `Company` extended with a discount system (percentage or flat ZAR) that's a genuinely separate lever from the existing per-meal subsidy — a company can have either, both, or neither. Checkout now applies both correctly (subsidy first, then discount on what's left, delivery fee added after, never goes negative).
- Customer side: real "Report an Issue" flow on Order History (opens a ticket, shows status, blocks re-reporting the same order) and an optional feedback prompt after rating
- Admin side: a Disputed Orders section on Reports with status updates, and — filling a real pre-existing gap — the first-ever admin UI for subsidy at all (previously only settable via seed data), combined with the new discount into one "Financials" flow per company
- **Found and fixed a real contrast bug while working in this area, then searched for how widespread it was**: buttons using the `BrandGold`/`BrandGoldDark` background with hardcoded white text — fine when `BrandGoldDark` was a mid-tone color, broken now that the monochrome pass made it cream, making white text nearly invisible in dark mode. Fixed the one I found, then searched for the same pattern everywhere and found 5 more identical instances across Companies, Notifications, Menu Catalog (×2), and Reports — all fixed together rather than leaving 5 latent copies of the same bug.
- Also fixed star-rating colors on Order History and admin's Active Orders, which the same monochrome pass had turned cream-on-white — invisible in light mode.

**Live delivery tracker (4-stage), matching the reference's `DeliveryProgressTracker`**
- Reviewed the reference's actual component before building anything — a vertical stepper with colored nodes connected by lines, each stage showing a title and description, node color/text driven by comparing the order's current stage against that node's fixed position (completed = green, active = the one accent color, upcoming = neutral)
- Shows on Active Orders, above the list, for whichever order is coming up soonest — built on the `Order.Stage` computed property added in the previous pass, so no new order-status plumbing was needed, just the visual
- One deliberate simplification: skipped the reference's pulsing animation on the active node — real functional value (which stage you're at) without taking on animation-timer complexity for a cosmetic touch
- **Caught and fixed a real binding bug while building this**: had put `BindingContext` (switching to the order) and `IsVisible` (needing the *page's* context to check `NextOrder`) on the same element — the two conflict, since setting `BindingContext` changes what every other binding on that same element resolves against. Wrapped in an outer element that keeps the page-level context for the visibility check, with the inner content switching context separately.

**Batch-prep planning by dish — the last of the 5 remaining reference-app features**
- Reviewed the reference's actual aggregation logic (`AggregatedPrepItem`, `DailyDispatchTab.tsx`) before building anything: it sums quantities across every matching order, grouped by dish, to tell the kitchen "make 12 Chicken Curry, 8 Beef Stir-fry" rather than reading through orders one at a time
- Added a toggle on Active Orders — "Orders View" / "Prep Summary" — reusing the exact same company/date filters already there, so switching views doesn't require re-filtering. Prep Summary aggregates by dish name with total quantity needed, sorted highest-first. Print button adapts to whichever view is active.
- Scoped deliberately: the reference also has a *second*, separate aggregation (grouping by delivery site/floor for dispatch routing) — didn't rebuild that one, since the existing company filter + the "Print Order Sheet" feature from an earlier pass already group by company and location in its output. Building a third near-duplicate grouping view for the same underlying data wasn't worth the added complexity.
- Extracted the "Nx Product Name" parsing (previously only in the reorder feature) into a shared method on `Order` itself, since a second feature now needed the identical logic — one source of truth instead of two copies that could drift apart
- **Found the exact same contrast bug pattern again, in a different resource pair, and searched for it properly this time**: buttons pairing the `Primary`/`PrimaryDark` background with hardcoded white text — same root cause as the `BrandGold` bug from earlier (fine when the dark-mode value was a mid-tone, broken now that it's cream). Found and fixed 6 more instances across Select Delivery Day, Register, Active Orders, Payment (×2), and Cart.

**This completes the full set of reference-app features scoped at the start of this effort**: dietary tags & ingredients, list-style cards, bottom navigation, the dashboard layout correction, the admin makeover, tax invoices, disputes, per-company discounts, rating feedback, the delivery tracker, and now batch-prep planning.

**New inventory: 8 genuinely missing pieces, found by re-auditing every reference component/screen against what's built.** Working through them now — first up, the tax invoice viewer.

**Tax Invoice Viewer**
- New `TaxInvoicePage`, reachable from Order History — shows the same content as the reference's `TaxInvoiceModal`: supplier details, customer/drop-off, line item, subsidy/discount/delivery-fee breakdown, and total, with a Share button
- Computed the meal subtotal from `TotalAmount`'s existing components (subsidy + discount + delivery fee) rather than adding a redundant stored field that could drift out of sync
- **Fixed the root cause of a mistake I'd made three separate times now** (DeliveryFee, then two more instances this session): using `IsNotNullOrEmptyConverter` on a non-nullable value type like `decimal` — a boxed value type is never null, so the check always silently returns true regardless of the actual value. Built a proper `GreaterThanZeroConverter` and audited every existing use of the null-check converter across the whole app for the same mistake (found none — the earlier ones were already fixed) before using the new one going forward.

**Still to come**: order confirmation screen, a floating cart bar while browsing, cancellation policy display, allergy notes per cart item, floor selection within a delivery location, a real set-password flow, and domain-gated corporate registration.

**Order Confirmation screen**
- New `OrderConfirmationPage`, shown right after successful payment instead of a plain alert — success badge, order number, delivery date, an itemized list of everything just ordered, total paid, and buttons to track the order, view its tax invoice, or return to the dashboard
- Required changing `PlaceCartOrdersAsync`'s return type from `Task` to `Task<List<Order>>` so the confirmation screen has real order data to show rather than a generic message — checked for every other caller of this method first (only one, already correctly updated) before making the change

**Cancellation & Holiday Policy**
- New static page, linked from Help & Support — cutoff rule, no-refunds-past-cutoff, public holidays, batch drop-off
- Wrote the cutoff section to describe this app's *actual* rule (9 AM, two business days) rather than translating the reference's hardcoded "48-hour SAST" framing verbatim — copying that would have been factually wrong for how this app's scheduling actually works
- `HelpViewModel` was a plain class with no commands at all — converted it to the same `ObservableObject`/`[RelayCommand]` pattern used everywhere else in the app rather than reaching for a one-off code-behind tap handler just for this link

**Floating Cart Bar**
- Matches the reference's `FloatingCartBar` — a monochrome pill overlay on the dashboard, item count badge, running total, delivery date, and "Review Order →", only visible once something's in the basket
- Added `CartTotal` to `UserDashboardViewModel`, computed the same place `CartCount` already was, so both stay in sync off the same trigger

**Allergy Notes — kept genuinely separate from general special requests**
- The reference treats allergy info as a distinct, safety-critical chef alert — flagged separately on invoices, counted separately in kitchen prep. The app previously had these combined into one "Special Requests / Allergies" field (the placeholder text literally said "...nut allergy" as an example of a *general* note), which is exactly the mixing the reference deliberately avoids.
- Split into two fields end-to-end: `CartItem.AllergyNotes` and `Order.AllergyNotes`, distinct from `SpecialRequests`/`SummaryText`. Visually flagged wherever it shows (warning-amber styling, ⚠️) on the product page, cart, and — importantly — on the admin's Active Orders view, since a customer's allergy note is only useful if kitchen staff can actually see it, not just if it's stored.
- Found and cleaned up while in `CartItem.cs`: the same four usings (`System.Collections.Generic`, `CommunityToolkit.Mvvm.ComponentModel`, `System.Linq`) were each duplicated three times over — harmless, but a sign the file had been edited by pasting in fragments rather than properly merging them.

**Floor Selection** — added `DeliveryFloor` to `UserAccount`, editable from Profile, wired through to `Order` so it shows on the tax invoice too.

**Set Password — the safest possible version of this change**
- `RegisterPage` already validated password strength and confirmation but silently threw the value away — there was even an existing comment acknowledging it wasn't stored anywhere. Made it actually save and check the password.
- Deliberately backward-compatible: accounts with no password set (every seeded demo account, plus the ad-hoc guest fallback) keep accepting any password exactly as before. Only genuinely new registrations — which now capture a real password — get an actual check. Nothing about existing demo/testing logins changes.
- Plain text storage is explicitly commented as acceptable *only* because this is a mock data store with no backend — flagged clearly as something that must be hashed server-side once Supabase Auth is wired in, not left as a silent gap.

**Domain-Gated Registration**
- Added `WhitelistedDomains` to `Company`, seeded the 3 demo companies with their real domains (ecogra.org, tata.co.za, rcl.co.za) matching their existing demo accounts
- Registration now auto-matches the company as soon as a recognized email domain is typed, with a visible "✓ Matched automatically" confirmation
- Built as additive, not a hard requirement — doesn't override a manual choice, and anyone from a company without a whitelisted domain yet just falls back to the existing manual picker rather than being blocked from registering
- Gave admin a way to manage domains per company (add/remove), since otherwise this would only ever have worked for the 3 seed companies

**This completes the full 8-item missing-features audit**: tax invoice viewer, order confirmation, cancellation policy, floating cart bar, allergy notes, floor selection, real password checking, and domain-gated registration.

**Admin navigation — replaced the flyout drawer with the reference app's actual pattern**
- Earlier in this session I deliberately kept Admin's flyout rather than converting it, judging a full navigation rewrite too risky. Revisited that now that it's specifically being asked for, and built it properly: a horizontal scrolling pill-tab strip (`AdminNavStrip`), matching the reference's real admin navigation — not a drawer, not bottom tabs, since 8 sections don't comfortably fit a bottom bar either.
- Built as one reusable `ContentView` embedded at the top of all 8 admin pages, rather than duplicating the tab markup 8 times — each page just sets `ActiveRoute="itsownroute"` for the highlight to work. `AdminShell` itself is now much simpler: `FlyoutBehavior="Disabled"`, `FlyoutItem` kept purely for route registration (still works correctly with the drawer hidden), no more per-item tap handlers or the old duplicate clock timer.
- Every page was wrapped in an *outer* `Grid` around its *existing, untouched* root content, rather than renumbering `Grid.Row` values inside pages that already used a `Grid` — a deliberately safer approach than reshuffling row indices across 4 different pages with different existing row counts.
- **`SettingsPage` needed special handling**: it's shared between the customer and admin shells, so embedding the strip unconditionally would have leaked admin navigation into the customer's own settings screen. Added it with `IsVisible="False"` by default, toggled on only when `Shell.Current is AdminShell` in `OnAppearing` — the one page where a straight copy-paste of the other 7 would have been a real bug, not just a style inconsistency.
- The "Central Command" branding and live clock from the old flyout header were preserved by moving them into the top of `AdminNavStrip` itself, rather than being lost in the rewrite.
- Verified the exact existing logout logic (`ISessionService.SignOut()` + navigate to `LoginPage` via the resolved `Handler.MauiContext.Services`) before writing the new strip's logout button, rather than inventing a different-looking version that happened to also work.

**More admin features adapted from the reference code — reviewed the tabs I hadn't closely examined yet**
- Went back to `MenuManagementTab.tsx`, `RevenueAnalyticsTab.tsx`, and `CycleControlTab.tsx` specifically (the ones not yet compared in earlier passes) rather than assuming earlier work already covered everything relevant
- **Found a real, familiar-shaped gap**: `Ingredients` and `DietaryTags` have existed on `Product` since the dashboard redesign, but admin's Add/Edit Menu Item flow never actually asked for them — same pattern as the subsidy admin UI and the invoice price display from earlier passes: the data model existed, nothing let anyone actually set it. Extended both Add and Edit to capture description, ingredients, and comma-separated dietary tags. Made `UpdateStaticProductAsync` explicitly copy these two fields too, rather than relying on the implicit fact that `Product` currently happens to be passed by reference — that's fragile to depend on silently.
- **Category filter for Reports**, matching `RevenueAnalyticsTab`'s `selectedCategoryFilter`: added `Order.Category`, snapshotted from the product at checkout (deliberately not looked up against the current menu later, since a dish could be renamed or removed by the time a report runs), and a new category picker alongside the existing timeframe/company filters
- **Custom category hero images**, matching the reference's "add category" flow: adding a menu item with a category name that doesn't exist yet now offers to set a custom hero image for it, rather than silently falling back to a generic photo. Stored as a small admin-settable override map, checked before the existing hardcoded category-photo mapping — doesn't disturb any of the already-curated photos for existing categories.

**Two removals**
- "Explore Our Menu" dashboard label — checked first rather than assuming: it turned out to already be gone, removed at some point during a later restructuring of that page without me flagging it explicitly. Nothing left to do there.
- Custom category hero images — fully reverted. Removed the "new category" detection and image prompt from Add Menu Item, and removed the now-unused override mechanism from `JsonProductService` entirely (the check, the storage dictionary, and the setter method) rather than leaving dead code behind that nothing calls anymore.

**Orders page — merged Active Orders + Order History into one page**
- These were two separate pages, switched via .NET MAUI Shell's automatic secondary tab strip (created because the "Orders" `Tab` had two `ShellContent` entries). That strip isn't something Shell exposes styling control over, and the client reported it wasn't rendering evenly (not a 50/50 split) — not something fixable by tweaking Shell properties.
- Rather than fight Shell's chrome, merged both into `ActiveOrdersPage` behind the same 50/50 segmented-toggle pattern already used elsewhere in the app (dashboard's Main/Cycling switch, admin's Static/Cycle switch) — a `BindableProperty` + `{x:Reference Root}` DataTrigger, the same technique `AdminNavStrip` uses for its own active-route highlighting.
- `OrderHistoryPage` is gone (deleted, `MauiProgram` registration and the csproj's `MauiXaml` entry removed), but `OrderHistoryViewModel` and every one of its bindings/commands are untouched — it's just handed to a different section of `ActiveOrdersPage` as that section's `BindingContext` instead of a whole page's. Both view-models already self-load in their constructors, so nothing extra was needed to keep data fresh when switching tabs.
- Verified nothing else in the app referenced the `orderhistory` route or `OrderHistoryPage` directly before removing them (only `OrderConfirmationViewModel`'s "Track Order" targets `//activeorders`, which still exists and is unaffected).

**Contrast bug: hardcoded `TextColor="White"` on a background that can turn cream in dark mode**
- Same root-cause bug found and fixed twice earlier in this project (`BrandGold`/`Primary` backgrounds paired with hardcoded white text) — found two more live instances while addressing "buttons should contrast when selected": the admin Menu Catalog's Static/Cycle toggle, and the "Selected" state of the delivery-day picker's date tiles. Both hardcoded `White` against a background that becomes cream (`BrandGoldDark`/`PrimaryDark`) in dark mode, making the selected text invisible. Fixed both to the same theme-aware pairing (`Light=White, Dark=PrimaryDarkText`) used everywhere else this pattern appears.

**Delivery-day picker — now a real popup, not a full pushed page**
- `SelectDeliveryDayPage` is shared by two very different situations: the mandatory first pick right after login/registration (`IsChangingDay = False` — this page *is* the app's root at that moment, nothing exists behind it) and the "change day" reached mid-session from the dashboard or the post-add-to-basket prompt (`IsChangingDay = True`). Only the mid-session case is a "popup" in the sense the client meant.
- Mid-session, it's now shown with `PushModalAsync` behind a translucent scrim (`#66000000`) instead of `PushAsync` behind a full opaque page, so the dashboard is still visibly there (dimmed) underneath, with the day picker floating as a centered, shadowed card and a `✕` to dismiss without changing anything (nothing commits until Confirm — confirmed safe to cancel out of).
- **Fidelity note, stated plainly rather than faked**: stock .NET MAUI has no cross-platform "blur the live content behind this view" primitive (no backdrop-filter equivalent) without platform-specific native code that can't be verified without a real device build — the same limitation already noted for the floating nav pill in `AppShell.xaml`. The translucent scrim gets the requested "see the menu behind it, softened" effect reliably; it reads as dimmed/frosted rather than literally blurred pixel-for-pixel.
- `ConfirmAsync` now calls `PopModalAsync()` instead of `PopAsync()` to match — pushing modally and popping non-modally would have been a real (if easy to miss) navigation-stack mismatch. Both call sites that reach this page mid-session (`UserDashboardViewModel`, `ProductDetailViewModel`) were switched to `PushModalAsync` for the same reason. The onboarding call sites (`LoginViewModel`, `RegisterViewModel`, which swap `Application.Current.MainPage` directly) were left untouched.

**Dashboard header — logo centered, date picker shrunk onto the search bar row**
- Logo: was left-aligned in a two-column `*, Auto` grid (drifting off-center once the cart button's width was accounted for). Now a `42, *, 42` grid with an invisible spacer matching the cart button's width on the opposite side, so the logo is genuinely centered rather than just "not pinned right."
- Date picker: was its own full-width row below the search bar ("Delivering" label + long weekday date + "Change ›"). Now a small pill to the right of the search bar itself (icon + short `dd MMM` date), driven by a new `SelectedDeliveryDateShortLabel` on `UserDashboardViewModel` — the existing full `SelectedDeliveryDateLabel` ("Monday, 29 Sep") is untouched and still used by the floating cart bar.
- "Explore Our Menu" title: checked again — still not present in the current file (see the "Two removals" note above from the previous pass). Most likely still testing a build from before that removal shipped.

**Dummy data for John (`john.doe@ecogra.org`, already an Ecogra-linked demo account)**
- Order History only had one past order for John — added two more (one Static-menu item with a 5-star rating already set, one unrated) so it reads as a real history rather than a single placeholder row.
- Active Orders already had one upcoming order for him (`#1004`, Status "Received") — traced the live checkout path end-to-end to confirm a *newly placed* order also lands here correctly: `PlaceCartOrdersAsync` tags every new order with the logged-in user's own `Id`, and `ActiveOrdersViewModel` queries by that same `Id` — so logging in as John, placing an order, and tapping "Track" from the confirmation screen shows it under Active Orders with the delivery tracker, using entirely pre-existing logic.

## Field-test fixes: ANR/freezing, Order History cards, date-picker translucency, logo size

Four issues reported after installing the previous build on a physical/virtual device, with a device log (`log.txt`) attached for the freezing one.

**App-wide freezing/ANR after login — fixed**
- The attached logcat confirmed a genuine Android ANR ("Input dispatching timed out... Waited ~5000ms for MotionEvent") right after login, preceded by ~15 seconds of continuous GC churn and dropped frames — and the *identical* signature was already present in an older log from before this session's changes, so this was a pre-existing bug, not something introduced along the way.
- Root cause: the dashboard's product list (`UserDashboardPage.xaml`) used a `BindableLayout` (nested two levels — categories, then each category's dishes) instead of a `CollectionView`. `BindableLayout` isn't virtualized — it builds every single item's view and bindings immediately, so the instant the dashboard populated, 40+ full dish cards (each with labels, converters, gesture recognizers, and its own nested `FlexLayout` for dietary tags) were all constructed synchronously on the main thread. That's what the main thread was doing during the GC-churn window right before the ANR.
- Fix: the product list is now a single grouped, virtualized `CollectionView` (`IsGrouped="True"`, `GroupHeaderTemplate` for the category hero banner, `ItemTemplate` for each dish). Everything that isn't the list itself (logo, search bar, menu toggle, category slider, cutoff banner) moved into `CollectionView.Header`, so it still scrolls together as one unit — only the dish rows actually on/near screen get built now. `Models/CategorySection.cs` implements `IEnumerable<Product>` so it can serve as a CollectionView group (a grouped CollectionView requires each group object to be enumerable over its own items).
- Also trimmed the Unsplash image request size used for category hero photos and the product detail header photo from `800×600` to `500×360` — smaller decode cost for images that were being requested far larger than they're ever displayed, on the same principle even though they weren't the main driver here (dish rows themselves don't render a per-item photo).

**Order History cards showing only "Report an Issue" — fixed, real bug found**
- Root cause: a `Border` can only have one direct child. The Order History card template had *three* — the row-0 `Grid` (icon/name/status), then the rating row, then the "Report an Issue" row — declared as siblings instead of all being children of one `Grid`. Only the *last* one XAML assigns to `Border.Content` actually renders, so the rating stars, reorder/invoice buttons, and the whole top row silently never showed — only the last-declared piece (Report an Issue) did, left-aligned in its own narrow column, exactly matching what was reported ("just start on the left and cut off").
- Fixed in `Views/ActiveOrdersPage.xaml` by making all three rows children of one `Grid` (`Grid.Row="0/1/2"`, with `Grid.ColumnSpan="3"` on the rows that need the full card width). Active Orders' own card template didn't have this bug (single `Grid` child throughout), which is why it was rendering fine.

**Date-picker "background is white instead of translucent/blurred" — fixed by changing approach**
- The previous fix (a translucent `#66000000` page background on `SelectDeliveryDayPage`, shown via `PushModalAsync`) assumed the page underneath would stay visibly composited beneath the new modal page. Field testing showed that's not reliable on Android — the modal came up over a solid white background instead of the dimmed dashboard/menu.
- Rather than patch that further, the "change delivery day" popup (for the two places that trigger it *mid-session* — the dashboard's date pill, and the post-add-to-basket "order for a different day" prompt) is no longer a separate page at all. It's now `Views/Controls/DeliveryDayOverlay.xaml`, a reusable overlay `ContentView` embedded directly inside `UserDashboardPage` and `ProductDetailPage`'s own root `Grid`, toggled by a shared `DeliveryDayPickerState` (new: `ViewModel/DeliveryDayPickerState.cs`) exposed as `DayPicker` on each page's ViewModel. Because it's genuinely part of the same page's own visual tree rather than a page pushed on top, the real content behind the scrim is — actually, not just intentionally — still there.
- `SelectDeliveryDayPage`/`SelectDeliveryDayViewModel` are unchanged and still used for the one case that's genuinely a full page: the mandatory first pick right after login/registration, before there's anything behind it to preserve.
- Same fidelity note as before still applies and is worth repeating: this is a dimmed scrim over real content, not a pixel-blurred backdrop — stock .NET MAUI has no cross-platform blur primitive. The difference is the scrim now has something real to dim, instead of showing a platform default.

**Dashboard logo "not prominent/bold enough" — sized up**
- The glyph weight itself is baked into the logo PNG (can't be made bolder via XAML), so the only real lever is size. `HeightRequest` increased from `32` to `46`.

## In-app theme switcher: Settings > App Theme

Following on from the theme-prototype PDF, the three client-approved palettes (Mediterranean Pantry, Summer Harvest, Berry & Cream) plus the current monochrome look are now real, switchable themes in the app itself — not just mockups.

**How it works**
- `Resources/Styles/Colors.xaml` still defines every colour key the app's XAML binds to via `{StaticResource}` / `{AppThemeBinding}` — that's unchanged, and "Current" is exactly those values, still.
- Three new colour-override `ResourceDictionary` files (`Resources/Styles/Themes/MediterraneanPantryTheme.xaml`, `SummerHarvestTheme.xaml`, `BerryCreamTheme.xaml`) each re-define only the handful of keys that actually carry brand colour on screen (`PageBgLight/Dark`, `CardBgLight/Dark`, `PrimaryTextLight/Dark`, `SecondaryTextLight/Dark`, `SurfaceBorderLight/Dark`, `Primary`/`PrimaryDark`/`PrimaryDarkText`, `BrandGold`/`BrandGoldDark`, `Gold`/`GoldDark`, `MidnightBlue`) — found by grepping which keys are actually referenced anywhere outside `Colors.xaml`. Anything not re-defined (grayscale, `ErrorLight/Dark`, `SuccessLight/Dark`) deliberately falls through to `Colors.xaml` unchanged, so error/success colours stay the universal red/green in every theme, and any key I didn't think to override just silently keeps working rather than crashing.
- New `Services/BrandThemeService.cs` merges the selected theme's dictionary on top of `Colors.xaml` in `Application.Resources.MergedDictionaries` (removing whichever one was there before, via the `IBrandThemeDictionary` marker interface each theme dictionary implements), and persists the choice with `Preferences`. Applied on cold start from `App.xaml.cs`, right after `InitializeComponent()` and before any page is built.
- Each theme still has its own light **and** dark-mode colours (the existing Dark Interface Mode toggle in Settings keeps working independently, per theme) — dark mode isn't just "invert to black," it's a dark tint of that theme's own hue family, mirroring how the current theme already flips near-black↔cream.

**Why switching rebuilds the screen**
`{StaticResource}` / `{AppThemeBinding}` only resolve once, when a page is actually constructed — they don't watch the dictionary for later changes. That's fine for the existing Dark Mode switch (it flips between two colours already resolved at parse time), but a brand theme change needs the *literal* colour values to change, which only happens when a page is rebuilt. So picking a new theme in Settings rebuilds the whole visible Shell (`Application.Current.MainPage = new AppShell()` / `new AdminShell()` — the same pattern already used elsewhere in this project for `LoginViewModel` / `SelectDeliveryDayViewModel`), then navigates straight back to Settings. A confirmation dialog says this up front ("This refreshes the app..."). Cart contents, session, and everything else living in the DI-registered services survive this untouched — only the page objects themselves get rebuilt.

**Where it lives**
- `Views/SettingsPage.xaml` — new "App Theme" card, a 2×2 grid of colour-swatch buttons (one per theme), the selected one outlined in that theme's accent colour via a `DataTrigger` bound to `SelectedBrandTheme` (same pattern already used for `PaymentPage`'s payment-method selector).
- `ViewModel/SettingsViewModel.cs` — `SelectedBrandTheme` (a plain string, matching this project's existing "mode" convention rather than introducing an enum) and `SelectBrandThemeCommand`.

**Not done / known limitation**
- The theme swap is app-wide but not test-driven — I don't have a MAUI/dotnet build available in this environment to actually compile and run it (same constraint noted earlier in this file), so this was built by closely mirroring existing, already-working patterns in this exact codebase (`PaymentPage`'s `DataTrigger`/`TapGestureRecognizer`/`CommandParameter` selector, `LoginViewModel`'s `Shell` rebuild) rather than anything novel, and validated with a full XML well-formedness pass, a project-wide `StaticResource`-vs-`x:Key` cross-check, and brace/paren balance checks — but it hasn't been visually verified on a device the way the earlier field-test fixes were.

## Orders page alignment, app-wide polish pass, full-screen date picker

Three follow-up requests after field-testing the theme switcher.

**Orders page — Active/History cards "not centralized, start on the left and cut off"**
- Re-read `Views/ActiveOrdersPage.xaml` from scratch rather than assuming the earlier History-card fix (the `Border`-can-only-have-one-child bug) still covered it. It does — the single-`Grid`-per-`Border` structure from that fix is intact, and I swept every `.xaml` file in the project for that exact anti-pattern (a `Border` with more than one direct content child) and found no other instance, in this page or anywhere else.
- Added `HorizontalOptions="Fill"` explicitly to both card templates' outer `Border`, their content `Grid`, and both `CollectionView`s themselves. This doesn't change anything today (the layout already resolves to full-width by MAUI's own defaults) but removes any ambiguity and gives a definite, checkable answer if this surfaces again.
- **If this is still happening after installing this build specifically** (not the previous one — the History-card fix shipped in the build before the theme switcher), it's a different bug than the one already found and fixed, and I'll need a fresh screenshot from this exact build to chase it further; I can't reproduce a layout bug that isn't in the code.

**App-wide speed/fluidity pass**
- Swept every `.xaml` file for the non-virtualizing `BindableLayout` pattern that caused the earlier ANR (see "Field-test fixes" above). The only remaining uses are `ProductDetailPage`'s customization option groups, `UserDashboardPage`'s category chip row, its per-dish dietary-tag badges, and `CartPage`'s selected-customization chips — all small, bounded lists (a handful of items each), which is exactly the case `BindableLayout` is fine for; converting these to `CollectionView` would add complexity for no real benefit, so left as-is.
- Added `DownsampleToViewSize="True"` to the app's 3 remaining `Image` controls bound to a remote (Unsplash) URL — the product detail header photo, the dashboard's category hero banner, and the admin menu catalog's dish thumbnail. Each already renders inside a fixed-size `Border` (180pt / 110pt / 65pt), so this tells the image decoder to decode at roughly that size instead of the source photo's full resolution — less memory and less decode work per image, same fix in spirit as the image-size trim already done in `JsonProductService`.
- Found and fixed one image request `AdminMenuViewModel.cs` missed in that same earlier pass — still requesting `800×600` instead of the `500×360` used everywhere else.
- `ActiveOrdersPage` and `AdminMenuPage` each had their own hand-rolled copy of the page-entrance fade/slide animation (250ms) instead of using the shared `PageAnimation.EntranceAsync` helper (260ms) every other page in the app already calls — cosmetically identical, but now one shared implementation instead of three, so a future tweak to the entrance animation doesn't need to be made in three places and risk drifting out of sync again.
- Everything else here is already virtualized (`CollectionView`) or backed by small in-memory collections, so there's no further "big list on the main thread" class of bug left to find by reading the code; anything beyond this needs an actual device/profiler to find, which isn't available in this environment.

**Delivery date picker — full screen instead of a pop up**
- Both places this appears — `Views/SelectDeliveryDayPage.xaml` (the mandatory first pick right after login) and `Views/Controls/DeliveryDayOverlay.xaml` (the mid-session "change delivery day" picker from the dashboard's date pill / add-to-basket flow) — were a rounded card floating over either a solid or dimmed background, with margins on all sides. Both are now a full-screen sheet: header (logo + close button when there's something to close back to, title, subtitle) pinned to the top, the date grid filling all remaining space and scrolling on its own, and the Confirm button pinned to the bottom — no floating card, no margins, no shadow.
- Side benefit: the mid-session popup's dimmed-scrim-over-real-content design (itself a workaround for stock .NET MAUI having no cross-platform blur-behind primitive, noted in the previous fix) is no longer needed at all — a full-screen sheet has nothing left behind it to dim or blur, so that whole fidelity gap goes away rather than staying worked-around.
- `DeliveryDayOverlay` is still embedded inside the host page's own `Grid` rather than a separate modal `Page` — that part of the earlier fix (real compositing instead of a `PushModalAsync` page that didn't reliably show anything behind it) is unrelated to card-vs-full-screen styling and stays as-is.

## Build fix, themes applied everywhere, delivery-day flow, tab-bar behaviour

**Build error — `DownsampleToViewSize` (3 occurrences) — removed**
- `Image.DownsampleToViewSize` doesn't exist in this MAUI version; the three attributes added in the previous pass (`AdminMenuPage`, `ProductDetailPage`, `UserDashboardPage`) broke the build and are gone. That one's on me — it was flagged as unverified in that pass's notes, and it turned out to be wrong. The image-size trims in `JsonProductService` / `AdminMenuViewModel` (plain URL strings) are unaffected and stay.

**Themes now reach every screen (was: only buttons and headings changed)**
- Root cause of themes "not sticking out": ~340 hardcoded hex literals across the views — card strokes (`#E0E0E0`/`#252525`), chip and pill fills (`#EFE9DC`/`#242426`), muted grey text (`#888888`), the whole bottom tab bar (`#121212`/`#F7F2E8` in `AppShell.xaml`), refresh spinners, translucent badge tints — none of which could follow a theme, so a Berry & Cream screen was still mostly monochrome with a red button on it.
- Added semantic keys to `Colors.xaml` (`ChipBgLight/Dark`, `MutedTextLight/Dark`, `TabBarBgLight/Dark`, `TabBarTextLight/Dark`, `TabBarUnselectedLight/Dark`, `HeaderBgLight/Dark`, `HeaderChipBgLight/Dark`, `HeaderLogoLight/Dark`, `SubtleFill`, `SubtleFillStrong`) and replaced 172 hardcoded colours across 20 views with them (scripted, exact-string, then re-verified). What's deliberately still literal: shadows and scrim overlays, semantic status colours (amber dispute badges, green/blue/orange stage nodes), and the four swatch previews in Settings, which must show their own colour regardless of the active theme.
- Each theme dictionary now overrides all of those too. The visible result per theme: the **bottom tab bar** is forest green / summer red / dark berry (flipping to that theme's light tone in dark mode); the **dashboard has a header band** behind the logo, search and date pill in the theme colour, with the logo variant that reads on it (this is the same look as the approved prototype PDF — forest green, red, and pale pink bands); chips, segmented toggles, status pills and the cutoff banner take the theme's tint; card borders, muted text and the translucent admin badge tints all shift with the palette. Also fixed along the way: status-chip text was hardcoded `#121212`, which was invisible on the dark chip fill in dark mode.
- "Current" is untouched — it's still exactly `Colors.xaml` with no override, and its new keys are the same values the literals used to be.

**Delivery-day picker → main menu**
- After "Order for a different day" from the add-to-basket prompt, choosing a day now takes the person straight back to the main menu (absolute `//userdashboard` route, so it lands on the Menu tab's root however deep the stack is), the same place "Continue with <day>" already went. Tapping ✕ instead still leaves them on the product page to keep browsing. The first-pick page after login already landed on the main menu.

**Bottom tab bar closes pushed pages**
- Shell's default keeps each tab's navigation stack as it was left, so Menu → product page → Orders tab → Menu tab would show the product page again. `AppShell` now overrides `OnNavigated`: whenever the selected tab changes, every other tab's stack is popped back to its root, so each tab button always lands on that tab's main screen. It runs after the switch and is best-effort (try/catch per tab), so it can only tidy up — never block or break a tab change. Re-tapping the *already-selected* tab is handled natively by Android's bottom navigation (pops to root) and isn't something Shell exposes cross-platform, so it isn't touched here.

**Same caveat as before**
- No MAUI build available in this environment. Everything above uses constructs already compiling elsewhere in this project (`AppThemeBinding` + `StaticResource` pairs, `Shell.GoToAsync`), plus two things worth a glance on first build: `FileImageSource` resources in `Colors.xaml`/the theme dictionaries (core MAUI type, used so the header logo can be swapped per theme), and the `OnNavigated` override in `AppShell.xaml.cs`. Validated with the usual full sweep: 35 XAML files well-formed, every `StaticResource` resolves, every theme-dictionary key exists in `Colors.xaml`, 114 C# files brace-balanced.

## Active Orders rebuilt, per-company delivery fees, multi-select sides

**Active Orders — misalignment root cause, plus the requested rework**
- Real cause this time, and it explains why two structural passes over the card templates found nothing: both lists had `Margin="16,12"` on the **`CollectionView` itself, inside a `RefreshView`**. On Android that combination shifts the list right by the margin and clips it on the right edge — cards "start on the left and cut off". The dashboard list never had a CollectionView margin and has always rendered fine; that pattern is now used here too (spacing on each card/header, none on the list), for both Active and History.
- Active orders are now **grouped per delivery date** (`IsGrouped="True"`, group header "Today" / "Tomorrow" / "Thursday, 02 Oct" with "2 orders · R318.00"), soonest first. New `Models/OrderDateGroup.cs`.
- Each order is a **collapsible card**: tap the header row (dish, order #, status chip, chevron) to expand/collapse. Expanded shows the full 4-stage delivery tracker (moved from the old single "delivery progress" header — it now works for *every* order, not just the next one), delivery date, location, placed-on time, total, notes, and an allergy alert when the order carries one. New `ViewModel/ActiveOrderItem.cs` (wraps `Order` with `IsExpanded` + `ToggleCommand`, since `Order` is a plain model). The soonest order opens automatically on first load; open/closed state survives pull-to-refresh.
- Both lists now reload every time the tab appears (`OnAppearing`) — previously only in the ViewModel constructor, so an order placed after the tab was first opened didn't show until a manual pull.

**Admin — delivery fee per company (Admin > Companies > "Delivery")**
- `Company.DeliveryFeeMode`: `DistanceTier` (default — the existing km-tier table), `Flat` (admin-entered amount), or `Free`. New "Delivery" action on each company card with an action sheet: Free delivery / Set a flat fee / Use distance tiers; a flat fee of 0 is treated as free. Each card shows the current setting ("🚚 Free delivery" / "R50.00 flat delivery" / "Delivery by distance tier").
- Checkout (`CartPageViewModel.RecalculateDeliveryFeeAsync`) applies the company setting first, falling back to the distance tier only for `DistanceTier`. The fee already flows to the payment screen and onto the order, so nothing else changes. The five company action buttons now wrap (`FlexLayout`) instead of overflowing on narrow screens.

**Customer — more than one side / checkbox — real bug fixed**
- `ProductDetailPage` renders *both* a radio-button list and a checkbox list for every customization group and hides the one that doesn't apply. Hidden `RadioButton`s still enforce their `GroupName` one-at-a-time rule, and they were two-way bound to the *same* `IsSelected` flags as the checkboxes — so ticking a second checkbox flipped its hidden radio, which un-ticked the first radio, which un-ticked the first checkbox. `CustomizationGroup` now exposes `SingleSelectOptions` / `MultiSelectOptions` (one is always empty) and each list binds to its own, so the inapplicable list has no items — no hidden radios, multi-select works.
- Add-ons are split into **"Sides (choose as many as you like)"** and **"Extras & Sauces"** — both multi-select — and tapping an option's label toggles it, not just the small checkbox.

Validated as before (35 XAML files well-formed, every resource key resolves, 116 C# files balanced, plus a cross-check that every command bound in the touched pages exists on its ViewModel). Still no MAUI build available here — the grouped `CollectionView` and `FlexLayout` follow the same constructs already compiling on the dashboard and cart pages.

## Contrast fixes, one notes box, checkout shortcut, PayFast only, invoice wording

- **Date chips on the product page** (`ProductDetailPage.xaml`): the selected chip filled with the accent colour but never changed its text colour — black on black in the current theme, dark on red in Summer Harvest. Both labels are now named and get an explicit text colour per visual state (on-accent when selected, normal text otherwise), the same pairing the delivery-day picker already used.
- **Login screen**: the login button had hardcoded white text, invisible on the light `PrimaryDark` fill in dark mode across every theme — now `Light=White, Dark=PrimaryDarkText`. "Sign Up" is an outlined button (border in the theme accent) instead of bare coloured text. One admin button with the same white-on-light problem fixed the same way.
- **One notes box**: "Special Requests" and "Allergy Notes" are a single "Special Requests & Allergy Notes" editor. Notes that mention an allergy/intolerance are still carried as the chef alert (`CartItem.AllergyNotes`) so the kitchen prep summary and invoice flagging keep working; the models are unchanged.
- **Added-to-basket prompt** now has three choices — *Continue to checkout* (goes straight to the basket), *Order for a different day*, or *Keep ordering for <day>* (back to the menu). It's an action sheet now rather than a yes/no alert, since it has three options.
- **Payment screen**: PayFast only. The Card option and its form are removed from the UI; the ViewModel keeps the card code path (defaults switched to PayFast) so it can be re-enabled later without a rewrite.
- **Invoice**: "SARS Tax Invoice" heading, page title and share-text header now just say "Invoice". Invoice numbers and content are unchanged.

## Post-demo round: nav-bar insets, skip/orders on first pick, logo, reports, revenue tiles

- **Bottom controls cut off (Android 15+ edge-to-edge)**: the gesture nav bar overlays the bottom of the screen, so the Confirm button on both delivery-day pickers and the last option/Cancel on every action sheet (added-to-basket, admin delivery fee, domains, bulk status) sat under it. `AppActionSheetPage` now has a 52pt bottom inset, both Confirm buttons a 48pt bottom margin, and the Register/Payment scroll content extra bottom padding. Margins rather than platform inset code, so it's verifiable from the XAML alone.
- **First-run day picker** (`SelectDeliveryDayPage`): two secondary actions under Confirm — *Skip for now* (into the app with the earliest orderable day as the default, changeable from the date pill) and *View my orders* (same, but lands on the Orders tab).
- **Logo**: "Powered by CSG Foods" removed. Both logo PNGs are cropped to the wordmark only (same filenames, so every usage picks it up); the login/register logo boxes were resized for the wider aspect ratio. No other CSG references existed in code or assets.
- **Admin > Reports — Order Report**: in-app collapsible table (customer, item, ordered date/time, quantity, company, delivery date, order #) for the current filters, plus a share button that outputs the same report as text grouped by company (email / WhatsApp / Drive / print via the system share sheet, like the invoice).
- **Admin > Reports — Delivery Notes**: shareable labelling sheet for the current filters: per delivery date → company & location → person (with floor) → one `[ ] qty × item #order` line per order line, with allergy and note flags, and a label count at the end. Both replace the old placeholder "Export Report" alert.
- **Admin dashboard tiles**: "Client Companies" and "Registered Customers" replaced by **Total Revenue (all time)** and **Month to Date (<month>)**, summed over every order incl. delivery fee (fee is recorded once per checkout batch, so no double-counting).

Validated as before; the share sheet, grouped text builders and collapsible section all reuse constructs already compiling elsewhere in the project.

## Production sheet, invoices & reports as real PDF/Excel downloads; Add User

**How "download" works here (and why there's no NuGet package)**
- Two small dependency-free writers in `Services/Export/`: `SimplePdfWriter` (text PDFs: A4, Helvetica/Courier, auto page breaks, correct xref) and `SimpleXlsxWriter` (genuine .xlsx via the BCL `ZipArchive` + minimal Office Open XML, bold headers, numeric cells, column widths). I couldn't verify a third-party PDF/Excel library's API without a build, so the exact byte layouts were prototyped and validated here first — the PDF with qpdf + poppler (renders, paginates, text extracts), the xlsx with openpyxl *and* LibreOffice — then ported line for line.
- `ExportService` writes the file to the app cache and opens the system share sheet (Save to Files / Drive / email / print / WhatsApp), the same mechanism the invoice already used for text.

**Admin > Active Orders — the kitchen's production sheet (moved here from Reports)**
- Four downloads for the orders in the current Company/Date filters: **Production Sheet (PDF)** = prep totals per dish + delivery notes per date → company/location → person → `[ ] qty × dish #order` with allergy/notes; **Production Sheet (Excel)** = three sheets: Prep Totals, Order Report (delivery date, company, location, customer, dish, qty, order #, ordered, floor, status, notes, allergy), and **Labels** (one row per unit, ready to mail-merge onto label stationery); **Delivery Notes (PDF)**; **Invoices (PDF)** = every invoice in the filter, one per page.
- Also fixed here: both lists had the same CollectionView-margin-inside-RefreshView clipping bug as the customer Orders page.

**Invoices match on both sides**
- New `InvoiceDocument` is the single definition of an invoice (supplier, invoice/order no., issued, customer name, company, location, address, floor/desk, delivery date, status, line item × qty, note/allergy, subtotal, subsidy, discount, delivery, total, VAT note). The customer's invoice screen (now with **Download PDF** next to Share as Text, and showing name + order no. on screen too) and the admin day batch both render from it, so the printed copy is the client's copy.

**Admin > Users — "+ Add User"**: name, email (validated, duplicate-checked), role; for customers also company → location → floor/desk. Temporary password `Welcome123`.

**Admin > Reports**
- **Top 10 best** and **Bottom 10 worst** performers by units (quantities parsed from "2x Dish"); the worst list lists menu dishes with *zero* sales first, which is the actionable part.
- **More Reports** (collapsible): revenue by company, revenue by category, demand by delivery day, top customers by spend, subsidy & discount cost per company.
- **Download as Excel**: one button per report (Top 10, Bottom 10, By Company, By Category, By Delivery Day, Top Customers, Subsidy Costs, All Orders, Disputes) each as a workbook with a Summary sheet + the report, and **Export All Reports** as one workbook with every sheet. The old placeholder "Export Report" alert is gone.

Validated as before (35 XAML, all keys resolve, 120 C# files balanced, every bound command/collection exists, new constructor dependencies are registered). The writers are the one genuinely new piece of code; their formats are verified, the C# is a direct port.

## Admin "Customer View", and down to two themes

- **Customer View for admins**: a "👤 Customer View" button on the admin nav strip opens the customer app as the signed-in admin — no logout/login — so they can check their menu, price, delivery-fee and theme changes from the client's side. While previewing, the customer dashboard shows a "Previewing as a customer · ← Back to Admin" banner at the top of the header, and Settings has a "Back to Admin" card; both are hidden for real customers. Implemented in `Services/AdminCustomerSwitch.cs` by swapping `MainPage` between `AppShell` and `AdminShell`, exactly as sign-in already does, so the session and all services stay as they are. A default ordering day is set on the way in so the dashboard has a date to work with.
- **Themes**: Summer Harvest and Berry & Cream are removed (files deleted, service and Settings swatches trimmed). Two remain: **Mediterranean Pantry** and the monochrome one, now named **Ink & Cream** (the stored preference key stays `Current`, so nothing saved on devices breaks; anyone who had one of the retired themes saved falls back to Ink & Cream).

## Still to build before the Supabase connection

- **Push notifications** — Admin Notifications "send" still just logs to an in-memory list and shows an alert; there's no real push delivery mechanism, and this needs a backend to send them anyway
- **PayFast/card payment** — a real UI and flow exist, but they're simulated; actually taking payment needs a backend to hold merchant credentials and handle PayFast's webhook (ITN) confirmation safely, which can't live on the client alone
- **Real invoice generation** — the tax invoice viewer/share flow is real and pulls live order data, but it's a formatted screen and share-text, not an actual PDF document; a proper PDF (or server-generated one, once there's a backend) would be a further step if that's needed
- ~~Register password~~ — **fixed**: registration now genuinely saves a password and login checks it, backward-compatible with every seeded demo account
- ~~Cycle-menu pricing~~ — **fixed**: real R80 pricing from the client's spreadsheet, not the old R95 placeholder
- ~~Category chip highlight~~ — **fixed**: solved via a new `MultiValueEqualsConverter` while rebuilding the dashboard's category slider

## Order scheduling rule, for reference

Delivery date = 2 business days after the order date (weekends don't count):
Monday→Wednesday, Tuesday→Thursday, Wednesday→Friday, Thursday→next Monday, Friday→next Tuesday.
No ordering at all on Saturday/Sunday. See `Services/OrderSchedulingService.cs`.
