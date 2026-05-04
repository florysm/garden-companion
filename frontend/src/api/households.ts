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

export interface WeatherStationDto {
  id: string
  provider: string
  stationId: string | null
  hasApiKey: boolean
  createdAt: string
}

export interface UpsertWeatherStationBody {
  provider: string
  stationId?: string
  apiKey?: string
}

export const getWeatherStation = (householdId: string): Promise<WeatherStationDto | null> =>
  apiClient
    .get<WeatherStationDto>(`/api/households/${householdId}/weather-station`)
    .then(r => r.data)
    .catch((e) => {
      if (e?.response?.status === 404) return null
      throw e
    })

export const upsertWeatherStation = (
  householdId: string,
  body: UpsertWeatherStationBody,
): Promise<WeatherStationDto> =>
  apiClient
    .put<WeatherStationDto>(`/api/households/${householdId}/weather-station`, body)
    .then(r => r.data)

export const deleteWeatherStation = (householdId: string): Promise<void> =>
  apiClient.delete(`/api/households/${householdId}/weather-station`).then(() => undefined)

export interface WeatherTestResult {
  temperatureF: number
  humidity: number
  windSpeedMph: number
  windDirectionDegrees: number | null
  precipitationRateInPerHr: number
  uvIndex: number | null
  dewPointF: number | null
  pressureInHg: number | null
  stationId: string | null
}

export const testWeatherStation = (
  householdId: string,
  body: UpsertWeatherStationBody,
): Promise<WeatherTestResult> =>
  apiClient
    .post<WeatherTestResult>(`/api/households/${householdId}/weather-station/test`, body)
    .then(r => r.data)
