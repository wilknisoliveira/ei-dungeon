import { Component, OnInit, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ThemeService } from 'src/app/service/theme/theme.service';

@Component({
    standalone: true,
    imports: [ReactiveFormsModule, MatButtonModule, MatIconModule],
    selector: 'app-theme-toggle',
    templateUrl: './theme-toggle.component.html',
    styleUrls: ['./theme-toggle.component.scss'],
})
export class ThemeToggleComponent implements OnInit {
    isDarkMode = new FormControl(true);

    private themeService = inject(ThemeService);

    ngOnInit(): void {
        this.isDarkMode.setValue(this.themeService.isDarkMode(), {
            emitEvent: false,
        });

        this.isDarkMode.valueChanges.subscribe((isDarkMode) => {
            this.themeService.setTheme(isDarkMode ?? true);
        });
    }

    toggleTheme(): void {
        this.isDarkMode.setValue(!this.isDarkMode.value);
    }
}
