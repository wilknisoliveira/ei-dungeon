import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

/**
 * Replaces the social proof block a landing page would normally carry. The product has no
 * users, testimonials or ratings yet, so this section says what is actually true instead.
 */
@Component({
    standalone: true,
    imports: [MatButtonModule, MatIconModule],
    selector: 'app-landing-development',
    templateUrl: './landing-development.component.html',
    styleUrls: ['./landing-development.component.scss'],
})
export class LandingDevelopmentComponent {
    readonly webRepo = 'https://github.com/wilknisoliveira/ei-dungeon-web';
    readonly apiRepo = 'https://github.com/wilknisoliveira/ei-dungeon-back';
}
