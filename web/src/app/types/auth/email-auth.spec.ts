import { HttpContext } from '@angular/common/http';
import { PUBLIC_AUTH, RecoveryConfirmation } from './email-auth';

describe('Email auth contract', () => {
    it('requires explicit public metadata and preserves string codes', () => {
        const body: RecoveryConfirmation = { email: 'p@example.com', code: '000123', newPassword: 'abcd' };
        expect(new HttpContext().get(PUBLIC_AUTH)).toBeFalse();
        expect(body.code).toBe('000123');
    });
});
