import { CurrentAccount, GoogleCredential } from './google-auth';

describe('Google auth contracts', () => {
    it('keeps current account capability data independent of removed profile fields', () => {
        const account: CurrentAccount = { id: 'internal-id', email: 'player@example.com', role: 'CommonUser', hasPassword: false, googleLinked: true };
        expect(Object.keys(account)).not.toContain('username');
        expect(Object.keys(account)).not.toContain('fullName');
        const proof: GoogleCredential = { challengeId: 'attempt-id', credential: 'provider-token' };
        expect(Object.keys(proof)).not.toContain('userId');
    });
});
