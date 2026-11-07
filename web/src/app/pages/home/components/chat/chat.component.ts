import {
    Component,
    ElementRef,
    Injector,
    NgZone,
    OnChanges,
    SimpleChanges,
    afterNextRender,
    afterRenderEffect,
    effect,
    input,
    output,
    signal,
    viewChild,
} from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSelectModule } from '@angular/material/select';
import { SmallLoadingComponent } from 'src/app/shared/small-loading/small-loading.component';
import { GameService } from 'src/app/service/game/game.service';
import { PlayService } from 'src/app/service/play/play.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { Game } from 'src/app/types/game/game';
import { PagedSearch } from 'src/app/types/general/paged-search';
import { NewPlay } from 'src/app/types/play/new-play';
import { Play } from 'src/app/types/play/play';
import { StreamPlay } from 'src/app/types/play/stream-play';
import { AuthService } from 'src/app/service/auth/auth.service';

/** Plays requested per page. */
const PLAYS_PAGE_SIZE = 20;

/**
 * Filler for the opening play. The player starts a session with an empty box,
 * but the API enforces a minimum prompt length. Not user-facing, so not localized.
 */
const INITIAL_PLAY_PROMPT = 'Lorem Ipsum';

/** How close to the top the list must be before older plays are fetched. */
const LOAD_MORE_SCROLL_THRESHOLD_PX = 50;

/** How far from the bottom still counts as "the user is following the chat". */
const STICK_TO_BOTTOM_THRESHOLD_PX = 100;

@Component({
    standalone: true,
    imports: [
        ReactiveFormsModule,
        MatButtonModule,
        MatIconModule,
        MatMenuModule,
        MatTooltipModule,
        MatSelectModule,
        SmallLoadingComponent,
    ],
    selector: 'app-chat',
    templateUrl: './chat.component.html',
    styleUrls: ['./chat.component.scss'],
})
export class ChatComponent implements OnChanges {
    gameId = input<string>('');
    gamePlayed = output<string>();

    messagesContainer = viewChild<ElementRef<HTMLDivElement>>('messagesContainer');
    messagesList = viewChild<ElementRef<HTMLDivElement>>('messagesList');
    textAreaContainer =
        viewChild<ElementRef<HTMLTextAreaElement>>('textAreaContainer');

    /** The rendered conversation. Everything else about the page is derived. */
    plays = signal<Play[]>([]);
    isLoadingMore = signal<boolean>(false);
    newPlayFormGroup: FormGroup;
    loading = signal<boolean>(false);
    initialLoading = signal<boolean>(false);

    /**
     * A failed request is not an empty session. Without this the template would
     * offer the "your new game is ready" Start state for a session that simply
     * failed to load.
     */
    loadFailed = signal<boolean>(false);
    game = signal<Game | null>(null);

    // Internal bookkeeping. None of it is read by the template, so plain fields
    // are enough -- as signals they would notify change detection for nothing.
    private currentPage = 1;
    private allPlaysLoaded = false;
    private streamedPlaysStartIndex: number | null = null;
    private streamFailed = false;
    private pendingPlaySequence = 0;

    /** Session currently being opened; guards against out-of-order responses. */
    private openingGameId: string | null = null;

    /** Whether the list should follow new content. Derived from real scroll position. */
    private stickToBottom = true;

    /** Set while older plays are prepended, so the reading position is preserved. */
    private pendingPrepend: { previousScrollHeight: number } | null = null;

    private readonly scrollListener = (): void => this.onScroll();

    gameLanguages: { name: string; value: string; abbreviation: string }[] = [
        { name: $localize`Portuguese`, value: 'Portuguese', abbreviation: 'PT' },
        { name: $localize`English`, value: 'English', abbreviation: 'EN' },
        { name: $localize`Spanish`, value: 'Spanish', abbreviation: 'ES' },
    ];

    gameLanguageControl: FormGroup;

