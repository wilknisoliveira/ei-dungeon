import { TestBed } from '@angular/core/testing';
import { provideRouter, ROUTES } from '@angular/router';
import { PagesModule } from './pages.module';

describe('PagesModule', () => {
    it('registers routes whose page implementations remain lazy', () => {
        TestBed.configureTestingModule({ imports: [PagesModule], providers: [provideRouter([])] });
        const registeredRoutes = TestBed.inject(ROUTES).flat();
        const admin = registeredRoutes.find(route => route.path === 'admin');
        expect(admin?.loadComponent).toBeDefined();
        expect(registeredRoutes.filter(route => route.path !== undefined).every(route => !route.component)).toBeTrue();
    });
});
