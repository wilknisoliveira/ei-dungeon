import { ADMIN_ROLES, isLastAdmin, roleForUser, roleLabel } from './admin';

describe('Admin role membership', () => {
    const user = { id: 'account-id', email: 'Admin@Example.com' };

    it('matches normalized email and canonical roles without changing the account ID', () => {
        expect(roleForUser(user, [{ name: 'Admin', users: [' admin@example.com '] }])).toBe('Admin');
        expect(user.id).toBe('account-id');
        expect(ADMIN_ROLES).toEqual(['Admin', 'CommonUser', 'PremiumUser']);
    });

    it('does not resolve missing, ambiguous or unknown membership', () => {
        expect(roleForUser(user, [])).toBeNull();
        expect(roleForUser(user, [{ name: 'Other', users: [user.email] }])).toBeNull();
        expect(roleForUser(user, [
            { name: 'Admin', users: [user.email] },
            { name: 'CommonUser', users: [user.email] },
        ])).toBeNull();
    });

    it('protects a single distinct admin and permits multiple admins', () => {
        expect(isLastAdmin(user, [{ name: 'Admin', users: [user.email, ' admin@example.com '] }])).toBeTrue();
        expect(isLastAdmin(user, [{ name: 'Admin', users: [user.email, 'second@example.com'] }])).toBeFalse();
        expect(isLastAdmin(user, [{ name: 'CommonUser', users: [user.email] }])).toBeFalse();
        expect(isLastAdmin(user, [])).toBeFalse();
    });

    it('has a display label for each canonical role', () => {
        expect(ADMIN_ROLES.map(roleLabel)).toEqual(['Administrator', 'Common user', 'Premium user']);
    });
});
