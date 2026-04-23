import { apiClient } from './client'

export interface UserInsight {
  id: string
  householdId: string
  gardenId: string | null
  gardenBedId: string | null
  insightType: string
  title: string
  body: string
  isRead: boolean
  expiresAt: string | null
  generatedAt: string
}

export const getInsights = (householdId: string): Promise<UserInsight[]> =>
  apiClient
    .get<UserInsight[]>(`/api/households/${householdId}/insights`, { params: { isRead: false } })
    .then(r => r.data)

export const markInsightRead = (householdId: string, insightId: string): Promise<UserInsight> =>
  apiClient
    .patch<UserInsight>(`/api/households/${householdId}/insights/${insightId}/read`)
    .then(r => r.data)
