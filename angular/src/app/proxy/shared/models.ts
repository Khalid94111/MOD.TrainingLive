
export interface SortOrderItem {
  id: string;
  sortOrder: number;
}

export interface UpdateSortOrderInput {
  items: SortOrderItem[];
}
