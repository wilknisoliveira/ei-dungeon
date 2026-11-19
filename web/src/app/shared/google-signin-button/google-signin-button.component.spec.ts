import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { GoogleSigninButtonComponent } from './google-signin-button.component';
import { GoogleAuthService } from 'src/app/service/auth/google-auth.service';
import { ThemeService } from 'src/app/service/theme/theme.service';

describe('GoogleSigninButtonComponent', () => {
    let fixture: ComponentFixture<GoogleSigninButtonComponent>;
    const google = { configured: true, render: jasmine.createSpy('render'), message: () => 'Google unavailable' };
    const view = { updateTheme: jasmine.createSpy('updateTheme') };
    beforeEach(() => {
        view.updateTheme.calls.reset();
        google.render.calls.reset();
        google.render.and.resolveTo(view);
        TestBed.configureTestingModule({ imports: [GoogleSigninButtonComponent, HttpClientTestingModule],
            providers: [{ provide: GoogleAuthService, useValue: google }] });
        fixture = TestBed.createComponent(GoogleSigninButtonComponent);
    });
    afterEach(() => fixture.destroy());
    it('renders the official button and emits credentials only while enabled', async () => {
        const emitted = jasmine.createSpy('emitted');
        fixture.componentInstance.credential.subscribe(emitted);
        fixture.detectChanges();
        await fixture.whenStable();
        expect(google.render).toHaveBeenCalled();
        const callback = google.render.calls.mostRecent().args[3];
        callback({ challengeId: 'id', credential: 'proof' });
        expect(emitted).toHaveBeenCalledTimes(1);
        fixture.componentRef.setInput('disabled', true);
        callback({ challengeId: 'id', credential: 'proof' });
        expect(emitted).toHaveBeenCalledTimes(1);
    });
    it('shows a retry action when the Google library is unavailable', async () => {
        google.render.and.rejectWith(new Error('network'));
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('Google unavailable');
        expect(fixture.nativeElement.querySelector('button')).not.toBeNull();
    });

    it('updates both themes while keeping the current authentication attempt and callback', async () => {
        const theme = TestBed.inject(ThemeService);
        theme.setTheme(true, false);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(view.updateTheme).toHaveBeenCalledWith(true);
        theme.setTheme(false, false);
        fixture.detectChanges();
        await fixture.whenStable();
        expect(view.updateTheme.calls.mostRecent().args[0]).toBeFalse();
        theme.setTheme(true, false);
        fixture.detectChanges();
        await fixture.whenStable();
        expect(view.updateTheme.calls.mostRecent().args[0]).toBeTrue();
        expect(google.render).toHaveBeenCalledTimes(1);
        const emitted = jasmine.createSpy('emitted');
        fixture.componentInstance.credential.subscribe(emitted);
        google.render.calls.mostRecent().args[3]({ challengeId: 'id', credential: 'proof' });
        expect(emitted).toHaveBeenCalledWith({ challengeId: 'id', credential: 'proof' });
    });

    it('uses the latest theme when initialization finishes after the theme changes', async () => {
        let complete!: (value: typeof view) => void;
        google.render.and.returnValue(new Promise(resolve => { complete = resolve; }));
        const theme = TestBed.inject(ThemeService);
        theme.setTheme(true, false);
        fixture.detectChanges();
        theme.setTheme(false, false);
        fixture.detectChanges();
        complete(view);
        await fixture.whenStable();
        fixture.detectChanges();
        expect(view.updateTheme.calls.mostRecent().args[0]).toBeFalse();
        expect(google.render).toHaveBeenCalledTimes(1);
    });
});
