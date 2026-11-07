import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatExpansionModule } from '@angular/material/expansion';

interface FaqEntry {
    question: string;
    answer: string;
}

@Component({
    standalone: true,
    imports: [CommonModule, MatExpansionModule],
    selector: 'app-landing-faq',
    templateUrl: './landing-faq.component.html',
    styleUrls: ['./landing-faq.component.scss'],
})
export class LandingFaqComponent {
    entries: FaqEntry[] = [
        {
            question: $localize`:@@landingFaqSoloQ:Do I need other players?`,
            answer: $localize`:@@landingFaqSoloA:No. EI-DUNGEON is built for solo play. The AI Game Master runs the world, the characters in it, and everything that happens to you.`,
        },
        {
            question: $localize`:@@landingFaqRulesQ:Do I need to know role-playing rules?`,
            answer: $localize`:@@landingFaqRulesA:No. Character creation walks you through race, attributes and description one step at a time. If you have never rolled a die in your life, you can still play.`,
        },
        {
            question: $localize`:@@landingFaqPriceQ:What does it cost?`,
            answer: $localize`:@@landingFaqPriceA:It is free to start. A paid tier is planned for later, but nothing is behind a paywall today.`,
        },
        {
            question: $localize`:@@landingFaqLanguageQ:What language is the story told in?`,
            answer: $localize`:@@landingFaqLanguageA:Portuguese, English or Spanish. You choose it per game, so the language of the narration does not have to match the language of the interface.`,
        },
        {
            question: $localize`:@@landingFaqSaveQ:Do my games get saved?`,
            answer: $localize`:@@landingFaqSaveA:Yes. Every game and every exchange is stored on your account. Come back a month later and the scene is where you left it.`,
        },
        {
            // Deliberately describes what the application does — sign-in gated, not published —
            // rather than guaranteeing isolation that has not been verified. See spec.md A-002.
            question: $localize`:@@landingFaqPrivacyQ:Who can see my games?`,
            answer: $localize`:@@landingFaqPrivacyA:Your games are tied to your account and reached by signing in. They are not published anywhere and they are not shared with other players.`,
        },
    ];
}
