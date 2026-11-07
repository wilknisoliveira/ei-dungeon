import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class TimezoneService {
    private cachedTimezone: string | null = null;

    getUserTimezone(): string {
        if (this.cachedTimezone) {
            return this.cachedTimezone;
        }

        try {
            const timezone =
                Intl.DateTimeFormat().resolvedOptions().timeZone;
            if (timezone) {
                this.cachedTimezone = timezone;
                return timezone;
            }
        } catch (e) {
            console.warn('Failed to detect timezone:', e);
        }

        return 'UTC';
    }

    getUtcOffset(): string {
        try {
            const now = new Date();
            const offsetMinutes = now.getTimezoneOffset();
            const sign = offsetMinutes <= 0 ? '+' : '-';
            const absOffset = Math.abs(offsetMinutes);
            const hours = String(Math.floor(absOffset / 60)).padStart(2, '0');
            const minutes = String(absOffset % 60).padStart(2, '0');
            return `${sign}${hours}:${minutes}`;
        } catch (e) {
            return '+00:00';
        }
    }

    formatDate(
        date: Date | string | number,
        options: Intl.DateTimeFormatOptions = {
            dateStyle: 'medium',
            timeStyle: 'short',
        },
        timezone?: string
    ): string {
        const dateObj =
            typeof date === 'string' || typeof date === 'number'
                ? new Date(date)
                : date;

        const tz = timezone || this.getUserTimezone();

        const formatOptions: Intl.DateTimeFormatOptions = {
            ...options,
            timeZone: tz,
        };

        try {
            return new Intl.DateTimeFormat(undefined, formatOptions).format(
                dateObj
            );
        } catch (e) {
            console.warn('Failed to format date:', e);
            return dateObj.toLocaleString();
        }
    }
}
