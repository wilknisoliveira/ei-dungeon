import { Injectable } from '@angular/core';
import { MatPaginatorIntl } from '@angular/material/paginator';

@Injectable()
export class AdminPaginatorIntl extends MatPaginatorIntl {
    private readonly numberFormat = new Intl.NumberFormat('en');
    override itemsPerPageLabel = 'Items per page:';
    override nextPageLabel = 'Next page';
    override previousPageLabel = 'Previous page';
    override firstPageLabel = 'First page';
    override lastPageLabel = 'Last page';

    override getRangeLabel = (page: number, pageSize: number, length: number): string => {
        const total = Math.max(0, length);
        const start = total === 0 || pageSize === 0 ? 0 : page * pageSize + 1;
        const end = total === 0 || pageSize === 0 ? 0 : Math.min((page + 1) * pageSize, total);
        return `${this.numberFormat.format(start)}–${this.numberFormat.format(end)} of ${this.numberFormat.format(total)}`;
    };
}

export function createAdminPaginatorIntl(): AdminPaginatorIntl {
    return new AdminPaginatorIntl();
}
