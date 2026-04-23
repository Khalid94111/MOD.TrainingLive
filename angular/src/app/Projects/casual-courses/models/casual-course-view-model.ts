import { CasualCourseStatus } from '../../shared';
import type { CasualCourseDto } from 'src/app/proxy/training/casual-courses/dtos/models';

export interface CasualCourseRowAction {
  labelAr: string;
  route: ((id: string) => string) | null;
  allowedFor: 'utm' | 'ugm' | 'staff' | 'td' | 'th' | 'readonly';
}

export function actionForRow(row: CasualCourseDto): CasualCourseRowAction {
  if (row.status === undefined) {
    return { labelAr: '—', route: null, allowedFor: 'readonly' };
  }
  switch (row.status) {
    case CasualCourseStatus.Draft:
      return { labelAr: 'تعديل / إرسال', route: id => `/training/casual-courses/${id}/edit`, allowedFor: 'utm' };
    case CasualCourseStatus.Submitted:
      return { labelAr: 'اعتماد UGM', route: id => `/training/casual-courses/${id}/review`, allowedFor: 'ugm' };
    case CasualCourseStatus.UGMApproved:
      return { labelAr: 'بدء المراجعة', route: id => `/training/casual-courses/${id}/review`, allowedFor: 'staff' };
    case CasualCourseStatus.UnderReview:
      return { labelAr: 'فتح صفحة المراجعة', route: id => `/training/casual-courses/${id}/review`, allowedFor: 'staff' };
    case CasualCourseStatus.StaffReviewed:
      return { labelAr: 'اعتماد TD', route: id => `/training/casual-courses/${id}/approve`, allowedFor: 'td' };
    case CasualCourseStatus.TDApproved:
      return { labelAr: 'اعتماد نهائي TH', route: id => `/training/casual-courses/${id}/approve`, allowedFor: 'th' };
    case CasualCourseStatus.ReturnedToCreator:
      return { labelAr: 'تعديل وإعادة إرسال', route: id => `/training/casual-courses/${id}/edit`, allowedFor: 'utm' };
    case CasualCourseStatus.THApproved:
      return { labelAr: 'عرض فقط (معتمد)', route: id => `/training/casual-courses/${id}/review`, allowedFor: 'readonly' };
    case CasualCourseStatus.Rejected:
      return { labelAr: 'عرض فقط (مرفوض)', route: id => `/training/casual-courses/${id}/review`, allowedFor: 'readonly' };
  }
}
