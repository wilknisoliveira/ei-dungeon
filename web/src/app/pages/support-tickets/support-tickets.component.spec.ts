import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { TestBed } from '@angular/core/testing';
import { MatSelectHarness } from '@angular/material/select/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { SupportTicketService } from 'src/app/service/support-tickets/support-ticket.service';
import { SupportTicketDetail, SupportTicketPage } from 'src/app/types/support-tickets/support-ticket';
import { SupportTicketsComponent } from './support-tickets.component';

describe('SupportTicketsComponent', () => {
    const id = '3617ec15-fae5-4cfc-a613-ddc845970b80';
    const date = '2026-11-25T10:00:00Z';
    const ticket: SupportTicketDetail = {
        id,
        subject: 'Help with sign-in',
        type: 'Help',
        status: 'Open',
        createdAt: date,
        updatedAt: date,
        rating: null,
        messages: [{ id: 'initial', body: 'I have a question.', createdAt: date, isFromTeam: false }],
    };
    const page: SupportTicketPage = { currentPage: 1, pageSize: 10, totalResults: 1, sortDirection: 'desc', items: [ticket] };
    let api: jasmine.SpyObj<SupportTicketService>;
    let snackbar: jasmine.SpyObj<SnackbarService>;
    let harness: RouterTestingHarness;

    beforeEach(() => {
        api = jasmine.createSpyObj<SupportTicketService>('SupportTicketService', ['list', 'detail', 'create', 'reply', 'rate']);
        api.list.and.returnValue(of(page));
        api.detail.and.returnValue(of(ticket));
        snackbar = jasmine.createSpyObj<SnackbarService>('SnackbarService', ['addError']);
        TestBed.configureTestingModule({
            providers: [
                provideRouter([{ path: 'support-tickets', component: SupportTicketsComponent }]),
                provideNoopAnimations(),
                { provide: SupportTicketService, useValue: api },
                { provide: SnackbarService, useValue: snackbar },
            ],
        });
    });

    async function open(conversationId?: string): Promise<void> {
        harness = await RouterTestingHarness.create();
        const url = conversationId ? `/support-tickets?ticket=${conversationId}` : '/support-tickets';
        await harness.navigateByUrl(url, SupportTicketsComponent);
    }

    function button(text: string): HTMLButtonElement | undefined {
        return Array.from(harness.routeNativeElement!.querySelectorAll('button')).find(item => item.textContent?.trim() === text);
    }

    function type(selector: string, value: string): void {
        const control = harness.routeNativeElement!.querySelector<HTMLInputElement | HTMLTextAreaElement>(selector)!;
        control.value = value;
        control.dispatchEvent(new Event('input', { bubbles: true }));
    }

    it('shows conversation history with type, status, dates, and a detail link', async () => {
        await open();
        expect(api.list).toHaveBeenCalledOnceWith(1, 10);
        const entry = harness.routeNativeElement!.querySelector('.history-entry')!;
        expect(entry.querySelector('a')?.getAttribute('href')).toBe(`/support-tickets?ticket=${id}`);
        expect(entry.textContent).toContain('Help request');
        expect(entry.textContent).toContain('Open');
        expect(entry.querySelectorAll('time')).toHaveSize(2);
    });

    it('opens a linked conversation and submits a reply with intentional trimming', async () => {
        await open(id);
        expect(api.detail).toHaveBeenCalledOnceWith(id);
        expect(harness.routeNativeElement!.textContent).toContain('I have a question.');
        api.reply.and.returnValue(of({ ...ticket, messages: [...ticket.messages, { id: 'reply', body: 'Thanks', createdAt: date, isFromTeam: false }] }));

        type('textarea', '  Thanks  ');
        button('Reply')!.click();
        harness.detectChanges();

        expect(api.reply).toHaveBeenCalledOnceWith(id, 'Thanks');
        expect(harness.routeNativeElement!.textContent).toContain('Thanks');
    });

    it('validates a new message and opens the saved conversation', async () => {
        await open();
        api.create.and.returnValue(of(ticket));
        button('New message')!.click();
        harness.detectChanges();
        button('Send message')!.click();
        expect(api.create).not.toHaveBeenCalled();

        const select = await TestbedHarnessEnvironment.loader(harness.fixture).getHarness(MatSelectHarness);
        await select.clickOptions({ text: 'Help request' });
        type('input[formcontrolname="subject"]', '   Help with sign-in  ');
        type('textarea', 'I have a question.');
        button('Send message')!.click();
        await harness.fixture.whenStable();

        expect(api.create).toHaveBeenCalledOnceWith({ type: 'Help', subject: 'Help with sign-in', message: 'I have a question.' });
        expect(TestBed.inject(Router).url).toBe(`/support-tickets?ticket=${id}`);
    });

    it('shows a read-only closed conversation and accepts one optional rating', async () => {
        const closed = { ...ticket, status: 'Closed' as const };
        api.detail.and.returnValue(of(closed));
        api.rate.and.returnValue(of({ ...closed, rating: 4 }));
        await open(id);
        expect(harness.routeNativeElement!.textContent).toContain('Conversation closed');
        expect(harness.routeNativeElement!.querySelector('textarea')).toBeNull();
        expect(button('Submit rating')?.disabled).toBeTrue();
        expect(button('New message')).toBeDefined();

        const choices = harness.routeNativeElement!.querySelectorAll<HTMLInputElement>('input[type="radio"]');
        choices[3].click();
        harness.detectChanges();
        button('Submit rating')!.click();
        harness.detectChanges();

        expect(api.rate).toHaveBeenCalledOnceWith(id, 4);
        expect(api.list).toHaveBeenCalledTimes(1);
        expect(harness.routeNativeElement!.textContent).toContain('Your rating: 4 out of 5 stars.');
        expect(button('Submit rating')).toBeUndefined();
    });

    it('redacts a failed detail request and offers a retry', async () => {
        api.detail.and.returnValue(throwError(() => new Error('Private API details')));
        await open(id);
        const error = harness.routeNativeElement!.querySelector('[role="alert"]');
        expect(error?.textContent).toContain('Could not load this conversation.');
        expect(harness.routeNativeElement!.textContent).not.toContain('Private API details');
        api.detail.and.returnValue(of(ticket));
        button('Try again')!.click();
        harness.detectChanges();
        expect(harness.routeNativeElement!.textContent).toContain('I have a question.');
    });
});
