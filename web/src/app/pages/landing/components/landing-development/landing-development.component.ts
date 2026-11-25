import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
    standalone: true,
    imports: [MatButtonModule, MatIconModule],
    selector: 'app-landing-development',
    templateUrl: './landing-development.component.html',
    styleUrls: ['./landing-development.component.scss'],
})
export class LandingDevelopmentComponent {}
