declare const GOOGLE_CLIENT_ID: string | undefined;
declare const API_URL: string | undefined;

export const environment = {
    googleClientId: typeof GOOGLE_CLIENT_ID === 'string' ? GOOGLE_CLIENT_ID : '',
    production: true,
    api: typeof API_URL === 'string' && API_URL ? API_URL : 'http://localhost:8080',
};
