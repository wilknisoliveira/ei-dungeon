import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, ElementRef, OnInit, afterRenderEffect, computed, inject, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { firstValueFrom, Observable } from 'rxjs';
import { AdminSparksService, SparkDashboard } from 'src/app/service/admin/admin-sparks.service';
import { ThemeService } from 'src/app/service/theme/theme.service';

@Component({
    selector: 'app-admin-sparks',
    imports: [CurrencyPipe, DecimalPipe, ReactiveFormsModule, MatButtonModule,
        MatFormFieldModule, MatInputModule, MatSelectModule, MatProgressBarModule],
    host: { lang: 'en' },
    templateUrl: './admin-sparks.component.html',
    styleUrl: './admin-sparks.component.scss',
})
export class AdminSparksComponent implements OnInit {
    private readonly service = inject(AdminSparksService);
    private readonly theme = inject(ThemeService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fb = inject(FormBuilder).nonNullable;
    private readonly chart = viewChild<ElementRef<HTMLCanvasElement>>('chart');
    readonly dashboard = signal<SparkDashboard | null>(null);
    readonly increases = computed(() => this.dashboard()?.changes.filter(change => change.capacity !== null) ?? []);
    readonly busy = signal(false);
    readonly error = signal('');
    readonly feedback = signal('');
    readonly settings = this.fb.group({
        monthlyCapacity: [0, [Validators.required, Validators.min(0), Validators.max(2_147_483_647)]],
        initialGrant: [0, [Validators.required, Validators.min(1), Validators.max(2_147_483_647)]],
        refillLot: [0, [Validators.required, Validators.min(1), Validators.max(2_147_483_647)]],
        estimatedCostPerSpark: [0, [Validators.required, Validators.min(0.0000000001), Validators.max(1_000_000_000)]],
        closingDay: [10, [Validators.required, Validators.min(1), Validators.max(31)]],
        renewalLeadDays: [3, [Validators.required, Validators.min(0), Validators.max(31)]],
    });
    readonly addedCapacity = this.fb.control(0, [Validators.required, Validators.min(1), Validators.max(2_147_483_647)]);
    readonly fields = [
        { key: 'monthlyCapacity', label: 'Monthly capacity (Sparks)', step: 1 },
        { key: 'initialGrant', label: 'Initial grant (Sparks)', step: 1 },
        { key: 'refillLot', label: 'Refill (Sparks)', step: 1 },
        { key: 'estimatedCostPerSpark', label: 'Estimated cost per Spark (USD)', step: 'any' },
        { key: 'closingDay', label: 'Monthly closing day', step: 1 },
        { key: 'renewalLeadDays', label: 'Renewal lead (days)', step: 1 },
    ] as const;
    private readonly cycleDateFormat = new Intl.DateTimeFormat('en-US', {
        timeZone: 'America/Sao_Paulo', dateStyle: 'medium',
    });
    private readonly cycleTimeFormat = new Intl.DateTimeFormat('en-US', {
        timeZone: 'America/Sao_Paulo', dateStyle: 'medium', timeStyle: 'short',
    });

    constructor() {
        afterRenderEffect(() => {
            const data = this.dashboard();
            const chart = this.chart()?.nativeElement;
            this.theme.theme();
            if (data?.cycle && chart) this.drawChart(chart, data);
        });
    }

    ngOnInit(): void {
        void this.load();
    }

    async load(cycleId?: string): Promise<void> {
        if (this.busy()) return;
        this.busy.set(true);
        this.error.set('');
        try {
            await this.loadDashboard(cycleId);
        } catch {
            this.error.set('The Sparks dashboard could not be loaded.');
        } finally {
            this.busy.set(false);
        }
    }

    async saveSettings(): Promise<void> {
        const settings = this.settings.getRawValue();
        if (this.settings.invalid || this.fields.some(field => field.step === 1 && !Number.isInteger(settings[field.key]))) return;
        await this.mutate(this.service.saveSettings(settings), 'Future cycle settings saved.');
    }

    async increase(): Promise<void> {
        const cycle = this.dashboard()?.cycle;
        const amount = this.addedCapacity.value;
        if (!cycle || cycle.state === 'Closed' || this.addedCapacity.invalid || !Number.isInteger(amount)) return;
        await this.mutate(this.service.increase(cycle.id, amount), 'Capacity increased.');
        if (!this.error()) this.addedCapacity.reset();
    }

    async reset(): Promise<void> {
        const cycle = this.dashboard()?.cycle;
        if (!cycle || cycle.state !== 'Blocked' || cycle.blockReason !== 'InsufficientCredits') return;
        await this.mutate(this.service.reset(cycle.id), 'Cycle reopened.');
    }

    date(instant: string): string {
        return this.cycleDateFormat.format(new Date(instant));
    }

    dateTime(instant: string): string {
        return this.cycleTimeFormat.format(new Date(instant));
    }

    private async loadDashboard(cycleId?: string): Promise<void> {
        const dashboard = await firstValueFrom(this.service.getDashboard(cycleId));
        if (this.destroyRef.destroyed) return;
        this.dashboard.set(dashboard);
        this.settings.reset(dashboard.settings);
    }

    private async mutate(request: Observable<void>, feedback: string): Promise<void> {
        if (this.busy()) return;
        this.busy.set(true);
        this.error.set('');
        this.feedback.set('');
        try {
            await firstValueFrom(request);
        } catch {
            this.error.set('The change could not be saved. Check coverage or the values entered and try again.');
            this.busy.set(false);
            return;
        }
        this.feedback.set(feedback);
        try {
            await this.loadDashboard(this.dashboard()?.cycle?.id);
        } catch {
            this.error.set('The change was saved, but the dashboard could not be refreshed.');
        } finally {
            this.busy.set(false);
        }
    }

    private drawChart(canvas: HTMLCanvasElement, data: SparkDashboard): void {
        const context = canvas.getContext('2d');
        if (!context || !data.cycle) return;
        const theme = getComputedStyle(canvas);
        const { width, height } = canvas;
        const left = 58;
        const right = width - 16;
        const top = 22;
        const bottom = height - 34;
        const firstDay = Date.parse(data.cycle.startsAt);
        const lastDay = Date.parse(data.cycle.endsAt);
        const days = Math.max(86_400_000, lastDay - firstDay);
        const x = (day: string): number => left + Math.min(1, Math.max(0, (Date.parse(day) - firstDay) / days)) * (right - left);
        const values = data.snapshots.flatMap(snapshot => [snapshot.capacity, snapshot.available, snapshot.controlLine]);
        const ceiling = Math.max(1, data.cycle.capacity, ...values);
        const floor = Math.min(0, ...values);
        const y = (value: number): number => bottom - (value - floor) * (bottom - top) / (ceiling - floor);
        context.clearRect(0, 0, width, height);
        context.font = '14px Manrope, sans-serif';
        context.fillStyle = theme.getPropertyValue('--mat-sys-on-surface-variant').trim();
        context.strokeStyle = theme.getPropertyValue('--mat-sys-outline-variant').trim();
        context.lineWidth = 1;
        for (let tick = 0; tick <= 4; tick++) {
            const value = floor + (ceiling - floor) * tick / 4;
            context.beginPath();
            context.moveTo(left, y(value));
            context.lineTo(right, y(value));
            context.stroke();
            context.textAlign = 'right';
            context.fillText(Math.round(value).toLocaleString('en-US'), left - 8, y(value) + 4);
        }
        context.textAlign = 'left';
        context.fillText(this.cycleDay(data.cycle.startsAt), left, height - 8);
        context.textAlign = 'right';
        context.fillText(this.cycleDay(data.cycle.endsAt), right, height - 8);

        for (const change of this.increases()) {
            const position = x(change.occurredAt);
            context.beginPath();
            context.setLineDash([5, 5]);
            context.moveTo(position, top);
            context.lineTo(position, bottom);
            context.stroke();
            context.setLineDash([]);
        }

        const snapshots = [...data.snapshots].sort((a, b) => a.date.localeCompare(b.date));
        for (const [key, color] of [['available', '--mat-sys-primary'], ['controlLine', '--mat-sys-tertiary']] as const) {
            context.strokeStyle = theme.getPropertyValue(color).trim();
            context.fillStyle = context.strokeStyle;
            context.lineWidth = 2.5;
            context.beginPath();
            snapshots.forEach((snapshot, index) => {
                const position = [x(`${snapshot.date}T00:00:00-03:00`), y(snapshot[key])] as const;
                if (index === 0) context.moveTo(...position);
                else context.lineTo(...position);
            });
            context.stroke();
            for (const snapshot of snapshots) {
                context.beginPath();
                context.arc(x(`${snapshot.date}T00:00:00-03:00`), y(snapshot[key]), 3, 0, Math.PI * 2);
                context.fill();
            }
        }
    }

    private cycleDay(instant: string): string {
        return new Intl.DateTimeFormat('en-US', { timeZone: 'America/Sao_Paulo', month: 'short', day: 'numeric' }).format(new Date(instant));
    }
}
