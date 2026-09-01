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
