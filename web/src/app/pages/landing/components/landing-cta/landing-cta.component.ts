import { Component, input } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

@Component({
    standalone: true,
    imports: [RouterModule, MatButtonModule],
    selector: 'app-landing-cta',
    templateUrl: './landing-cta.component.html',
    styleUrls: ['./landing-cta.component.scss'],
})
export class LandingCtaComponent {
    /** A signed-in player is sent to their games rather than to a signup form. */
    isLoggedIn = input<boolean>(false);

    /** Disables account and game navigation while MVP features are unfinished. */
    isInDevelopment = input<boolean>(false);
}
