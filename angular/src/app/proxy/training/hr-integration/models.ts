
export interface EmployeeLookupDto {
  id?: string;
  userId?: string;
  serviceNumber?: string;
  fullNameAr?: string;
  fullNameEn?: string;
  rankNameAr?: string;
  rankNameEn?: string;
  rankSortOrder?: number;
  personnelType?: string;
  mainUnitId?: string;
}

export interface RankLookupDto {
  id?: string;
  nameAr?: string;
  nameEn?: string;
  sortOrder?: number;
  personnelType?: string;
}