    get activePlaceholder(): string {
        return this.game()?.gameStatus !== 'PlayerDied'
            ? $localize`What do you do?`
            : $localize`This game has been finished, create a new one to start a new adventure!`;
    }

    get isDisabled(): boolean {
        return this.game()?.gameStatus === 'PlayerDied';
    }

    get isCommonUser(): boolean {
        return this.authService.getRole() === 'CommonUser';
    }

    get currentGameLanguage(): string {
        return this.gameLanguageControl.get('gameLanguage')?.value || 'English';
    }

    selectLanguage(value: string): void {
        const previousLanguage = this.currentGameLanguage;
        if (previousLanguage === value) return;

        this.gameLanguageControl
            .get('gameLanguage')
            ?.setValue(value, { emitEvent: false });

        void this.updateGameLanguage(previousLanguage);
    }

    private async updateGameLanguage(previousLanguage: string): Promise<void> {
        const game = this.game();
        if (!game) return;

        const newLanguage = this.gameLanguageControl.get('gameLanguage')?.value;

        try {
            const updatedGame = await lastValueFrom(
                this.gameService.patchGame(game.id, {
                    gameLanguage: newLanguage,
                }),
            );
            this.game.set(updatedGame);
            this.snackBar.addSuccess($localize`Game language updated.`);
        } catch (error) {
            // The menu ticks the current language, so leaving the failed value
            // in place would keep showing the wrong one.
            this.gameLanguageControl
                .get('gameLanguage')
                ?.setValue(previousLanguage, { emitEvent: false });

            console.error('Failed to update game language', error);
            this.snackBar.addError(
                $localize`Something went wrong while attempting to update the game language.`,
            );
        }
    }

    getDisplayName(play: Play): string {
        if (play.playType === 'Protagonist') {
            return this.game()?.protagonistName ?? $localize`Player`;
        }
        return $localize`Game Master`;
    }

    private readonly storagePrefix = 'game-cache-';

    private saveGameCache(gameId: string, data: Record<string, string>): void {
        sessionStorage.setItem(
            `${this.storagePrefix}${gameId}`,
            JSON.stringify(data),
        );
    }

    private getGameCache(gameId: string): Record<string, string> | null {
        const raw = sessionStorage.getItem(`${this.storagePrefix}${gameId}`);
        if (!raw) return null;
        try {
            return JSON.parse(raw);
        } catch {
            return null;
        }
    }

    private removeGameCache(gameId: string): void {
        sessionStorage.removeItem(`${this.storagePrefix}${gameId}`);
    }

    private saveDraft(gameId: string): void {
        this.saveGameCache(gameId, {
            textbox: this.newPlayFormGroup.get('newPlayControl')?.value ?? '',
        });
    }

    private restoreDraft(gameId: string): void {
        const cache = this.getGameCache(gameId);
        this.newPlayFormGroup
            .get('newPlayControl')
            ?.setValue(cache?.['textbox'] ?? '');

        // setValue does not fire the (input) handler, so the textarea would stay
        // one row tall even when the restored draft spans several lines.
        afterNextRender(() => this.resizeInputTextArea(), {
            injector: this.injector,
        });
    }

    private resizeInputTextArea(): void {
        const textArea = this.textAreaContainer()?.nativeElement;
        if (textArea) {
            this.adjustTextAreaHeightElement(textArea);
        }
    }

