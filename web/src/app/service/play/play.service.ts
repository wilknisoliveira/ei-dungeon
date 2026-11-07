import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { lastValueFrom } from 'rxjs';
import { PagedSearch } from 'src/app/types/general/paged-search';
import { NewPlay } from 'src/app/types/play/new-play';
import { Play } from 'src/app/types/play/play';
import { StreamPlay } from 'src/app/types/play/stream-play';
import { environment } from 'src/environments/environment';
import { AuthService } from '../auth/auth.service';
import { SnackbarService } from '../snackbar/snackbar.service';

@Injectable({
    providedIn: 'root',
})
export class PlayService {
    private baseUrl = environment.api;

    constructor(
        private http: HttpClient,
        private authService: AuthService,
        private snackBar: SnackbarService,
        private router: Router,
    ) {}

    async getPlays(
        gameId: string,
        sortDirection: string,
        pageSize: number,
        page: number,
    ): Promise<PagedSearch<Play>> {
        const params = new HttpParams()
            .set('gameId', gameId)
            .set('sortDirection', sortDirection)
            .set('pageSize', pageSize)
            .set('page', page);

        const playPaged$ = this.http.get<PagedSearch<Play>>(
            `${this.baseUrl}/api/Play`,
            { params },
        );

        return lastValueFrom(playPaged$);
    }

    async streamNewPlay(
        newPlay: NewPlay,
        onChunk: (chunk: StreamPlay) => void,
    ): Promise<void> {
        let token = this.authService.getAuthToken();
        if (!token) {
            return Promise.reject(new Error('No token found'));
        }

        let response = await this.fetchStream(newPlay, token);

        if (!response.ok && response.status === 401) {
            const refreshToken = this.getStoredRefreshToken();
            if (refreshToken) {
                try {
                    const tokenObject = await this.authService.refreshToken(
                        token,
                        refreshToken,
                    );
                    token = tokenObject.accessToken;
                    response = await this.fetchStream(newPlay, token);
                } catch {
                    this.authService.logout();
                    this.snackBar.addError(
                        'Session expired. Please log in again.',
                    );
                    this.router.navigate(['login']);
                    return Promise.reject(new Error('Session expired'));
                }
            } else {
                this.authService.logout();
                this.snackBar.addError(
                    'Session expired. Please log in again.',
                );
                this.router.navigate(['login']);
                return Promise.reject(
                    new Error(`Request failed with status ${response.status}`),
                );
            }
        }

        if (!response.ok) {
            return Promise.reject(
                new Error(`Request failed with status ${response.status}`),
            );
        }

        if (!response.body) {
            return Promise.reject(new Error('No body received'));
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder('utf-8');

        let buffer = '';
        let processedCount = 0;

        while (true) {
            const { done, value } = await reader.read();

            if (done) {
                break;
            }

            buffer += decoder.decode(value, { stream: true });

            const trimmed = buffer.trim();
            const startIdx = trimmed.indexOf('[');
            if (startIdx === -1) continue;

            let jsonStr = trimmed.substring(startIdx);
            if (!jsonStr.endsWith(']')) {
                jsonStr += ']';
            }

            try {
                const all: StreamPlay[] = JSON.parse(jsonStr);
                for (let i = processedCount; i < all.length; i++) {
                    onChunk(all[i]);
                }
                processedCount = all.length;
            } catch {
                // Partial data — wait for more chunks
            }
        }
    }

    private async fetchStream(
        newPlay: NewPlay,
        token: string,
    ): Promise<Response> {
        return fetch(`${this.baseUrl}/api/Play`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                Authorization: `Bearer ${token}`,
            },
            body: JSON.stringify(newPlay),
        });
    }

    private getStoredRefreshToken(): string {
        const tokenJson = localStorage.getItem('tokenInfo');
        if (tokenJson) {
            return JSON.parse(tokenJson).refreshToken;
        }
        return '';
    }
}
