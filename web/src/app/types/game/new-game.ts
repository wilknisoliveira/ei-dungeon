export interface NewGame {
    operationId?: string;
    protagonistName: string;
    protagonistDescription: string;
    name: string;
    protagonistRace: string;
    gameLanguage: string;
    skills: Skills;
}

export interface Skills {
    Strength: number;
    Dexterity: number;
    Constitution: number;
    Intelligence: number;
    Wisdom: number;
    Charisma: number;
}
