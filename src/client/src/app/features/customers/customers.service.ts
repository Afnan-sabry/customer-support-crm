import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { PaginatedList } from '../../core/models/paginated-list.model';
import { Observable } from 'rxjs';

export interface CustomerDto {
  id: string;
  name: string;
  nameAr: string;
  email: string | null;
  phone: string | null;
  company: string | null;
  companyAr: string | null;
  isActive: boolean;
}

export interface CustomerContactDto {
  id: string;
  name: string;
  nameAr: string;
  email: string | null;
  phone: string | null;
  title: string | null;
  isPrimary: boolean;
}

export interface CustomerDetailDto extends CustomerDto {
  address: string | null;
  contacts: CustomerContactDto[];
}

export interface CreateCustomerRequest {
  name: string;
  nameAr: string;
  email?: string;
  phone?: string;
  company?: string;
  companyAr?: string;
  address?: string;
}

export interface UpdateCustomerRequest extends CreateCustomerRequest {
  id: string;
}

export interface CreateCustomerContactRequest {
  customerId: string;
  name: string;
  nameAr: string;
  email?: string;
  phone?: string;
  title?: string;
  isPrimary: boolean;
}

@Injectable({ providedIn: 'root' })
export class CustomersService extends ApiService {
  getCustomers(params?: { search?: string; isActive?: boolean; page?: number; pageSize?: number }): Observable<PaginatedList<CustomerDto>> {
    return this.get<PaginatedList<CustomerDto>>('/v1/customers', params);
  }

  getCustomerById(id: string): Observable<CustomerDetailDto> {
    return this.get<CustomerDetailDto>(`/v1/customers/${id}`);
  }

  createCustomer(request: CreateCustomerRequest): Observable<CustomerDto> {
    return this.post<CustomerDto>('/v1/customers', request);
  }

  updateCustomer(id: string, request: UpdateCustomerRequest): Observable<void> {
    return this.put<void>(`/v1/customers/${id}`, request);
  }

  deleteCustomer(id: string): Observable<void> {
    return this.delete<void>(`/v1/customers/${id}`);
  }

  addContact(customerId: string, request: CreateCustomerContactRequest): Observable<CustomerContactDto> {
    return this.post<CustomerContactDto>(`/v1/customers/${customerId}/contacts`, request);
  }
}
