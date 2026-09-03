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

export interface TicketTrendDto {
  date: string;
  createdCount: number;
  resolvedCount: number;
}

export interface CategoryDistributionDto {
  categoryId: string;
  categoryName: string;
  categoryNameAr: string;
  ticketCount: number;
}

export interface PriorityBreakdownDto {
  priorityId: string;
  priorityName: string;
  priorityNameAr: string;
  level: number;
  ticketCount: number;
}

export interface ChannelVolumeDto {
  channel: string;
  conversationCount: number;
  date: string;
}

export interface SlaBreachDto {
  ticketId: string;
  ticketNumber: string;
  breachType: string;
  policyName: string;
  dueAt: string;
  breachedAt: string;
  minutesLate: number;
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

  getTicketTrends(days = 30): Observable<TicketTrendDto[]> {
    return this.get<TicketTrendDto[]>('/v1/Dashboard/ticket-trends', { days });
  }

  getCategoryDistribution(): Observable<CategoryDistributionDto[]> {
    return this.get<CategoryDistributionDto[]>('/v1/Dashboard/category-distribution');
  }

  getPriorityBreakdown(): Observable<PriorityBreakdownDto[]> {
    return this.get<PriorityBreakdownDto[]>('/v1/Dashboard/priority-breakdown');
  }

  getChannelVolume(days = 30): Observable<ChannelVolumeDto[]> {
    return this.get<ChannelVolumeDto[]>('/v1/Dashboard/channel-volume', { days });
  }

  getRecentSlaBreaches(count = 10): Observable<SlaBreachDto[]> {
    return this.get<SlaBreachDto[]>('/v1/Dashboard/recent-sla-breaches', { count });
  }
}
