import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { SparksService } from 'src/app/service/sparks/sparks.service';
import { environment } from 'src/environments/environment';
import { SparksPanelComponent } from './sparks-panel.component';

describe('SparksPanelComponent', () => {
    it('leaves an offer undecided when closed and submits only an explicit decision', async () => {
        TestBed.configureTestingModule({
            imports: [SparksPanelComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()],
        });
        const sparks = TestBed.inject(SparksService);
        const http = TestBed.inject(HttpTestingController);
        const dialog = TestBed.inject(MatDialog);
        const status = {
            balance: 0, expiresAt: null, canRequest: false, requestKind: 'Initial',
            queueState: 'Offered', queueRequestId: 'offer',
        };
        const request = sparks.refresh();
        http.expectOne(`${environment.api}/api/sparks`).flush(status);
        await request;
        const closed = dialog.open(SparksPanelComponent);
        TestBed.tick();
        closed.close();
        http.expectNone(`${environment.api}/api/sparks/queue/offer`);

        const ref = dialog.open(SparksPanelComponent);
        TestBed.tick();
        ref.componentInstance.sparks.decide(true);
        const decision = http.expectOne(`${environment.api}/api/sparks/queue/offer`);
        expect(decision.request.body).toEqual({ accepted: true });
        decision.flush({ ...status, queueState: 'Waiting' });
        ref.close();
        http.verify();
    });
});
