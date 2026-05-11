import { CasualCourseStatus } from '../../shared';
import type { CasualCourseDto } from 'src/app/proxy/training/casual-courses/dtos/models';

export interface CasualCourseRowAction {
  labelAr: string;
  route: ((id: string) => string) | null;
  allowedFor: 'utm' | 'ugm' | 'staff' | 'td' | 'th' | 'readonly';
}

/**
 * Every row routes to the same tabbed-shell URL — `…/{id}/details` — and the
 * Details tab dispatches the right inner view (request / review / approval)
 * based on status. The label below is what shows in the list's last column
 * to telegraph what the user will see when they open the row.
 */
function detailsRoute(id: string): string {
  return `/training/casual-courses/${id}/details`;
}

export function actionForRow(row: CasualCourseDto): CasualCourseRowAction {
  if (row.status === undefined) {
    return { labelAr: '—', route: null, allowedFor: 'readonly' };
  }
  switch (row.status) {
    case CasualCourseStatus.Draft:
      return { labelAr: 'تعديل / إرسال',           route: detailsRoute, allowedFor: 'utm' };
    case CasualCourseStatus.Submitted:
      return { labelAr: 'اعتماد UGM',              route: detailsRoute, allowedFor: 'ugm' };
    case CasualCourseStatus.UGMApproved:
      return { labelAr: 'بدء المراجعة',            route: detailsRoute, allowedFor: 'staff' };
    case CasualCourseStatus.UnderReview:
      return { labelAr: 'فتح صفحة المراجعة',       route: detailsRoute, allowedFor: 'staff' };
    case CasualCourseStatus.StaffReviewed:
      return { labelAr: 'اعتماد TD',               route: detailsRoute, allowedFor: 'td' };
    case CasualCourseStatus.TDApproved:
      return { labelAr: 'اعتماد نهائي TH',         route: detailsRoute, allowedFor: 'th' };
    case CasualCourseStatus.ReturnedToCreator:
      return { labelAr: 'تعديل وإعادة إرسال',     route: detailsRoute, allowedFor: 'utm' };
    case CasualCourseStatus.THApproved:
      return { labelAr: 'عرض فقط (معتمد)',         route: detailsRoute, allowedFor: 'readonly' };
    case CasualCourseStatus.Rejected:
      return { labelAr: 'عرض فقط (مرفوض)',         route: detailsRoute, allowedFor: 'readonly' };
  }
}
