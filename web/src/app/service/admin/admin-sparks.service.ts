import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from 'src/environments/environment';

export interface CycleSummary {
    id: string;
    startsAt: string;
    endsAt: string;
    initialCapacity: number;
    capacity: number;
    state: 'Open' | 'Blocked' | 'Closed';
    blockReason: string | null;
}

export interface SparkSettings {
    monthlyCapacity: number;
    initialGrant: number;
    refillLot: number;
    estimatedCostPerSpark: number;
    closingDay: number;
    renewalLeadDays: number;
}

export interface SparkDashboard {
    cycle: (CycleSummary & { available: number; controlLine: number }) | null;
    cycles: CycleSummary[];
    settings: SparkSettings;
    snapshots: { date: string; capacity: number; available: number; controlLine: number }[];
    changes: {
        occurredAt: string;
        previousCapacity: number | null;
        capacity: number | null;
        previousState?: string | null;
        state?: string | null;
        adminId?: string | null;
    }[];
    queue: { initialWaiting: number; refillWaiting: number; declined: number; declinedDate: string };
    openRouter: {
        dollars: number | null;
        estimatedSparks: number | null;
        observedAt: string | null;
        stale: boolean;
    } | null;
}

@Injectable({ providedIn: 'root' })
export class AdminSparksService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.api}/api/admin/sparks`;

    getDashboard(cycleId?: string) {
        const params = cycleId ? new HttpParams().set('cycleId', cycleId) : undefined;
        return this.http.get<SparkDashboard>(this.url, { params });
    }

    saveSettings(settings: SparkSettings) {
        return this.http.patch<void>(`${this.url}/settings`, settings);
    }

    increase(cycleId: string, amount: number) {
        return this.http.post<void>(`${this.url}/cycles/${cycleId}/increases`, { amount });
    }

    reset(cycleId: string) {
        return this.http.post<void>(`${this.url}/cycles/${cycleId}/reset`, null);
    }
}
