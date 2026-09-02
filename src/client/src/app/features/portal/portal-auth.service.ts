import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

export interface PortalLoginRequest { email: string; password: string; }
export interface PortalRegisterRequest { email: string; password: string; fullName: string; fullNameAr: string; phone?: string; }
export interface PortalUserInfo { id: string; email: string; fullName: string; fullNameAr: string; phone: string | null; customerId: string; }
export interface PortalTokenResponse { accessToken: string; refreshToken: string; user: PortalUserInfo; }

@Injectable({ providedIn: 'root' })
export class PortalAuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly TOKEN_KEY = 'portal-access-token';
  private readonly REFRESH_KEY = 'portal-refresh-token';
  private readonly USER_KEY = 'portal-user';

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());
  private currentUserSubject = new BehaviorSubject<PortalUserInfo | null>(this.getStoredUser());

  isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  currentUser$ = this.currentUserSubject.asObservable();

  login(request: PortalLoginRequest): Observable<PortalTokenResponse> {
    return this.http.post<PortalTokenResponse>(`${environment.apiUrl}/v1/portal/auth/login`, request).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  register(request: PortalRegisterRequest): Observable<PortalTokenResponse> {
    return this.http.post<PortalTokenResponse>(`${environment.apiUrl}/v1/portal/auth/register`, request).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  refresh(): Observable<PortalTokenResponse> {
    const accessToken = this.getToken();
    const refreshToken = localStorage.getItem(this.REFRESH_KEY);
    return this.http.post<PortalTokenResponse>(`${environment.apiUrl}/v1/portal/auth/refresh`, {
      accessToken, refreshToken
    }).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.isAuthenticatedSubject.next(false);
    this.currentUserSubject.next(null);
    this.router.navigate(['/portal/login']);
  }

  getToken(): string | null { return localStorage.getItem(this.TOKEN_KEY); }
  isAuthenticated(): boolean { return this.hasToken(); }
  getCurrentUser(): PortalUserInfo | null { return this.currentUserSubject.value; }

  updateStoredUser(user: PortalUserInfo): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  private hasToken(): boolean { return !!localStorage.getItem(this.TOKEN_KEY); }
  private getStoredUser(): PortalUserInfo | null {
    const stored = localStorage.getItem(this.USER_KEY);
    return stored ? JSON.parse(stored) : null;
  }
  private storeTokens(response: PortalTokenResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.accessToken);
    localStorage.setItem(this.REFRESH_KEY, response.refreshToken);
    localStorage.setItem(this.USER_KEY, JSON.stringify(response.user));
    this.isAuthenticatedSubject.next(true);
    this.currentUserSubject.next(response.user);
  }
}
