import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { Observable } from 'rxjs';

export interface RoleDto {
  id: string;
  name: string;
  nameAr: string;
  isSystem: boolean;
  permissions: PermissionDto[];
}

export interface PermissionDto {
  id: string;
  key: string;
  module: string;
  description: string;
}

@Injectable({ providedIn: 'root' })
export class RolesService extends ApiService {
  getRoles(): Observable<RoleDto[]> {
    return this.get<RoleDto[]>('/v1/roles');
  }

  getPermissions(): Observable<PermissionDto[]> {
    return this.get<PermissionDto[]>('/v1/roles/permissions');
  }

  createRole(request: { name: string; nameAr: string }): Observable<RoleDto> {
    return this.post<RoleDto>('/v1/roles', request);
  }

  updateRole(roleId: string, request: { roleId: string; name: string; nameAr: string }): Observable<RoleDto> {
    return this.put<RoleDto>(`/v1/roles/${roleId}`, request);
  }

  assignPermissions(roleId: string, permissionIds: string[]): Observable<void> {
    return this.post<void>(`/v1/roles/${roleId}/permissions`, { roleId, permissionIds });
  }
}
