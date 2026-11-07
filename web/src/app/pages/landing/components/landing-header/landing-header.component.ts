import { Component, input } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { LanguageSelectorComponent } from 'src/app/shared/components/language-selector/language-selector.component';

@Component({
    standalone: true,
    imports: [RouterModule, MatButtonModule, LanguageSelectorComponent],
    selector: 'app-landing-header',
    templateUrl: './landing-header.component.html',
    styleUrls: ['./landing-header.component.scss'],
})
export class LandingHeaderComponent {
    /** Swaps the sign-in control for a route back into the player's games. */
    isLoggedIn = input<boolean>(false);

    /** Disables account and game navigation while MVP features are unfinished. */
    isInDevelopment = input<boolean>(false);
}
