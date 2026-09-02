import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { Observable } from 'rxjs';

export type EscalationTriggerType = 'FirstResponseBreached' | 'ResolutionBreached';
export type EscalationActionType = 'Reassign' | 'ChangePriority';

export interface EscalationRuleDto {
  id: string;
  name: string;
  nameAr: string;
  priorityId: string | null;
  priorityName: string | null;
  categoryId: string | null;
  categoryName: string | null;
  triggerType: EscalationTriggerType;
  triggerAfterMinutes: number;
  actionType: EscalationActionType;
  actionTarget: string | null;
  order: number;
  isActive: boolean;
}

export interface EscalationRuleRequest {
  name: string;
  nameAr: string;
  priorityId?: string | null;
  categoryId?: string | null;
  triggerType: EscalationTriggerType;
  triggerAfterMinutes: number;
  actionType: EscalationActionType;
  actionTarget?: string | null;
  order: number;
}

@Injectable({ providedIn: 'root' })
export class EscalationService extends ApiService {
  getEscalationRules(isActive?: boolean): Observable<EscalationRuleDto[]> {
    return this.get<EscalationRuleDto[]>('/v1/escalation-rules', { isActive });
  }

  createEscalationRule(request: EscalationRuleRequest): Observable<EscalationRuleDto> {
    return this.post<EscalationRuleDto>('/v1/escalation-rules', request);
  }

  updateEscalationRule(id: string, request: EscalationRuleRequest): Observable<EscalationRuleDto> {
    return this.put<EscalationRuleDto>(`/v1/escalation-rules/${id}`, { id, ...request });
  }

  deleteEscalationRule(id: string): Observable<void> {
    return this.delete<void>(`/v1/escalation-rules/${id}`);
  }
}
