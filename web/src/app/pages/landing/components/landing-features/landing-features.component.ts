import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

interface Feature {
    icon: string;
    title: string;
    body: string;
}

/**
 * Every claim here is sourced from `first-steps.component.ts` — the nine races, the six
 * attributes, the 8-18 range and the 30 distributable points, and the three narration
 * languages. Nothing describes behaviour the application does not have.
 */
@Component({
    standalone: true,
    imports: [CommonModule, MatIconModule],
    selector: 'app-landing-features',
    templateUrl: './landing-features.component.html',
    styleUrls: ['./landing-features.component.scss'],
})
export class LandingFeaturesComponent {
    features: Feature[] = [
        {
            icon: 'groups',
            title: $localize`:@@landingFeatureRacesTitle:Nine races, real trade-offs`,
            body: $localize`:@@landingFeatureRacesBody:Human, Elf, Dwarf, Half-Elf, Halfling, Tiefling, Dragonborn, Half-Orc and Gnome. Each one carries its own attribute bonuses.`,
        },
        {
            icon: 'tune',
            title: $localize`:@@landingFeaturePointsTitle:Thirty points, your call`,
            body: $localize`:@@landingFeaturePointsBody:Six attributes, a floor of 8 and a ceiling of 18. Build the hero you want, then live with the character you made.`,
        },
        {
            icon: 'translate',
            title: $localize`:@@landingFeatureLanguageTitle:Narrated in your language`,
            body: $localize`:@@landingFeatureLanguageBody:The Game Master tells the story in Portuguese, English or Spanish. You set it per game, so it does not have to match the interface.`,
        },
        {
            icon: 'bookmark',
            title: $localize`:@@landingFeatureSaveTitle:Your campaign keeps its place`,
            body: $localize`:@@landingFeatureSaveBody:Every game is saved to your account. Close the tab mid-sentence and the scene is still waiting when you come back.`,
        },
    ];
}
