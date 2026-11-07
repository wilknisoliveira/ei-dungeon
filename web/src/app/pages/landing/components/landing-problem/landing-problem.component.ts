import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

interface Pain {
    icon: string;
    title: string;
    body: string;
}

@Component({
    standalone: true,
    imports: [CommonModule, MatIconModule],
    selector: 'app-landing-problem',
    templateUrl: './landing-problem.component.html',
    styleUrls: ['./landing-problem.component.scss'],
})
export class LandingProblemComponent {
    pains: Pain[] = [
        {
            icon: 'event_busy',
            title: $localize`:@@landingPainScheduleTitle:Four people, one calendar`,
            body: $localize`:@@landingPainScheduleBody:Finding a night everyone is free takes longer than the session itself.`,
        },
        {
            icon: 'menu_book',
            title: $localize`:@@landingPainMasterTitle:Somebody has to run it`,
            body: $localize`:@@landingPainMasterBody:And that somebody spends the week preparing while everyone else just shows up.`,
        },
        {
            icon: 'notifications_off',
            title: $localize`:@@landingPainDeadTitle:The campaign that died at session three`,
            body: $localize`:@@landingPainDeadBody:Not from a total party kill. From a group chat that slowly went quiet.`,
        },
    ];
}
