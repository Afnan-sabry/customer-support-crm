import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { PaginatedList } from '../../core/models/paginated-list.model';
import { Observable } from 'rxjs';

export interface UserDetail {
  id: string;
  email: string;
  fullName: string;
  fullNameAr: string;
  phone: string | null;
  tenantId: string;
  preferredLanguage: string;
  isActive: boolean;
  roles: string[];
}

export interface CreateUserRequest {
  email: string;
  password: string;
  fullName: string;
  fullNameAr: string;
  phone?: string;
  preferredLanguage: string;
  roleNames: string[];
}

export interface UpdateUserRequest {
  fullName: string;
  fullNameAr: string;
  phone?: string;
  preferredLanguage: string;
  isActive: boolean;
  roleNames: string[];
}

@Injectable({ providedIn: 'root' })
export class UsersService extends ApiService {
  getUsers(params?: { search?: string; page?: number; pageSize?: number }): Observable<PaginatedList<UserDetail>> {
    return this.get<PaginatedList<UserDetail>>('/v1/users', params);
  }

  getUserById(id: string): Observable<UserDetail> {
    return this.get<UserDetail>(`/v1/users/${id}`);
  }

  createUser(request: CreateUserRequest): Observable<UserDetail> {
    return this.post<UserDetail>('/v1/users', request);
  }

  updateUser(id: string, request: UpdateUserRequest): Observable<void> {
    return this.put<void>(`/v1/users/${id}`, request);
  }

  deactivateUser(id: string): Observable<void> {
    return this.delete<void>(`/v1/users/${id}`);
  }
}
