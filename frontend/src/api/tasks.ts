import { apiClient } from './client'

export interface GardenTask {
  id: string
  gardenId: string
  gardenBedId: string | null
  plantingId: string | null
  assignedToUserId: string | null
  assignedToDisplayName: string | null
  title: string
  description: string | null
  taskType: string
  dueDate: string | null
  isOverdue: boolean
  completedAt: string | null
  createdAt: string
}

export interface CreateGardenTaskBody {
  gardenBedId: string | null
  plantingId: string | null
  assignedToUserId: string | null
  title: string
  description: string | null
  taskType: string
  dueDate: string | null
}

export interface GetGardenTasksParams {
  gardenBedId?: string
  isCompleted?: boolean
  isOverdue?: boolean
  taskType?: string
}

export const getGardenTasks = (gardenId: string, params?: GetGardenTasksParams): Promise<GardenTask[]> =>
  apiClient.get<GardenTask[]>(`/api/gardens/${gardenId}/tasks`, { params }).then(r => r.data)

export const createGardenTask = (gardenId: string, body: CreateGardenTaskBody): Promise<GardenTask> =>
  apiClient.post<GardenTask>(`/api/gardens/${gardenId}/tasks`, body).then(r => r.data)

export const completeGardenTask = (gardenId: string, taskId: string): Promise<GardenTask> =>
  apiClient.post<GardenTask>(`/api/gardens/${gardenId}/tasks/${taskId}/complete`).then(r => r.data)

export const deleteGardenTask = (gardenId: string, taskId: string): Promise<void> =>
  apiClient.delete(`/api/gardens/${gardenId}/tasks/${taskId}`).then(() => undefined)

export interface UpdateGardenTaskBody {
  title: string
  description: string | null
  taskType: string
  dueDate: string | null
  gardenBedId: string | null
  assignedToUserId: string | null
}

export const getGardenTask = (gardenId: string, taskId: string): Promise<GardenTask> =>
  apiClient.get<GardenTask>(`/api/gardens/${gardenId}/tasks/${taskId}`).then(r => r.data)

export const updateGardenTask = (gardenId: string, taskId: string, body: UpdateGardenTaskBody): Promise<GardenTask> =>
  apiClient.put<GardenTask>(`/api/gardens/${gardenId}/tasks/${taskId}`, body).then(r => r.data)
