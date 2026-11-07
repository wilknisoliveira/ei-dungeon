import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';

import { ChatComponent } from './chat.component';
import { GameService } from 'src/app/service/game/game.service';
import { PlayService } from 'src/app/service/play/play.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { Game } from 'src/app/types/game/game';
import { PagedSearch } from 'src/app/types/general/paged-search';
import { Play } from 'src/app/types/play/play';
import { StreamPlay } from 'src/app/types/play/stream-play';
import { AuthService } from 'src/app/service/auth/auth.service';

const PAGE_SIZE = 20;

/** Pretend height of one rendered play, and of the scroll viewport. */
const ROW_PX = 250;
const VIEWPORT_PX = 600;

function makeGame(id: string): Game {
    return {
        id,
        name: `Game ${id}`,
        protagonistName: `Hero ${id}`,
        ownerUserId: 'owner',
        gameLanguage: 'English',
        gameStatus: 'Playing',
        lastPlayedAt: null,
    };
}

/** Plays as the API returns them: newest first. */
function makePage(count: number, prefix = 'p'): PagedSearch<Play> {
    const items: Play[] = Array.from({ length: count }, (_, index) => ({
        id: `${prefix}-${index}`,
        playType: 'GameMaster' as const,
        response: `${prefix}-${index}`,
        createdAt: new Date(0),
    }));

    return {
        currentPage: 1,
        pageSize: PAGE_SIZE,
        totalResults: count,
        sortDirection: 'desc',
        items,
    };
}

