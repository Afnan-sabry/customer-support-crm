import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { Observable } from 'rxjs';

export interface AiSuggestionDto {
  id: string;
  ticketId: string;
  type: string;
  output: string;
  confidence: number | null;
  status: string;
  appliedAt: string | null;
  model: string;
  tokensUsed: number;
  createdAt: string;
}

export interface AiCategorizationResult {
  suggestedCategoryId: string | null;
  suggestedCategoryName: string | null;
  suggestedPriorityId: string | null;
  suggestedPriorityName: string | null;
  confidence: number;
  autoApplied: boolean;
  suggestionId: string;
}

export interface AiSummaryResult {
  summary: string;
  suggestionId: string;
}

export interface AiSuggestedRepliesResult {
  suggestions: string[];
}

@Injectable({ providedIn: 'root' })
export class AiService extends ApiService {
  categorize(ticketId: string): Observable<AiCategorizationResult> {
    return this.post<AiCategorizationResult>(`/v1/Ai/tickets/${ticketId}/categorize`, {});
  }

  summarize(ticketId: string): Observable<AiSummaryResult> {
    return this.post<AiSummaryResult>(`/v1/Ai/tickets/${ticketId}/summarize`, {});
  }

  suggestReplies(ticketId: string): Observable<AiSuggestedRepliesResult> {
    return this.post<AiSuggestedRepliesResult>(`/v1/Ai/tickets/${ticketId}/suggest-replies`, {});
  }

  getSuggestions(ticketId: string): Observable<AiSuggestionDto[]> {
    return this.get<AiSuggestionDto[]>(`/v1/Ai/tickets/${ticketId}/suggestions`);
  }

  acceptSuggestion(suggestionId: string): Observable<any> {
    return this.put(`/v1/Ai/suggestions/${suggestionId}/accept`, {});
  }

  rejectSuggestion(suggestionId: string): Observable<any> {
    return this.put(`/v1/Ai/suggestions/${suggestionId}/reject`, {});
  }
}
