import { Component, afterNextRender, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ThemeService } from 'src/app/service/theme/theme.service';
import {
    trigger,
    transition,
    style,
    query,
    group,
    animate,
} from '@angular/animations';

@Component({
    standalone: true,
    imports: [RouterModule],
    selector: 'app-root',
    templateUrl: 'app.component.html',
    animations: [
        trigger('routeAnimation', [
            transition('* <=> *', [
                style({ position: 'relative' }),
                query(':enter, :leave', [
                    style({
                        position: 'absolute',
                        width: '100%',
                        opacity: 0,
                    }),
                ], { optional: true }),
                query(':enter', [
                    style({ opacity: 0, transform: 'translateY(8px)' }),
                ], { optional: true }),
                group([
                    query(':leave', [
                        animate('200ms ease-out', style({ opacity: 0, transform: 'translateY(-8px)' })),
                    ], { optional: true }),
                    query(':enter', [
                        animate('300ms ease-out', style({ opacity: 1, transform: 'translateY(0)' })),
                    ], { optional: true }),
                ]),
            ]),
        ]),
    ],
})
export class AppComponent {
    title = 'ei-dungeon-web';

    private themeService = inject(ThemeService);

    constructor() {
        afterNextRender(() => this.themeService.initTheme());
    }

    getRouteAnimation(): number {
        return 1;
    }
}
