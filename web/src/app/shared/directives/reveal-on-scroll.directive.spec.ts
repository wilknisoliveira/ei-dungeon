import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RevealOnScrollDirective } from './reveal-on-scroll.directive';

@Component({
    standalone: true,
    imports: [RevealOnScrollDirective],
    template: `<div appRevealOnScroll class="target">Scene content</div>`,
})
class HostComponent {}

describe('RevealOnScrollDirective', () => {
    let fixture: ComponentFixture<HostComponent>;
    let target: HTMLElement;
    let observerInstances: FakeObserver[];

    class FakeObserver {
        static disconnectCount = 0;

        observed: Element[] = [];
        unobserved: Element[] = [];
        disconnected = false;

        constructor(
            public callback: IntersectionObserverCallback,
            public options?: IntersectionObserverInit,
        ) {
            observerInstances.push(this);
        }

        observe(element: Element): void {
            this.observed.push(element);
        }

        unobserve(element: Element): void {
            this.unobserved.push(element);
        }

        disconnect(): void {
            this.disconnected = true;
            FakeObserver.disconnectCount++;
        }

        takeRecords(): IntersectionObserverEntry[] {
            return [];
        }

        /** Drives the callback the way the browser would. */
        trigger(isIntersecting: boolean): void {
            this.callback(
                this.observed.map(
                    (target) =>
                        ({ target, isIntersecting }) as IntersectionObserverEntry,
                ),
                this as unknown as IntersectionObserver,
            );
        }
    }

    let originalObserver: typeof IntersectionObserver;

    beforeEach(() => {
        observerInstances = [];
        FakeObserver.disconnectCount = 0;

        originalObserver = window.IntersectionObserver;
        window.IntersectionObserver =
            FakeObserver as unknown as typeof IntersectionObserver;

        TestBed.configureTestingModule({ imports: [HostComponent] });
        fixture = TestBed.createComponent(HostComponent);
        fixture.detectChanges();

        target = fixture.nativeElement.querySelector('.target');
    });

    afterEach(() => {
        window.IntersectionObserver = originalObserver;
    });

    it('should keep the content in the DOM before revealing', () => {
        expect(target.textContent).toContain('Scene content');
    });

    it('should mark the element pending until it intersects', () => {
        expect(target.classList.contains('reveal-pending')).toBeTrue();
        expect(target.classList.contains('is-revealed')).toBeFalse();
    });

    it('should reveal the element once it intersects', () => {
        observerInstances[0].trigger(true);

        expect(target.classList.contains('is-revealed')).toBeTrue();
        expect(target.classList.contains('reveal-pending')).toBeFalse();
    });

    it('should ignore entries that are not intersecting', () => {
        observerInstances[0].trigger(false);

        expect(target.classList.contains('is-revealed')).toBeFalse();
        expect(observerInstances[0].unobserved.length).toBe(0);
    });

    it('should reveal only once', () => {
        const observer = observerInstances[0];

        observer.trigger(true);
        expect(observer.unobserved).toContain(target);

        observer.trigger(true);
        expect(observer.unobserved.length).toBe(2);
        expect(target.classList.contains('is-revealed')).toBeTrue();
    });

    it('should disconnect the observer on destroy', () => {
        fixture.destroy();

        expect(observerInstances[0].disconnected).toBeTrue();
    });

    it('should leave content visible when IntersectionObserver is unavailable', () => {
        fixture.destroy();
        (window as unknown as Record<string, unknown>)['IntersectionObserver'] =
            undefined;

        const bare = TestBed.createComponent(HostComponent);
        bare.detectChanges();
        const bareTarget: HTMLElement =
            bare.nativeElement.querySelector('.target');

        expect(bareTarget.classList.contains('reveal-pending')).toBeFalse();
        expect(bareTarget.classList.contains('is-revealed')).toBeTrue();
    });
});
