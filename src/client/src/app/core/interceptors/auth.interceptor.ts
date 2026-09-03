import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { PortalAuthService } from '../../features/portal/portal-auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const portalAuthService = inject(PortalAuthService);

  const isPortalRequest = req.url.includes('/portal/') || req.url.includes('/hubs/chat');
  const token = isPortalRequest ? portalAuthService.getToken() : authService.getToken();

  if (!token) {
    const fallbackToken = isPortalRequest ? authService.getToken() : portalAuthService.getToken();
    if (fallbackToken) {
      req = req.clone({ setHeaders: { Authorization: `Bearer ${fallbackToken}` } });
    }
    return next(req);
  }

  req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  return next(req);
};