    constructor(
        private snackBar: SnackbarService,
        private playService: PlayService,
        private gameService: GameService,
        private formBuilder: FormBuilder,
        private injector: Injector,
        private ngZone: NgZone,
        private authService: AuthService,
    ) {
        this.newPlayFormGroup = this.formBuilder.group({
            newPlayControl: ['', Validators.required],
        });
        this.gameLanguageControl = this.formBuilder.group({
            gameLanguage: ['English', Validators.required],
        });

        // Primary trigger. The play list is the only thing that makes the view
        // need re-scrolling, and afterRenderEffect runs once Angular has
        // rendered it -- which is what makes reading scrollHeight meaningful.
        // A signal write on its own is synchronous and lands before layout,
        // and reading scrollHeight there was the root cause of the scroll bugs.
        afterRenderEffect(() => {
            this.plays();
            this.applyScrollPosition();
        });

        // Keeps the scroll listener attached to whichever container is on
        // screen. It only reads the position; the list is moved from
        // applyScrollPosition.
        effect((onCleanup) => {
            const container = this.messagesContainer()?.nativeElement;
            if (!container) return;

            this.ngZone.runOutsideAngular(() => {
                container.addEventListener('scroll', this.scrollListener, {
                    passive: true,
                });
            });

            onCleanup(() =>
                container.removeEventListener('scroll', this.scrollListener),
            );
        });

        // The textarea uses formControlName, so the disabled state has to come
        // from the control itself: a [disabled] binding on the element is
        // ignored by Angular and logs a warning.
        effect(() => {
            const control = this.newPlayFormGroup.get('newPlayControl');
            if (!control) return;

            if (this.isDisabled) {
                control.disable({ emitEvent: false });
            } else {
                control.enable({ emitEvent: false });
            }
        });
    }

    async ngOnChanges(changes: SimpleChanges): Promise<void> {
        const gameIdChange = changes['gameId'];
        if (!gameIdChange) return;

        const previousGameId = gameIdChange.previousValue as string | undefined;
        if (previousGameId) {
            this.saveDraft(previousGameId);
        }

        await this.openSession(this.gameId());
    }

    /**
     * Loads everything a session needs. ngOnChanges also covers the first
     * change, so there is no ngOnInit doing the same work a second time.
     *
     * Every step re-checks `openingGameId`: switching sessions quickly leaves
     * more than one load in flight, and the slower one must not overwrite the
     * session the user is actually looking at.
     */
    private async openSession(gameId: string): Promise<void> {
        this.openingGameId = gameId;
        this.stickToBottom = true;
        this.pendingPrepend = null;

        if (!this.loading()) {
            this.initialLoading.set(true);
        }

        await this.loadGame(gameId);
        if (this.openingGameId !== gameId) return;

        await this.loadInitialPlays(gameId);
        if (this.openingGameId !== gameId) return;

        this.restoreDraft(gameId);
        this.initialLoading.set(false);
    }

    private async loadGame(gameId: string): Promise<void> {
        try {
            const game = await this.gameService.getById(gameId);
            if (this.openingGameId !== gameId) return;

            this.game.set(game);
            this.gameLanguageControl
                .get('gameLanguage')
                ?.setValue(game.gameLanguage, { emitEvent: false });
        } catch (error) {
            console.error('Failed to load game', error);
        }
    }

    private fetchPlays(page: number): Promise<PagedSearch<Play>> {
        return this.playService.getPlays(
            this.gameId(),
            'desc',
            PLAYS_PAGE_SIZE,
            page,
        );
    }

    private reportPlaysError(error: unknown): void {
        this.snackBar.addError(
            $localize`Something went wrong while attempting to get the play list.`,
        );
        console.error('Failed to load plays', error);
    }

    private async loadInitialPlays(gameId: string): Promise<void> {
        this.currentPage = 1;
        this.allPlaysLoaded = false;
        this.isLoadingMore.set(false);
        this.loadFailed.set(false);

        try {
            const result = await this.fetchPlays(1);
            if (this.openingGameId !== gameId) return;

            this.plays.set([...(result.items ?? [])].reverse());
        } catch (error) {
            if (this.openingGameId !== gameId) return;

            this.reportPlaysError(error);
            this.loadFailed.set(true);
            this.plays.set([]);
        }
    }

