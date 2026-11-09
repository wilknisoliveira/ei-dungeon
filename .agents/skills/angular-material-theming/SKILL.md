---
name: angular-material-theming
description: Guidance for styling custom Angular components (and answering questions about Angular Material's theming system) so they match an app's Angular Material theme. Covers the two theming mechanisms — `--mat-sys-*` CSS variables from `mat.theme()` and the `.mat-*` utility classes from `mat.system-classes()` — plus color roles, typography scales, corner/shape radii, borders, and elevation shadows. Use this whenever the user is building or styling a custom Angular component to fit an Angular Material design system, asks which system token or utility class to use for a color/font/radius/shadow, is migrating a legacy Material Design 2 (M2) theme-config component to system tokens, wants light/dark mode to work automatically, or mentions terms like `mat-sys`, `mat.system-classes()`, `mat.theme()`, Material Design 3 tokens, or Angular Material design tokens — even if they don't say "theming" explicitly.
---

# Angular Material Theming

Angular Material (Material Design 3) exposes its theme to custom, non-Material components through
system tokens, so hand-built components can look consistent with Angular Material components and
automatically support light/dark mode. There are two interchangeable ways to consume a token:

1. **CSS variables** (`--mat-sys-*`), enabled by `mat.theme()` — use these in component stylesheets.
2. **Utility classes** (`.mat-*`), enabled by `mat.system-classes()` — use these directly in
   templates when you don't want a separate stylesheet rule.

Both read from the same underlying theme, so pick whichever fits the component: a stylesheet rule
for `.widget-a { background-color: var(--mat-sys-surface); }` is equivalent to putting
`class="mat-bg-surface"` on the element in the template.

## How to use this skill

1. Figure out which **category** of styling is being asked about: color (background/text), typography,
   shape (corner radius), or elevation (border/shadow).
2. Read `references/theming-guide.md` — it has the full token list, the exact utility-class-to-CSS-variable
   mapping, and a "used for / Angular Material examples" table for every token in each category. Don't
   guess token names from memory; the naming is specific (e.g. `--mat-sys-surface-container-highest` vs
   `--mat-sys-surface-container-high` are different tokens) and picking the wrong one is a common mistake.
3. Apply the token that matches the *semantic role* the element plays, not just a color that looks
   right. Material's system is built around roles (primary vs. secondary vs. surface vs. error, and
   emphasis levels within each), not raw color values — see the guidance below.
4. When styling text on a colored background, always pair the background token with its matching
   `on-*` token for contrast (e.g. `--mat-sys-primary` background pairs with `--mat-sys-on-primary`
   text; a plain `--mat-sys-surface` background pairs with `--mat-sys-on-surface` or
   `--mat-sys-on-surface-variant` for lower-emphasis text).

## Picking the right token: quick heuristics

- **Is this the "main" call-to-action or most prominent element?** → `primary` role.
- **Is this a secondary/supporting element with its own but less prominent scheme?** → `secondary` role.
- **Is this communicating a problem, invalid state, or destructive action?** → `error` role.
- **Is this a general container/panel/background (dialog, card, table, menu)?** → a `surface` /
  `surface-container-*` variant — pick the "-highest" / "-high" / "" / "-low" / "-lowest" step based
  on how much it should stand out from its surrounding surface.
- **Does it need to visually pop above everything else briefly (snackbar, tooltip)?** → `inverse-surface`.
- **Is it disabled?** → the disabled background/text mix (`color-mix` of `on-surface` at 12%/38%),
  not a role color.

For typography, shape, and elevation, match the *size/level* to how much visual weight the element
needs (see the tables in the reference file) rather than picking a size that merely "looks about
right" at a glance — Angular Material's own components follow this same size-to-role mapping, so
matching it keeps a custom component visually consistent with the surrounding Material components.

## Writing the styles

Prefer CSS variables in component stylesheets for anything reusable or conditional; use utility
classes for quick one-off template styling. Example of the two being equivalent:

```scss
// Stylesheet approach
.widget-a {
  background-color: var(--mat-sys-surface);
  color: var(--mat-sys-on-surface);
  font: var(--mat-sys-body-medium);
}
```

```html
<!-- Utility class approach -->
<widget-a class="mat-bg-surface mat-text-on-surface mat-font-body-medium" />
```

## Legacy Material Design 2 (M2) projects

If the app still defines its theme with the older `mat.m2-define-light-theme` / theme-config Sass
APIs rather than `mat.theme()`, the same system tokens are still available — but they need to be
opted into explicitly by adding `@include mat.m2-theme($theme);` alongside the existing theme
mixins, plus `@include mat.system-classes();` if utility classes are wanted too. See the
"Material Design 2 Support" section of `references/theming-guide.md` for the exact setup and a
before/after example showing how this replaces bespoke `@mixin my-component-theme($theme)` files
that manually pull colors out of the `$theme` Sass map.

## Reference

`references/theming-guide.md` contains the complete, authoritative token reference:
- Every `--mat-sys-*` CSS variable name, grouped by category (color, typography, shape, elevation/border)
- Every `.mat-*` utility class and the CSS it expands to
- A "used for / Angular Material examples" table per token, explaining the intended semantic use
- The full M2 legacy migration example

Always check this file rather than recalling exact token names from memory — Angular Material's
token naming is dense and easy to mix up (e.g. `-fixed` vs `-fixed-dim` vs `-container`, or
`corner-large` vs `corner-large-start/-end/-top`), and getting a hand-written component to actually
match the design system depends on using the exact right token.
