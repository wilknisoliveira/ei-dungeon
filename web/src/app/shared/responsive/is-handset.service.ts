import { Injectable, inject } from '@angular/core';
import { BreakpointObserver } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

/**
 * Reactive handset detection. The drawer layout switches to overlay mode on
 * handsets (width < 768px), matching the existing first-steps grid breakpoint.
 */
@Injectable({ providedIn: 'root' })
export class IsHandsetService {
    private readonly breakpointObserver = inject(BreakpointObserver);

    /** True when the viewport is below the 768px handset breakpoint. */
    readonly isHandset = toSignal(
        this.breakpointObserver
            .observe('(max-width: 767.98px)')
            .pipe(map((state) => state.matches)),
        { initialValue: false },
    );
}
