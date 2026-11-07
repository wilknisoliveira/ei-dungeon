import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
    standalone: true,
    imports: [CommonModule, MatProgressSpinnerModule],
    selector: 'app-small-loading',
    templateUrl: './small-loading.component.html',
    styleUrls: ['./small-loading.component.scss'],
})
export class SmallLoadingComponent {
    @Input() isLoading: boolean = false;
}
