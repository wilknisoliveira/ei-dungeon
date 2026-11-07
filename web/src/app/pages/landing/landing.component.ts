import {
    Component,
    OnDestroy,
    OnInit,
    afterNextRender,
    inject,
    signal,
} from '@angular/core';
import { AuthService } from 'src/app/service/auth/auth.service';
import { ThemeService } from 'src/app/service/theme/theme.service';
import { RevealOnScrollDirective } from 'src/app/shared/directives/reveal-on-scroll.directive';
import { LandingHeaderComponent } from './components/landing-header/landing-header.component';
import { LandingHeroComponent } from './components/landing-hero/landing-hero.component';
import { LandingProblemComponent } from './components/landing-problem/landing-problem.component';
import { LandingHowItWorksComponent } from './components/landing-how-it-works/landing-how-it-works.component';
import { LandingFeaturesComponent } from './components/landing-features/landing-features.component';
import { LandingDevelopmentComponent } from './components/landing-development/landing-development.component';
import { LandingFaqComponent } from './components/landing-faq/landing-faq.component';
import { LandingCtaComponent } from './components/landing-cta/landing-cta.component';
import { LandingFooterComponent } from './components/landing-footer/landing-footer.component';
import { LandingSeoService } from 'src/app/core/seo/landing-seo.service';

/**
 * Public marketing page at the application root. Open to everyone, including players who are
 * already signed in — for them the calls to action point at their games instead of signup.
 *
 * Dark only: it forces the dark theme while displayed but never persists it, so a visitor
 * who prefers the light theme still has that preference once they navigate into the app.
 */
@Component({
    standalone: true,
    imports: [
        RevealOnScrollDirective,
        LandingHeaderComponent,
        LandingHeroComponent,
        LandingProblemComponent,
        LandingHowItWorksComponent,
        LandingFeaturesComponent,
        LandingDevelopmentComponent,
        LandingFaqComponent,
        LandingCtaComponent,
        LandingFooterComponent,
    ],
    selector: 'app-landing',
    templateUrl: './landing.component.html',
    styleUrls: ['./landing.component.scss'],
})
export class LandingComponent implements OnInit, OnDestroy {
    private themeService = inject(ThemeService);
    private authService = inject(AuthService);
    private landingSeo = inject(LandingSeoService);

    /** Resolved once on init. The sections are presentational and just read this. */
    readonly isLoggedIn = signal(false);

    /** Temporary MVP gate for account and game actions on the public landing page. */
    readonly isInDevelopment = true;

    constructor() {
        afterNextRender(() => {
            this.isLoggedIn.set(this.authService.isUserLoggedIn());
        });
    }

    ngOnInit(): void {
        this.themeService.forceDark();
        this.landingSeo.apply();
    }

    ngOnDestroy(): void {
        this.themeService.restoreStoredTheme();
        this.landingSeo.clear();
    }
}
