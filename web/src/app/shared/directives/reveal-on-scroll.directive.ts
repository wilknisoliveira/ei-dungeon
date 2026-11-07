import { Directive, ElementRef, OnDestroy, afterNextRender, inject } from '@angular/core';

/**
 * Reveals an element once it scrolls into view, by adding `is-revealed`.
 *
 * The element must be visible in the DOM to begin with — the transition only moves it into
 * its resting state. Nothing here may be the reason content appears, otherwise a visitor
 * with reduced motion, an old browser or a disabled observer would see an empty page.
 */
@Directive({
    standalone: true,
    selector: '[appRevealOnScroll]',
})
export class RevealOnScrollDirective implements OnDestroy {
    private elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
    private observer?: IntersectionObserver;

    constructor() {
        afterNextRender(() => this.startObserver());
    }

    private startObserver(): void {
        const element = this.elementRef.nativeElement;

        // Without IntersectionObserver the element simply stays in its revealed state.
        if (typeof IntersectionObserver === 'undefined') {
            this.reveal();
            return;
        }

        element.classList.add('reveal-pending');

        this.observer = new IntersectionObserver(
            (entries) => {
                entries.forEach((entry) => {
                    if (!entry.isIntersecting) {
                        return;
                    }

                    this.reveal();
                    this.observer?.unobserve(entry.target);
                });
            },
            { threshold: 0.12, rootMargin: '0px 0px -5% 0px' },
        );

        this.observer.observe(element);
    }

    ngOnDestroy(): void {
        this.observer?.disconnect();
        this.observer = undefined;
    }

    private reveal(): void {
        const element = this.elementRef.nativeElement;
        element.classList.remove('reveal-pending');
        element.classList.add('is-revealed');
    }
}
