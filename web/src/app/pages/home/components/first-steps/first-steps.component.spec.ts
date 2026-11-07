import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { FirstStepsComponent } from './first-steps.component';
import { AuthService } from 'src/app/service/auth/auth.service';
import { GameService } from 'src/app/service/game/game.service';

describe('FirstStepsComponent', () => {
    let component: FirstStepsComponent;
    let fixture: ComponentFixture<FirstStepsComponent>;
    let authService: jasmine.SpyObj<AuthService>;
    let gameService: jasmine.SpyObj<GameService>;

    beforeEach(() => {
        authService = jasmine.createSpyObj<AuthService>('AuthService', ['getRole']);
        gameService = jasmine.createSpyObj<GameService>('GameService', ['newGame']);
        authService.getRole.and.returnValue('Admin');
        TestBed.configureTestingModule({
            imports: [
                FirstStepsComponent,
                HttpClientTestingModule,
                NoopAnimationsModule,
            ],
            providers: [
                { provide: AuthService, useValue: authService },
                { provide: GameService, useValue: gameService },
            ],
        });
        fixture = TestBed.createComponent(FirstStepsComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('disables and ignores game creation for a common user', () => {
        authService.getRole.and.returnValue('CommonUser');
        fixture.detectChanges();

        const button = fixture.nativeElement.querySelector(
            'button[type="submit"]',
        ) as HTMLButtonElement;

        expect(button.disabled).toBeTrue();

        component.onSubmit();

        expect(gameService.newGame).not.toHaveBeenCalled();
    });
});
