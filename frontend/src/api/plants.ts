import { apiClient } from './client'

export interface PlantSummary {
  id: string
  commonName: string
  scientificName: string | null
  family: string | null
  daysToMaturity: number | null
  isGlobal: boolean
}

export interface PlantDetail {
  id: string
  commonName: string
  scientificName: string | null
  description: string | null
  family: string | null
  daysToMaturity: number | null
  heatLevelShu: number | null
  minSpacingInches: number | null
  minDepthInches: number | null
  sunRequirement: string | null
  waterRequirement: string | null
  isGlobal: boolean
  isApproved: boolean
  externalSource: string
  contributedByUserId: string | null
}

export interface PlantCompanion {
  plantId: string
  companionPlantId: string
  companionCommonName: string
  companionScientificName: string | null
  relationshipType: 'Beneficial' | 'Harmful'
}

export interface CreatePlantBody {
  commonName: string
  scientificName?: string
  description?: string
  family?: string
  daysToMaturity?: number
  minSpacingInches?: number
  minDepthInches?: number
  sunRequirement?: string
  waterRequirement?: string
}

export const searchPlants = (q: string): Promise<PlantSummary[]> =>
  apiClient.get<PlantSummary[]>('/api/plants', { params: { q } }).then(r => r.data)

export const getPlant = (id: string): Promise<PlantDetail> =>
  apiClient.get<PlantDetail>(`/api/plants/${id}`).then(r => r.data)

export const getPlantCompanions = (id: string): Promise<PlantCompanion[]> =>
  apiClient.get<PlantCompanion[]>(`/api/plants/${id}/companions`).then(r => r.data)

export const createPlant = (body: CreatePlantBody): Promise<PlantDetail> =>
  apiClient.post<PlantDetail>('/api/plants', body).then(r => r.data)
