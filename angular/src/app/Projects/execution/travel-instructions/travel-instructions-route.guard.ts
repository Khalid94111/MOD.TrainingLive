import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { CourseSessionService } from 'src/app/proxy/training/plans/course-session.service';
import { CourseType } from 'src/app/proxy/training/enums/course-type.enum';

// Phase 4C-α Patch 4 (v4.10.4) — Travel section is hidden for Internal and ExternalLocal
// sessions. The detail shell already hides Section 4 + the pipeline node; this guard blocks
// the standalone backward-compat URL so a deep-link can't bypass that visibility.
export const sessionTravelInstructionsGuard: CanActivateFn = async route => {
  const router = inject(Router);
  const sessionService = inject(CourseSessionService);

  const id = route.paramMap.get('id');
  if (!id) return router.createUrlTree(['/training/annual-plan/sessions-queue']);

  const session = await firstValueFrom(sessionService.get(id));
  if (session.courseType === CourseType.Internal || session.courseType === CourseType.ExternalLocal) {
    return router.createUrlTree(['/training/sessions', id]);
  }
  return true;
};
