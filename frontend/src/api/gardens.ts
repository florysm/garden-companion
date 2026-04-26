import { apiClient } from './client'

export interface GardenType {
  id: number
  name: string
}

export interface GardenSummary {
  id: string
  name: string
  description: string | null
  types: string[]
  bedCount: number
  userRole: string
  createdAt: string
}

export interface GardenBedSummary {
  id: string
  name: string
  type: string
  shape: string
  sunExposure: string
  activePlantingCount: number
}

export interface GardenMember {
  userId: string
  displayName: string
  role: string
}

export interface GardenDetail {
  id: string
  name: string
  description: string | null
  types: string[]
  beds: GardenBedSummary[]
  members: GardenMember[]
  userRole: string
  createdAt: string
}

export interface CreateGardenBody {
  householdId: string
  name: string
  description: string | null
  gardenTypeIds: number[]
}

export const getGardenTypes = (): Promise<GardenType[]> =>
  apiClient.get<GardenType[]>('/api/garden-types').then(r => r.data)

export const getGardens = (): Promise<GardenSummary[]> =>
  apiClient.get<GardenSummary[]>('/api/gardens').then(r => r.data)

export const getGarden = (id: string): Promise<GardenDetail> =>
  apiClient.get<GardenDetail>(`/api/gardens/${id}`).then(r => r.data)

export const createGarden = (body: CreateGardenBody): Promise<GardenDetail> =>
  apiClient.post<GardenDetail>('/api/gardens', body).then(r => r.data)

export interface GardenBedDetail {
  id: string
  gardenId: string
  name: string
  type: string
  shape: string
  lengthFeet: number | null
  widthFeet: number | null
  diameterFeet: number | null
  depthInches: number | null
  volumeGallons: number | null
  soilType: string | null
  sunExposure: string
  notes: string | null
  activePlantingCount: number
}

export interface CreateGardenBedBody {
  name: string
  type: string
  shape: string
  lengthFeet: number | null
  widthFeet: number | null
  diameterFeet: number | null
  depthInches: number | null
  volumeGallons: number | null
  soilType: string | null
  sunExposure: string
  notes: string | null
}

export const getGardenBed = (gardenId: string, bedId: string): Promise<GardenBedDetail> =>
  apiClient.get<GardenBedDetail>(`/api/gardens/${gardenId}/beds/${bedId}`).then(r => r.data)

export const createGardenBed = (gardenId: string, body: CreateGardenBedBody): Promise<GardenBedDetail> =>
  apiClient.post<GardenBedDetail>(`/api/gardens/${gardenId}/beds`, body).then(r => r.data)

export type PlantingSource = 'DirectSeed' | 'IndoorSeedStart' | 'PurchasedTransplant'

export interface PlantingSummary {
  id: string
  gardenBedId: string
  gardenBedName: string
  plantId: string
  plantCommonName: string
  plantedDate: string
  expectedHarvestDate: string | null
  actualEndDate: string | null
  status: string
  plantingType: string
  source: PlantingSource
  quantity: number
  seasonYear: number
  seasonType: string
  isActive: boolean
}

export interface PlantingDetail extends PlantingSummary {
  gardenId: string
  plantScientificName: string | null
  plantFamily: string | null
  observationCount: number
  harvestCount: number
}

export interface CreatePlantingBody {
  gardenBedId: string
  plantId: string
  plantedDate: string
  expectedHarvestDate: string | null
  plantingType: string
  source: PlantingSource
  quantity: number
  seasonYear: number | null
  seasonType: string | null
}

export const getPlantings = (gardenId: string, bedId?: string): Promise<PlantingSummary[]> =>
  apiClient.get<PlantingSummary[]>(`/api/gardens/${gardenId}/plantings`, {
    params: { ...(bedId ? { bedId } : {}), isActive: true },
  }).then(r => r.data)

export const createPlanting = (gardenId: string, body: CreatePlantingBody): Promise<PlantingDetail> =>
  apiClient.post<PlantingDetail>(`/api/gardens/${gardenId}/plantings`, body).then(r => r.data)

export const getPlanting = (plantingId: string): Promise<PlantingDetail> =>
  apiClient.get<PlantingDetail>(`/api/plantings/${plantingId}`).then(r => r.data)

