export interface Game {
    id: string;
    name: string;
    protagonistName: string;
    ownerUserId: string;
    gameLanguage: string;
    gameStatus: string;
    lastPlayedAt: Date | null;
}
