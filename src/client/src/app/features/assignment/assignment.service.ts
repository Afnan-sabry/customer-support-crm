import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { Observable } from 'rxjs';

export type AssignmentStrategy = 'RoundRobin' | 'LeastLoad';

export interface AssignmentRuleDto {
  id: string;
  name: string;
  nameAr: string;
  categoryId: string | null;
  categoryName: string | null;
  priorityId: string | null;
  priorityName: string | null;
  strategy: AssignmentStrategy;
  agentPool: string | null;
  order: number;
  isActive: boolean;
}

export interface AssignmentRuleRequest {
  name: string;
  nameAr: string;
  categoryId?: string | null;
  priorityId?: string | null;
  strategy: AssignmentStrategy;
  agentPool?: string | null;
  order: number;
}

@Injectable({ providedIn: 'root' })
export class AssignmentService extends ApiService {
  getAssignmentRules(isActive?: boolean): Observable<AssignmentRuleDto[]> {
    return this.get<AssignmentRuleDto[]>('/v1/assignment-rules', { isActive });
  }

  createAssignmentRule(request: AssignmentRuleRequest): Observable<AssignmentRuleDto> {
    return this.post<AssignmentRuleDto>('/v1/assignment-rules', request);
  }

  updateAssignmentRule(id: string, request: AssignmentRuleRequest): Observable<AssignmentRuleDto> {
    return this.put<AssignmentRuleDto>(`/v1/assignment-rules/${id}`, { id, ...request });
  }

  deleteAssignmentRule(id: string): Observable<void> {
    return this.delete<void>(`/v1/assignment-rules/${id}`);
  }
}
