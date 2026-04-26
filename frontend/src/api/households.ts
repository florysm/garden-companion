import { apiClient } from './client'

export interface HouseholdMember {
  userId: string
  displayName: string
  email: string
  role: string
}

export interface Household {
  id: string
  name: string
  members: HouseholdMember[]
}

export interface UpdateHouseholdBody {
  name: string
}

export const getHousehold = (id: string): Promise<Household> =>
  apiClient.get<Household>(`/api/households/${id}`).then(r => r.data)

export const createHousehold = (name: string): Promise<Household> =>
  apiClient.post<Household>('/api/households', { name }).then(r => r.data)

export const updateHousehold = (id: string, body: UpdateHouseholdBody): Promise<Household> =>
  apiClient.put<Household>(`/api/households/${id}`, body).then(r => r.data)

export const inviteHouseholdMember = (householdId: string, email: string): Promise<HouseholdMember> =>
  apiClient.post<HouseholdMember>(`/api/households/${householdId}/members`, { email }).then(r => r.data)

export const removeHouseholdMember = (householdId: string, userId: string): Promise<void> =>
  apiClient.delete(`/api/households/${householdId}/members/${userId}`).then(() => undefined)