export const updatePlantingStatus = (plantingId: string, status: string): Promise<PlantingSummary> =>
  apiClient.patch<PlantingSummary>(`/api/plantings/${plantingId}/status`, { status }).then(r => r.data)

// ── Garden update / delete ────────────────────────────────────────────────────

export interface UpdateGardenBody {
  name: string
  description: string | null
  gardenTypeIds: number[]
}

export const updateGarden = (id: string, body: UpdateGardenBody): Promise<GardenDetail> =>
  apiClient.put<GardenDetail>(`/api/gardens/${id}`, body).then(r => r.data)

export const deleteGarden = (id: string): Promise<void> =>
  apiClient.delete(`/api/gardens/${id}`).then(() => undefined)

// ── Garden bed update / delete ────────────────────────────────────────────────

export interface UpdateGardenBedBody {
  name: string
  type: string
  shape: string
  lengthFeet: number | null
  widthFeet: number | null
  diameterFeet: number | null
  depthInches: number | null
  volumeGallons: number | null
  soilType: string | null
  sunExposure: string
  notes: string | null
}

export const updateGardenBed = (gardenId: string, bedId: string, body: UpdateGardenBedBody): Promise<GardenBedDetail> =>
  apiClient.put<GardenBedDetail>(`/api/gardens/${gardenId}/beds/${bedId}`, body).then(r => r.data)

export const deleteGardenBed = (gardenId: string, bedId: string): Promise<void> =>
  apiClient.delete(`/api/gardens/${gardenId}/beds/${bedId}`).then(() => undefined)

// ── Planting update / delete ──────────────────────────────────────────────────

export interface UpdatePlantingBody {
  expectedHarvestDate: string | null
  plantingType: string
  source: PlantingSource
  quantity: number
  seasonYear: number
  seasonType: string
}

export const updatePlanting = (plantingId: string, body: UpdatePlantingBody): Promise<PlantingDetail> =>
  apiClient.put<PlantingDetail>(`/api/plantings/${plantingId}`, body).then(r => r.data)

export const deletePlanting = (plantingId: string): Promise<void> =>
  apiClient.delete(`/api/plantings/${plantingId}`).then(() => undefined)

// ── Planting observations ─────────────────────────────────────────────────────

export type ObservationType = 'General' | 'Pest' | 'Disease' | 'Growth' | 'Fertilized' | 'Watered'

export interface PlantingObservation {
  id: string
  plantingId: string
  observationType: ObservationType
  note: string
  observedAt: string
}

export interface AddObservationBody {
  observationType: ObservationType
  note: string
  observedAt?: string
}

export const getObservations = (plantingId: string): Promise<PlantingObservation[]> =>
  apiClient.get<PlantingObservation[]>(`/api/plantings/${plantingId}/observations`).then(r => r.data)

export const addObservation = (plantingId: string, body: AddObservationBody): Promise<PlantingObservation> =>
  apiClient.post<PlantingObservation>(`/api/plantings/${plantingId}/observations`, body).then(r => r.data)

// ── Harvest logs ──────────────────────────────────────────────────────────────

export type QuantityUnit = 'Count' | 'Grams' | 'Ounces' | 'Pounds' | 'Kilograms' | 'Gallons' | 'Liters' | 'Milliliters'

export interface HarvestLog {
  id: string
  plantingId: string
  harvestedByUserId: string
  harvestedByDisplayName: string
  harvestDate: string
  quantity: number
  quantityUnit: QuantityUnit
  notes: string | null
  createdAt: string
}

export interface LogHarvestBody {
  harvestDate: string
  quantity: number
  quantityUnit: QuantityUnit
  notes?: string
}

export const getHarvestLogs = (plantingId: string): Promise<HarvestLog[]> =>
  apiClient.get<HarvestLog[]>(`/api/plantings/${plantingId}/harvests`).then(r => r.data)

export const logHarvest = (plantingId: string, body: LogHarvestBody): Promise<HarvestLog> =>
  apiClient.post<HarvestLog>(`/api/plantings/${plantingId}/harvests`, body).then(r => r.data)

// ── Soil tests ────────────────────────────────────────────────────────────────

export type SoilTestSource = 'HomeKit' | 'LabTest' | 'Manual'

