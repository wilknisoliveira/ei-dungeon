export type GoogleAuthPurpose = 'SignIn' | 'Link' | 'CreatePassword';

export interface GoogleChallenge {
    challengeId: string;
    nonce: string;
    expiresAt: string;
}

export interface GoogleCredential {
    challengeId: string;
    credential: string;
}

export interface CurrentAccount {
    id: string;
    email: string;
    role: string;
    hasPassword: boolean;
    googleLinked: boolean;
}

