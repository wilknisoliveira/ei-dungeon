export interface Play {
    id: string;
    playType: 'Protagonist' | 'GameMaster' | 'Summary';
    response: string;
    createdAt: Date;
}
