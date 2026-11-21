export const ADMIN_ROLES = ['Admin', 'CommonUser', 'PremiumUser'] as const;
export type AdminRole = typeof ADMIN_ROLES[number];

export interface AdminUser {
    id: string;
    email: string;
}

export interface RoleMembership {
    name: string;
    users: string[];
}

export interface AdminQuery {
    name: string;
    sortDirection: 'asc' | 'desc';
    pageSize: number;
    page: number;
}

const normalizeEmail = (email: string): string => email.trim().toUpperCase();

export function roleForUser(user: AdminUser, roles: readonly RoleMembership[]): AdminRole | null {
    const memberships = roles.filter(group =>
        group.users.some(email => normalizeEmail(email) === normalizeEmail(user.email)));
    if (memberships.length !== 1) return null;
    const role = memberships[0].name;
    return ADMIN_ROLES.find(candidate => candidate === role) ?? null;
}

export function isLastAdmin(user: AdminUser, roles: readonly RoleMembership[]): boolean {
    if (roleForUser(user, roles) !== 'Admin') return false;
    const admins = roles.filter(group => group.name === 'Admin').flatMap(group => group.users);
    return new Set(admins.map(normalizeEmail)).size === 1;
}

export function roleLabel(role: AdminRole): string {
    switch (role) {
        case 'Admin': return 'Administrator';
        case 'CommonUser': return 'Common user';
        case 'PremiumUser': return 'Premium user';
    }
}
