import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

interface Step {
    icon: string;
    title: string;
    body: string;
}

@Component({
    standalone: true,
    imports: [CommonModule, MatIconModule],
    selector: 'app-landing-how-it-works',
    templateUrl: './landing-how-it-works.component.html',
    styleUrls: ['./landing-how-it-works.component.scss'],
})
export class LandingHowItWorksComponent {
    steps: Step[] = [
        {
            icon: 'person_add',
            title: $localize`:@@landingStepCharacterTitle:Build your character`,
            body: $localize`:@@landingStepCharacterBody:Choose a race, spend thirty points across six attributes, and write who they are. Nine races, each with its own bonuses.`,
        },
        {
            icon: 'auto_stories',
            title: $localize`:@@landingStepMasterTitle:The Game Master sets the scene`,
            body: $localize`:@@landingStepMasterBody:An AI narrator opens the world in the language you picked, and reacts to what you actually do — not to a script.`,
        },
        {
            icon: 'forum',
            title: $localize`:@@landingStepPlayTitle:Play by chat`,
            body: $localize`:@@landingStepPlayBody:Type what your character does. The world answers. Stop in the middle of a scene and pick it up tomorrow.`,
        },
    ];
}
