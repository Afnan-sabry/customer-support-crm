import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { PaginatedList } from '../../core/models/paginated-list.model';
import { TicketDto } from '../tickets/tickets.service';
import { Observable } from 'rxjs';

export interface DashboardStatsDto {
  openTickets: number;
  overdueTickets: number;
  resolvedToday: number;
  unassignedTickets: number;
  myOpenTickets: number;
  myOverdueTickets: number;
}

export interface SlaSummaryDto {
  totalTracked: number;
  firstResponseOnTime: number;
  firstResponseBreached: number;
  resolutionOnTime: number;
  resolutionBreached: number;
  firstResponseCompliancePercent: number;
  resolutionCompliancePercent: number;
}

export interface AgentWorkloadDto {
  agentId: string;
  agentName: string;
  openTickets: number;
  overdueTickets: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardService extends ApiService {
  getStats(): Observable<DashboardStatsDto> {
    return this.get<DashboardStatsDto>('/v1/Dashboard/stats');
  }

  getSlaSummary(): Observable<SlaSummaryDto> {
    return this.get<SlaSummaryDto>('/v1/Dashboard/sla-summary');
  }

  getMyTickets(page = 1, pageSize = 20): Observable<PaginatedList<TicketDto>> {
    return this.get<PaginatedList<TicketDto>>('/v1/Dashboard/my-tickets', { page, pageSize });
  }

  getTeamWorkload(): Observable<AgentWorkloadDto[]> {
    return this.get<AgentWorkloadDto[]>('/v1/Dashboard/team-workload');
  }
}
