import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
    providedIn: 'root',
})
export class SnackbarService {
    constructor(private snackBar: MatSnackBar) {}

    addSuccess(msg: string) {
        this.snackBar.open(msg, '', {
            duration: 5 * 1000,
            panelClass: 'snackbar-sucess',
        });
    }

    addError(msg: string) {
        this.snackBar.open(msg, '', {
            duration: 5 * 1000,
            panelClass: 'snackbar-error',
        });
    }
}
