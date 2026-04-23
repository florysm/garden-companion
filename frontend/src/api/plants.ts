import { apiClient } from './client'

export interface PlantSummary {
  id: string
  commonName: string
  scientificName: string | null
  family: string | null
  daysToMaturity: number | null
  isGlobal: boolean
}

export const searchPlants = (q: string): Promise<PlantSummary[]> =>
  apiClient.get<PlantSummary[]>('/api/plants', { params: { q } }).then(r => r.data)
