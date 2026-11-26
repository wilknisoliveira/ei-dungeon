import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, Injector, afterNextRender, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EMPTY, Observable, Subject, catchError, distinctUntilChanged, finalize, map, switchMap, tap } from 'rxjs';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { SupportTicketService } from 'src/app/service/support-tickets/support-ticket.service';
import {
    SUPPORT_TICKET_TYPES,
    SupportTicketDetail,
    SupportTicketPage,
    SupportTicketStatus,
    SupportTicketType,
    getSupportTicketId,
} from 'src/app/types/support-tickets/support-ticket';

const PAGE_SIZE = 10;
const SUBJECT_LIMIT = 200;
const MESSAGE_LIMIT = 8000;
const REQUIRED_TEXT = [Validators.required, Validators.pattern(/\S/)];

@Component({
    standalone: true,
    selector: 'app-support-tickets',
    templateUrl: './support-tickets.component.html',
    styleUrls: ['./support-tickets.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        DatePipe,
        ReactiveFormsModule,
        RouterLink,
        MatButtonModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatProgressBarModule,
        MatSelectModule,
    ],
})
export class SupportTicketsComponent {
    private readonly api = inject(SupportTicketService);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly snackbar = inject(SnackbarService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly injector = inject(Injector);
    private readonly listRequests = new Subject<number>();
    private readonly detailRequests = new Subject<string | null>();
    private readonly historyHeading = viewChild<ElementRef<HTMLElement>>('historyHeading');
    private readonly composeHeading = viewChild<ElementRef<HTMLElement>>('composeHeading');
    private readonly conversationHeading = viewChild<ElementRef<HTMLElement>>('conversationHeading');
    private requestedPage = 1;

    protected readonly page = signal<SupportTicketPage | null>(null);
    protected readonly selectedId = signal<string | null>(null);
    protected readonly detail = signal<SupportTicketDetail | null>(null);
    protected readonly composing = signal(false);
    protected readonly saving = signal(false);
    protected readonly listLoading = signal(false);
    protected readonly detailLoading = signal(false);
    protected readonly listError = signal(false);
    protected readonly detailError = signal(false);

    protected readonly types = SUPPORT_TICKET_TYPES;
    protected readonly typeLabels: Record<SupportTicketType, string> = {
        Help: $localize`:@@guildMailTypeHelp:Help request`,
        Bug: $localize`:@@guildMailTypeBug:Bug report`,
        Problem: $localize`:@@guildMailTypeProblem:Issue resolution`,
        Suggestion: $localize`:@@guildMailTypeSuggestion:Suggestion`,
    };
    protected readonly statusLabels: Record<SupportTicketStatus, string> = {
        Open: $localize`:@@guildMailStatusOpen:Open`,
        InProgress: $localize`:@@guildMailStatusInProgress:In progress`,
        WaitingForPlayer: $localize`:@@guildMailStatusWaiting:Waiting for your reply`,
        Closed: $localize`:@@guildMailStatusClosed:Closed`,
    };
    protected readonly subjectLimit = SUBJECT_LIMIT;
    protected readonly messageLimit = MESSAGE_LIMIT;
    protected readonly stars = [1, 2, 3, 4, 5];
    protected readonly createForm = new FormGroup({
        type: new FormControl<SupportTicketType | null>(null, Validators.required),
        subject: new FormControl('', { nonNullable: true, validators: [...REQUIRED_TEXT, Validators.maxLength(SUBJECT_LIMIT)] }),
        message: new FormControl('', { nonNullable: true, validators: [...REQUIRED_TEXT, Validators.maxLength(MESSAGE_LIMIT)] }),
    });
    protected readonly replyForm = new FormGroup({
        message: new FormControl('', { nonNullable: true, validators: [...REQUIRED_TEXT, Validators.maxLength(MESSAGE_LIMIT)] }),
    });
    protected readonly rating = new FormControl<number | null>(null);

    constructor() {
        this.listRequests.pipe(
            switchMap(page => {
                this.listLoading.set(true);
                this.listError.set(false);
                return this.api.list(page, PAGE_SIZE).pipe(
                    catchError(() => {
                        this.listError.set(true);
                        return EMPTY;
                    }),
                    finalize(() => this.listLoading.set(false)),
                );
            }),
            takeUntilDestroyed(this.destroyRef),
        ).subscribe(page => this.page.set(page));

        this.detailRequests.pipe(
            switchMap(id => {
                this.detail.set(null);
                this.detailError.set(false);
                if (!id) return EMPTY;

                this.detailLoading.set(true);
                return this.api.detail(id).pipe(
                    catchError(() => {
                        this.detailError.set(true);
                        return EMPTY;
                    }),
                    finalize(() => this.detailLoading.set(false)),
                );
            }),
            takeUntilDestroyed(this.destroyRef),
        ).subscribe(ticket => {
            this.detail.set(ticket);
            this.focusHeading(this.conversationHeading);
        });

        this.route.queryParamMap.pipe(
            map(params => getSupportTicketId(params.get('ticket'))),
            distinctUntilChanged(),
            tap(id => {
                if (!id && this.selectedId()) this.focusHeading(this.historyHeading);
                this.selectedId.set(id);
                this.composing.set(false);
                this.replyForm.reset();
                this.rating.reset();
            }),
            takeUntilDestroyed(this.destroyRef),
        ).subscribe(id => this.detailRequests.next(id));

        this.loadPage(1);
    }

    protected loadPage(page: number): void {
        this.requestedPage = page;
        this.listRequests.next(page);
    }

    protected retryList(): void {
        this.loadPage(this.requestedPage);
    }

    protected retryDetail(): void {
        this.detailRequests.next(this.selectedId());
    }

    protected startMessage(): void {
        this.composing.set(true);
        this.focusHeading(this.composeHeading);
    }

    protected cancelMessage(): void {
        this.composing.set(false);
        this.focusHeading(this.selectedId() ? this.conversationHeading : this.historyHeading);
    }

    protected submitMessage(): void {
        this.createForm.markAllAsTouched();
        const { type, subject, message } = this.createForm.getRawValue();
        if (this.createForm.invalid || !type || this.saving()) return;

        this.save(this.api.create({ type, subject: subject.trim(), message: message.trim() }), ticket => {
            this.createForm.reset();
            this.composing.set(false);
            this.loadPage(1);
            void this.router.navigate([], { relativeTo: this.route, queryParams: { ticket: ticket.id } });
        });
    }

    protected submitReply(ticket: SupportTicketDetail): void {
        this.replyForm.markAllAsTouched();
        if (this.replyForm.invalid || ticket.status === 'Closed' || this.saving()) return;

        this.save(this.api.reply(ticket.id, this.replyForm.controls.message.value.trim()), updated => {
            if (this.selectedId() === updated.id) {
                this.detail.set(updated);
                this.replyForm.reset();
            }
            this.loadPage(1);
        });
    }

    protected submitRating(ticket: SupportTicketDetail): void {
        const rating = this.rating.value;
        if (rating === null || !this.stars.includes(rating) || ticket.status !== 'Closed' || ticket.rating !== null || this.saving()) return;

        this.save(this.api.rate(ticket.id, rating), updated => {
            if (this.selectedId() === updated.id) this.detail.set(updated);
        });
    }

    private save(request: Observable<SupportTicketDetail>, onSuccess: (ticket: SupportTicketDetail) => void): void {
        this.saving.set(true);
        const selectedId = this.selectedId();
        request.pipe(
            takeUntilDestroyed(this.destroyRef),
            finalize(() => this.saving.set(false)),
        ).subscribe({
            next: onSuccess,
            error: (error: unknown) => {
                if (selectedId && selectedId === this.selectedId() && error instanceof HttpErrorResponse && error.status === 409) {
                    this.retryDetail();
                }
                this.snackbar.addError($localize`:@@guildMailSaveFailed:Could not save your update. Please try again.`);
            },
        });
    }

    private focusHeading(heading: () => ElementRef<HTMLElement> | undefined): void {
        afterNextRender(() => heading()?.nativeElement.focus(), { injector: this.injector });
    }
}
