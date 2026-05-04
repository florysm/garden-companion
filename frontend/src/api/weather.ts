import { apiClient } from './client'

export interface WeatherObservation {
  id: string
  householdId: string
  observedAt: string
  temperatureF: number
  humidity: number
  windSpeedMph: number
  windDirectionDegrees: number | null
  precipitationRateInPerHr: number
  precipitationTotalIn: number
  uvIndex: number | null
  dewPointF: number | null
  pressureInHg: number | null
  source: string
  stationId: string | null
}

export const getLatestWeather = (householdId: string): Promise<WeatherObservation | null> =>
  apiClient
    .get<WeatherObservation[]>(`/api/households/${householdId}/weather`, { params: { limit: 1 } })
    .then(r => r.data[0] ?? null)

export const refreshLatestWeather = (householdId: string): Promise<WeatherObservation | null> =>
  apiClient
    .post<WeatherObservation | null>(`/api/households/${householdId}/weather/refresh`)
    .then(r => r.data)
