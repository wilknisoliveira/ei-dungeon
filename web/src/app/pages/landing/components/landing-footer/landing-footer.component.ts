import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
    standalone: true,
    imports: [RouterModule],
    selector: 'app-landing-footer',
    templateUrl: './landing-footer.component.html',
    styleUrls: ['./landing-footer.component.scss'],
})
export class LandingFooterComponent {
    readonly year = new Date().getFullYear();
    readonly webRepo = 'https://github.com/wilknisoliveira/ei-dungeon-web';
    readonly apiRepo = 'https://github.com/wilknisoliveira/ei-dungeon-back';
}
