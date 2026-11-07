import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { lastValueFrom, Observable } from 'rxjs';
import { Game } from 'src/app/types/game/game';
import { NewGame } from 'src/app/types/game/new-game';
import { PagedSearch } from 'src/app/types/general/paged-search';
import { environment } from 'src/environments/environment';

@Injectable({
    providedIn: 'root',
})
export class GameService {
    private baseUrl = environment.api;

    constructor(private http: HttpClient) {}

    async getGames(
        sortDirection: string,
        pageSize: number,
        page: number
    ): Promise<PagedSearch<Game>> {
        const params = new HttpParams()
            .set('sortDirection', sortDirection)
            .set('pageSize', pageSize)
            .set('page', page);

        const gamesPaged$ = this.http.get<PagedSearch<Game>>(
            `${this.baseUrl}/api/Game`,
            { params }
        );

        return lastValueFrom(gamesPaged$);
    }

    async getById(id: string): Promise<Game> {
        const game$ = this.http.get<Game>(`${this.baseUrl}/api/Game/${id}`);

        return lastValueFrom(game$);
    }

    newGame(newGame: NewGame): Observable<Game> {
        return this.http.post<Game>(`${this.baseUrl}/api/Game`, newGame);
    }

    patchGame(id: string, updates: { name?: string; gameLanguage?: string }): Observable<Game> {
        return this.http.patch<Game>(`${this.baseUrl}/api/Game/${id}`, updates);
    }

    deleteGame(id: string): Observable<Object> {
        return this.http.delete(`${this.baseUrl}/api/Game/${id}`);
    }
}
