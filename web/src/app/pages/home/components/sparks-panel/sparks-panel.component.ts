import { DatePipe } from '@angular/common';
import { Component, Signal, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { SparksService } from 'src/app/service/sparks/sparks.service';

@Component({
    selector: 'app-sparks-panel',
    imports: [DatePipe, MatButtonModule, MatDialogModule, MatIconModule],
    templateUrl: './sparks-panel.component.html',
    styleUrl: './sparks-panel.component.scss',
})
export class SparksPanelComponent {
    readonly sparks = inject(SparksService);
    readonly clock = inject<{ balance: Signal<number>; minutes: Signal<number> } | null>(MAT_DIALOG_DATA);
}
