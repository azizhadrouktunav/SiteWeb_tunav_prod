export type DemoRequestEntryPoint =
  | 'DemoPage'
  | 'SolutionsList'
  | 'SolutionDetail'
  | 'PacksPage';

export type DemoRequestStatus =
  | 'Nouvelle'
  | 'EnCours'
  | 'Acceptee'
  | 'Refusee';

export interface DemoRequest {
  id: number;
  solutionId: number;
  solutionTitle: string;
  packId: number | null;
  packName: string | null;
  firstName: string;
  lastName: string;
  email: string;
  hasWhatsapp: boolean;
  whatsappNumber: string | null;
  entryPoint: DemoRequestEntryPoint;
  status: DemoRequestStatus;
  internalNote: string | null;
  submittedAt: string;
  updatedAt: string | null;
}

export interface CreateDemoRequestPayload {
  solutionId: number;
  packId?: number | null;
  firstName: string;
  lastName: string;
  email: string;
  hasWhatsapp: boolean;
  whatsappNumber?: string | null;
  entryPoint: DemoRequestEntryPoint;
}

export interface DemoRequestSubmissionResponse {
  message: string;
  requestId: number;
  data: DemoRequest;
}
