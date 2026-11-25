import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { By } from '@angular/platform-browser';
import { MatTooltip } from '@angular/material/tooltip';

import { FirstStepsComponent } from './first-steps.component';
import { AuthService } from 'src/app/service/auth/auth.service';
import { GameService } from 'src/app/service/game/game.service';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { Game } from 'src/app/types/game/game';

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

    it('keeps the creation cost tooltip focusable when Start game is disabled', () => {
        authService.getRole.and.returnValue('CommonUser');
        fixture.componentRef.setInput('sparkBalance', 1);
        fixture.detectChanges();

        const button = fixture.debugElement.query(By.css('button[type="submit"]'));
        const trigger = button.parent!;
        const tooltip = trigger.injector.get(MatTooltip);

        expect(button.nativeElement.disabled).toBeTrue();
        expect(trigger.nativeElement.tabIndex).toBe(0);
        expect(tooltip.disabled).toBeFalse();
        expect(tooltip.message).toBe('Creating the game costs 1 Spark. Starting the first scene costs 1 more.');
        expect(button.injector.get(MatTooltip).disabled).toBeTrue();
    });

    it('disables and ignores game creation for a common user without Sparks', () => {
        authService.getRole.and.returnValue('CommonUser');
        fixture.componentRef.setInput('sparkBalance', 0);
        fixture.detectChanges();

        const button = fixture.nativeElement.querySelector(
            'button[type="submit"]',
        ) as HTMLButtonElement;

        expect(button.disabled).toBeTrue();

        component.onSubmit();

        expect(gameService.newGame).not.toHaveBeenCalled();
    });

    it('retries a Free creation with the same identity and selects the saved game ID', () => {
        authService.getRole.and.returnValue('CommonUser');
        fixture.componentRef.setInput('sparkBalance', 1);
        component.gameFormGroup.patchValue({
            gameNameControl: 'Dusk', characterNameControl: 'Iris',
            characterDescriptionControl: 'Scout', characterRaceControl: 'Human',
        });
        gameService.newGame.and.returnValue(throwError(() => new HttpErrorResponse({ status: 0 })));
        component.onSubmit();

        const operationId = gameService.newGame.calls.mostRecent().args[0].operationId;
        const created = jasmine.createSpy('created');
        component.gameCreated.subscribe(created);
        fixture.componentRef.setInput('sparkBalance', 0);
        gameService.newGame.and.returnValue(of({ id: 'saved-id' } as Game));
        component.onSubmit();
        expect(operationId).toBeTruthy();
        expect(gameService.newGame.calls.mostRecent().args[0].operationId).toBe(operationId);
        expect(created).toHaveBeenCalledOnceWith('saved-id');
    });
});
