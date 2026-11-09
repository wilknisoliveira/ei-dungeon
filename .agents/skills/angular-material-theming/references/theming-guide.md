# Angular Material — Theming Custom Components (System Tokens Reference)

Angular Material provides two approaches to styling custom components to match the application's
theme:

- **CSS variables** included by `mat.theme()`. These are prefixed with `--mat-sys` and can be used
  in component stylesheets to match the theme and design of the application.
- **Utility classes** included by `mat.system-classes()`. These wrap the CSS variables and allow
  applying theme styles directly from component templates.

Both approaches are valid ways to keep an application consistently designed alongside Angular
Material's components. Using the system tokens for color automatically supports both light and
dark mode, controlled via the `color-scheme` CSS property.

If the application is based on the older Material Design 2 APIs with theme configs, the CSS
variables and classes are still usable — see [Material Design 2 Support](#material-design-2-support).

## Colors

Material Design uses color to create accessible, personal color schemes that communicate a
product's hierarchy, state, and brand (see Material Design's Color Roles documentation:
https://m3.material.io/styles/color/roles).

Using the system tokens matches the application's theme color scheme automatically and correctly
handles both light and dark mode.

### All color system tokens

```css
/* Primary */
--mat-sys-primary
--mat-sys-on-primary
--mat-sys-primary-container
--mat-sys-on-primary-container
--mat-sys-primary-fixed
--mat-sys-on-primary-fixed
--mat-sys-on-primary-fixed-variant
--mat-sys-primary-fixed-dim
--mat-sys-inverse-primary

/* Secondary */
--mat-sys-secondary
--mat-sys-on-secondary
--mat-sys-secondary-container
--mat-sys-on-secondary-container
--mat-sys-secondary-fixed
--mat-sys-on-secondary-fixed
--mat-sys-on-secondary-fixed-variant
--mat-sys-secondary-fixed-dim

/* Tertiary */
--mat-sys-tertiary
--mat-sys-on-tertiary
--mat-sys-tertiary-container
--mat-sys-on-tertiary-container
--mat-sys-tertiary-fixed
--mat-sys-on-tertiary-fixed
--mat-sys-on-tertiary-fixed-variant
--mat-sys-tertiary-fixed-dim

/* Error */
--mat-sys-error
--mat-sys-on-error
--mat-sys-error-container
--mat-sys-on-error-container

/* Surface */
--mat-sys-surface
--mat-sys-on-surface
--mat-sys-on-surface-variant
--mat-sys-surface-bright
--mat-sys-surface-container
--mat-sys-surface-container-high
--mat-sys-surface-container-highest
--mat-sys-surface-container-low
--mat-sys-surface-container-lowest
--mat-sys-surface-dim
--mat-sys-surface-tint
--mat-sys-surface-variant
--mat-sys-inverse-surface
--mat-sys-inverse-on-surface

/* Miscellaneous */
--mat-sys-background
--mat-sys-on-background
--mat-sys-neutral-variant20
--mat-sys-neutral10
--mat-sys-outline
--mat-sys-outline-variant
--mat-sys-scrim
--mat-sys-shadow
```

### Background tokens — meaning and where Angular Material uses them

| Token | Utility class | Used for | Angular Material examples |
|---|---|---|---|
| `--mat-sys-primary` | `.mat-bg-primary` | Key components with greater importance on the page | Selected date in a datepicker, slider handle, checkbox background |
| `--mat-sys-primary-container` | `.mat-bg-primary-container` | Filling components that should stand out on a surface | Floating action button container |
| `--mat-sys-secondary` | `.mat-bg-secondary` | Less prominent components with a different color scheme than primary | — |
| `--mat-sys-secondary-container` | `.mat-bg-secondary-container` | Components needing less emphasis than secondary | Selected list items, tonal button container |
| `--mat-sys-error` | `.mat-bg-error` | Error state / important notification background | Badge background |
| `--mat-sys-error-container` | `.mat-bg-error-container` | Components needing less emphasis than error | Container for error text |
| `--mat-sys-surface` | `.mat-bg-surface` | General surfaces of components | Tables, dialogs, menus, toolbars |
| `--mat-sys-surface-variant` | `.mat-bg-surface-variant` | Surfaces that need to stand out from the main surface color | Filled form field background, progress bar track |
| `--mat-sys-surface-container-highest` | `.mat-bg-surface-container-highest` | Most emphasis against the main surface color | Filled card background |
| `--mat-sys-surface-container-high` | `.mat-bg-surface-container-high` | More emphasis against the main surface color | Datepicker background |
| `--mat-sys-surface-container` | `.mat-bg-surface-container` | Standing out from the main surface color | Menu background |
| `--mat-sys-surface-container-low` | `.mat-bg-surface-container-low` | Less emphasis against the main surface color | Bottom sheet background |
| `--mat-sys-surface-container-lowest` | `.mat-bg-surface-container-lowest` | Least emphasis against the main surface color | — |
| `--mat-sys-inverse-surface` | `.mat-bg-inverse-surface` | Standing out against the default color scheme; temporary notifications above content | Snackbar, tooltip background |
| disabled (12% mix) | `.mat-bg-disabled` | Disabled components normally filled with primary color | — |

Disabled background class definition:
```css
.mat-bg-disabled {
  background-color: color-mix(in srgb, var(--mat-sys-on-surface) 12%, transparent);
}
```

**Contrast rule:** whenever a background token from the table above is used, pair it with its
matching "on-*" text token (e.g. `--mat-sys-primary` background → `--mat-sys-on-primary` text) to
ensure accessible contrast. Surface backgrounds pair with `on-surface` or `on-surface-variant`.

### Text tokens — meaning and where Angular Material uses them

| Token | Utility class | Used for | Angular Material examples |
|---|---|---|---|
| `--mat-sys-primary` | `.mat-text-primary` | Text that needs to stand out | Text button label, selected tab label |
| `--mat-sys-secondary` | `.mat-text-secondary` | Text that needs to stand out apart from the main theme color | — |
| `--mat-sys-error` | `.mat-text-error` | Text indicating an issue or warning | Form field error text |
| disabled (38% mix) | `.mat-text-disabled` | Elements disabled on either disabled or surface backgrounds | — |
| `--mat-sys-on-surface-variant` | `.mat-text-on-surface-variant` | Lower-emphasis text on a surface | Subheadings, captions, hint text |
| `--mat-sys-on-primary` | `.mat-text-on-primary` | Text/icons on primary backgrounds | — |
| `--mat-sys-on-primary-container` | `.mat-text-on-primary-container` | Text/icons on primary-container backgrounds | — |
| `--mat-sys-on-secondary` | `.mat-text-on-secondary` | Text/icons on secondary backgrounds | — |
| `--mat-sys-on-secondary-container` | `.mat-text-on-secondary-container` | Text on secondary-container backgrounds | — |
| `--mat-sys-on-error` | `.mat-text-on-error` | Text on error backgrounds | — |
| `--mat-sys-on-error-container` | `.mat-text-on-error-container` | Text on error-container backgrounds | — |
| `--mat-sys-on-surface` | `.mat-text-on-surface` | Text on surface backgrounds | — |
| `--mat-sys-inverse-on-surface` | `.mat-text-inverse-on-surface` | Text on inverse-surface backgrounds | — |

Disabled text class definition:
```css
.mat-text-disabled {
  color: color-mix(in srgb, var(--mat-sys-on-surface) 38%, transparent);
}
```

## Typography

Material Design provides five categories of font types: **body, display, headline, label, title**.
Each category has three sizes: **small, medium, large** (see Material Design's Applying Type
documentation: https://m3.material.io/styles/typography/applying-type).

### All typography system tokens (pattern repeats per category/size)

For each of `body`, `display`, `headline`, `label`, `title` × `small`/`medium`/`large`, the
following variables exist (e.g. for `body-large`):

```css
--mat-sys-body-large
--mat-sys-body-large-font
--mat-sys-body-large-line-height
--mat-sys-body-large-size
--mat-sys-body-large-tracking
--mat-sys-body-large-weight
```

`label` sizes additionally have a `-weight-prominent` variant (e.g. `--mat-sys-label-large-weight-prominent`).

### Usage guidance by category/size

| Token | Utility class | Used for | Angular Material examples |
|---|---|---|---|
| `body-small` | `.mat-font-body-sm` | Captions | Form field subscript text, paginator text |
| `body-medium` | `.mat-font-body-md` | Default body font | Table row text, dialog supporting text |
| `body-large` | `.mat-font-body-lg` | Introductory paragraph | List item text, select trigger text |
| `display-small` | `.mat-font-display-sm` | Short, important text/numerals | Hero sections, key info |
| `display-medium` | `.mat-font-display-md` | Short, impactful text | Hero sections, titles on larger screens |
| `display-large` | `.mat-font-display-lg` | Short, high-emphasis text, most visual weight | Hero sections, titles on larger screens |
| `headline-small` | `.mat-font-headline-sm` | Page title | Dialog headline |
| `headline-medium` | `.mat-font-headline-md` | Section title | — |
| `headline-large` | `.mat-font-headline-lg` | Page title on a large screen | — |
| `label-small` | `.mat-font-label-sm` | Small labels | Badge text |
| `label-medium` | `.mat-font-label-md` | Medium labels | Slider label |
| `label-large` | `.mat-font-label-lg` | Buttons, chips, menu labels | — |
| `title-small` | `.mat-font-title-sm` | Card title | Table header, option group label |
| `title-medium` | `.mat-font-title-md` | Dialog title, primary list-item text | Card subtitle, expansion panel header |
| `title-large` | `.mat-font-title-lg` | Page title on a small screen | Card title, toolbar title |

Utility class pattern (applies the font shorthand + letter spacing):
```css
.mat-font-body-sm {
  font: var(--mat-sys-body-small);
  letter-spacing: var(--mat-sys-body-small-tracking);
}
```

## Shape

Material Design uses border radius to direct attention, identify components, communicate state,
and express brand (see Material Design's Corner Radius Scale documentation:
https://m3.material.io/styles/shape/corner-radius-scale).

### All corner shape system tokens

```css
--mat-sys-corner-extra-large: 28px;
--mat-sys-corner-extra-large-top: 28px 28px 0 0;
--mat-sys-corner-extra-small: 4px;
--mat-sys-corner-extra-small-top: 4px 4px 0 0;
--mat-sys-corner-full: 9999px;
--mat-sys-corner-large: 16px;
--mat-sys-corner-large-end: 0 16px 16px 0;
--mat-sys-corner-large-start: 16px 0 0 16px;
--mat-sys-corner-large-top: 16px 16px 0 0;
--mat-sys-corner-medium: 12px;
--mat-sys-corner-none: 0;
--mat-sys-corner-small: 8px;
```

### Usage guidance

| Token | Utility class | Used for | Angular Material examples |
|---|---|---|---|
| `corner-extra-small` (4px) | `.mat-corner-xs` | Small amount of rounding | Chip, snackbar, tooltip |
| `corner-small` (8px) | `.mat-corner-sm` | Small amount of rounding | Text field |
| `corner-medium` (12px) | `.mat-corner-md` | Medium amount of rounding | Button, card |
| `corner-large` (16px) | `.mat-corner-lg` | Large amount of rounding | Floating action button, datepicker, card |
| `corner-extra-large` (28px) | `.mat-corner-xl` | Large amount of rounding | Button toggle, dialog |
| `corner-full` (9999px) | `.mat-corner-full` | Circular components | Avatar, badge, button |

Utility class pattern:
```css
.mat-corner-sm {
  border-radius: var(--mat-sys-corner-small);
}
```

## Elevation

Material Design uses borders and shadows to create a sense of depth and hierarchy (see Material
Design's Applying Elevation documentation: https://m3.material.io/styles/elevation/applying-elevation).

### Border tokens

```css
--mat-sys-outline
--mat-sys-outline-variant
```

| Token | Utility class | Used for | Angular Material examples |
|---|---|---|---|
| `outline` | `.mat-border` | Visible boundary | Outlined button border |
| `outline-variant` | `.mat-border-subtle` | Less obvious boundary | Outlined card border, divider color |

```css
.mat-border {
  border: 1px solid var(--mat-sys-outline);
}
.mat-border-subtle {
  border: 1px solid var(--mat-sys-outline-variant);
}
```

### Shadow tokens (elevation levels)

```css
--mat-sys-level0: 0px 0px 0px 0px rgba(0, 0, 0, .2), 0px 0px 0px 0px rgba(0, 0, 0, .14), 0px 0px 0px 0px rgba(0, 0, 0, .12);
--mat-sys-level1: 0px 2px 1px -1px rgba(0, 0, 0, .2), 0px 1px 1px 0px rgba(0, 0, 0, .14), 0px 1px 3px 0px rgba(0, 0, 0, .12);
--mat-sys-level2: 0px 3px 3px -2px rgba(0, 0, 0, .2), 0px 3px 4px 0px rgba(0, 0, 0, .14), 0px 1px 8px 0px rgba(0, 0, 0, .12);
--mat-sys-level3: 0px 3px 5px -1px rgba(0, 0, 0, .2), 0px 6px 10px 0px rgba(0, 0, 0, .14), 0px 1px 18px 0px rgba(0, 0, 0, .12);
--mat-sys-level4: 0px 5px 5px -3px rgba(0, 0, 0, .2), 0px 8px 10px 1px rgba(0, 0, 0, .14), 0px 3px 14px 2px rgba(0, 0, 0, .12);
--mat-sys-level5: 0px 7px 8px -4px rgba(0, 0, 0, .2), 0px 12px 17px 2px rgba(0, 0, 0, .14), 0px 5px 22px 4px rgba(0, 0, 0, .12);
```

| Level | Used for | Angular Material examples |
|---|---|---|
| Level 1 | Slightly raise a surface | Elevated card, slider handle |
| Level 2 | Raise a surface | Menu, select panel |
| Level 3 | Raise a surface | Floating action button |
| Level 4 | Interaction states (focus/hover) | Hovered floating action button |
| Level 5 | Greatly raise a surface, interaction states | — |

```css
.mat-shadow-1 { box-shadow: var(--mat-sys-level1); }
.mat-shadow-2 { box-shadow: var(--mat-sys-level2); }
.mat-shadow-3 { box-shadow: var(--mat-sys-level3); }
.mat-shadow-4 { box-shadow: var(--mat-sys-level4); }
.mat-shadow-5 { box-shadow: var(--mat-sys-level5); }
```

## Material Design 2 Support

Applications defining their theme with the legacy "M2" Sass APIs (theme-config approach) can still
get the CSS variables and utility classes by calling `@include mat.m2-theme($theme)`, which defines
system tokens according to the Material Design 2 system matching the current theme configuration.

```scss
@use '@angular/material' as mat;

$theme: mat.m2-define-light-theme((
  color: (
    primary: mat.m2-define-palette(mat.$m2-indigo-palette, 500),
  ),
  ...
));

html {
  @include mat.core-theme($theme);
  @include mat.button-theme($theme);
  ...
  @include mat.m2-theme($theme);
  @include mat.system-classes();
}
```

This avoids needing component-specific theme files/mixins that extract values from the `$theme`
Sass map. Old pattern:

```scss
@use '@angular/material' as mat;
@use 'sass:map';

@mixin my-component-theme($theme) {
  $foreground: map.get(mat.m2-get-color-config($theme), foreground);
  $background: map.get(mat.m2-get-color-config($theme), background);
  $primary: map.get(mat.m2-get-color-config($theme), primary);
  $typography: mat.m2-get-typography-config($config-or-theme);

  .widget-a {
    background-color: mat.m2-get-color-from-palette($background, card);
    color: mat.m2-get-color-from-palette($foreground, text);
    @include mat.m2-typography-level($config, body-1);
  }

  .widget-b {
    background-color: mat.m2-get-color-from-palette($primary, default);
    color: mat.m2-get-color-from-palette($primary, default-contrast);
  }
}
```

New pattern using CSS variables directly in the component stylesheet:

```scss
.widget-a  {
  background-color: var(--mat-sys-surface);
  color: var(--mat-sys-on-surface);
  font: var(--mat-sys-body-medium);
}

.widget-b {
  background-color: var(--mat-sys-primary);
  color: var(--mat-sys-on-primary);
}
```

Or, using the utility classes directly in the template:

```html
<widget-a class="mat-bg-surface mat-text-on-surface mat-font-body-medium"/>

<widget-b class="mat-bg-primary mat-text-on-primary"/>
```
