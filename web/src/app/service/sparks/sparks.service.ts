import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { PUBLIC_AUTH } from 'src/app/types/auth/email-auth';
import { environment } from 'src/environments/environment';

export interface SparkStatus {
    balance: number;
    expiresAt: string | null;
    canRequest: boolean;
    requestKind: 'Initial' | 'Refill';
    queueState: 'Offered' | 'Waiting' | null;
    queueRequestId: string | null;
    serverTime?: string;
}

@Injectable({ providedIn: 'root' })
export class SparksService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.api}/api/sparks`;
    private readonly current = signal<SparkStatus | null>(null);
    private readonly pending = signal(false);
    private readonly failed = signal(false);
    private readonly clockOffset = signal(0);
    private revision = 0;

    readonly status = this.current.asReadonly();
    readonly busy = this.pending.asReadonly();
    readonly error = this.failed.asReadonly();
    readonly serverOffset = this.clockOffset.asReadonly();

    async getInitialGrant(): Promise<number> {
        try {
            const { initialGrant } = await firstValueFrom(this.http.get<{ initialGrant: number }>(
                `${this.url}/availability`,
                { context: new HttpContext().set(PUBLIC_AUTH, true) },
            ));
            return initialGrant;
        } catch {
            return 15;
        }
    }

    refresh(): Promise<void> {
        if (this.pending()) return Promise.resolve();
        return this.update(() => firstValueFrom(this.http.get<SparkStatus>(this.url)));
    }

    request(): Promise<void> {
        return this.update(() => firstValueFrom(this.http.post<SparkStatus>(this.url, null)));
    }

    decide(accepted: boolean): Promise<void> {
        const id = this.current()?.queueRequestId;
        if (!id) return Promise.resolve();
        return this.update(() => firstValueFrom(this.http.patch<SparkStatus>(
            `${this.url}/queue/${id}`,
            { accepted },
        )));
    }

    clear(): void {
        this.revision++;
        this.current.set(null);
        this.pending.set(false);
        this.failed.set(false);
        this.clockOffset.set(0);
    }

    private async update(send: () => Promise<SparkStatus>): Promise<void> {
        if (this.pending()) return;
        const revision = this.revision;
        this.pending.set(true);
        this.failed.set(false);
        try {
            const status = await send();
            if (revision !== this.revision) return;
            const serverTime = Date.parse(status.serverTime ?? '');
            if (Number.isFinite(serverTime)) this.clockOffset.set(serverTime - Date.now());
            this.current.set(status);
        } catch {
            if (revision === this.revision) this.failed.set(true);
        } finally {
            if (revision === this.revision) this.pending.set(false);
        }
    }
}