    async loadMorePlays(): Promise<void> {
        if (this.isLoadingMore() || this.allPlaysLoaded || this.pendingPrepend) {
            return;
        }

        this.isLoadingMore.set(true);
        const nextPage = this.currentPage + 1;

        let result: PagedSearch<Play>;
        try {
            result = await this.fetchPlays(nextPage);
        } catch (error) {
            // A transient network failure must not disable pagination forever,
            // so currentPage stays put and allPlaysLoaded is left alone.
            this.reportPlaysError(error);
            this.isLoadingMore.set(false);
            return;
        }

        this.currentPage = nextPage;

        const olderPlays = [...(result.items ?? [])].reverse();

        if (olderPlays.length === 0) {
            this.allPlaysLoaded = true;
            this.isLoadingMore.set(false);
            return;
        }

        const container = this.messagesContainer()?.nativeElement;

        // Measured before the older plays land, and compensated by the render
        // effect once they are on screen. Reading the new height here instead
        // would give the pre-render value and drop the reader near the top.
        this.pendingPrepend = container
            ? { previousScrollHeight: container.scrollHeight }
            : null;

        this.plays.update((current) => [...olderPlays, ...current]);
        this.isLoadingMore.set(false);

        if (olderPlays.length < PLAYS_PAGE_SIZE) {
            this.allPlaysLoaded = true;
        }
    }

    /** Runs outside the Angular zone: keep it cheap. */
    onScroll(): void {
        const container = this.messagesContainer()?.nativeElement;
        if (!container) return;

        const distanceFromBottom =
            container.scrollHeight - container.scrollTop - container.clientHeight;
        this.stickToBottom = distanceFromBottom <= STICK_TO_BOTTOM_THRESHOLD_PX;

        if (
            this.pendingPrepend ||
            this.isLoadingMore() ||
            this.allPlaysLoaded ||
            container.scrollTop > LOAD_MORE_SCROLL_THRESHOLD_PX
        ) {
            return;
        }

        this.ngZone.run(() => this.loadMorePlays());
    }

    async onSubmit(initialPlay: boolean = false): Promise<void> {
        if (this.isCommonUser) return;

        const prompt: string = (
            this.newPlayFormGroup.get('newPlayControl')?.value ?? ''
        ).trim();

        if (!initialPlay && !prompt) return;

        this.loading.set(true);
        const newPlay: NewPlay = {
            gameId: this.gameId(),
            prompt: prompt || INITIAL_PLAY_PROMPT,
        };

        try {
            await this.playService.streamNewPlay(newPlay, (chunk: StreamPlay) =>
                this.handleStreamEvent(chunk, prompt, initialPlay),
            );
        } catch (error) {
            // The stream reports its own failures through the 'Error' event and
            // has already told the user about them. Only a rejection that never
            // produced one is reported here.
            if (!this.streamFailed) {
                console.error('Failed to stream a new play', error);
                this.loading.set(false);
                this.snackBar.addError(
                    $localize`Something went wrong. Please try again.`,
                );
            }
        }
    }

    private handleStreamEvent(
        chunk: StreamPlay,
        prompt: string,
        initialPlay: boolean,
    ): void {
        switch (chunk.eventType) {
            case 'Start':
                this.handleStreamStart(prompt, initialPlay);
                break;
            case 'Chunk':
                this.handleStreamChunk(chunk.content);
                break;
            case 'End':
                this.handleStreamEnd();
                break;
            case 'Error':
                this.handleStreamError();
                break;
        }
    }

    /**
     * Shows the player's own message and an empty Game Master bubble right away,
     * before the server has confirmed either. Both are dropped again if the
     * stream fails.
     */
    private handleStreamStart(prompt: string, initialPlay: boolean): void {
        const playsToAdd: Play[] = [];

        if (!initialPlay) {
            playsToAdd.push(this.createPendingPlay('Protagonist', prompt));
        }
        playsToAdd.push(this.createPendingPlay('GameMaster', ''));

        this.streamedPlaysStartIndex = this.plays().length;
        this.plays.update((current) => [...current, ...playsToAdd]);
        this.stickToBottom = true;
    }

