declare const API_URL: string | undefined;

export const environment = {
    production: true,
    api: typeof API_URL === 'string' && API_URL ? API_URL : 'http://localhost:8080',
};
