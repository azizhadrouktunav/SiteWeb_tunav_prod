export type PartnerRequestType = 'Franchise' | 'Revendeur' | 'Commissionnaire';
export type PartnerRequestPersonType = 'Physique' | 'Morale';
export type PartnerRequestStatus = 'Nouvelle' | 'EnCours' | 'Acceptee' | 'Refusee';

export interface CreatePartnerRequestPayload {
  partnerType: PartnerRequestType;
  fullName: string;
  email: string;
  phone: string;
  company?: string | null;
  city: string;
  personType: PartnerRequestPersonType;
  selectedSolutions: string[];
}

export interface PartnerRequest {
  id: number;
  partnerType: PartnerRequestType;
  fullName: string;
  email: string;
  phone: string;
  company: string | null;
  city: string;
  personType: PartnerRequestPersonType;
  selectedSolutions: string[];
  status: PartnerRequestStatus;
  internalNote: string | null;
  submittedAt: string;
  updatedAt: string | null;
}

export interface PartnerRequestSubmissionResponse {
  message: string;
  requestId: number;
  data: PartnerRequest;
}