describe('ChatComponent', () => {
    let component: ChatComponent;
    let fixture: ComponentFixture<ChatComponent>;
    let playService: jasmine.SpyObj<PlayService>;
    let gameService: jasmine.SpyObj<GameService>;
    let snackBar: jasmine.SpyObj<SnackbarService>;
    let authService: jasmine.SpyObj<AuthService>;

    /** Reaches the scroll state the component deliberately keeps private. */
    function internals(): {
        stickToBottom: boolean;
        pendingPrepend: { previousScrollHeight: number } | null;
    } {
        return component as unknown as {
            stickToBottom: boolean;
            pendingPrepend: { previousScrollHeight: number } | null;
        };
    }

    function messagesContainer(): HTMLElement {
        return fixture.nativeElement.querySelector('.messages') as HTMLElement;
    }

    /**
     * Gives the scroll container geometry a detached fixture cannot have.
     *
     * scrollHeight is a getter derived from the play count, so it grows and
     * shrinks with the list the way real layout does -- a frozen number could
     * never show that a prepend was compensated. scrollTop becomes a plain
     * writable property so assignments are not clamped back to zero.
     */
    function fakeGeometry(container: HTMLElement): void {
        Object.defineProperty(container, 'scrollHeight', {
            get: () => component.plays().length * ROW_PX,
            configurable: true,
        });
        Object.defineProperty(container, 'clientHeight', {
            value: VIEWPORT_PX,
            configurable: true,
        });
        Object.defineProperty(container, 'scrollTop', {
            value: 0,
            writable: true,
            configurable: true,
        });
    }

    function settle(): Promise<void> {
        fixture.detectChanges();
        return fixture.whenStable();
    }

    /**
     * Drives the production trigger: a change to the play list followed by a
     * render, which is what makes afterRenderEffect re-apply the position.
     */
    function reapplyLayout(): Promise<void> {
        component.plays.update((current) => [...current]);
        return settle();
    }

    function lastPlayElement(): HTMLElement {
        return fixture.nativeElement.querySelector(
            '.messages-list .message:last-child',
        ) as HTMLElement;
    }

    function makeLastPlayTallerThanViewport(): void {
        Object.defineProperty(lastPlayElement(), 'offsetHeight', {
            value: VIEWPORT_PX + 300,
            configurable: true,
        });
    }

    /** Opens a session and lets it lay out. Returns its scroll container. */
    async function openSession(
        gameId: string,
        page: PagedSearch<Play>,
    ): Promise<HTMLElement> {
        gameService.getById.and.resolveTo(makeGame(gameId));
        playService.getPlays.and.resolveTo(page);

        fixture.componentRef.setInput('gameId', gameId);
        await settle();
        fixture.detectChanges();

        const container = messagesContainer();
        if (container) {
            fakeGeometry(container);
            await reapplyLayout();
        }
        return container;
    }

    beforeEach(async () => {
        sessionStorage.removeItem('game-cache-a');
        sessionStorage.removeItem('game-cache-b');

        playService = jasmine.createSpyObj<PlayService>('PlayService', [
            'getPlays',
            'streamNewPlay',
        ]);
        gameService = jasmine.createSpyObj<GameService>('GameService', [
            'getById',
            'patchGame',
        ]);
        snackBar = jasmine.createSpyObj<SnackbarService>('SnackbarService', [
            'addError',
            'addSuccess',
        ]);
        authService = jasmine.createSpyObj<AuthService>('AuthService', ['getRole']);
        authService.getRole.and.returnValue('Admin');

        gameService.getById.and.resolveTo(makeGame('a'));
        gameService.patchGame.and.returnValue(of(makeGame('a')));
        playService.getPlays.and.resolveTo(makePage(0));
        playService.streamNewPlay.and.resolveTo();

        await TestBed.configureTestingModule({
            imports: [ChatComponent],
            providers: [
                provideNoopAnimations(),
                { provide: PlayService, useValue: playService },
                { provide: GameService, useValue: gameService },
                { provide: SnackbarService, useValue: snackBar },
                { provide: AuthService, useValue: authService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(ChatComponent);
        component = fixture.componentInstance;
    });

    afterEach(() => {
        sessionStorage.removeItem('game-cache-a');
        sessionStorage.removeItem('game-cache-b');
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('disables and ignores plays for a common user', async () => {
        await openSession('a', makePage(1));
        authService.getRole.and.returnValue('CommonUser');
        component.newPlayFormGroup.get('newPlayControl')?.setValue('go north');
        fixture.detectChanges();

        const submitButton = fixture.nativeElement.querySelector(
            'button[type="submit"]',
        ) as HTMLButtonElement;

        expect(submitButton.disabled).toBeTrue();

        await component.onSubmit();

        expect(playService.streamNewPlay).not.toHaveBeenCalled();
    });

    describe('opening a session', () => {
        it('renders the plays oldest first', async () => {
            await openSession('a', makePage(3));

            expect(component.plays().map((play) => play.id)).toEqual([
                'p-2',
                'p-1',
                'p-0',
            ]);
        });

        it('opens at the bottom', async () => {
            const container = await openSession('a', makePage(30));

            expect(container.scrollTop).toBe(30 * ROW_PX);
        });

        it('still reaches the bottom for a long session opened after a short one', async () => {
            // The original bug: the short session left the container unscrolled,
            // and the long one that followed stayed at the top or mid-list.
            const shortContainer = await openSession('a', makePage(2));
            expect(shortContainer.scrollTop).toBe(2 * ROW_PX);

            const longContainer = await openSession('b', makePage(30));

            expect(longContainer.scrollTop).toBe(30 * ROW_PX);
        });

        it('discards a slow response for a session the user already left', async () => {
            const pending: Record<string, (page: PagedSearch<Play>) => void> = {};
            playService.getPlays.and.callFake(
                (gameId: string) =>
                    new Promise<PagedSearch<Play>>((resolve) => {
                        pending[gameId] = resolve;
                    }),
            );

            // Both sessions end up parked on their own getPlays call.
            fixture.componentRef.setInput('gameId', 'a');
            await settle();

            fixture.componentRef.setInput('gameId', 'b');
            await settle();

            pending['b'](makePage(3, 'second'));
            await settle();

            // The abandoned session answers last and must not win.
            pending['a'](makePage(9, 'first'));
            await settle();

            expect(component.plays().length).toBe(3);
            expect(component.plays()[0].id).toContain('second');
        });

        it('does not offer the Start state when the request failed', async () => {
            gameService.getById.and.resolveTo(makeGame('a'));
            playService.getPlays.and.rejectWith(new Error('network down'));

            fixture.componentRef.setInput('gameId', 'a');
            await settle();
            fixture.detectChanges();

            expect(component.loadFailed()).toBeTrue();
            expect(component.plays()).toEqual([]);
            expect(snackBar.addError).toHaveBeenCalled();
            expect(fixture.nativeElement.textContent as string).not.toContain(
                'Your new game is ready',
            );
        });
    });

    describe('pagination', () => {
        it('keeps the reading position instead of jumping to the top', async () => {
            const container = await openSession('a', makePage(PAGE_SIZE));
            container.scrollTop = 10;

            playService.getPlays.and.resolveTo(makePage(PAGE_SIZE, 'older'));
            await component.loadMorePlays();
            await settle();

            // Scrolled down by exactly the height that was prepended above.
            expect(container.scrollTop).toBe(10 + PAGE_SIZE * ROW_PX);
            expect(internals().pendingPrepend).toBeNull();
        });

        it('does not cascade into loading the whole history', async () => {
            const container = await openSession('a', makePage(PAGE_SIZE));
            container.scrollTop = 10;

            playService.getPlays.and.resolveTo(makePage(PAGE_SIZE, 'older'));
            await component.loadMorePlays();
            await settle();
            playService.getPlays.calls.reset();

            // The scroll event the compensation itself produces.
            component.onScroll();

            expect(container.scrollTop).toBeGreaterThan(50);
            expect(playService.getPlays).not.toHaveBeenCalled();
        });

        it('survives a transient failure and can retry', async () => {
            await openSession('a', makePage(PAGE_SIZE));

            playService.getPlays.and.rejectWith(new Error('network blip'));
            await component.loadMorePlays();
            expect(snackBar.addError).toHaveBeenCalled();

            playService.getPlays.calls.reset();
            playService.getPlays.and.resolveTo(makePage(PAGE_SIZE, 'older'));
            await component.loadMorePlays();

            // Page 2 is requested again rather than being skipped for good.
            expect(playService.getPlays).toHaveBeenCalledWith(
                'a',
                'desc',
                PAGE_SIZE,
                2,
            );
            expect(component.plays().length).toBe(PAGE_SIZE * 2);
        });
    });

    describe('following the conversation', () => {
        it('stops following once the user scrolls up', async () => {
            const container = await openSession('a', makePage(30));

            container.scrollTop = 1000;
            component.onScroll();
            expect(internals().stickToBottom).toBeFalse();

            await reapplyLayout();

            expect(container.scrollTop).toBe(1000);
        });

        it('follows again once the user returns to the bottom', async () => {
            const container = await openSession('a', makePage(30));

            container.scrollTop = 1000;
            component.onScroll();
            expect(internals().stickToBottom).toBeFalse();

            container.scrollTop = 30 * ROW_PX - VIEWPORT_PX;
            component.onScroll();

            expect(internals().stickToBottom).toBeTrue();
        });

        it('re-pins the list when the growing textarea shrinks the viewport', async () => {
            const container = await openSession('a', makePage(30));
            // Nothing about the play list changes here, so the render effect
            // cannot be what brings the list back to the bottom.
            container.scrollTop = 0;

            const textArea = fixture.nativeElement.querySelector(
                'textarea',
            ) as HTMLTextAreaElement;
            textArea.value = 'first line\nsecond line\nthird line';
            textArea.dispatchEvent(new Event('input'));

            expect(container.scrollTop).toBe(30 * ROW_PX);
        });
    });

    describe('streaming', () => {
        function streamThrough(events: StreamPlay[]): void {
            playService.streamNewPlay.and.callFake(async (_newPlay, onChunk) => {
                events.forEach((event) => onChunk(event));
            });
        }

        it('appends chunks by emitting a new array, not by mutating in place', async () => {
            await openSession('a', makePage(1));
            component.newPlayFormGroup
                .get('newPlayControl')
                ?.setValue('open the door');

            const before = component.plays();
            streamThrough([
                { eventType: 'Start', content: '' } as StreamPlay,
                { eventType: 'Chunk', content: 'The ' } as StreamPlay,
                { eventType: 'Chunk', content: 'door creaks.' } as StreamPlay,
            ]);

            await component.onSubmit();

            const after = component.plays();
            expect(after).not.toBe(before);
            expect(after[after.length - 1].response).toBe('The door creaks.');
        });

        it('gives every optimistic play its own id so its node is stable', async () => {
            await openSession('a', makePage(1));
            component.newPlayFormGroup.get('newPlayControl')?.setValue('wait');

            streamThrough([
                { eventType: 'Start', content: '' } as StreamPlay,
                { eventType: 'Chunk', content: 'tick' } as StreamPlay,
            ]);

            await component.onSubmit();

            const ids = component.plays().map((play) => play.id);
            expect(new Set(ids).size).toBe(ids.length);
            expect(ids.filter((id) => id.startsWith('pending-')).length).toBe(2);
        });

        it('follows the reply as it streams in', async () => {
            const container = await openSession('a', makePage(4));
            component.newPlayFormGroup.get('newPlayControl')?.setValue('go north');

            streamThrough([
                { eventType: 'Start', content: '' } as StreamPlay,
                { eventType: 'Chunk', content: 'The path opens.' } as StreamPlay,
            ]);

            await component.onSubmit();
            await settle();

            // Start added the player's play and the Master's bubble.
            expect(container.scrollTop).toBe(6 * ROW_PX);
        });

        it('does not drag the reader down a reply taller than the viewport', async () => {
            const container = await openSession('a', makePage(4));
            component.loading.set(true);
            makeLastPlayTallerThanViewport();

            await reapplyLayout();

            expect(container.scrollTop).toBe(4 * ROW_PX);
            expect(internals().stickToBottom).toBeFalse();
        });

        it('stays frozen once the tall reply finishes streaming', async () => {
            const container = await openSession('a', makePage(4));
            component.newPlayFormGroup
                .get('newPlayControl')
                ?.setValue('look around');

            playService.streamNewPlay.and.callFake(async (_newPlay, onChunk) => {
                onChunk({ eventType: 'Start', content: '' } as StreamPlay);
                onChunk({
                    eventType: 'Chunk',
                    content: 'A very long tale.',
                } as StreamPlay);
                await settle();

                makeLastPlayTallerThanViewport();
                await reapplyLayout();

                onChunk({ eventType: 'End', content: '' } as StreamPlay);
            });

            await component.onSubmit();
            const frozenAt = container.scrollTop;
            expect(internals().stickToBottom).toBeFalse();

            // Anything that re-applies the position after the stream ended must
            // leave the reader where the freeze put them.
            await reapplyLayout();

            expect(container.scrollTop).toBe(frozenAt);
        });

        it('follows again once the player scrolls back to the bottom', async () => {
            const container = await openSession('a', makePage(4));
            component.loading.set(true);
            makeLastPlayTallerThanViewport();

            await reapplyLayout();
            expect(internals().stickToBottom).toBeFalse();

            container.scrollTop = 4 * ROW_PX - VIEWPORT_PX;
            component.onScroll();

            expect(internals().stickToBottom).toBeTrue();
        });

        it('rolls the optimistic plays back when the stream fails', async () => {
            await openSession('a', makePage(2));
            component.newPlayFormGroup.get('newPlayControl')?.setValue('attack');

            const countBefore = component.plays().length;
            streamThrough([
                { eventType: 'Start', content: '' } as StreamPlay,
                { eventType: 'Chunk', content: 'partial' } as StreamPlay,
                { eventType: 'Error', content: '' } as StreamPlay,
            ]);

            await component.onSubmit();

            expect(component.plays().length).toBe(countBefore);
            expect(snackBar.addError).toHaveBeenCalled();
        });

        it('ignores a submit with an empty prompt', async () => {
            await openSession('a', makePage(1));
            component.newPlayFormGroup.get('newPlayControl')?.setValue('   ');

            await component.onSubmit();

            expect(playService.streamNewPlay).not.toHaveBeenCalled();
        });

        it('falls back to the filler prompt for the opening play', async () => {
            await openSession('a', makePage(0));

            await component.onSubmit(true);

            // The box is empty, but the API rejects prompts that are too short.
            expect(playService.streamNewPlay).toHaveBeenCalledWith(
                { gameId: 'a', prompt: 'Lorem Ipsum' },
                jasmine.any(Function),
            );
        });
    });

    describe('language', () => {
        it('restores the previous language when the update fails', async () => {
            await openSession('a', makePage(1));
            gameService.patchGame.and.returnValue(
                throwError(() => new Error('rejected')),
            );

            component.selectLanguage('Portuguese');
            await fixture.whenStable();

            expect(component.currentGameLanguage).toBe('English');
            expect(snackBar.addError).toHaveBeenCalled();
        });
    });
});
