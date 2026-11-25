import { Component, input } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

@Component({
    standalone: true,
    imports: [RouterModule, MatButtonModule],
    selector: 'app-landing-hero',
    templateUrl: './landing-hero.component.html',
    styleUrls: ['./landing-hero.component.scss'],
})
export class LandingHeroComponent {
    /** A signed-in player is sent to their games rather than to a signup form. */
    isLoggedIn = input<boolean>(false);
    initialGrant = input(15);

    logoAlt = $localize`:@@landingLogoCompletedAlt:EI-DUNGEON — an A.I. driven RPG simulator`;
}
