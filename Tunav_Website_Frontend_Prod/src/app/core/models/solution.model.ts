export type SolutionType = 'General' | 'Sectorial';

export type PackIconKey =
  | 'map-pin'
  | 'zap'
  | 'building'
  | 'tag'
  | 'droplet'
  | 'camera';

export type PackSolutionThemeKey =
  | 'blue-cyan'
  | 'yellow-orange'
  | 'teal-green'
  | 'pink-rose'
  | 'sky-cyan'
  | 'red-pink';

export interface SolutionUseCase {
  title: string;
  description: string;
}

export interface Solution {
  id: number;
  title: string;
  slug: string;
  description: string;
  type: SolutionType;
  sectorName: string | null;
  baseSolutionId: number | null;
  packIconKey: PackIconKey;
  packThemeKey: PackSolutionThemeKey;
  hasPacks: boolean;
  effectivePackSolutionSlug: string | null;
  coverImageUrl: string | null;
  youtubeUrl: string | null;
  sectors: string[];
  topClients: string[];
  functionalities: string[];
  advantages: string[];
  useCases: SolutionUseCase[];
  isActive: boolean;
  createdByName: string;
  createdAt: string;
  updatedAt: string | null;
}