export interface SoilTest {
  id: string
  gardenBedId: string
  testedAt: string
  phLevel: number | null
  nitrogenPpm: number | null
  phosphorusPpm: number | null
  potassiumPpm: number | null
  organicMatterPercent: number | null
  testSource: string
  notes: string | null
  createdAt: string
}

export interface CreateSoilTestBody {
  testedAt: string
  phLevel?: number
  nitrogenPpm?: number
  phosphorusPpm?: number
  potassiumPpm?: number
  organicMatterPercent?: number
  testSource: SoilTestSource
  notes?: string
}

export const getSoilTests = (gardenId: string, bedId: string): Promise<SoilTest[]> =>
  apiClient.get<SoilTest[]>(`/api/gardens/${gardenId}/beds/${bedId}/soil-tests`).then(r => r.data)

export const createSoilTest = (gardenId: string, bedId: string, body: CreateSoilTestBody): Promise<SoilTest> =>
  apiClient.post<SoilTest>(`/api/gardens/${gardenId}/beds/${bedId}/soil-tests`, body).then(r => r.data)

// ── Amendment logs ────────────────────────────────────────────────────────────

export type AmendmentType = 'Fertilizer' | 'Compost' | 'Mulch' | 'PhAdjuster' | 'Pesticide' | 'HerbControl' | 'Other'

export interface AmendmentLog {
  id: string
  gardenBedId: string
  plantingId: string | null
  appliedAt: string
  productName: string
  amendmentType: AmendmentType
  quantity: number
  quantityUnit: QuantityUnit
  notes: string | null
}

export interface LogAmendmentBody {
  appliedAt: string
  productName: string
  amendmentType: AmendmentType
  quantity: number
  quantityUnit: QuantityUnit
  notes?: string
}

export const getAmendmentLogs = (gardenId: string, bedId: string): Promise<AmendmentLog[]> =>
  apiClient.get<AmendmentLog[]>(`/api/gardens/${gardenId}/beds/${bedId}/amendments`).then(r => r.data)

export const logAmendment = (gardenId: string, bedId: string, body: LogAmendmentBody): Promise<AmendmentLog> =>
  apiClient.post<AmendmentLog>(`/api/gardens/${gardenId}/beds/${bedId}/amendments`, body).then(r => r.data)

// ── Pest & disease logs ───────────────────────────────────────────────────────

export type PestDiseaseType = 'Pest' | 'Disease' | 'NutrientDeficiency'
export type Severity = 'Low' | 'Medium' | 'High'

export interface PestDiseaseLog {
  id: string
  gardenBedId: string
  plantingId: string | null
  observedAt: string
  type: PestDiseaseType
  name: string
  severity: Severity
  treatmentApplied: string | null
  resolvedAt: string | null
  notes: string | null
}

export interface LogPestDiseaseBody {
  type: PestDiseaseType
  name: string
  severity: Severity
  observedAt?: string
  treatmentApplied?: string
  notes?: string
}

export const getPestDiseaseLogs = (gardenId: string, bedId: string): Promise<PestDiseaseLog[]> =>
  apiClient.get<PestDiseaseLog[]>(`/api/gardens/${gardenId}/beds/${bedId}/pest-disease-logs`).then(r => r.data)

export const logPestDisease = (gardenId: string, bedId: string, body: LogPestDiseaseBody): Promise<PestDiseaseLog> =>
  apiClient.post<PestDiseaseLog>(`/api/gardens/${gardenId}/beds/${bedId}/pest-disease-logs`, body).then(r => r.data)

export const resolvePestDiseaseLog = (gardenId: string, bedId: string, logId: string): Promise<PestDiseaseLog> =>
  apiClient.patch<PestDiseaseLog>(`/api/gardens/${gardenId}/beds/${bedId}/pest-disease-logs/${logId}/resolve`, {}).then(r => r.data)

// ── Garden members ────────────────────────────────────────────────────────────

export const addGardenMember = (gardenId: string, email: string): Promise<GardenMember> =>
  apiClient.post<GardenMember>(`/api/gardens/${gardenId}/members`, { email }).then(r => r.data)

export const removeGardenMember = (gardenId: string, userId: string): Promise<void> =>
  apiClient.delete(`/api/gardens/${gardenId}/members/${userId}`).then(() => undefined)