    private handleStreamChunk(content: string): void {
        // Replacing the last play is what actually notifies the signal. The old
        // version mutated it and returned the same reference, so nothing was
        // emitted -- the text only reached the screen because zone.js happened
        // to run change detection right after the callback.
        this.plays.update((current) => {
            if (current.length === 0) return current;

            const lastIndex = current.length - 1;
            const updated = [...current];
            updated[lastIndex] = {
                ...updated[lastIndex],
                response: updated[lastIndex].response + content,
            };
            return updated;
        });
    }

    private handleStreamEnd(): void {
        this.loading.set(false);
        this.streamedPlaysStartIndex = null;

        if (!this.streamFailed) {
            this.newPlayFormGroup.get('newPlayControl')?.reset();
            this.removeGameCache(this.gameId());
            this.gamePlayed.emit(this.gameId());
        }
        this.streamFailed = false;

        const textArea = this.textAreaContainer()?.nativeElement;
        if (textArea) {
            this.adjustTextAreaHeightElement(textArea);
            textArea.focus();
        }
    }

    private handleStreamError(): void {
        if (this.streamedPlaysStartIndex !== null) {
            const startIndex = this.streamedPlaysStartIndex;
            this.plays.update((current) => current.slice(0, startIndex));
            this.streamedPlaysStartIndex = null;
        }

        this.streamFailed = true;
        this.loading.set(false);
        this.snackBar.addError(
            $localize`The gods have not answered your call. Speak again, brave adventurer!`,
        );
    }

    /** A play that only exists in the browser until the server sends the real one. */
    private createPendingPlay(
        playType: Play['playType'],
        response: string,
    ): Play {
        return {
            // Local id, distinct from any server id. The list is tracked by id,
            // and streaming replaces the last play on every chunk -- without a
            // stable key Angular would rebuild its node on each one.
            id: `pending-${++this.pendingPlaySequence}`,
            playType,
            response,
            createdAt: new Date(),
        };
    }

    adjustTextAreaHeightEvent(event: Event): void {
        const textArea = event.target as HTMLTextAreaElement;
        this.adjustTextAreaHeightElement(textArea);
    }

    onInputChange(event: Event): void {
        this.adjustTextAreaHeightEvent(event);
        this.saveDraft(this.gameId());

        // A growing textarea shrinks the message viewport. Nothing about the
        // play list changed, so the render effect will not fire on its own and
        // the newest message would slip behind the input.
        this.applyScrollPosition();
    }

    adjustTextAreaHeightElement(textArea: HTMLTextAreaElement): void {
        textArea.style.height = 'auto';
        textArea.style.height = `${textArea.scrollHeight}px`;
    }

    /**
     * The single place that moves the message list. Idempotent, so both
     * triggers can call it freely.
     */
    private applyScrollPosition(): void {
        const container = this.messagesContainer()?.nativeElement;
        if (!container) return;

        if (this.pendingPrepend) {
            container.scrollTop +=
                container.scrollHeight - this.pendingPrepend.previousScrollHeight;
            this.pendingPrepend = null;
            return;
        }

        if (!this.stickToBottom) return;

        if (this.isStreamedPlayTallerThanViewport(container)) {
            // Latch it. Deriving this on every call would un-freeze the list the
            // moment the stream ends, because the condition depends on loading()
            // -- and the end of a stream resizes the input area, which triggers
            // another pass. Following only resumes when the player scrolls back
            // down to the bottom themselves.
            this.stickToBottom = false;
            return;
        }

        container.scrollTop = container.scrollHeight;
    }

    /**
     * While the Game Master is writing, a reply taller than the viewport stops
     * being followed so the player can read it from the beginning. Only asked
     * while streaming: a session whose last play is tall still opens at the end.
     */
    private isStreamedPlayTallerThanViewport(container: HTMLDivElement): boolean {
        if (!this.loading()) return false;

        const lastPlay = this.messagesList()?.nativeElement
            .lastElementChild as HTMLElement | null;

        return !!lastPlay && lastPlay.offsetHeight > container.clientHeight;
    }
}
