import { apiClient } from './client'

export interface UserSettings {
  displayName: string
  emailNotificationsEnabled: boolean
}

export interface UpdateUserSettingsBody {
  displayName?: string
  emailNotificationsEnabled?: boolean
}

export const getUserSettings = (): Promise<UserSettings> =>
  apiClient.get<UserSettings>('/api/users/me/settings').then(r => r.data)

export const updateUserSettings = (body: UpdateUserSettingsBody): Promise<UserSettings> =>
  apiClient.put<UserSettings>('/api/users/me/settings', body).then(r => r.data)
