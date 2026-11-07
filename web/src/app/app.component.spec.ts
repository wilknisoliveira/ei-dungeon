import { TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { DOCUMENT } from '@angular/common';
import { AppComponent } from './app.component';
import {
    DARK_THEME,
    LIGHT_THEME,
    THEME_STORAGE_KEY,
} from 'src/app/service/theme/theme.service';

describe('AppComponent', () => {
    let document: Document;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [RouterTestingModule, NoopAnimationsModule, AppComponent],
        });

        document = TestBed.inject(DOCUMENT);

        localStorage.removeItem(THEME_STORAGE_KEY);
        document.body.classList.remove(DARK_THEME, LIGHT_THEME);
    });

    afterEach(() => {
        localStorage.removeItem(THEME_STORAGE_KEY);
        document.body.classList.remove(DARK_THEME, LIGHT_THEME);
    });

    it('should create the app', () => {
        const fixture = TestBed.createComponent(AppComponent);
        const app = fixture.componentInstance;
        expect(app).toBeTruthy();
    });

    it(`should have as title 'ei-dungeon-web'`, () => {
        const fixture = TestBed.createComponent(AppComponent);
        const app = fixture.componentInstance;
        expect(app.title).toEqual('ei-dungeon-web');
    });

    it('should leave the main landmark to the active route', () => {
        const fixture = TestBed.createComponent(AppComponent);
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('main')).toBeNull();
    });

    it('should apply the dark theme on startup when nothing is stored', () => {
        const fixture = TestBed.createComponent(AppComponent);
        fixture.detectChanges();

        expect(document.body.classList.contains(DARK_THEME)).toBeTrue();
    });

    it('should apply the stored theme on startup', () => {
        localStorage.setItem(THEME_STORAGE_KEY, LIGHT_THEME);

        const fixture = TestBed.createComponent(AppComponent);
        fixture.detectChanges();

        expect(document.body.classList.contains(LIGHT_THEME)).toBeTrue();
        expect(document.body.classList.contains(DARK_THEME)).toBeFalse();
    });
});
