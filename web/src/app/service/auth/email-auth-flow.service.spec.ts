import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { SESSION_STORAGE } from 'src/app/core/storage/app-storage';
import { EmailAuthFlowService } from './email-auth-flow.service';

describe('EmailAuthFlowService', () => {
    let service: EmailAuthFlowService;
    let values: Map<string, string>;
    const create = () => TestBed.runInInjectionContext(() => new EmailAuthFlowService());
    beforeEach(() => {
        values = new Map();
        TestBed.configureTestingModule({ providers: [{ provide: SESSION_STORAGE, useValue: {
            getItem: (key: string) => values.get(key) ?? null,
            setItem: (key: string, value: string) => values.set(key, value),
            removeItem: (key: string) => values.delete(key),
        } }] });
        service = TestBed.inject(EmailAuthFlowService);
    });
    it('retains signup context and deadline through reload and email edits without secrets', () => {
        service.registered('player', ' p@example.com ');
        const restored = create();
        expect(restored.context('verification').email).toBe('p@example.com');
        expect(restored.context('verification').userName).toBe('player');
        expect(restored.remaining('verification')).toBe(60);
        restored.setEmail('verification', 'other@example.com');
        expect(restored.remaining('verification')).toBe(60);
        expect(restored.context('verification').requested).toBeFalse();
        expect(values.get('email-auth-flow')).not.toContain('password');
        expect(values.get('email-auth-flow')).not.toContain('code');
    });
    it('only reuses signup email for matching username', () => {
        service.registered('player', 'p@example.com');
        service.prepareVerification('player');
        expect(service.context('verification').email).toBe('p@example.com');
        service.prepareVerification('someoneelse');
        expect(service.context('verification').email).toBe('');
    });
    it('shares request waits, preserves longer waits and does not block confirmation after signup', () => {
        service.requested('verification');
        expect(service.remaining('recovery')).toBe(60);
        // The request cooldown must not block entering a code already received.
        expect(service.remaining('verification', Date.now(), false)).toBe(0);
        service.wait('recovery', 120, true);
        service.requested('verification');
        expect(service.remaining('verification')).toBe(120);
        service.complete('verification');
        expect(service.context('verification').email).toBe('');
        expect(service.remaining('recovery')).toBe(120);
    });
    it('handles empty throttles and numeric Retry-After', () => {
        service.error(new HttpErrorResponse({ status: 429, headers: new HttpHeaders({ 'Retry-After': '90' }) }), 'recovery');
        expect(service.remaining('recovery')).toBe(90);
        service.error(new HttpErrorResponse({ status: 429 }), 'recovery');
        expect(service.remaining('recovery')).toBe(90);
    });
    it('maps field keys without exposing server messages', () => {
        const result = service.error(new HttpErrorResponse({ status: 400, error: {
            errors: { Email: ['private'], Code: ['internal'], NewPassword: ['secret'] },
        } }), 'recovery', true);
        expect(Object.keys(result.fields)).toEqual(['email', 'code', 'newPassword']);
        expect(JSON.stringify(result)).not.toContain('private');
        expect(service.error(new HttpErrorResponse({ status: 400, error: { code: 'invalid_verification_code' } }), 'verification').message).toContain('could not be confirmed');
        expect(service.error(new HttpErrorResponse({ status: 0 }), 'recovery', true).message).toContain('could not confirm the result');
        expect(service.error(new HttpErrorResponse({ status: 500 }), 'recovery').message).toContain('Something went wrong');
    });
    it('ignores corrupt storage and tolerates denied storage', () => {
        values.set('email-auth-flow', '{broken');
        expect(create().context('recovery').email).toBe('');
        const storage = TestBed.inject(SESSION_STORAGE);
        spyOn(storage, 'setItem').and.throwError('denied');
        expect(() => service.registered('player', 'p@example.com')).not.toThrow();
    });
    it('expires deadlines against absolute time', () => {
        service.requested('verification');
        expect(service.remaining('verification', Date.now() + 61000)).toBe(0);
    });
});
