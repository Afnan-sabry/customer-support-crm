import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface TicketVolumeReportDto {
  timeSeries: { period: string; createdCount: number; resolvedCount: number }[];
  categoryBreakdown: { categoryName: string; categoryNameAr: string; count: number }[];
  priorityBreakdown: { priorityName: string; priorityNameAr: string; count: number }[];
  totalCreated: number;
  totalResolved: number;
}

export interface SlaPerformanceReportDto {
  overallFirstResponseCompliance: number;
  overallResolutionCompliance: number;
  timeSeries: { period: string; firstResponseOnTime: number; firstResponseBreached: number; resolutionOnTime: number; resolutionBreached: number }[];
  breachDetails: { ticketId: string; ticketNumber: string; breachType: string; policyName: string; dueAt: string; breachedAt: string; minutesLate: number }[];
}

export interface AgentPerformanceReportDto {
  agents: { agentId: string; agentName: string; ticketsHandled: number; ticketsResolved: number; avgResolutionMinutes: number; avgFirstResponseMinutes: number; slaCompliancePercent: number }[];
  topPerformer: { agentId: string; agentName: string } | null;
}

export interface ChannelAnalyticsReportDto {
  channelBreakdown: { channel: string; conversationCount: number; messageCount: number; avgResponseMinutes: number }[];
  timeSeries: { period: string; channel: string; conversationCount: number }[];
}

export interface AiUsageReportDto {
  suggestionsByType: { type: string; totalCount: number; acceptedCount: number; rejectedCount: number; pendingCount: number; acceptanceRate: number }[];
  avgConfidence: number;
  totalTokensUsed: number;
  timeSeries: { period: string; suggestionCount: number; acceptanceRate: number }[];
}

export interface CsatReportDto {
  averageRating: number;
  totalResponses: number;
  ratingDistribution: { rating: number; count: number; percentage: number }[];
  recentFeedback: { ticketId: string; ticketNumber: string; customerName: string; rating: number; comment: string | null; submittedAt: string }[];
  timeSeries: { period: string; averageRating: number; responseCount: number }[];
}

@Injectable({ providedIn: 'root' })
export class ReportsService extends ApiService {
  getTicketVolume(params: Record<string, any>): Observable<TicketVolumeReportDto> {
    return this.get<TicketVolumeReportDto>('/v1/Reports/ticket-volume', params);
  }

  getSlaPerformance(params: Record<string, any>): Observable<SlaPerformanceReportDto> {
    return this.get<SlaPerformanceReportDto>('/v1/Reports/sla-performance', params);
  }

  getAgentPerformance(params: Record<string, any>): Observable<AgentPerformanceReportDto> {
    return this.get<AgentPerformanceReportDto>('/v1/Reports/agent-performance', params);
  }

  getChannelAnalytics(params: Record<string, any>): Observable<ChannelAnalyticsReportDto> {
    return this.get<ChannelAnalyticsReportDto>('/v1/Reports/channel-analytics', params);
  }

  getAiUsage(params: Record<string, any>): Observable<AiUsageReportDto> {
    return this.get<AiUsageReportDto>('/v1/Reports/ai-usage', params);
  }

  getCsat(params: Record<string, any>): Observable<CsatReportDto> {
    return this.get<CsatReportDto>('/v1/Reports/csat', params);
  }

  exportReport(reportType: string, format: string, params: Record<string, any>): void {
    const queryString = Object.entries({ ...params, format })
      .filter(([, v]) => v != null && v !== '')
      .map(([k, v]) => `${k}=${encodeURIComponent(v)}`)
      .join('&');
    const url = `${environment.apiUrl}/v1/Reports/${reportType}/export?${queryString}`;
    window.open(url, '_blank');
  }
}
