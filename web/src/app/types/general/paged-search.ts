export interface PagedSearch<T> {
    currentPage: number;
    pageSize: number;
    totalResults: number;
    sortDirection: string;
    items: T[];
}
