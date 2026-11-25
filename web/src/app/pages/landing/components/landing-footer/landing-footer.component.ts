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
}
