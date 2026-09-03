import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PortalAuthService } from '../../features/portal/portal-auth.service';

export const portalAuthGuard: CanActivateFn = () => {
  const portalAuthService = inject(PortalAuthService);
  const router = inject(Router);

  if (portalAuthService.isAuthenticated()) return true;
  return router.createUrlTree(['/portal/login']);
};
