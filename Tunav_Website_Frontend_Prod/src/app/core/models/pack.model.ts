import { PackIconKey, PackSolutionThemeKey } from './solution.model';

export type PackThemeKey = 'green' | 'orange' | 'rose';

export interface PackCatalogPack {
  id: number;
  solutionId: number;
  solutionTitle: string;
  solutionSlug: string;
  name: string;
  description: string;
  features: string[];
  themeKey: PackThemeKey;
  displayOrder: number;
  isPopular: boolean;
  videoUrl: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface PackCatalogSolution {
  solutionId: number;
  solutionTitle: string;
  solutionSlug: string;
  packIconKey: PackIconKey;
  packThemeKey: PackSolutionThemeKey;
  packs: PackCatalogPack[];
}

export interface CreateCustomPackRequestPayload {
  solutionId: number;
  contactName: string;
  company: string;
  email: string;
  phone: string;
  message?: string | null;
  selectedFeatures: string[];
}

export interface CustomPackRequest {
  id: number;
  solutionId: number;
  solutionTitle: string;
  contactName: string;
  company: string;
  email: string;
  phone: string;
  message: string | null;
  selectedFeatures: string[];
  status: string;
  internalNote: string | null;
  submittedAt: string;
  updatedAt: string | null;
}

export interface CustomPackRequestSubmissionResponse {
  message: string;
  requestId: number;
  data: CustomPackRequest;
}
