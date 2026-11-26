import { DatePipe } from '@angular/common';
import {
    Component, ElementRef, Injector, OnInit, afterNextRender, effect, inject, input, signal, untracked, viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { Subscription } from 'rxjs';
import {
    AdminSupportTicketQuery,
    AdminSupportTicketService,
    AdminSupportTicketUpdate,
    SupportTicketSort,
} from 'src/app/service/admin/admin-support-ticket.service';
import {
    SUPPORT_TICKET_STATUSES,
    SUPPORT_TICKET_TYPES,
    SupportTicketDetail,
    SupportTicketPage,
    SupportTicketStatus,
    SupportTicketSummary,
    SupportTicketType,
} from 'src/app/types/support-tickets/support-ticket';

const initialQuery: AdminSupportTicketQuery = {
    sortBy: 'updatedAt',
    sortDirection: 'desc',
    page: 1,
    pageSize: 10,
};

@Component({
    selector: 'app-admin-support-tickets',
    imports: [
        DatePipe, ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatIconModule,
        MatInputModule, MatPaginatorModule, MatProgressBarModule, MatSelectModule, MatSortModule, MatTableModule,
    ],
    host: { lang: 'en' },
    templateUrl: './admin-support-tickets.component.html',
    styleUrl: './admin-support-tickets.component.scss',
})
export class AdminSupportTicketsComponent implements OnInit {
    private readonly service = inject(AdminSupportTicketService);
    private readonly formBuilder = inject(FormBuilder).nonNullable;
    private readonly injector = inject(Injector);
    private readonly listUntilDestroyed = takeUntilDestroyed<SupportTicketPage>();
    private readonly detailUntilDestroyed = takeUntilDestroyed<SupportTicketDetail>();
    private readonly backButton = viewChild<unknown, ElementRef<HTMLButtonElement>>('backButton', { read: ElementRef });
    private readonly playerEmailFilter = viewChild<unknown, ElementRef<HTMLInputElement>>('playerEmailFilter', { read: ElementRef });
    private listRequest?: Subscription;
    private detailRequest?: Subscription;
    private openTrigger?: HTMLButtonElement;

    readonly initialTicketId = input<string | null>(null);
    readonly query = signal(initialQuery);
    readonly tickets = signal<SupportTicketSummary[]>([]);
    readonly total = signal(0);
    readonly loading = signal(false);
    readonly listError = signal(false);
    readonly filterError = signal('');
    readonly selectedId = signal<string | null>(null);
    readonly ticket = signal<SupportTicketDetail | null>(null);
    readonly detailLoading = signal(false);
    readonly detailError = signal(false);
    readonly saving = signal(false);
    readonly saveError = signal(false);
    readonly saved = signal(false);

    readonly columns = ['subject', 'user', 'type', 'status', 'rating', 'createdAt', 'updatedAt'];
    readonly types = SUPPORT_TICKET_TYPES;
    readonly statuses = SUPPORT_TICKET_STATUSES;
    readonly ratings = [1, 2, 3, 4, 5];
    readonly typeLabels: Record<SupportTicketType, string> = {
        Help: 'Help request',
        Bug: 'Bug report',
        Problem: 'Problem resolution',
        Suggestion: 'Suggestion',
    };
    readonly statusLabels: Record<SupportTicketStatus, string> = {
        Open: 'Open',
        InProgress: 'In progress',
        WaitingForPlayer: 'Waiting for player',
        Closed: 'Closed',
    };

    readonly filters = this.formBuilder.group({
        userEmail: '',
        type: this.formBuilder.control<SupportTicketType | ''>(''),
        status: this.formBuilder.control<SupportTicketStatus | ''>(''),
        rating: this.formBuilder.control<number | 'unrated' | ''>(''),
        createdFrom: '',
        createdTo: '',
        updatedFrom: '',
        updatedTo: '',
    });
    readonly editor = this.formBuilder.group({
        type: this.formBuilder.control<SupportTicketType>('Help'),
        status: this.formBuilder.control<SupportTicketStatus>('Open'),
        message: ['', Validators.maxLength(8000)],
    });

    constructor() {
        effect(() => {
            const id = this.initialTicketId();
            if (id) untracked(() => this.open(id));
        });
    }

    ngOnInit(): void {
        this.load();
    }

    applyFilters(): void {
        const filters = this.filters.getRawValue();
        if ((filters.createdFrom && filters.createdTo && filters.createdFrom > filters.createdTo)
            || (filters.updatedFrom && filters.updatedTo && filters.updatedFrom > filters.updatedTo)) {
            this.filterError.set('Each end date must be on or after its start date.');
            return;
        }

        this.filterError.set('');
        this.query.update(query => ({
            ...query,
            userEmail: filters.userEmail.trim() || undefined,
            type: filters.type || undefined,
            status: filters.status || undefined,
            rating: filters.rating || undefined,
            createdFrom: filters.createdFrom || undefined,
            createdTo: filters.createdTo || undefined,
            updatedFrom: filters.updatedFrom || undefined,
            updatedTo: filters.updatedTo || undefined,
            page: 1,
        }));
        this.load();
    }

    clearFilters(): void {
        this.filters.reset();
        this.applyFilters();
    }

    sort(event: Sort): void {
        this.query.update(query => ({
            ...query,
            sortBy: event.active as SupportTicketSort,
            sortDirection: event.direction || 'desc',
            page: 1,
        }));
        this.load();
    }

    paginate(event: PageEvent): void {
        this.query.update(query => ({
            ...query,
            page: event.pageSize === query.pageSize ? event.pageIndex + 1 : 1,
            pageSize: event.pageSize,
        }));
        this.load();
    }

    typeLabel(type: SupportTicketType): string {
        return this.typeLabels[type];
    }

    statusLabel(status: SupportTicketStatus): string {
        return this.statusLabels[status];
    }

    load(): void {
        this.listRequest?.unsubscribe();
        this.loading.set(true);
        this.listError.set(false);
        const query = this.query();
        this.listRequest = this.service.list(query).pipe(this.listUntilDestroyed).subscribe({
            next: page => {
                const lastPage = Math.max(1, Math.ceil(page.totalResults / query.pageSize));
                if (query.page > lastPage) {
                    this.query.update(current => ({ ...current, page: lastPage }));
                    this.load();
                    return;
                }

                this.tickets.set(page.items);
                this.total.set(page.totalResults);
                this.loading.set(false);
            },
            error: () => {
                this.listError.set(true);
                this.loading.set(false);
            },
        });
    }

    open(id: string, trigger?: EventTarget | null): void {
        this.detailRequest?.unsubscribe();
        if (trigger && trigger instanceof HTMLButtonElement) {
            this.openTrigger = trigger;
            afterNextRender(() => this.backButton()?.nativeElement.focus(), { injector: this.injector });
        } else if (id !== this.selectedId()) {
            this.openTrigger = undefined;
        }
        this.selectedId.set(id);
        this.ticket.set(null);
        this.detailLoading.set(true);
        this.detailError.set(false);
        this.saved.set(false);
        this.saveError.set(false);
        this.detailRequest = this.service.get(id).pipe(this.detailUntilDestroyed).subscribe({
            next: ticket => {
                this.showTicket(ticket);
                this.detailLoading.set(false);
            },
            error: () => {
                this.detailError.set(true);
                this.detailLoading.set(false);
            },
        });
    }

    back(): void {
        if (this.saving()) return;

        this.detailRequest?.unsubscribe();
        this.selectedId.set(null);
        this.ticket.set(null);
        const trigger = this.openTrigger;
        this.openTrigger = undefined;
        afterNextRender(() => {
            const target = trigger?.isConnected ? trigger : this.playerEmailFilter()?.nativeElement;
            target?.focus();
        }, { injector: this.injector });
    }

    save(): void {
        const ticket = this.ticket();
        if (!ticket || this.saving() || this.editor.invalid) return;

        const { type, status, message } = this.editor.getRawValue();
        const update: AdminSupportTicketUpdate = {
            ...(type !== ticket.type && { type }),
            ...(status !== ticket.status && { status }),
            ...(message.trim() && { message: message.trim() }),
        };
        if (!Object.keys(update).length) return;

        this.saving.set(true);
        this.editor.disable();
        this.saved.set(false);
        this.saveError.set(false);
        this.service.update(ticket.id, update).pipe(this.detailUntilDestroyed).subscribe({
            next: updated => {
                this.editor.enable();
                this.saving.set(false);
                if (updated.id === this.selectedId()) {
                    this.showTicket(updated);
                    this.saved.set(true);
                }
                this.load();
            },
            error: () => {
                this.editor.enable();
                this.saving.set(false);
                if (ticket.id === this.selectedId()) {
                    this.saveError.set(true);
                }
            },
        });
    }

    private showTicket(ticket: SupportTicketDetail): void {
        this.ticket.set(ticket);
        this.editor.reset({ type: ticket.type, status: ticket.status, message: '' });
    }
}
